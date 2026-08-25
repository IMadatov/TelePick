using Avalonia;
using AvaloniaEdit;
using AvaloniaEdit.Document;

namespace TelePick.Desktop.Controls;

public class BindableTextEditor : TextEditor
{
    public static readonly StyledProperty<string> TextContentProperty =
        AvaloniaProperty.Register<BindableTextEditor, string>(nameof(TextContent));

    public string TextContent
    {
        get => GetValue(TextContentProperty);
        set => SetValue(TextContentProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == TextContentProperty)
        {
            var newText = change.GetNewValue<string>() ?? string.Empty;
            if (Document == null)
            {
                Document = new TextDocument(newText);
            }
            else if (Document.Text != newText)
            {
                Document.Text = newText;
            }
        }
    }
}
