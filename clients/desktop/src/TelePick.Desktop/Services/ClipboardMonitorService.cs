using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using TelePick.Desktop.Models;
using TelePick.Desktop.Services.NativeClipboard;

namespace TelePick.Desktop.Services;

public class ClipboardMonitorService : IClipboardMonitorService
{
    private Avalonia.Input.Platform.IClipboard? _clipboard;
    private INativeClipboardListener? _listener;

    public ObservableCollection<ClipboardItem> History { get; } = new();

    public void StartMonitoring(Avalonia.Input.Platform.IClipboard clipboard)
    {
        if (_listener != null) return;

        _clipboard = clipboard;
        _listener = NativeClipboardListenerFactory.Create();
        _listener.ClipboardChanged += OnClipboardChanged;
        _listener.StartListening();
        
        // Initial check
        _ = ProcessClipboardAsync();
    }

    public void StopMonitoring()
    {
        if (_listener != null)
        {
            _listener.StopListening();
            _listener.ClipboardChanged -= OnClipboardChanged;
            _listener = null;
        }

        _clipboard = null;
    }

    private void OnClipboardChanged(object? sender, EventArgs e)
    {
        _ = ProcessClipboardAsync();
    }

    private async Task ProcessClipboardAsync()
    {
        if (_clipboard == null) return;

        try
        {
            var files = await _clipboard.TryGetFilesAsync();
            if (files != null && files.Any())
            {
                var filePaths = files.Select(f => f.Path.LocalPath).Where(p => p != null).ToList();
                if (filePaths.Any())
                {
                    var preview = filePaths.Count == 1 ? filePaths.First() : $"{filePaths.Count} files copied";
                    var latestItem = History.FirstOrDefault();
                    
                    if (latestItem == null || latestItem.Type != ClipboardItemType.Files || latestItem.PreviewText != preview)
                    {
                        var item = new ClipboardItem
                        {
                            Type = ClipboardItemType.Files,
                            PreviewText = preview,
                            RawData = filePaths
                        };

                        // Try generate thumbnail if single file and image extension
                        if (filePaths.Count == 1)
                        {
                            var ext = Path.GetExtension(filePaths.First())?.ToLowerInvariant();
                            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
                            {
                                try
                                {
                                    using var stream = File.OpenRead(filePaths.First());
                                    item.Thumbnail = Bitmap.DecodeToWidth(stream, 100);
                                }
                                catch { } // Ignore thumbnail errors
                            }
                        }

                        AddItem(item);
                    }
                    return;
                }
            }

            var text = await _clipboard.TryGetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var latestTextItem = History.FirstOrDefault();
                if (latestTextItem == null || latestTextItem.Type != ClipboardItemType.Text || latestTextItem.PreviewText != text)
                {
                    var item = new ClipboardItem
                    {
                        Type = ClipboardItemType.Text,
                        PreviewText = text,
                        RawData = text
                    };
                    AddItem(item);
                }
                return;
            }

            var bitmap = await _clipboard.TryGetBitmapAsync();
            if (bitmap != null)
            {
                using var ms = new MemoryStream();
                bitmap.Save(ms);
                var bytes = ms.ToArray();
                var hash = Convert.ToBase64String(MD5.HashData(bytes));

                var latestItem = History.FirstOrDefault();
                if (latestItem == null || latestItem.Type != ClipboardItemType.Image || latestItem.DataHash != hash)
                {
                    // For raw screenshots, we save them to a temp folder so we have an address
                    var tempPath = Path.Combine(Path.GetTempPath(), "TelePick", "Screenshots");
                    Directory.CreateDirectory(tempPath);
                    var filePath = Path.Combine(tempPath, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    
                    File.WriteAllBytes(filePath, bytes);
                    
                    var item = new ClipboardItem
                    {
                        Type = ClipboardItemType.Image,
                        PreviewText = "Screenshot",
                        RawData = filePath,
                        DataHash = hash
                    };

                    try
                    {
                        using var stream = new MemoryStream(bytes);
                        item.Thumbnail = Bitmap.DecodeToWidth(stream, 100);
                    }
                    catch { }
                    
                    AddItem(item);
                }
            }
        }
        catch (Exception)
        {
            // Ignore errors
        }
    }

    private void AddItem(ClipboardItem item)
    {
        Dispatcher.UIThread.Post(() =>
        {
            History.Insert(0, item);
            if (History.Count > 50)
            {
                History.RemoveAt(History.Count - 1);
            }
        });
    }
}
