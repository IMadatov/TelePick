using System.Collections.Generic;

namespace TelePick.Desktop.Models;

public class Settings
{
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public List<Recipient> Recipients { get; set; } = [];
    public string ClipboardPopupHotkey { get; set; } = "Control+Shift+V";
}
