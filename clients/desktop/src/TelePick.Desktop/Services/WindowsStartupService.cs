using System;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace TelePick.Desktop.Services;

[SupportedOSPlatform("windows")]
public class WindowsStartupService : IStartupService
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "TelePick";
    private readonly string _executablePath;

    public WindowsStartupService()
    {
        _executablePath = Environment.ProcessPath ?? string.Empty;
    }

    public bool IsEnabled()
    {
        if (string.IsNullOrEmpty(_executablePath)) return false;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            var value = key?.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value) && value.Contains(_executablePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public void Enable()
    {
        if (string.IsNullOrEmpty(_executablePath)) return;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.SetValue(AppName, $"\"{_executablePath}\"");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to enable Windows startup: {ex.Message}");
        }
    }

    public void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.DeleteValue(AppName, false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to disable Windows startup: {ex.Message}");
        }
    }
}
