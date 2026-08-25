# Telegram Message Styling Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Update the Telegram service to format text based on the clipboard item type (e.g. code as monospace `<pre><code>`).

**Architecture:** Modify `ITelegramService.SendMessageAsync` to accept a `ClipboardItem` object, allowing `TelegramService.BuildMessage` to inspect `IsLikelyCode` and `Type` to wrap the content in appropriate HTML tags (like `<pre><code>` for code), while removing hardcoded double quotes for standard text and links.

**Tech Stack:** C#, Avalonia UI, Telegram Bot API

## Global Constraints

- Do not introduce heavy third-party language detection libraries. Use generic monospace blocks for code.
- Ensure HTML escaping (`EscapeHtml`) is preserved for all text to prevent Telegram API parsing errors.

---

### Task 1: Expose Code Detection and Update Service Interface

**Files:**
- Modify: `clients/desktop/src/TelePick.Desktop/Models/ClipboardItem.cs`
- Modify: `clients/desktop/src/TelePick.Desktop/Services/ITelegramService.cs`

**Interfaces:**
- Produces: `public void DetermineIfCode()`
- Produces: `Task<SendResult> SendMessageAsync(ClipboardItem item, string note, Settings settings, List<Destination>? selectedDestinations = null)`

- [ ] **Step 1: Make DetermineIfCode public**

Modify `ClipboardItem.cs` to change `private void DetermineIfCode()` to `public void DetermineIfCode()`.

- [ ] **Step 2: Update ITelegramService interface**

Modify `ITelegramService.cs` to change `SendMessageAsync`:
```csharp
    Task<SendResult> SendMessageAsync(
        ClipboardItem item,
        string note,
        Settings settings,
        List<Destination>? selectedDestinations = null);
```

- [ ] **Step 3: Commit**

```bash
git add clients/desktop/src/TelePick.Desktop/Models/ClipboardItem.cs clients/desktop/src/TelePick.Desktop/Services/ITelegramService.cs
git commit -m "refactor(desktop): update interface to accept ClipboardItem for formatting"
```

---

### Task 2: Implement Formatting Logic in TelegramService

**Files:**
- Modify: `clients/desktop/src/TelePick.Desktop/Services/TelegramService.cs`

**Interfaces:**
- Consumes: `SendMessageAsync(ClipboardItem item, ...)`
- Produces: Type-aware formatted Telegram messages.

- [ ] **Step 1: Update SendMessageAsync signature**

Change `SendMessageAsync` signature in `TelegramService.cs` to match the interface (`ClipboardItem item` instead of `string text`). Handle empty text checks against `item.PreviewText`.

- [ ] **Step 2: Update BuildMessage for dynamic formatting**

Change `BuildMessage` to accept `ClipboardItem item, string note`:
```csharp
    private static string BuildMessage(ClipboardItem item, string note)
    {
        var parts = new List<string>();
        string escapedText = EscapeHtml(item.PreviewText);
        
        if (item.IsLikelyCode)
        {
            parts.Add($"<pre><code>{escapedText}</code></pre>");
        }
        else
        {
            parts.Add(escapedText);
        }

        if (!string.IsNullOrWhiteSpace(note))
            parts.Add($"\n📝 Note: {EscapeHtml(note.Trim())}");

        return string.Join("\n", parts);
    }
```

- [ ] **Step 3: Call the new BuildMessage**

In `SendMessageAsync`, update the call to `BuildMessage(item, note);`.

- [ ] **Step 4: Commit**

```bash
git add clients/desktop/src/TelePick.Desktop/Services/TelegramService.cs
git commit -m "feat(desktop): format Telegram messages based on ClipboardItem type"
```

---

### Task 3: Update ViewModels to Pass ClipboardItem

**Files:**
- Modify: `clients/desktop/src/TelePick.Desktop/ViewModels/MainWindowViewModel.cs`

**Interfaces:**
- Consumes: `SendMessageAsync(ClipboardItem item, ...)` and `DetermineIfCode()`

- [ ] **Step 1: Update quick send (SendToTelegramAsync)**

In `SendToTelegramAsync`, wrap the manual `ClipboardText` in a temporary `ClipboardItem`:
```csharp
        var item = new ClipboardItem 
        { 
            Type = ClipboardItemType.Text, 
            PreviewText = ClipboardText 
        };
        item.DetermineIfCode(); // Check if manually typed/pasted text is code

        var result = await _telegramService.SendMessageAsync(item, Note, settings);
```

- [ ] **Step 2: Update history send (SendItemAsync)**

In `SendItemAsync` (around line 409 and 432), change `_telegramService.SendMessageAsync(item.PreviewText, Note, settings)` to `_telegramService.SendMessageAsync(item, Note, settings)`.
Note: Look for `result = await _telegramService.SendMessageAsync(item.PreviewText, Note, settings);` and change to `result = await _telegramService.SendMessageAsync(item, Note, settings);`.

- [ ] **Step 3: Update file paths send (SendItemAsync)**

In `SendItemAsync` for `ClipboardItemType.Files` (around line 427), where it sends a list of file paths as text:
```csharp
                var pathsItem = new ClipboardItem 
                { 
                    Type = ClipboardItemType.Text, 
                    PreviewText = string.Join("\n", filePaths) 
                };
                result = await _telegramService.SendMessageAsync(pathsItem, Note, settings);
```

- [ ] **Step 4: Build and Verify**

Run `dotnet build clients/desktop/TelePick.slnx` to ensure everything compiles without errors.

- [ ] **Step 5: Commit**

```bash
git add clients/desktop/src/TelePick.Desktop/ViewModels/MainWindowViewModel.cs
git commit -m "refactor(desktop): pass ClipboardItem to TelegramService for rich formatting"
```
