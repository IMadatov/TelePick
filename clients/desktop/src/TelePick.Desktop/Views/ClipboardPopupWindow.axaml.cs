using Avalonia.Controls;
using Avalonia.Interactivity;
using TelePick.Desktop.Models;
using TelePick.Desktop.ViewModels;
using TelePick.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Input.Platform;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.VisualTree;

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

        this.Opened += async (s, e) =>
        {
            var listBox = this.FindControl<ListBox>("ClipboardListBox");
            if (listBox != null)
            {
                if (listBox.ItemCount > 0)
                {
                    listBox.SelectedIndex = 0;
                }
                
                // Wait for Avalonia's initial layout and default focus to finish
                await Task.Delay(10);
                
                listBox.Focus();
                
                // Also attempt to focus the container if it exists
                var container = listBox.ContainerFromIndex(0) as Control;
                container?.Focus();
            }
        };
    }

    private async void OnItemTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (e.Source is Avalonia.Controls.Control ctrl && (ctrl is Button || ctrl.FindAncestorOfType<Button>() != null))
        {
            return;
        }

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
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to set bitmap: {ex}");
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
            else if (selectedItem.Type == ClipboardItemType.Files)
            {
                var paths = selectedItem.RawData as System.Collections.Generic.List<string>;
                if (paths != null && paths.Count > 0)
                {
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                    {
                        try
                        {
                            var uriList = string.Join("\n", System.Linq.Enumerable.Select(paths, p => $"file://{p}"));
                            var process = new System.Diagnostics.Process
                            {
                                StartInfo = new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = "xclip",
                                    Arguments = "-selection clipboard -t text/uri-list",
                                    RedirectStandardInput = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                }
                            };
                            process.Start();
                            process.StandardInput.Write(uriList);
                            process.StandardInput.Close();
                            process.WaitForExit(1000);
                        }
                        catch (System.Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to paste file via xclip: {ex}");
                        }
                    }
                    else
                    {
                        var topLevel = TopLevel.GetTopLevel(this);
                        if (topLevel != null)
                        {
                            var storageFiles = new System.Collections.Generic.List<Avalonia.Platform.Storage.IStorageFile>();
                            foreach (var p in paths)
                            {
                                var file = await topLevel.StorageProvider.TryGetFileFromPathAsync(new System.Uri(p.StartsWith("file://") ? p : "file://" + p));
                                if (file != null) storageFiles.Add(file);
                            }
                            if (storageFiles.Count > 0)
                            {
                                await clipboard.ClearAsync();
                                await clipboard.SetFilesAsync(storageFiles);
                            }
                        }
                    }
                }
            }
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
