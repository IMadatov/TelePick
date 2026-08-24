using System;
using Avalonia.Media.Imaging;

namespace TelePick.Desktop.Models;

public class ClipboardItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ClipboardItemType Type { get; set; }
    public string PreviewText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public object? RawData { get; set; }
    public Bitmap? Thumbnail { get; set; }
    public string DataHash { get; set; } = string.Empty;
}
