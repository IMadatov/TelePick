using System;
using Avalonia.Threading;

namespace TelePick.Desktop.Services.NativeClipboard;

public class FallbackClipboardListener : INativeClipboardListener, IDisposable
{
    public event EventHandler? ClipboardChanged;
    
    private DispatcherTimer? _timer;

    public void StartListening()
    {
        if (_timer != null) return;
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _timer.Tick += (s, e) => ClipboardChanged?.Invoke(this, EventArgs.Empty);
        _timer.Start();
    }

    public void StopListening()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer = null;
        }
    }

    public void Dispose()
    {
        StopListening();
        GC.SuppressFinalize(this);
    }
}
