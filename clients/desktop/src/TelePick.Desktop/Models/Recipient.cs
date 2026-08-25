using System.Collections.Generic;

namespace TelePick.Desktop.Models;

public class Recipient
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public List<Topic> Topics { get; set; } = [];
}
