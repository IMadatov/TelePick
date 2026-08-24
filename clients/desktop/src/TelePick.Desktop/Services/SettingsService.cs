using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TelePick.Desktop.Models;

namespace TelePick.Desktop.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;

    public SettingsService()
    {
        var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "TelePick");
        Directory.CreateDirectory(configDir);
        _settingsFilePath = Path.Combine(configDir, "settings.json");
    }

    public async Task<Settings> LoadSettingsAsync()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new Settings();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsFilePath);
            return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
        }
        catch
        {
            return new Settings();
        }
    }

    public async Task SaveSettingsAsync(Settings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsFilePath, json);
    }

    public bool IsConfigured(Settings settings)
    {
        return !string.IsNullOrWhiteSpace(settings.BotToken) && !string.IsNullOrWhiteSpace(settings.ChatId);
    }
}
