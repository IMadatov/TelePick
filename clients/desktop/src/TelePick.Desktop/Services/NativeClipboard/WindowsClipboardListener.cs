using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Avalonia.Threading;

namespace TelePick.Desktop.Services.NativeClipboard;

public class WindowsClipboardListener : INativeClipboardListener, IDisposable
{
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public WndProcDelegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hwnd);
    
    private const uint WM_CLIPBOARDUPDATE = 0x031D;
    private const int HWND_MESSAGE = -3;
    
    public event EventHandler? ClipboardChanged;
    
    private IntPtr _hwnd;
    private WndProcDelegate? _wndProcDelegate;

    public void StartListening()
    {
        if (_hwnd != IntPtr.Zero) return;

        // Ensure we create the window on the UI thread so Avalonia's message pump handles it
        Dispatcher.UIThread.Post(() =>
        {
            _wndProcDelegate = WndProc;
            
            WNDCLASSEX wndClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                lpfnWndProc = _wndProcDelegate,
                lpszClassName = "TelePickClipboardListenerClass_" + Guid.NewGuid().ToString("N"),
                hInstance = Marshal.GetHINSTANCE(typeof(WindowsClipboardListener).Module)
            };
            
            RegisterClassEx(ref wndClass);

            _hwnd = CreateWindowEx(0, wndClass.lpszClassName, "ClipboardListener", 0, 0, 0, 0, 0, new IntPtr(HWND_MESSAGE), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            
            if (_hwnd != IntPtr.Zero)
            {
                AddClipboardFormatListener(_hwnd);
            }
        });
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void StopListening()
    {
        if (_hwnd != IntPtr.Zero)
        {
            RemoveClipboardFormatListener(_hwnd);
            DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        StopListening();
        GC.SuppressFinalize(this);
    }
}
