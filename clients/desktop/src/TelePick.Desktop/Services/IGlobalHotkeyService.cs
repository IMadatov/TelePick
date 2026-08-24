using System;

namespace TelePick.Desktop.Services;

public interface IGlobalHotkeyService : IDisposable
{
    void Start();
    void Stop();
    event EventHandler? HotkeyPressed;
    event EventHandler? ClipboardPopupHotkeyPressed;

    int LastMouseX { get; }
    int LastMouseY { get; }

    void SimulatePaste();
    void SetPopupHotkey(string hotkey);
}
