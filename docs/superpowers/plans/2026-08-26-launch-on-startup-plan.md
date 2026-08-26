# Launch on Startup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement cross-platform OS-level autostart functionality via a new `IStartupService`.

**Architecture:** Create `IStartupService` with `LinuxStartupService` and `WindowsStartupService` implementations. Use `StartupServiceFactory` to inject the correct service into `MainWindowViewModel`, tying the UI toggle to actual OS registry/filesystem changes.

**Tech Stack:** C#, Avalonia, `Microsoft.Win32.Registry` (built-in to .NET cross-platform libraries).

## Global Constraints

- Do not use third-party Nuget packages for startup logic.
- Ensure cross-platform compatibility by using `RuntimeInformation.IsOSPlatform`.
- Catch exceptions in OS-level operations (IO/Registry) to avoid crashing the application.

---

### Task 1: Create IStartupService and Factory

**Files:**
- Create: `clients/desktop/src/TelePick.Desktop/Services/IStartupService.cs`
- Create: `clients/desktop/src/TelePick.Desktop/Services/StartupServiceFactory.cs`
- Modify: `clients/desktop/src/TelePick.Desktop/App.axaml.cs` (or wherever services are registered)

**Interfaces:**
- Consumes: Nothing
- Produces: `IStartupService` (`bool IsEnabled()`, `void Enable()`, `void Disable()`), `StartupServiceFactory.Create()`

- [ ] **Step 1: Write IStartupService interface**

```csharp
namespace TelePick.Desktop.Services;

public interface IStartupService
{
    bool IsEnabled();
    void Enable();
    void Disable();
}
```

- [ ] **Step 2: Write StartupServiceFactory**

```csharp
using System.Runtime.InteropServices;

namespace TelePick.Desktop.Services;

public static class StartupServiceFactory
{
    public static IStartupService Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxStartupService(); // To be created in Task 2
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsStartupService(); // To be created in Task 3
        }
        
        return new DummyStartupService();
    }
}

public class DummyStartupService : IStartupService
{
    public bool IsEnabled() => false;
    public void Enable() { }
    public void Disable() { }
}
```

- [ ] **Step 3: Register in Dependency Injection**

In `App.axaml.cs` (or `Program.cs` / DI setup), register the factory result:
*Search for where `IClipboardMonitorService` or `ISettingsService` is instantiated and add:*
```csharp
var startupService = StartupServiceFactory.Create();
```
Pass it to `MainWindowViewModel` constructor (this might require updating constructor signatures in Task 4).

- [ ] **Step 4: Commit**
```bash
git add clients/desktop/src/TelePick.Desktop/Services/IStartupService.cs clients/desktop/src/TelePick.Desktop/Services/StartupServiceFactory.cs
git commit -m "feat: add IStartupService and StartupServiceFactory"
```

---

### Task 2: Implement LinuxStartupService

**Files:**
- Create: `clients/desktop/src/TelePick.Desktop/Services/LinuxStartupService.cs`

**Interfaces:**
- Consumes: `IStartupService`
- Produces: `LinuxStartupService` class

- [ ] **Step 1: Write LinuxStartupService implementation**

```csharp
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
```

- [ ] **Step 2: Commit**
```bash
git add clients/desktop/src/TelePick.Desktop/Services/LinuxStartupService.cs
git commit -m "feat: implement Linux startup service using desktop files"
```

---

### Task 3: Implement WindowsStartupService

**Files:**
- Create: `clients/desktop/src/TelePick.Desktop/Services/WindowsStartupService.cs`

**Interfaces:**
- Consumes: `IStartupService`
- Produces: `WindowsStartupService` class

- [ ] **Step 1: Write WindowsStartupService implementation**

```csharp
using System;
using Microsoft.Win32;

namespace TelePick.Desktop.Services;

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
```

- [ ] **Step 2: Check project file for Microsoft.Win32.Registry**
Verify if `Microsoft.Win32.Registry` needs to be installed, though it's typically available in standard .NET 10 cross-platform SDK. Run `dotnet build` to verify it compiles.

- [ ] **Step 3: Commit**
```bash
git add clients/desktop/src/TelePick.Desktop/Services/WindowsStartupService.cs
git commit -m "feat: implement Windows startup service using registry"
```

---

### Task 4: Integrate StartupService in MainWindowViewModel

**Files:**
- Modify: `clients/desktop/src/TelePick.Desktop/ViewModels/MainWindowViewModel.cs`
- Modify: `clients/desktop/src/TelePick.Desktop/App.axaml.cs` (to pass dependency)

**Interfaces:**
- Consumes: `IStartupService`
- Produces: Updated ViewModel logic tying `LaunchOnStartup` property to OS state.

- [ ] **Step 1: Update MainWindowViewModel constructor**

Inject `IStartupService` into `MainWindowViewModel`:
```csharp
private readonly IStartupService _startupService;

public MainWindowViewModel(
    ISettingsService settingsService,
    IClipboardMonitorService clipboardMonitorService,
    ITelegramService telegramService,
    IStartupService startupService) // <-- Add this
{
    // ... existing setup
    _startupService = startupService;
}
```

- [ ] **Step 2: Update App.axaml.cs to pass the service**

Find the instantiation of `MainWindowViewModel` in `App.axaml.cs` (usually in `OnFrameworkInitializationCompleted`) and pass the factory created service:
```csharp
var startupService = StartupServiceFactory.Create();
var viewModel = new MainWindowViewModel(settingsService, monitorService, telegramService, startupService);
```

- [ ] **Step 3: Update Property setter and Load logic**

In `MainWindowViewModel.cs`, find the `LaunchOnStartup` property backing field and setter. If it's using an `[ObservableProperty]`, we need to hook into the change or change it to a manual property:
```csharp
public bool LaunchOnStartup
{
    get => _launchOnStartup;
    set
    {
        if (SetProperty(ref _launchOnStartup, value))
        {
            if (value) _startupService.Enable();
            else _startupService.Disable();
            
            // Re-save settings if necessary
            _settingsService.SaveSettingsAsync(BuildCurrentSettings());
        }
    }
}
```

In `LoadSettingsAsync`, override the stored setting with the actual OS reality:
```csharp
LaunchOnStartup = _startupService.IsEnabled();
```

- [ ] **Step 4: Commit**
```bash
git add clients/desktop/src/TelePick.Desktop/ViewModels/MainWindowViewModel.cs clients/desktop/src/TelePick.Desktop/App.axaml.cs
git commit -m "feat: integrate OS startup service with UI toggle"
```
