# Global OS Shortcut Design

## Objective
Implement a cross-platform global hotkey feature for the TelePick Avalonia desktop application. The hotkey should work regardless of whether the application is in focus, allowing the user to instantly capture clipboard content.

## Chosen Approach
We will use **SharpHook** (`libuiohook` wrapper), which provides a unified, highly reliable API across Linux, Windows, and macOS for intercepting global keyboard events.

## Architecture

1. **IGlobalHotkeyService**
   - An interface defining the contract for hotkey registration and listening.
   - Methods: `void StartListening()`, `void StopListening()`, `void RegisterHotkey(string keyCombination, Action onTriggered)`.

2. **SharpHookGlobalHotkeyService**
   - The concrete implementation implementing `IGlobalHotkeyService` using `TaskPoolGlobalHook` from `SharpHook`.
   - Runs on a background thread to prevent blocking the Avalonia UI thread.
   - Listens to `Hook.KeyPressed` and `Hook.KeyReleased` to maintain the state of pressed modifier keys and trigger the callback when the registered combination matches.

3. **Settings Integration**
   - The `Settings` model will be updated to include a `CaptureHotkey` property (e.g., `"Ctrl+Shift+T"`).
   - `SettingsWindow.axaml` (or the Settings tab in MainWindow) will have an input to configure or "record" this shortcut.

4. **Event Flow & UI Trigger**
   - `App.axaml.cs` or `MainWindowViewModel` will instantiate the service and register the shortcut upon startup.
   - When triggered, the callback will:
     1. Automatically read the clipboard text.
     2. Execute `SendToTelegramAsync` to send it directly to Telegram without requiring manual confirmation.
     3. Show a brief native OS notification or update the app's status bar to confirm it was sent.

## Error Handling
- If `SharpHook` fails to bind to the OS (e.g., due to missing X11/Wayland dependencies on some minimal Linux setups), the service will catch the exception and disable the hotkey feature gracefully, showing a status message instead of crashing the app.
- Key combinations will be parsed safely; invalid formats will fallback to a default (e.g., `Ctrl+Shift+T`).

## Testing Strategy
- Manual verification on Linux to ensure background detection works.
- Verify that standard Avalonia inputs inside the app are not swallowed or blocked by the global hook.
