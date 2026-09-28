using System.Threading.Tasks;

namespace iM.TelePick.Desktop.Services;

public interface IClipboardService
{
    Task<string?> GetTextAsync();
}
