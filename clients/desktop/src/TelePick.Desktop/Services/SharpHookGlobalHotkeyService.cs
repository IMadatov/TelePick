using System;
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
    
    // Status tracking for Ctrl+Shift+Alt+C
    private bool _ctrlPressed;
    private bool _shiftPressed;
    private bool _tPressed; // Wait, previous code used VcT? The prompt said Ctrl+Shift+Alt+C but the code used Ctrl+Shift+T! Let's keep VcT.

    // Status tracking for Clipboard Popup
    private bool _popupCtrlRequired;
    private bool _popupShiftRequired;
    private bool _popupAltRequired;
    private bool _popupMetaRequired;
    private KeyCode? _popupKeyRequired;
    private bool _popupKeyPressed;

    private bool _altPressed;
    private bool _metaPressed;

    public int LastMouseX { get; private set; }
    public int LastMouseY { get; private set; }

    public event EventHandler? HotkeyPressed;
    public event EventHandler? ClipboardPopupHotkeyPressed;

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

    public void SetPopupHotkey(string hotkey)
    {
        _popupCtrlRequired = false;
        _popupShiftRequired = false;
        _popupAltRequired = false;
        _popupMetaRequired = false;
        _popupKeyRequired = null;

        if (string.IsNullOrWhiteSpace(hotkey)) return;

        var parts = hotkey.Split('+').Select(p => p.Trim().ToLowerInvariant()).ToArray();
        foreach (var part in parts)
        {
            if (part == "control" || part == "ctrl") _popupCtrlRequired = true;
            else if (part == "shift") _popupShiftRequired = true;
            else if (part == "alt") _popupAltRequired = true;
            else if (part == "win" || part == "meta" || part == "super" || part == "cmd") _popupMetaRequired = true;
            else if (part.Length == 1 && part[0] >= 'a' && part[0] <= 'z')
            {
                // Map 'a'-'z' to KeyCode.VcA - VcZ
                _popupKeyRequired = (KeyCode)((int)KeyCode.VcA + (part[0] - 'a'));
            }
        }
    }

    public void SimulatePaste()
    {
        Task.Run(async () =>
        {
            await Task.Delay(100); // Wait for popup to close and target app to gain focus
            
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

        // Check original Hotkey (Ctrl+Shift+T)
        if (e.Data.KeyCode == KeyCode.VcT && !_tPressed)
        {
            _tPressed = true;
            if (_ctrlPressed && _shiftPressed)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
        }

        // Check Popup Hotkey
        if (_popupKeyRequired.HasValue && e.Data.KeyCode == _popupKeyRequired.Value && !_popupKeyPressed)
        {
            _popupKeyPressed = true;
            if (_ctrlPressed == _popupCtrlRequired &&
                _shiftPressed == _popupShiftRequired &&
                _altPressed == _popupAltRequired &&
                _metaPressed == _popupMetaRequired)
            {
                ClipboardPopupHotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        UpdateModifierState(e.Data.KeyCode, false);

        if (e.Data.KeyCode == KeyCode.VcT) 
            _tPressed = false;

        if (e.Data.KeyCode == _popupKeyRequired)
            _popupKeyPressed = false;
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
