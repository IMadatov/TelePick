using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Threading;

namespace TelePick.Desktop.Services.NativeClipboard;

public class MacClipboardListener : INativeClipboardListener, IDisposable
{
    private const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";
    
    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    public event EventHandler? ClipboardChanged;
    
    private DispatcherTimer? _timer;
    private long _lastChangeCount = -1;

    public void StartListening()
    {
        if (_timer != null) return;
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        try
        {
            var nsPasteboard = objc_getClass("NSPasteboard");
            var generalPasteboardSel = sel_registerName("generalPasteboard");
            var generalPasteboard = objc_msgSend(nsPasteboard, generalPasteboardSel);
            
            var changeCountSel = sel_registerName("changeCount");
            var changeCount = (long)objc_msgSend(generalPasteboard, changeCountSel);
            
            if (_lastChangeCount == -1)
            {
                _lastChangeCount = changeCount;
                return;
            }

            if (changeCount != _lastChangeCount)
            {
                _lastChangeCount = changeCount;
                ClipboardChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        catch
        {
            // Ignore objective-c errors
        }
    }

    public void StopListening()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
            _timer = null;
        }
    }

    public void Dispose()
    {
        StopListening();
        GC.SuppressFinalize(this);
    }
}
