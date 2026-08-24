using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace TelePick.Desktop.Services;

public class ClipboardService : IClipboardService
{
    public async Task<string?> GetTextAsync()
    {
        var clipboard = GetClipboard();
        if (clipboard == null) return null;

        return await clipboard.TryGetTextAsync();
    }

    private Avalonia.Input.Platform.IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow != null)
            {
                return TopLevel.GetTopLevel(mainWindow)?.Clipboard;
            }
        }
        return null;
    }
}
