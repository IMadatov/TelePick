# TelePick Desktop

Cross-platform desktop client for TelePick, built with .NET 8 and Avalonia UI.

> **Status:** Minimal MVP available on Linux — settings, clipboard capture, note, and Telegram send.

## Overview

The desktop client provides the same core workflow as the browser extension: read text from the clipboard, optionally add a note, and send it to your Telegram chat via your own bot.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Linux (primary MVP target), Windows, or macOS

Install Avalonia templates (one-time):

```bash
dotnet new install Avalonia.Templates
```

## Build and run

```bash
cd clients/desktop
dotnet build
dotnet test
dotnet run --project src/TelePick.Desktop
```

## Usage

1. On first launch, **Settings** opens automatically.
2. Enter your **Bot Token** and **Chat ID**, then click **Save**.
3. Set your **global capture shortcut** (default `Ctrl+Alt+T`):
   - Click **Record shortcut**
   - Press the key combination you want (e.g. `Alt+C`)
   - Click **Save** to apply
4. Click **Send test message** to verify the connection.
5. Anywhere on the desktop: use your shortcut to copy the current selection, open TelePick, and load clipboard text.
6. Add an optional note and click **Send to Telegram**.

Settings are stored at `~/.config/TelePick/settings.json` (XDG config directory).

### Global shortcut notes

- Works system-wide while TelePick is running (X11 recommended on Linux).
- On **Linux X11**, selected text is read from the native **PRIMARY** selection first (`xclip` or `xsel`); no simulated Ctrl+C when that works.
- Install one helper if missing: `sudo apt install xclip` (or `xsel`).
- On **Wayland**, PRIMARY may be unavailable; TelePick falls back to simulated copy, then clipboard.
- After changing the shortcut in Settings, click **Save** to re-register it.

## Project structure

```
clients/desktop/
  TelePick.sln
  Directory.Build.props
  src/
    TelePick.Core/           # Telegram API, message composition, settings validation
    TelePick.Platform/       # Linux settings store, platform abstractions
    TelePick.Desktop/        # Avalonia MVVM UI
  tests/
    TelePick.Core.Tests/
```

## Architecture

### TelePick.Core

- `MessageComposer` — same HTML message format as the browser extension
- `TelegramBotClient` — `sendMessage` via Telegram Bot API
- `SettingsService` — bot token and chat ID validation
- `NoteSendService` — orchestrates clipboard text → Telegram send

### TelePick.Platform

- `JsonSettingsStore` — persists settings to `~/.config/TelePick/settings.json`
- `IClipboardService` — clipboard abstraction (Avalonia implementation in Desktop)

### TelePick.Desktop

- **MainWindow** — clipboard preview, note input, send
- **SettingsWindow** — bot token, chat ID, save, test message
- MVVM with CommunityToolkit.Mvvm and Microsoft.Extensions.DependencyInjection

## Roadmap

- Multi-recipient routing (extension parity)
- Screenshot capture
- System tray and global hotkey
- OS credential store for bot token

## Relationship to the extension

| Concern | Extension | Desktop (MVP) |
|---------|-----------|---------------|
| Text capture | Browser selection | Clipboard |
| Settings storage | `chrome.storage.sync` | `~/.config/TelePick/settings.json` |
| Telegram API | `background.js` | `TelePick.Core` |
| UI | HTML popup + options | Avalonia windows |
