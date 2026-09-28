using System;

namespace iM.TelePick.Desktop.Services.NativeClipboard;

public interface INativeClipboardListener
{
    event EventHandler? ClipboardChanged;
    void StartListening();
    void StopListening();
}
