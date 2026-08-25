using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TelePick.Desktop.Models;
using TelePick.Desktop.Services;

namespace TelePick.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IClipboardService _clipboardService;
    private readonly ITelegramService _telegramService;
    private readonly ISettingsService _settingsService;
    private readonly IClipboardMonitorService _clipboardMonitorService;
    private readonly IGlobalHotkeyService _globalHotkeyService;
    
    public System.Collections.ObjectModel.ObservableCollection<ClipboardItem> History => _clipboardMonitorService.History;

    [ObservableProperty]
    private string _clipboardText = string.Empty;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready.";

    [ObservableProperty]
    private bool _isStatusError = false;

    // Settings
    [ObservableProperty]
    private string _botToken = string.Empty;

    [ObservableProperty]
    private string _chatId = string.Empty;

    [ObservableProperty]
    private string _clipboardPopupHotkey = "Control+Shift+V";

    private List<Recipient> _recipients = [];

    public MainWindowViewModel(
        IClipboardService clipboardService,
        ITelegramService telegramService,
        ISettingsService settingsService,
        IClipboardMonitorService clipboardMonitorService,
        IGlobalHotkeyService globalHotkeyService)
    {
        _clipboardService = clipboardService;
        _telegramService = telegramService;
        _settingsService = settingsService;
        _clipboardMonitorService = clipboardMonitorService;
        _globalHotkeyService = globalHotkeyService;

        _ = LoadSettingsAsync();
        
        globalHotkeyService.HotkeyPressed += async (s, e) =>
        {
            await ReadClipboardAsync();
            await SendToTelegramAsync();
        };
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();
        BotToken = settings.BotToken;
        ChatId = settings.ChatId;
        ClipboardPopupHotkey = settings.ClipboardPopupHotkey;
        _recipients = settings.Recipients;
        _globalHotkeyService.SetPopupHotkey(ClipboardPopupHotkey);
    }

    private Settings BuildCurrentSettings() => new()
    {
        BotToken = BotToken,
        ChatId = ChatId,
        Recipients = _recipients,
        ClipboardPopupHotkey = ClipboardPopupHotkey
    };

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var settings = BuildCurrentSettings();
        await _settingsService.SaveSettingsAsync(settings);
        _globalHotkeyService.SetPopupHotkey(ClipboardPopupHotkey);
        SetStatus("Settings saved successfully.", false);
    }

    [RelayCommand]
    private async Task ChangeHotkeyAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var dialog = new Views.HotkeyRecorderWindow();
            var result = await dialog.ShowDialog<string>(desktop.MainWindow);
            
            if (!string.IsNullOrWhiteSpace(result))
            {
                ClipboardPopupHotkey = result;
                // Auto-save setting when changed via dialog
                SaveSettingsCommand.Execute(null);
            }
        }
    }

    [RelayCommand]
    private async Task ReadClipboardAsync()
    {
        var text = await _clipboardService.GetTextAsync();
        ClipboardText = text?.Trim() ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(ClipboardText))
        {
            SetStatus("Clipboard is empty or does not contain text.", true);
        }
        else
        {
            SetStatus("Clipboard text loaded.", false);
        }
    }

    [RelayCommand]
    private async Task SendToTelegramAsync()
    {
        var settings = BuildCurrentSettings();

        if (!_settingsService.IsConfigured(settings))
        {
            SetStatus("Please configure Bot Token and Chat ID in Settings.", true);
            return;
        }

        SetStatus("Sending...", false);

        var result = await _telegramService.SendMessageAsync(ClipboardText, Note, settings);
        
        if (result.Success)
        {
            var countInfo = result.TotalCount > 1 ? $" ({result.SuccessCount}/{result.TotalCount})" : "";
            SetStatus($"Sent successfully!{countInfo}", false);
            ClipboardText = string.Empty;
            Note = string.Empty;
        }
        else
        {
            SetStatus(result.ErrorMessage ?? "Failed to send.", true);
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        var settings = BuildCurrentSettings();

        if (!_settingsService.IsConfigured(settings))
        {
            SetStatus("Please configure Bot Token and Chat ID in Settings.", true);
            return;
        }

        SetStatus("Testing connection...", false);

        var result = await _telegramService.TestConnectionAsync(settings);

        if (result.Success)
        {
            var countInfo = result.TotalCount > 1 ? $" ({result.SuccessCount}/{result.TotalCount})" : "";
            SetStatus($"Connection test passed!{countInfo}", false);
        }
        else
        {
            SetStatus(result.ErrorMessage ?? "Connection test failed.", true);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }

    [RelayCommand]
    private void SetActiveClipboardItem(ClipboardItem item)
    {
        if (item != null && item.Type == ClipboardItemType.Text)
        {
            ClipboardText = item.PreviewText;
            SetStatus("Clipboard item selected.", false);
        }
    }

    [RelayCommand]
    private void TogglePinItem(ClipboardItem? item)
    {
        if (item == null) return;
        item.IsPinned = !item.IsPinned;

        if (item.IsPinned && History.Contains(item))
        {
            var index = History.IndexOf(item);
            if (index > 0)
            {
                History.Move(index, 0);
            }
        }
    }

    [RelayCommand]
    private void DeleteClipboardItem(ClipboardItem? item)
    {
        if (item == null) return;
        if (History.Contains(item))
        {
            History.Remove(item);
            item.Dispose();
            SetStatus("Item removed from history.", false);
        }
    }

    [RelayCommand]
    private async Task ShareClipboardItemAsync(ClipboardItem? item)
    {
        if (item == null) return;
        var settings = BuildCurrentSettings();

        if (!_settingsService.IsConfigured(settings))
        {
            SetStatus("Please configure Bot Token and Chat ID in Settings.", true);
            return;
        }

        SetStatus("Sending item to Telegram...", false);

        SendResult result;
        if (item.Type == ClipboardItemType.Text)
        {
            result = await _telegramService.SendMessageAsync(item.PreviewText, Note, settings);
        }
        else if (item.Type == ClipboardItemType.Image && item.RawData is string imagePath && File.Exists(imagePath))
        {
            using var fileStream = File.OpenRead(imagePath);
            result = await _telegramService.SendPhotoAsync(fileStream, Path.GetFileName(imagePath), Note, settings);
        }
        else if (item.Type == ClipboardItemType.Files && item.RawData is List<string> filePaths && filePaths.Count > 0)
        {
            var firstFile = filePaths.First();
            var ext = Path.GetExtension(firstFile).ToLowerInvariant();
            if ((ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp") && File.Exists(firstFile))
            {
                using var fileStream = File.OpenRead(firstFile);
                result = await _telegramService.SendPhotoAsync(fileStream, Path.GetFileName(firstFile), Note, settings);
            }
            else
            {
                result = await _telegramService.SendMessageAsync(string.Join("\n", filePaths), Note, settings);
            }
        }
        else
        {
            result = await _telegramService.SendMessageAsync(item.PreviewText, Note, settings);
        }

        if (result.Success)
        {
            SetStatus("Item sent to Telegram successfully!", false);
        }
        else
        {
            SetStatus(result.ErrorMessage ?? "Failed to send item.", true);
        }
    }
}
