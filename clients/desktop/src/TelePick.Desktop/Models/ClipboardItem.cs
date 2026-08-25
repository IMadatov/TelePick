using System;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TelePick.Desktop.Models;

public partial class ClipboardItem : ObservableObject, IDisposable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ClipboardItemType Type { get; set; }

    private string? _title;
    public string Title
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_title)) return _title;
            return Type switch
            {
                ClipboardItemType.Text => !string.IsNullOrWhiteSpace(PreviewText)
                    ? (PreviewText.Length > 40 ? PreviewText[..37] + "..." : PreviewText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Text Snippet")
                    : "Text Snippet",
                ClipboardItemType.Image => "Image",
                ClipboardItemType.Files => "Files",
                _ => "Clipboard Item"
            };
        }
        set => SetProperty(ref _title, value);
    }

    [ObservableProperty]
    private string _previewText = string.Empty;

    [ObservableProperty]
    private DateTime _timestamp = DateTime.Now;

    public string FormattedTimestamp => Timestamp.ToString("HH:mm");

    [ObservableProperty]
    private bool _isPinned;

    public object? RawData { get; set; }
    public Bitmap? Thumbnail { get; set; }
    public string DataHash { get; set; } = string.Empty;
    public string? IconKind { get; set; }

    /// <summary>
    /// Approximate memory footprint of this item in bytes.
    /// </summary>
    public long EstimatedSizeBytes { get; set; }

    /// <summary>
    /// Path to temp screenshot file (for image items captured from clipboard bitmap).
    /// Used for cleanup on disposal.
    /// </summary>
    public string? ScreenshotPath { get; set; }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Thumbnail?.Dispose();
        Thumbnail = null;

        if (!string.IsNullOrEmpty(ScreenshotPath))
        {
            try
            {
                if (File.Exists(ScreenshotPath))
                    File.Delete(ScreenshotPath);
            }
            catch
            {
                // Best-effort cleanup
            }
        }

        GC.SuppressFinalize(this);
    }
}

