# Clipboard History Bubble-Up Design

## Problem
Currently, when a user selects an item from the clipboard history popup, the item is copied to the system clipboard. The `ClipboardMonitorService` detects this change and checks if the new clipboard content matches the *very first* item in the history. Since the selected item is often not the first item, the service assumes it's a completely new entry and adds it to the top of the list. This results in duplicate items in the history.

## Proposed Solution (Bubble-Up)
We will implement a "bubble-up" mechanism similar to Windows' native Win+V clipboard history.

Instead of only checking the first element, `ClipboardMonitorService` will search the entire `History` collection.
1. When a new text or image is detected in the clipboard, we check if an item with the exact same content (same `PreviewText` for text, or same `DataHash` for images/files) already exists anywhere in the list.
2. If it exists, we remove the existing item from its current position.
3. We then insert the item at the top (index 0) of the list.
4. If it doesn't exist, it is simply inserted at the top as a new item.

## Changes Required
- **`ClipboardMonitorService.cs`**:
  - Update the text monitoring logic to use `History.FirstOrDefault(x => x.PreviewText == text)` instead of checking only index 0.
  - Update the image monitoring logic to use `History.FirstOrDefault(x => x.DataHash == hash)`.
  - Update the `AddItem` method (or add logic before it) to remove the existing item from the `ObservableCollection` before inserting the new/existing one at the top.

## Constraints & Trade-offs
- **Performance**: Searching a list of up to 50 items (our current limit) is an O(N) operation, which takes less than a millisecond. This is completely negligible for performance.
- **UI Updates**: `ObservableCollection` handles the removal and insertion automatically, which will nicely animate or update the Avalonia UI.

## Verification
- Copy item A, copy item B, copy item C.
- Open popup, select item A.
- Re-open popup. The history should now be: A, C, B. No duplicates should exist.
