using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpHook;
using SharpHook.Native;
using System.Runtime.InteropServices;

namespace TelePick.Desktop.Services;

public class SharpHookGlobalHotkeyService : IGlobalHotkeyService
{
    private readonly TaskPoolGlobalHook _hook;
    private readonly EventSimulator _simulator;
    
    private bool _ctrlPressed;
    private bool _shiftPressed;
    private bool _altPressed;
    private bool _metaPressed;

    public int LastMouseX { get; private set; }
    public int LastMouseY { get; private set; }

    private class RegisteredHotkey
    {
        public bool CtrlRequired { get; set; }
        public bool ShiftRequired { get; set; }
        public bool AltRequired { get; set; }
        public bool MetaRequired { get; set; }
        public KeyCode? KeyRequired { get; set; }
        public Action Callback { get; set; } = null!;
        public bool IsCurrentlyPressed { get; set; }
    }

    private readonly Dictionary<string, RegisteredHotkey> _hotkeys = new();

    public SharpHookGlobalHotkeyService()
    {
        _hook = new TaskPoolGlobalHook();
        _simulator = new EventSimulator();
        
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
        _hook.MouseMoved += OnMouseMoved;
    }

    private bool _isRunning;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _ = _hook.RunAsync();
    }

    public void Stop()
    {
        _hook.Dispose();
    }

    public void RegisterHotkey(string id, string hotkeyString, Action callback)
    {
        if (string.IsNullOrWhiteSpace(hotkeyString))
        {
            _hotkeys.Remove(id);
            return;
        }

        var hotkey = new RegisteredHotkey { Callback = callback };
        var parts = hotkeyString.Split('+').Select(p => p.Trim().ToLowerInvariant()).ToArray();
        foreach (var part in parts)
        {
            if (part == "control" || part == "ctrl") hotkey.CtrlRequired = true;
            else if (part == "shift") hotkey.ShiftRequired = true;
            else if (part == "alt") hotkey.AltRequired = true;
            else if (part == "win" || part == "meta" || part == "super" || part == "cmd") hotkey.MetaRequired = true;
            else if (part == "space") hotkey.KeyRequired = KeyCode.VcSpace;
            else if (part.Length == 1 && part[0] >= 'a' && part[0] <= 'z')
            {
                hotkey.KeyRequired = (KeyCode)((int)KeyCode.VcA + (part[0] - 'a'));
            }
        }

        _hotkeys[id] = hotkey;
    }

    public void UnregisterHotkey(string id)
    {
        _hotkeys.Remove(id);
    }

    public void SimulatePaste()
    {
        Task.Run(async () =>
        {
            await Task.Delay(100);
            
            var modifier = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? KeyCode.VcLeftMeta : KeyCode.VcLeftControl;
            
            _simulator.SimulateKeyPress(modifier);
            await Task.Delay(10);
            _simulator.SimulateKeyPress(KeyCode.VcV);
            await Task.Delay(10);
            _simulator.SimulateKeyRelease(KeyCode.VcV);
            await Task.Delay(10);
            _simulator.SimulateKeyRelease(modifier);
        });
    }

    private void OnMouseMoved(object? sender, MouseHookEventArgs e)
    {
        LastMouseX = e.Data.X;
        LastMouseY = e.Data.Y;
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        UpdateModifierState(e.Data.KeyCode, true);

        foreach (var hotkey in _hotkeys.Values)
        {
            if (hotkey.KeyRequired.HasValue && e.Data.KeyCode == hotkey.KeyRequired.Value && !hotkey.IsCurrentlyPressed)
            {
                hotkey.IsCurrentlyPressed = true;
                if (_ctrlPressed == hotkey.CtrlRequired &&
                    _shiftPressed == hotkey.ShiftRequired &&
                    _altPressed == hotkey.AltRequired &&
                    _metaPressed == hotkey.MetaRequired)
                {
                    hotkey.Callback?.Invoke();
                }
            }
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        UpdateModifierState(e.Data.KeyCode, false);

        foreach (var hotkey in _hotkeys.Values)
        {
            if (e.Data.KeyCode == hotkey.KeyRequired)
            {
                hotkey.IsCurrentlyPressed = false;
            }
        }
    }

    private void UpdateModifierState(KeyCode key, bool pressed)
    {
        if (key == KeyCode.VcLeftControl || key == KeyCode.VcRightControl) _ctrlPressed = pressed;
        if (key == KeyCode.VcLeftShift || key == KeyCode.VcRightShift) _shiftPressed = pressed;
        if (key == KeyCode.VcLeftAlt || key == KeyCode.VcRightAlt) _altPressed = pressed;
        if (key == KeyCode.VcLeftMeta || key == KeyCode.VcRightMeta) _metaPressed = pressed;
    }

    public void Dispose()
    {
        Stop();
    }
}
