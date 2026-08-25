# Silent Send to Telegram via Hotkey — Design Spec

## Goal

Add a configurable global hotkey that silently reads the clipboard (text or image) and sends it to the default Telegram recipient. No window opens — success/error feedback will be handled by a custom floating notification (out of scope for this spec, to be implemented later).

## Scope

- **In scope:** Configurable "Send to Telegram" hotkey, silent clipboard read (text + image), send to default recipient, default recipient flag on Recipient model, Settings UI for hotkey, console log for feedback (placeholder for notification)
- **Out of scope:** Custom floating notification UI, recipient selection popup, file sending

## Flow

```
User presses hotkey (default: Ctrl+Shift+S)
  → SharpHookGlobalHotkeyService fires SendToTelegramHotkeyPressed event
    → App.axaml.cs handler (on UI thread):
      1. ClipboardService.GetTextAsync()
         → text found? → TelegramService.SendMessageAsync(text, "", settings, defaultDest)
         → no text? → ClipboardService.GetBitmapStreamAsync()
           → image found? → TelegramService.SendPhotoAsync(stream, "clipboard.png", null, settings, defaultDest)
           → nothing? → log "Clipboard is empty"
      2. Log result to console (notification placeholder)
```

### Default recipient resolution

```
settings.Recipients.FirstOrDefault(r => r.IsDefault)
  ?? settings.Recipients.FirstOrDefault()
```

If no recipients configured, log error.

---

## Changes

### [MODIFY] `Models/Recipient.cs`

Add `IsDefault` flag:

```csharp
public bool IsDefault { get; set; }
```

### [MODIFY] `Models/Settings.cs`

Add `SendToTelegramHotkey`:

```csharp
public string SendToTelegramHotkey { get; set; } = "Control+Shift+S";
```

---

### [MODIFY] `Services/IClipboardService.cs`

Add image reading method:

```csharp
Task<Stream?> GetBitmapStreamAsync();
```

### [MODIFY] `Services/ClipboardService.cs`

Implement `GetBitmapStreamAsync()` — uses Avalonia clipboard API to read image data formats, encode to PNG stream via `Avalonia.Media.Imaging.Bitmap`.

---

### [MODIFY] `Services/IGlobalHotkeyService.cs`

Add:

```csharp
event EventHandler? SendToTelegramHotkeyPressed;
void SetSendToTelegramHotkey(string hotkey);
```

### [MODIFY] `Services/SharpHookGlobalHotkeyService.cs`

Add a third hotkey slot (same pattern as popup hotkey):
- `_sendCtrlRequired`, `_sendShiftRequired`, `_sendAltRequired`, `_sendMetaRequired`, `_sendKeyRequired`, `_sendKeyPressed` fields
- `SetSendToTelegramHotkey()` — parse hotkey string (reuse same logic as `SetPopupHotkey`)
- In `OnKeyPressed` — check third hotkey and fire `SendToTelegramHotkeyPressed`
- In `OnKeyReleased` — reset `_sendKeyPressed`

**Refactoring note:** The hotkey parsing logic is duplicated between `SetPopupHotkey` and the new `SetSendToTelegramHotkey`. Extract a shared `ParseHotkey()` method that returns a `HotkeyBinding` record:

```csharp
private record HotkeyBinding(bool Ctrl, bool Shift, bool Alt, bool Meta, KeyCode? Key);

private static HotkeyBinding ParseHotkey(string hotkey) { ... }
```

This also fixes the existing bug where `SetPopupHotkey` only parses `a-z` keys — the shared parser should support digits and function keys too.

---

### [MODIFY] `App.axaml.cs`

Add event handler for `SendToTelegramHotkeyPressed`:

```csharp
hotkeyService.SendToTelegramHotkeyPressed += async (s, e) =>
{
    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
    {
        await SendClipboardToTelegramAsync();
    });
};
```

Add `SendClipboardToTelegramAsync()` private method:
1. Load settings via `ISettingsService`
2. Find default recipient → build `Destination`
3. Try text first via `IClipboardService.GetTextAsync()`
4. If no text, try image via `IClipboardService.GetBitmapStreamAsync()`
5. Call appropriate `TelegramService` method
6. Log result to console (`Console.WriteLine` — placeholder)

---

### [MODIFY] `ViewModels/MainWindowViewModel.cs`

- Add `SendToTelegramHotkey` observable property (loaded from / saved to Settings)
- Add `ChangeSendHotkeyCommand` — opens HotkeyRecorderWindow, saves result
- Update `LoadSettingsAsync()` and `SaveSettingsAsync()` to include `SendToTelegramHotkey`
- Call `_globalHotkeyService.SetSendToTelegramHotkey()` on load and after change

---

## Files Summary

| File | Action | ~Lines changed |
|------|--------|----------------|
| `Models/Recipient.cs` | MODIFY | +1 |
| `Models/Settings.cs` | MODIFY | +1 |
| `Services/IClipboardService.cs` | MODIFY | +1 |
| `Services/ClipboardService.cs` | MODIFY | +20 |
| `Services/IGlobalHotkeyService.cs` | MODIFY | +2 |
| `Services/SharpHookGlobalHotkeyService.cs` | MODIFY | +50 (refactor + new slot) |
| `App.axaml.cs` | MODIFY | +30 |
| `ViewModels/MainWindowViewModel.cs` | MODIFY | +20 |

## Verification

```bash
cd clients/desktop && dotnet build src/TelePick.Desktop
```

Manual test: run app → set hotkey in settings → copy text → press hotkey → check Telegram chat.
