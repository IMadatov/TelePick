using System;

namespace TelePick.Desktop.Services;

public interface IGlobalHotkeyService : IDisposable
{
    void Start();
    void Stop();

    int LastMouseX { get; }
    int LastMouseY { get; }

    void SimulatePaste();
    
    void RegisterHotkey(string id, string hotkey, Action callback);
    void UnregisterHotkey(string id);
}
