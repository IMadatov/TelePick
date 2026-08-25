using Avalonia;
using AvaloniaEdit;
using AvaloniaEdit.Document;

namespace TelePick.Desktop.Helpers;

public static class TextEditorAttached
{
    public static readonly AttachedProperty<string> TextProperty =
        AvaloniaProperty.RegisterAttached<TextEditor, string>("Text", typeof(TextEditorAttached));

    static TextEditorAttached()
    {
        TextProperty.Changed.AddClassHandler<TextEditor>((editor, e) =>
        {
            if (e.NewValue is string newText)
            {
                if (editor.Document == null)
                {
                    editor.Document = new TextDocument(newText);
                }
                else if (editor.Document.Text != newText)
                {
                    editor.Document.Text = newText;
                }
            }
        });
    }

    public static string GetText(TextEditor element)
    {
        return element.GetValue(TextProperty);
    }

    public static void SetText(TextEditor element, string value)
    {
        element.SetValue(TextProperty, value);
    }
}
