using System;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using AvaloniaEdit.Document;
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
                ClipboardItemType.Text => IsLikelyCode ? "Code" : "Text",
                ClipboardItemType.Link => "Link",
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

    public bool IsImage => Type == ClipboardItemType.Image;
    
    private bool _isLikelyCode;
    public bool IsLikelyCode 
    {
        get => _isLikelyCode;
        set => SetProperty(ref _isLikelyCode, value);
    }

    private void DetermineIfCode()
    {
        if (Type != ClipboardItemType.Text || string.IsNullOrWhiteSpace(PreviewText))
        {
            IsLikelyCode = false;
            return;
        }

        // Basic heuristic: density of programming symbols and keywords
        var codeChars = new[] { '{', '}', ';', '<', '>', '=', '(', ')', '[', ']' };
        int symbolCount = PreviewText.Count(c => codeChars.Contains(c));
        
        string[] keywords = { "class ", "public ", "private ", "void ", "function ", "const ", "let ", "var ", "using ", "import ", "def ", "return ", "if ", "else ", "for " };
        int keywordCount = keywords.Count(kw => PreviewText.Contains(kw, StringComparison.OrdinalIgnoreCase));

        // If high symbol density or contains multiple keywords, treat as code
        IsLikelyCode = (symbolCount > 5) || (keywordCount >= 2) || PreviewText.Contains("=>") || PreviewText.Contains("==") || PreviewText.Contains("</");
        
        OnPropertyChanged(nameof(Title));
    }

    private TextDocument? _document;
    public TextDocument Document
    {
        get
        {
            if (_document == null)
            {
                _document = new TextDocument(PreviewText);
            }
            return _document;
        }
    }

    partial void OnPreviewTextChanged(string value)
    {
        DetermineIfCode();
        if (_document != null && _document.Text != value)
        {
            _document.Text = value;
        }
    }

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

