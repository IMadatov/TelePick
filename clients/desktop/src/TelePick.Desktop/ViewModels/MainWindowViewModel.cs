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
    
    public string SearchShortcutHint 
    {
        get
        {
            var key = LocalSearchFocusHotkey;
            if (string.IsNullOrEmpty(key)) return "";
            
            bool isMac = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
            if (isMac)
            {
                key = key.Replace("Control", "⌘").Replace("Alt", "⌥").Replace("Shift", "⇧").Replace("+", "");
            }
            else
            {
                key = key.Replace("Control", "Ctrl");
            }
            return key;
        }
    }
    
    public System.Collections.ObjectModel.ObservableCollection<ClipboardItem> History => _clipboardMonitorService.History;

    public IEnumerable<ClipboardItem> FilteredHistory 
    {
        get 
        {
            var query = History.AsEnumerable();
            if (FilterCategory != "All")
            {
                if (FilterCategory == "Text")
                    query = query.Where(x => x.Type == ClipboardItemType.Text && !(x.PreviewText?.StartsWith("http", System.StringComparison.OrdinalIgnoreCase) == true));
                else if (FilterCategory == "Images")
                    query = query.Where(x => x.Type == ClipboardItemType.Image);
                else if (FilterCategory == "Files")
                    query = query.Where(x => x.Type == ClipboardItemType.Files);
                else if (FilterCategory == "Links")
                    query = query.Where(x => x.Type == ClipboardItemType.Text && x.PreviewText?.StartsWith("http", System.StringComparison.OrdinalIgnoreCase) == true);
            }
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(x => (x.Title != null && x.Title.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)) ||
                                         (x.PreviewText != null && x.PreviewText.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)));
            }
            return query.OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.Timestamp);
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set 
        {
            if (SetProperty(ref _searchText, value))
            {
                OnPropertyChanged(nameof(FilteredHistory));
            }
        }
    }

    private string _filterCategory = "All";
    public string FilterCategory
    {
        get => _filterCategory;
        set 
        {
            if (SetProperty(ref _filterCategory, value))
            {
                OnPropertyChanged(nameof(FilteredHistory));
            }
        }
    }

    [RelayCommand]
    private void SetFilter(string filter)
    {
        FilterCategory = string.IsNullOrEmpty(filter) ? "All" : filter;
    }

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
    
    // Advanced Settings
    [ObservableProperty]
    private int _selectedPageIndex = 1;

    [ObservableProperty]
    private bool _launchOnStartup = true;

    [ObservableProperty]
    private bool _syncAcrossDevices = false;

    [ObservableProperty]
    private string _historyLimit = "500";

    [ObservableProperty]
    private bool _verboseLogging = false;

    [ObservableProperty]
    private string _sendToTelegramHotkey = "Control+T";

    [ObservableProperty]
    private string _globalSearchHotkey = "Control+Space";

    [ObservableProperty]
    private string _clearHistoryHotkey = "";

    [ObservableProperty]
    private string _pauseMonitoringHotkey = "Alt+P";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchShortcutHint))]
    private string _localSearchFocusHotkey = "Control+K";

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
        
        _clipboardMonitorService.History.CollectionChanged += (s, e) => OnPropertyChanged(nameof(FilteredHistory));
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();
        BotToken = settings.BotToken;
        ChatId = settings.ChatId;
        ClipboardPopupHotkey = settings.ClipboardPopupHotkey;
        LaunchOnStartup = settings.LaunchOnStartup;
        SyncAcrossDevices = settings.SyncAcrossDevices;
        HistoryLimit = settings.HistoryLimit;
        VerboseLogging = settings.VerboseLogging;
        SendToTelegramHotkey = settings.SendToTelegramHotkey;
        GlobalSearchHotkey = settings.GlobalSearchHotkey;
        ClearHistoryHotkey = settings.ClearHistoryHotkey;
        PauseMonitoringHotkey = settings.PauseMonitoringHotkey;
        LocalSearchFocusHotkey = settings.LocalSearchFocusHotkey;
        _recipients = settings.Recipients;
        
        // Re-register hotkeys through App.axaml.cs or a separate initialization method.
        // For now, setting the property is enough; App.axaml.cs will handle registration.
    }

    private Settings BuildCurrentSettings() => new()
    {
        BotToken = BotToken,
        ChatId = ChatId,
        Recipients = _recipients,
        ClipboardPopupHotkey = ClipboardPopupHotkey,
        LaunchOnStartup = LaunchOnStartup,
        SyncAcrossDevices = SyncAcrossDevices,
        HistoryLimit = HistoryLimit,
        VerboseLogging = VerboseLogging,
        SendToTelegramHotkey = SendToTelegramHotkey,
        GlobalSearchHotkey = GlobalSearchHotkey,
        ClearHistoryHotkey = ClearHistoryHotkey,
        PauseMonitoringHotkey = PauseMonitoringHotkey,
        LocalSearchFocusHotkey = LocalSearchFocusHotkey
    };

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var settings = BuildCurrentSettings();
        await _settingsService.SaveSettingsAsync(settings);

        if (Avalonia.Application.Current is App app)
        {
            app.RegisterAllHotkeys(settings);
        }

        SetStatus("Settings saved successfully.", false);
    }

    [RelayCommand]
    private async Task ChangeHotkey(string hotkeyName)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var window = new Views.HotkeyRecorderWindow();
            var result = await window.ShowDialog<string?>(desktop.MainWindow);
            if (!string.IsNullOrEmpty(result))
            {
                switch (hotkeyName)
                {
                    case "ClipboardPopupHotkey": ClipboardPopupHotkey = result; break;
                    case "SendToTelegramHotkey": SendToTelegramHotkey = result; break;
                    case "GlobalSearchHotkey": GlobalSearchHotkey = result; break;
                    case "ClearHistoryHotkey": ClearHistoryHotkey = result; break;
                    case "PauseMonitoringHotkey": PauseMonitoringHotkey = result; break;
                    case "LocalSearchFocusHotkey": LocalSearchFocusHotkey = result; break;
                }
                
                await SaveSettingsAsync();
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

        var item = new ClipboardItem 
        { 
            Type = ClipboardItemType.Text, 
            PreviewText = ClipboardText 
        };
        item.DetermineIfCode();

        var result = await _telegramService.SendMessageAsync(item, Note, settings);
        
        if (result.Success)
        {
            var countInfo = result.TotalCount > 1 ? $" ({result.SuccessCount}/{result.TotalCount})" : "";
            SetStatus($"Sent successfully!{countInfo}", false);
            NotificationService.ShowSuccess("Sent to Telegram", $"Successfully delivered{countInfo}");
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
            NotificationService.ShowSuccess("Connection Test Passed", $"Settings are working{countInfo}");
        }
        else
        {
            SetStatus(result.ErrorMessage ?? "Connection test failed.", true);
        }
    }

    [RelayCommand]
    private void OpenLink(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open link: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearLocalCache()
    {
        History.Clear();
        SetStatus("Local cache cleared.", false);
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
        OnPropertyChanged(nameof(FilteredHistory));
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
            result = await _telegramService.SendMessageAsync(item, Note, settings);
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
                var pathsItem = new ClipboardItem 
                { 
                    Type = ClipboardItemType.Text, 
                    PreviewText = string.Join("\n", filePaths) 
                };
                result = await _telegramService.SendMessageAsync(pathsItem, Note, settings);
            }
        }
        else
        {
            result = await _telegramService.SendMessageAsync(item, Note, settings);
        }

        if (result.Success)
        {
            SetStatus("Item sent to Telegram successfully!", false);
            NotificationService.ShowSuccess("Sent to Telegram", "1 item successfully delivered");
        }
        else
        {
            SetStatus(result.ErrorMessage ?? "Failed to send item.", true);
        }
    }
}
