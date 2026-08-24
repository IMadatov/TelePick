using System;

namespace TelePick.Desktop.Services;

public interface IGlobalHotkeyService : IDisposable
{
    void Start();
    void Stop();
    event EventHandler? HotkeyPressed;
}
