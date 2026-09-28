using iM.TelePick.Desktop.Models;
using System.Threading.Tasks;

namespace iM.TelePick.Desktop.Services;

public interface ISettingsService
{
    Task<Settings> LoadSettingsAsync();
    Task SaveSettingsAsync(Settings settings);
    bool IsConfigured(Settings settings);
}
