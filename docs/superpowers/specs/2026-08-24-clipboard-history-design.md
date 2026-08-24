# Clipboard History (Buffer System) Design

## Objective
Implement an in-memory, cross-platform clipboard history manager (similar to Windows Win+V) inside the TelePick application. It must capture and hold Text, Images, and File paths copied to the system clipboard while the application is running.

## Chosen Approach
**Polling (Timer-based):** A background service will poll the clipboard every 500ms to detect changes, ensuring 100% cross-platform compatibility without native OS hooks.

## Architecture

1. **Models**
   - `ClipboardItem`: Represents a single historical entry.
     - `Id`: Unique identifier (GUID).
     - `Type`: Enum (`Text`, `Image`, `Files`).
     - `PreviewText`: Truncated text, file names, or "[Image]" for UI rendering.
     - `Timestamp`: When it was captured.
     - `RawData`: The actual `string`, `IImage` (Avalonia bitmap), or `IEnumerable<string>` (paths).

2. **ClipboardMonitorService**
   - Runs a continuous loop using `PeriodicTimer` (e.g., 500ms intervals).
   - Reads `clipboard.GetFormatsAsync()`.
   - To detect changes without heavy memory allocation:
     - For Text: Compares the new string with the last saved text.
     - For Files: Compares the list of file paths.
     - For Images: Checks if an image is present and compares a lightweight hash of the image, or simply relies on a change in other formats/time.
   - Pushes new distinct items to an in-memory `ObservableCollection<ClipboardItem>`.
   - Enforces a maximum limit (e.g., keeping only the last 50 items) to prevent RAM bloat, especially with images.

3. **User Interface (MainWindow)**
   - A new **"History"** tab or side-panel will be added.
   - Uses an Avalonia `ListBox` bound to the history collection.
   - **DataTemplates**:
     - *Text*: Shows a short snippet of the text.
     - *Files*: Shows the number of files and their names.
     - *Image*: Shows a small scaled thumbnail preview.
   - **Interaction**: Clicking an item in the history list will make it the "active" item, placing it into the main Capture tab (and optionally pushing it back to the OS clipboard) so it can be sent to Telegram.

## Data Lifecycle & Scope
- History is kept **strictly in RAM**. 
- When the application is closed, the history is wiped. 
- No data is saved to the disk (except the application settings themselves).

## Error Handling
- The background loop will swallow and log exceptions (e.g., if the OS clipboard is locked by another app) and gracefully retry on the next tick.
- Very large images will be scaled down for the preview to prevent excessive RAM usage over time.
