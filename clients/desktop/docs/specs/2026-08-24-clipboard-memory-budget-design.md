# Clipboard Memory Budget

## Problem

ClipboardMonitorService stores up to 50 clipboard items in memory with no memory awareness:
- `Bitmap Thumbnail` is `IDisposable` but never disposed on eviction
- Screenshot temp files (`/tmp/TelePick/Screenshots/`) accumulate without cleanup
- No memory budget — a single large screenshot can consume several MB, 50 items can consume hundreds of MB
- Text items with large content (e.g. pasted log files) also accumulate unchecked

## Solution

Introduce a memory budget calculated as a percentage of system RAM. Track each item's estimated memory footprint. Evict oldest items when the budget is exceeded, properly disposing resources.

## Design

### Memory Budget Calculation

- On `StartMonitoring()`, calculate budget:
  ```csharp
  var totalRam = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
  _memoryBudget = totalRam / 100; // 1% of system RAM
  ```
- Examples: 8 GB RAM → ~80 MB budget, 16 GB → ~160 MB, 32 GB → ~320 MB
- Hard cap of 50 items remains as a secondary safety limit

### ClipboardItem Changes

- Implement `IDisposable`
- Add `EstimatedSizeBytes` (long) — calculated once at creation time
- Add `ScreenshotPath` (string?) — for temp file tracking, replaces overloading `RawData`
- `Dispose()`:
  - `Thumbnail?.Dispose()` — free native bitmap memory
  - If `ScreenshotPath` is set and file exists → `File.Delete()`

### Size Estimation

| Type  | Formula                                    |
|-------|--------------------------------------------|
| Text  | `text.Length * 2 + 50` (UTF-16 + overhead) |
| Image | `Thumbnail pixel area * 4 + 50` (ARGB)    |
| Files | `sum of path string lengths * 2 + 50`     |

### ClipboardMonitorService Changes

- New fields: `_memoryBudget` (long), `_currentUsage` (long)
- `AddItem()`:
  1. Calculate new item's `EstimatedSizeBytes`
  2. While `_currentUsage + newSize > _memoryBudget` AND `History.Count > 0` → evict oldest
  3. Insert item at index 0, add to `_currentUsage`
- `EvictOldest()`:
  1. Remove last item from `History`
  2. Subtract its `EstimatedSizeBytes` from `_currentUsage`
  3. Call `item.Dispose()`
- `StopMonitoring()`: Dispose all items in History, clear collection
- `StartMonitoring()`: Clean up stale `/tmp/TelePick/Screenshots/` directory from previous sessions

### Temp File Lifecycle

- Created in `ProcessClipboardAsync()` when a bitmap is captured
- Tracked via `ClipboardItem.ScreenshotPath`
- Deleted when item is evicted or on `StopMonitoring()`
- Stale files from crashed sessions cleaned on next `StartMonitoring()`
