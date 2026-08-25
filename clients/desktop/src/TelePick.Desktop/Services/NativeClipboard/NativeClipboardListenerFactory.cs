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

        // Linux: try X11 XFixes first, fall back to polling
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var x11Listener = new LinuxX11ClipboardListener();
            if (x11Listener.TryInitialize())
            {
                return x11Listener;
            }
            x11Listener.Dispose();
        }

        return new FallbackClipboardListener();
    }
}
