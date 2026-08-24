using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TelePick.Desktop.Models;

namespace TelePick.Desktop.Services;

public interface IClipboardMonitorService
{
    ObservableCollection<ClipboardItem> History { get; }
    void StartMonitoring(Avalonia.Input.Platform.IClipboard clipboard);
    void StopMonitoring();
}
