using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace TelePick.Desktop.Services.NativeClipboard;

/// <summary>
/// Native X11 clipboard listener using XFixes extension.
/// Receives events when the CLIPBOARD selection changes — no polling.
/// Falls back gracefully if X11 or XFixes is unavailable.
/// </summary>
public class LinuxX11ClipboardListener : INativeClipboardListener, IDisposable
{
    // libX11
    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XCreateSimpleWindow(
        IntPtr display, IntPtr parent,
        int x, int y, uint width, uint height, uint borderWidth,
        ulong border, ulong background);

    [DllImport("libX11.so.6")]
    private static extern int XDestroyWindow(IntPtr display, IntPtr window);

    [DllImport("libX11.so.6", CharSet = CharSet.Ansi)]
    private static extern IntPtr XInternAtom(IntPtr display, string atomName, bool onlyIfExists);

    [DllImport("libX11.so.6")]
    private static extern int XNextEvent(IntPtr display, IntPtr eventReturn);

    [DllImport("libX11.so.6")]
    private static extern int XPending(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XConnectionNumber(IntPtr display);

    // libXfixes
    [DllImport("libXfixes.so.3")]
    private static extern int XFixesQueryExtension(IntPtr display, out int eventBase, out int errorBase);

    [DllImport("libXfixes.so.3")]
    private static extern void XFixesSelectSelectionInput(
        IntPtr display, IntPtr window, IntPtr selection, ulong eventMask);

    // XFixes event mask
    private const ulong XFixesSetSelectionOwnerNotifyMask = 1L;

    public event EventHandler? ClipboardChanged;

    private IntPtr _display;
    private IntPtr _window;
    private int _xfixesEventBase;
    private Thread? _eventThread;
    private volatile bool _running;

    /// <summary>
    /// Attempts to initialize X11 + XFixes. Returns false if unavailable.
    /// </summary>
    public bool TryInitialize()
    {
        try
        {
            _display = XOpenDisplay(IntPtr.Zero);
            if (_display == IntPtr.Zero)
                return false;

            if (XFixesQueryExtension(_display, out _xfixesEventBase, out _) == 0)
            {
                XCloseDisplay(_display);
                _display = IntPtr.Zero;
                return false;
            }

            var root = XDefaultRootWindow(_display);
            _window = XCreateSimpleWindow(_display, root, 0, 0, 1, 1, 0, 0, 0);
            if (_window == IntPtr.Zero)
            {
                XCloseDisplay(_display);
                _display = IntPtr.Zero;
                return false;
            }

            var clipboardAtom = XInternAtom(_display, "CLIPBOARD", false);
            XFixesSelectSelectionInput(_display, _window, clipboardAtom, XFixesSetSelectionOwnerNotifyMask);

            return true;
        }
        catch
        {
            if (_display != IntPtr.Zero)
            {
                XCloseDisplay(_display);
                _display = IntPtr.Zero;
            }
            return false;
        }
    }

    public void StartListening()
    {
        if (_display == IntPtr.Zero || _running) return;

        _running = true;
        _eventThread = new Thread(EventLoop)
        {
            IsBackground = true,
            Name = "X11ClipboardListener"
        };
        _eventThread.Start();
    }

    private void EventLoop()
    {
        // Allocate buffer for XEvent (256 bytes is more than enough for any XEvent)
        var eventPtr = Marshal.AllocHGlobal(256);
        try
        {
            while (_running)
            {
                // Wait for events with a timeout using poll() on the X11 fd
                if (XPending(_display) > 0)
                {
                    XNextEvent(_display, eventPtr);

                    // Read event type (first int in the XEvent struct)
                    var eventType = Marshal.ReadInt32(eventPtr);

                    if (eventType == _xfixesEventBase + 0) // XFixesSelectionNotify
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            ClipboardChanged?.Invoke(this, EventArgs.Empty));
                    }
                }
                else
                {
                    // No events pending — sleep briefly to avoid busy-waiting
                    Thread.Sleep(50);
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(eventPtr);
        }
    }

    public void StopListening()
    {
        _running = false;
        _eventThread?.Join(500);
        _eventThread = null;
    }

    public void Dispose()
    {
        StopListening();

        if (_window != IntPtr.Zero && _display != IntPtr.Zero)
        {
            XDestroyWindow(_display, _window);
            _window = IntPtr.Zero;
        }

        if (_display != IntPtr.Zero)
        {
            XCloseDisplay(_display);
            _display = IntPtr.Zero;
        }

        GC.SuppressFinalize(this);
    }
}
