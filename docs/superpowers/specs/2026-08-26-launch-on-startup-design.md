# Launch on Startup Design Spec

## Overview
This document outlines the architecture for the "Launch on Startup" feature in TelePick, a cross-platform Avalonia application. The current UI has a toggle for this feature, but there is no underlying OS integration. This design provides a clean, OS-agnostic interface with platform-specific implementations.

## Architecture

### 1. Interfaces
A new interface will be created to abstract the OS startup mechanics.
`IStartupService`
- `bool IsEnabled()`: Checks if the application is currently configured to run on startup.
- `void Enable()`: Registers the application to run on startup.
- `void Disable()`: Removes the application from startup.

### 2. Platform Implementations
- **LinuxStartupService**: Modifies the `~/.config/autostart` directory. It will create a `telepick.desktop` file with an `Exec` directive pointing to the currently running executable (`System.Environment.ProcessPath`).
- **WindowsStartupService**: Interacts with `Microsoft.Win32.Registry` to add/remove a string value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. The value will point to `System.Environment.ProcessPath`.
- **MacStartupService**: A stub implementation throwing `NotSupportedException` (or logging a warning) as macOS is not targeted for this phase.

### 3. Factory / Dependency Injection
A `StartupServiceFactory` will be used during application startup (in `App.axaml.cs` or `Program.cs` / DI setup) to instantiate the correct service based on `RuntimeInformation.IsOSPlatform`. 
```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return new LinuxStartupService();
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return new WindowsStartupService();
return new DummyStartupService();
```

### 4. Integration with ViewModels
In `MainWindowViewModel.cs`:
- On initialization, synchronize the UI toggle with the actual OS state:
  `LaunchOnStartup = _startupService.IsEnabled();`
- When `LaunchOnStartup` is toggled by the user, update the OS accordingly:
  ```csharp
  if (value) _startupService.Enable(); 
  else _startupService.Disable();
  ```
- Persist the boolean flag in `Settings.cs` as a fallback or for UI restoration before the service fully initializes.

## Error Handling
- The services will wrap OS-level IO or Registry calls in try/catch blocks. If a permission error or IO error occurs, it should be caught, potentially logged, and fail gracefully without crashing the application.
- If the executable path (`ProcessPath`) cannot be determined, `Enable()` should abort safely.

## Testing Strategy
- Manual verification on Linux: toggle the setting in the UI and verify `~/.config/autostart/telepick.desktop` is created/removed correctly.
- Ensure the app launches successfully on the next login (or via manual execution of the desktop file).
