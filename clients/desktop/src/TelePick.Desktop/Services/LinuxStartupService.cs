using System;
using System.IO;

namespace TelePick.Desktop.Services;

public class LinuxStartupService : IStartupService
{
    private readonly string _autostartDir;
    private readonly string _desktopFilePath;
    private readonly string _executablePath;

    public LinuxStartupService()
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrEmpty(configHome))
        {
            configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }
        _autostartDir = Path.Combine(configHome, "autostart");
        _desktopFilePath = Path.Combine(_autostartDir, "telepick.desktop");
        _executablePath = Environment.ProcessPath ?? string.Empty;
    }

    public bool IsEnabled()
    {
        return File.Exists(_desktopFilePath);
    }

    public void Enable()
    {
        if (string.IsNullOrEmpty(_executablePath)) return;

        try
        {
            Directory.CreateDirectory(_autostartDir);
            var content = $"""
                [Desktop Entry]
                Type=Application
                Name=TelePick
                Exec="{_executablePath}"
                Hidden=false
                NoDisplay=false
                X-GNOME-Autostart-enabled=true
                """;
            File.WriteAllText(_desktopFilePath, content);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to enable Linux startup: {ex.Message}");
        }
    }

    public void Disable()
    {
        try
        {
            if (File.Exists(_desktopFilePath))
            {
                File.Delete(_desktopFilePath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to disable Linux startup: {ex.Message}");
        }
    }
}
