using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TelePick.Desktop.Models;
using Avalonia.Input.Platform;

namespace TelePick.Desktop.Services;

public interface IClipboardMonitorService
{
    ObservableCollection<ClipboardItem> History { get; }
    void StartMonitoring(IClipboard clipboard);
    void StopMonitoring();
    bool IsPaused { get; set; }
}
