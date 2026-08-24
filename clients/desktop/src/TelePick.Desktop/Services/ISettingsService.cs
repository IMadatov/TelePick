using TelePick.Desktop.Models;
using System.Threading.Tasks;

namespace TelePick.Desktop.Services;

public interface ISettingsService
{
    Task<Settings> LoadSettingsAsync();
    Task SaveSettingsAsync(Settings settings);
    bool IsConfigured(Settings settings);
}
