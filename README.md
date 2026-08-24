# TelePick

**Pick selected text and send it as a note to your Telegram chat.**

TelePick is a monorepo with two clients that share the same core purpose — capture text (and screenshots), add an optional note, and send it to your Telegram chat via your own bot.

## Clients

| Client | Path | Status | Description |
|--------|------|--------|-------------|
| **Browser extension** | [`clients/extension/`](clients/extension/) | Available | Chrome extension (Manifest V3) — select text on any webpage and send via floating button |
| **Desktop app** | [`clients/desktop/`](clients/desktop/) | Available (Linux MVP) | .NET 8 + Avalonia UI — read clipboard text, add a note, send to Telegram |

## Quick start (extension)

1. Clone this repository.
2. Open Chrome and go to `chrome://extensions`.
3. Enable **Developer mode** (top right).
4. Click **Load unpacked** and select the [`clients/extension/`](clients/extension/) folder.
5. Open extension **Settings** and enter your Bot Token and Chat ID.

See [`clients/extension/README.md`](clients/extension/README.md) for full setup instructions, Telegram bot configuration, and usage details.

## Desktop app

Minimal MVP is available. See [`clients/desktop/README.md`](clients/desktop/README.md) for build instructions and usage.

```bash
cd clients/desktop
dotnet run --project src/TelePick.Desktop
```

## Repository structure

```
TelePick/
  README.md
  clients/
    extension/          # Chrome MV3 extension
      manifest.json
      src/
      icons/
      README.md
      PRIVACY.md
    desktop/            # .NET 8 Avalonia desktop app
      TelePick.sln
      src/
      tests/
      README.md
```

## Privacy

- The browser extension stores credentials in `chrome.storage.sync`. See [`clients/extension/PRIVACY.md`](clients/extension/PRIVACY.md) for details.
- The desktop app stores credentials in `~/.config/TelePick/settings.json`.
- Selected text is sent only to Telegram's API using your bot; no third-party servers are used.

## License

MIT
