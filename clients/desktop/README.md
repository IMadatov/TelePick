# TelePick Desktop

Cross-platform desktop client for TelePick, built with .NET and Avalonia UI.

> **Status:** Planned — solution scaffolding and implementation are upcoming tasks.

## Overview

The desktop client will provide the same core workflow as the browser extension: capture selected text (and screenshots), optionally add a note, and send it to your Telegram chat via your own bot.

Unlike the extension, the desktop app runs outside the browser and uses OS-level services for text selection, screenshots, global hotkeys, and secure credential storage.

## Target architecture

```
clients/desktop/
  TelePick.sln
  src/
    TelePick.Core/           # Shared business logic (no UI)
    TelePick.Desktop/        # Avalonia UI application (MVVM)
    TelePick.Platform/       # Platform abstractions and implementations
```

### TelePick.Core

Shared .NET library with no UI dependencies:

- Telegram Bot API client (send text, photos, test messages)
- Message composition (selected text, note, source metadata)
- Settings and credential abstractions (`ISettingsStore`, `ICredentialStore`)
- Domain models and validation (bot token, chat ID, routing targets)

This layer mirrors the logic currently in `clients/extension/src/background.js`, adapted for desktop use.

### TelePick.Desktop

Avalonia UI application targeting Windows, macOS, and Linux:

- **MVVM** with view models for Settings, Main, and Capture flows
- **Dependency injection** for services and platform bindings
- **Settings window** — bot token, chat ID, multi-recipient routing
- **Capture UI** — note input, destination picker, send confirmation
- **System tray** — quick access and background operation

### TelePick.Platform

Platform-specific services behind interfaces:

| Service | Purpose |
|---------|---------|
| `IGlobalHotkeyService` | Register system-wide shortcuts to trigger capture |
| `IScreenshotService` | Capture screen regions or full screen |
| `IClipboardService` | Read selected text from clipboard |
| `ICredentialStore` | OS keychain / credential manager for bot token |
| `ISettingsStore` | Persist user preferences (JSON or platform store) |

Implementations are split per OS where needed (Windows, macOS, Linux).

## Data flow

```
User action (hotkey / tray / UI)
        │
        ▼
TelePick.Desktop (ViewModel)
        │
        ├──► Platform services (clipboard, screenshot)
        │
        └──► TelePick.Core (compose message, call Telegram API)
                    │
                    ▼
             api.telegram.org
```

## Relationship to the extension

| Concern | Extension (`clients/extension/`) | Desktop (`clients/desktop/`) |
|---------|----------------------------------|------------------------------|
| Text capture | Browser selection + content script | Clipboard / OS selection |
| Screenshot | Page area via content script | OS screenshot API |
| Settings storage | `chrome.storage.sync` | Local file + OS credential store |
| Telegram API | `background.js` service worker | `TelePick.Core` HTTP client |
| UI | HTML/CSS popup + options page | Avalonia XAML views |

Shared behavior (message format, routing logic, validation) will live in `TelePick.Core` so both clients stay consistent.

## Next steps

1. Scaffold .NET solution with Avalonia template (`dotnet new avalonia.app`)
2. Extract core logic from extension into `TelePick.Core`
3. Implement platform service interfaces for the primary target OS
4. Build Settings and Capture UI with MVVM
5. Add system tray and global hotkey support
