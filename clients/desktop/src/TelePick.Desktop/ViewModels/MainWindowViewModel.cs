using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        _ = LoadSettingsAsync();
        
        globalHotkeyService.HotkeyPressed += async (s, e) =>
        {
            await ReadClipboardAsync();
            await SendToTelegramAsync();
        };
        globalHotkeyService.Start();
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();
        BotToken = settings.BotToken;
        ChatId = settings.ChatId;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var settings = new Settings
        {
            BotToken = this.BotToken,
            ChatId = this.ChatId
        };
        await _settingsService.SaveSettingsAsync(settings);
        SetStatus("Settings saved successfully.", false);
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
        var settings = new Settings
        {
            BotToken = this.BotToken,
            ChatId = this.ChatId
        };

        if (!_settingsService.IsConfigured(settings))
        {
            SetStatus("Please configure Bot Token and Chat ID in Settings.", true);
            return;
        }

        SetStatus("Sending...", false);

        var result = await _telegramService.SendMessageAsync(ClipboardText, Note, settings);
        
        if (result.Success)
        {
            SetStatus("Sent successfully!", false);
            ClipboardText = string.Empty;
            Note = string.Empty;
        }
        else
        {
            SetStatus(result.ErrorMessage ?? "Failed to send.", true);
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
}
