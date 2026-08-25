using System.Collections.Generic;

namespace TelePick.Desktop.Models;

public class Settings
{
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public List<Recipient> Recipients { get; set; } = [];
    public string ClipboardPopupHotkey { get; set; } = "Control+Shift+V";
    
    // Advanced Settings
    public bool LaunchOnStartup { get; set; } = true;
    public bool SyncAcrossDevices { get; set; } = false;
    public string HistoryLimit { get; set; } = "500";
    public bool VerboseLogging { get; set; } = false;

    // Hotkeys
    public string SendToTelegramHotkey { get; set; } = "Control+T";
    public string GlobalSearchHotkey { get; set; } = "Control+Space";
    public string ClearHistoryHotkey { get; set; } = "";
    public string PauseMonitoringHotkey { get; set; } = "Alt+P";
    public string LocalSearchFocusHotkey { get; set; } = "Control+K";
}
