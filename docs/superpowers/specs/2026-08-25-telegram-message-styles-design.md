# Telegram Message Styling Design

## Overview
Currently, all text-based clipboard items are sent to Telegram wrapped in double quotes (`"text"`). The goal is to format messages differently based on their `ClipboardItemType` to make them look professional in Telegram.

## Proposed Styles

### 1. Code (`ClipboardItemType.Text` with `IsLikelyCode = true`)
- **Format:** Wrapped in generic `<pre><code>...</code></pre>` tags.
- **Reason:** As agreed with the user, attempting to precisely detect the language requires heavy libraries (like `highlight.js` + JS engine). A generic monospace block provides the core benefit (easy one-click copying and clean structure) without bloating the application.

### 2. Standard Text (`ClipboardItemType.Text` with `IsLikelyCode = false`)
- **Format:** Sent exactly as it is, without the currently hardcoded double quotes (`"..."`).
- **Reason:** Adding quotes around every text item is unnecessary and can be annoying for standard clipboard sharing.

### 3. Links (`ClipboardItemType.Link`)
- **Format:** Sent exactly as it is (raw text).
- **Reason:** Telegram automatically parses URLs and creates hyperlinks with rich previews. Wrapping it in `<a>` tags provides no extra benefit since we don't have alternative anchor text.

### 4. Images (`ClipboardItemType.Image`)
- **Format:** Sent using `SendPhotoAsync` with the note as the caption (existing behavior).

## Implementation Details
1. Update `ITelegramService.SendMessageAsync` to accept a `ClipboardItem` instead of just `string text`.
2. Move formatting logic into `TelegramService.BuildMessage` to read the item's type and apply the correct HTML tags.
3. Update `MainWindowViewModel.cs` to pass the `ClipboardItem` object to `SendMessageAsync` where applicable.

## Scope
This is a small, focused change isolated entirely to `TelegramService` and its direct callers in `MainWindowViewModel`.
