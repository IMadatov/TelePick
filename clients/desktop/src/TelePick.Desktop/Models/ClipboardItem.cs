using System;
using System.IO;
using Avalonia.Media.Imaging;

namespace TelePick.Desktop.Models;

public class ClipboardItem : IDisposable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ClipboardItemType Type { get; set; }
    public string PreviewText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
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
