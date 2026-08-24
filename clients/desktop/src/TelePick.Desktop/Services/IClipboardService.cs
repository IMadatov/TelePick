using System.Threading.Tasks;

namespace TelePick.Desktop.Services;

public interface IClipboardService
{
    Task<string?> GetTextAsync();
}
