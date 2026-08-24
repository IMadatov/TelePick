using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Collections.Generic;

namespace TelePick.Desktop.Views;

public partial class HotkeyRecorderWindow : Window
{
    public string RecordedHotkey { get; private set; } = string.Empty;
    private HashSet<Key> _pressedKeys = new();

    public HotkeyRecorderWindow()
    {
        InitializeComponent();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        e.Handled = true;
        _pressedKeys.Add(e.Key);
        UpdateDisplay(e.KeyModifiers, e.Key);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        e.Handled = true;
        _pressedKeys.Remove(e.Key);
    }

    private void UpdateDisplay(KeyModifiers modifiers, Key latestKey)
    {
        var parts = new List<string>();

        if (modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Control");
        if (modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(KeyModifiers.Meta)) parts.Add("Win"); // Or Cmd

        // If the key is not a modifier itself, add it
        if (latestKey != Key.LeftCtrl && latestKey != Key.RightCtrl &&
            latestKey != Key.LeftShift && latestKey != Key.RightShift &&
            latestKey != Key.LeftAlt && latestKey != Key.RightAlt &&
            latestKey != Key.LWin && latestKey != Key.RWin)
        {
            parts.Add(latestKey.ToString());
        }

        if (parts.Count > 0)
        {
            RecordedHotkey = string.Join("+", parts);
            HotkeyDisplay.Text = RecordedHotkey;
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        RecordedHotkey = string.Empty;
        Close(null);
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        Close(RecordedHotkey);
    }
}
