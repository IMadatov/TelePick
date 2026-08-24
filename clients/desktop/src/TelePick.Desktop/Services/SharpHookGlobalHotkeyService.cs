using System;
using System.Threading.Tasks;
using SharpHook;
using SharpHook.Native;

namespace TelePick.Desktop.Services;

public class SharpHookGlobalHotkeyService : IGlobalHotkeyService
{
    private readonly TaskPoolGlobalHook _hook;
    private bool _ctrlPressed;
    private bool _shiftPressed;
    private bool _tPressed;

    public event EventHandler? HotkeyPressed;

    public SharpHookGlobalHotkeyService()
    {
        _hook = new TaskPoolGlobalHook();
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
    }

    public void Start()
    {
        _ = _hook.RunAsync();
    }

    public void Stop()
    {
        _hook.Dispose();
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (e.Data.KeyCode == KeyCode.VcLeftControl || e.Data.KeyCode == KeyCode.VcRightControl) 
            _ctrlPressed = true;
        if (e.Data.KeyCode == KeyCode.VcLeftShift || e.Data.KeyCode == KeyCode.VcRightShift) 
            _shiftPressed = true;

        if (e.Data.KeyCode == KeyCode.VcT && !_tPressed)
        {
            _tPressed = true;
            if (_ctrlPressed && _shiftPressed)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        if (e.Data.KeyCode == KeyCode.VcLeftControl || e.Data.KeyCode == KeyCode.VcRightControl) 
            _ctrlPressed = false;
        if (e.Data.KeyCode == KeyCode.VcLeftShift || e.Data.KeyCode == KeyCode.VcRightShift) 
            _shiftPressed = false;
        if (e.Data.KeyCode == KeyCode.VcT) 
            _tPressed = false;
    }

    public void Dispose()
    {
        _hook.Dispose();
    }
}
