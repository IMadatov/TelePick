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
    private const int MaxItems = 50;
    private static readonly string ScreenshotDir = Path.Combine(Path.GetTempPath(), "TelePick", "Screenshots");

    private Avalonia.Input.Platform.IClipboard? _clipboard;
    private INativeClipboardListener? _listener;
    private long _memoryBudget;
    private long _currentUsage;

    public ObservableCollection<ClipboardItem> History { get; } = new();

    public void StartMonitoring(Avalonia.Input.Platform.IClipboard clipboard)
    {
        if (_listener != null) return;

        _clipboard = clipboard;

        // Calculate memory budget: 1% of system RAM
        var memInfo = GC.GetGCMemoryInfo();
        _memoryBudget = memInfo.TotalAvailableMemoryBytes / 100;

        // Clean up stale temp screenshots from previous sessions
        CleanupStaleScreenshots();

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

        // Dispose all history items
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var item in History)
                item.Dispose();
            History.Clear();
            _currentUsage = 0;
        });
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
                            Title = filePaths.Count == 1 ? Path.GetFileName(filePaths.First()) : $"{filePaths.Count} Files",
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
                                catch (System.Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to create thumbnail from file: {ex}");
                                }
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

                        // Estimate size: path strings + thumbnail
                        item.EstimatedSizeBytes = EstimateFilesSize(filePaths, item.Thumbnail);
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
                        Title = GetTextTitle(text),
                        PreviewText = text,
                        RawData = text,
                        IconKind = "TextSubject",
                        EstimatedSizeBytes = EstimateTextSize(text)
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
                    Directory.CreateDirectory(ScreenshotDir);
                    var filePath = Path.Combine(ScreenshotDir, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    
                    File.WriteAllBytes(filePath, bytes);
                    
                    var item = new ClipboardItem
                    {
                        Type = ClipboardItemType.Image,
                        Title = "Image Screenshot",
                        PreviewText = Path.GetFileName(filePath),
                        RawData = filePath,
                        DataHash = hash,
                        IconKind = "ImageOutline",
                        ScreenshotPath = filePath
                    };

                    try
                    {
                        using var stream = new MemoryStream(bytes);
                        item.Thumbnail = Bitmap.DecodeToWidth(stream, 100);
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to create thumbnail from bytes: {ex}");
                    }

                    item.EstimatedSizeBytes = EstimateImageSize(item.Thumbnail);
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
            // Evict oldest items while over budget or max count
            while (History.Count > 0 &&
                   (_currentUsage + item.EstimatedSizeBytes > _memoryBudget || History.Count >= MaxItems))
            {
                EvictOldest();
            }

            History.Insert(0, item);
            _currentUsage += item.EstimatedSizeBytes;
        });
    }

    private void EvictOldest()
    {
        if (History.Count == 0) return;

        var oldest = History[History.Count - 1];
        History.RemoveAt(History.Count - 1);
        _currentUsage -= oldest.EstimatedSizeBytes;
        oldest.Dispose();
    }

    private static long EstimateTextSize(string text)
    {
        // UTF-16 encoding (2 bytes per char) + object overhead
        // Text is stored twice: PreviewText + RawData
        return (text.Length * 2L * 2) + 100;
    }

    private static long EstimateImageSize(Bitmap? thumbnail)
    {
        if (thumbnail == null) return 100;

        // ARGB = 4 bytes per pixel
        return (long)thumbnail.PixelSize.Width * thumbnail.PixelSize.Height * 4 + 100;
    }

    private static long EstimateFilesSize(System.Collections.Generic.List<string> paths, Bitmap? thumbnail)
    {
        var pathBytes = paths.Sum(p => (long)p.Length * 2) + 100;
        var thumbBytes = EstimateImageSize(thumbnail);
        return pathBytes + thumbBytes;
    }

    private static string GetTextTitle(string text)
    {
        var firstLine = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
        if (!string.IsNullOrEmpty(firstLine))
        {
            return firstLine.Length > 40 ? firstLine.Substring(0, 37) + "..." : firstLine;
        }
        return "Text Snippet";
    }

    private static void CleanupStaleScreenshots()
    {
        try
        {
            if (Directory.Exists(ScreenshotDir))
                Directory.Delete(ScreenshotDir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}
