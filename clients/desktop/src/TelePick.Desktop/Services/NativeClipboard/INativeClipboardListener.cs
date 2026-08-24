using System;

namespace TelePick.Desktop.Services.NativeClipboard;

public interface INativeClipboardListener
{
    event EventHandler? ClipboardChanged;
    void StartListening();
    void StopListening();
}
