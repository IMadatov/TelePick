using Avalonia.Controls;
using Avalonia.Interactivity;
using TelePick.Desktop.Models;
using TelePick.Desktop.ViewModels;
using TelePick.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Input.Platform;
using System.Threading.Tasks;
using Avalonia;

namespace TelePick.Desktop.Views;

public partial class ClipboardPopupWindow : Window
{
    public ClipboardPopupWindow()
    {
        InitializeComponent();
        
        // Hide window when it loses focus
        this.Deactivated += (s, e) => this.Close();

        // Close on Escape
        this.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                this.Close();
            }
        };
    }

    private async void OnItemTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is Avalonia.Controls.Control control && control.DataContext is ClipboardItem selectedItem)
        {
            await PasteItemAsync(selectedItem);
        }
    }

    private async void OnListBoxKeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter && sender is ListBox listBox && listBox.SelectedItem is ClipboardItem selectedItem)
        {
            await PasteItemAsync(selectedItem);
        }
    }

    private async Task PasteItemAsync(ClipboardItem selectedItem)
    {
        // 1. Set to system clipboard FIRST while window still has focus
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            if (selectedItem.Type == ClipboardItemType.Text)
            {
                await clipboard.ClearAsync();
                await clipboard.SetTextAsync(selectedItem.RawData as string);
            }
            else if (selectedItem.Type == ClipboardItemType.Image)
            {
                var path = selectedItem.RawData as string;
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        // Clean path string
                        var actualPath = path;
                        if (actualPath.StartsWith("file://"))
                        {
                            actualPath = actualPath.Substring(7);
                        }
                        
                        var bitmap = new Avalonia.Media.Imaging.Bitmap(actualPath);
                        await clipboard.ClearAsync();
                        await clipboard.SetBitmapAsync(bitmap);
                    }
                    catch
                    {
                        // Fallback to pasting as file
                        var topLevel = TopLevel.GetTopLevel(this);
                        if (topLevel != null)
                        {
                            var file = await topLevel.StorageProvider.TryGetFileFromPathAsync(new System.Uri(path.StartsWith("file://") ? path : "file://" + path));
                            if (file != null)
                            {
                                await clipboard.ClearAsync();
                                await clipboard.SetFilesAsync(new[] { file });
                            }
                        }
                    }
                }
            }
            // TODO: Files paste — revisit with xclip or native approach
            // else if (selectedItem.Type == ClipboardItemType.Files) { ... }
        }

        // 2. Wait a tiny bit for OS clipboard to sync while we still have focus
        await Task.Delay(50);

        // 3. NOW hide the popup to return focus to the underlying app
        this.Hide();

        // 4. Wait for focus to return to underlying app
        await Task.Delay(50);

        // 5. Simulate Paste
        var hotkeyService = App.Services?.GetService<IGlobalHotkeyService>();
        hotkeyService?.SimulatePaste();
        
        this.Close();
    }
}
