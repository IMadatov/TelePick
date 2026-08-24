using System;
using System.Runtime.InteropServices;

namespace TelePick.Desktop.Services.NativeClipboard;

public static class NativeClipboardListenerFactory
{
    public static INativeClipboardListener Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsClipboardListener();
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacClipboardListener();
        }

        // Fallback for Linux and others
        return new FallbackClipboardListener();
    }
}
