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
                    var dataHash = string.Join("|", filePaths);
                    var preview = filePaths.Count == 1 ? Path.GetFileName(filePaths.First()) : $"{filePaths.Count} files copied";
                    
                    var existingItem = History.FirstOrDefault(x => x.Type == ClipboardItemType.Files && x.DataHash == dataHash);
                    if (existingItem != null)
                    {
                        BubbleUpItem(existingItem);
                    }
                    else
                    {
                        var item = new ClipboardItem
                        {
                            Type = ClipboardItemType.Files,
                            PreviewText = preview,
                            RawData = filePaths,
                            DataHash = dataHash
                        };

                        if (filePaths.Count == 1)
                        {
                            var filePath = filePaths.First();
                            var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
                            
                            // Determine IconKind
                            if (Directory.Exists(filePath))
                            {
                                item.IconKind = "FolderOutline";
                            }
                            else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif" || ext == ".svg")
                            {
                                item.IconKind = "FileImageOutline";
                                try
                                {
                                    using var stream = File.OpenRead(filePath);
                                    item.Thumbnail = Bitmap.DecodeToWidth(stream, 100);
                                }
                                catch { }
                            }
                            else if (ext == ".pdf" || ext == ".doc" || ext == ".docx" || ext == ".txt")
                            {
                                item.IconKind = "FileDocumentOutline";
                            }
                            else if (ext == ".mp3" || ext == ".wav" || ext == ".ogg")
                            {
                                item.IconKind = "FileMusicOutline";
                            }
                            else if (ext == ".mp4" || ext == ".avi" || ext == ".mkv" || ext == ".mov")
                            {
                                item.IconKind = "FileVideoOutline";
                            }
                            else if (ext == ".zip" || ext == ".rar" || ext == ".7z" || ext == ".tar" || ext == ".gz")
                            {
                                item.IconKind = "ZipBoxOutline";
                            }
                            else
                            {
                                item.IconKind = "FileOutline";
                            }
                        }
                        else
                        {
                            item.IconKind = "FolderMultipleOutline";
                        }

                        AddItem(item);
                    }
                    return;
                }
            }

            var text = await _clipboard.TryGetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var existingItem = History.FirstOrDefault(x => x.Type == ClipboardItemType.Text && x.PreviewText == text);
                if (existingItem != null)
                {
                    BubbleUpItem(existingItem);
                }
                else
                {
                    var item = new ClipboardItem
                    {
                        Type = ClipboardItemType.Text,
                        PreviewText = text,
                        RawData = text,
                        IconKind = "TextSubject"
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

                var existingItem = History.FirstOrDefault(x => x.Type == ClipboardItemType.Image && x.DataHash == hash);
                if (existingItem != null)
                {
                    BubbleUpItem(existingItem);
                }
                else
                {
                    // For raw screenshots, we save them to a temp folder so we have an address
                    var tempPath = Path.Combine(Path.GetTempPath(), "TelePick", "Screenshots");
                    Directory.CreateDirectory(tempPath);
                    var filePath = Path.Combine(tempPath, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    
                    File.WriteAllBytes(filePath, bytes);
                    
                    var item = new ClipboardItem
                    {
                        Type = ClipboardItemType.Image,
                        PreviewText = Path.GetFileName(filePath),
                        RawData = filePath,
                        DataHash = hash,
                        IconKind = "ImageOutline"
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

    private void BubbleUpItem(ClipboardItem item)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var index = History.IndexOf(item);
            if (index > 0)
            {
                History.RemoveAt(index);
                History.Insert(0, item);
            }
        });
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
