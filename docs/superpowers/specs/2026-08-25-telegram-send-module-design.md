# Desktop Telegram Send Module — Design Spec

## Goal

Replace the minimal `TelegramService` (single-chat, text-only `sendMessage`) with a full-featured Telegram sending module matching the Chrome extension's capabilities: multi-recipient routing, forum topic support, `sendPhoto`, HTML formatting, test connection, and legacy settings migration.

## Scope

- **In scope:** TelegramService rewrite, new models, ITelegramService interface expansion, minimal ViewModel integration
- **Out of scope:** Recipients management UI (add/edit/delete recipients in Settings tab), MainWindow.axaml changes — these are separate tasks

## Models

### [NEW] `Models/Recipient.cs`

```csharp
public class Recipient
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public List<Topic> Topics { get; set; } = [];
}
```

### [NEW] `Models/Topic.cs`

```csharp
public class Topic
{
    public string Id { get; set; } = string.Empty;
    public string TopicId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
```

### [NEW] `Models/Destination.cs`

Resolved send target — a specific chatId + optional topicId.

```csharp
public class Destination
{
    public string ChatId { get; set; } = string.Empty;
    public string? TopicId { get; set; }
}
```

### [MODIFY] `Models/Settings.cs`

Add `Recipients` list. Keep `ChatId` for backward-compatible deserialization (legacy migration).

```csharp
public class Settings
{
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;           // legacy, kept for migration
    public List<Recipient> Recipients { get; set; } = [];
    public string ClipboardPopupHotkey { get; set; } = "Control+Shift+V";
}
```

### [MODIFY] `Models/SendResult.cs`

Add multi-destination result fields.

```csharp
public class SendResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int TotalCount { get; set; }
    public List<string> Errors { get; set; } = [];

    public static SendResult Ok() => new() { Success = true, SuccessCount = 1, TotalCount = 1 };
    public static SendResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}
```

## ITelegramService Interface

### [MODIFY] `Services/ITelegramService.cs`

```csharp
public interface ITelegramService
{
    Task<SendResult> SendMessageAsync(
        string text,
        string note,
        Settings settings,
        List<Destination>? selectedDestinations = null);

    Task<SendResult> SendPhotoAsync(
        Stream photoStream,
        string fileName,
        string? caption,
        Settings settings,
        List<Destination>? selectedDestinations = null);

    Task<SendResult> TestConnectionAsync(
        Settings settings,
        List<Destination>? selectedDestinations = null);
}
```

- `selectedDestinations = null` → send to all recipients
- `Settings` carries `BotToken` and `Recipients`
- `Stream` for photo — memory-efficient, works with Avalonia clipboard bitmap

## TelegramService Implementation

### [REWRITE] `Services/TelegramService.cs`

Internal structure (all private except the 3 interface methods):

| Method | Purpose |
|--------|---------|
| `SendMessageAsync` | Orchestrator — resolve destinations, build message, dispatch to all, aggregate |
| `SendPhotoAsync` | Orchestrator — resolve destinations, build caption, read stream to bytes once, dispatch to all |
| `TestConnectionAsync` | Orchestrator — resolve destinations, send test message to each |
| `SendMessageSingleAsync` | POST to `sendMessage` API for one destination |
| `SendPhotoSingleAsync` | POST to `sendPhoto` API (multipart/form-data) for one destination |
| `BuildMessage` | Format: `"quoted text"` + `📝 Note:` — HTML escaped |
| `BuildPhotoCaption` | Format: `📝 Note:` — HTML escaped, max 1024 chars |
| `EscapeHtml` | Escape `&`, `<`, `>`, `"` |
| `ResolveDestinations` | If selectedDestinations provided → use those; else → flatten all recipients + topics. Deduplicate by `chatId|topicId` |
| `MigrateSettings` | If `Recipients` empty but `ChatId` set → create single recipient |
| `BuildAggregateResult` | Merge individual results → single `SendResult` with counts |

### Telegram API details

- **Endpoint:** `https://api.telegram.org/bot{token}/sendMessage` and `/sendPhoto`
- **parse_mode:** `"HTML"` (same as extension)
- **sendMessage body:** `{ chat_id, text, parse_mode, disable_web_page_preview: false, message_thread_id? }`
- **sendPhoto body:** `multipart/form-data` with `chat_id`, `caption`, `parse_mode`, `photo` (file), `message_thread_id?`
- **Message length limit:** 4096 chars for text, 1024 chars for photo caption. Truncate with `"… [truncated]"` suffix if exceeded.
- **Multi-destination:** `Task.WhenAll` for parallel dispatch (same as extension's `Promise.all`)

### Legacy migration

Called at the start of each public method:

```
if settings.Recipients is empty AND settings.ChatId is not empty:
    create Recipient { Id="legacy-1", Label="Default", ChatId=settings.ChatId, Topics=[] }
    add to settings.Recipients
```

This is in-memory only — does not auto-save. The caller (ViewModel) decides when to persist.

### Message formatting

Matches extension's `buildMessage`:
```
"{escaped_text}"

📝 Note: {escaped_note}
```

Photo caption matches extension's `buildPhotoCaption`:
```
📝 Note: {escaped_note}
```

## ViewModel Integration

### [MODIFY] `ViewModels/MainWindowViewModel.cs`

Minimal changes — just enough to use the new service:

1. **Remove:** `ChatId` property (replaced by Recipients in Settings)
2. **Add:** `TestConnectionCommand` — calls `_telegramService.TestConnectionAsync()`
3. **Modify:** `SendToTelegramAsync()` — pass `selectedDestinations: null` (all recipients)
4. **Modify:** `SaveSettingsAsync()` — include `Recipients` in saved Settings
5. **Modify:** `LoadSettingsAsync()` — load `Recipients` from Settings
6. **Add:** `SendPhotoCommand` — placeholder, reads clipboard image and sends via `SendPhotoAsync()`

> **Note:** Full Recipients management UI and photo send UX are out of scope. The ViewModel exposes the capabilities, UI wiring is a separate task.

## Files Changed Summary

| File | Action | ~Lines |
|------|--------|--------|
| `Models/Recipient.cs` | NEW | ~10 |
| `Models/Topic.cs` | NEW | ~10 |
| `Models/Destination.cs` | NEW | ~8 |
| `Models/Settings.cs` | MODIFY | +2 lines |
| `Models/SendResult.cs` | MODIFY | +6 lines |
| `Services/ITelegramService.cs` | MODIFY | rewrite ~20 |
| `Services/TelegramService.cs` | REWRITE | ~220 |
| `ViewModels/MainWindowViewModel.cs` | MODIFY | ~30 lines changed |

## Verification

### Build
```bash
cd clients/desktop && dotnet build src/TelePick.Desktop
```

### Manual testing
- Load app → settings should still load (backward compat with old settings.json that has `ChatId` only)
- Legacy migration: old `ChatId` converted to recipient on send
- Send text message to Telegram → verify multi-dest dispatch
- Test connection → verify test message arrives
