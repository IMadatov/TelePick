# Clipboard History & Global Hotkey Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a kross-platform RAM-based clipboard history monitor in a separate module (`TelePick.Clipboard`), and a cross-platform global hotkey (`SharpHook`) to directly send clipboard content to Telegram.

**Architecture:** A separate `TelePick.Clipboard` library handles monitoring (using a 500ms `PeriodicTimer`). The main `TelePick.Desktop` app provides the UI (History tab) and integrates `SharpHook` for background OS shortcuts.

**Tech Stack:** Avalonia 12, .NET 8, `SharpHook`

## Global Constraints

- Do not write test code unless the user explicitly asks for tests.
- Code must follow SOLID principles.
- Use standard standard C# MVVM patterns for Avalonia.
- Strict cross-platform design (no hardcoded Windows paths/hooks outside of abstractions).

---

### Task 1: Implement Clipboard History Core Models

**Files:**
- Create: `clients/desktop/src/TelePick.Desktop/Models/ClipboardItem.cs`
- Create: `clients/desktop/src/TelePick.Desktop/Models/ClipboardItemType.cs`

**Interfaces:**
- Produces: `ClipboardItem` (Id, Type, PreviewText, Timestamp, RawData).

- [ ] **Step 1: Write `ClipboardItemType.cs`**
```csharp
namespace TelePick.Desktop.Models;

public enum ClipboardItemType
{
    Text,
    Image,
    Files
}
```

- [ ] **Step 2: Write `ClipboardItem.cs`**
```csharp
using System;

namespace TelePick.Desktop.Models;

public class ClipboardItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ClipboardItemType Type { get; set; }
    public string PreviewText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public object? RawData { get; set; }
}
```

- [ ] **Step 3: Commit**
```bash
git add .
git commit -m "feat: add clipboard item models"
```

---

### Task 2: Implement ClipboardMonitorService

**Files:**
- Create: `clients/desktop/src/TelePick.Desktop/Services/IClipboardMonitorService.cs`
- Create: `clients/desktop/src/TelePick.Desktop/Services/ClipboardMonitorService.cs`

**Interfaces:**
- Consumes: Avalonia 12 `IClipboard` API.
- Produces: `IClipboardMonitorService` which exposes an `ObservableCollection<ClipboardItem>` and starts/stops polling.

- [ ] **Step 1: Implement `IClipboardMonitorService`**
```csharp
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TelePick.Desktop.Models;

namespace TelePick.Desktop.Services;

public interface IClipboardMonitorService
{
    ObservableCollection<ClipboardItem> History { get; }
    void StartMonitoring(Avalonia.Input.Platform.IClipboard clipboard);
    void StopMonitoring();
}
```

- [ ] **Step 2: Implement `ClipboardMonitorService`**
Write a service that uses `PeriodicTimer` (500ms). It queries `clipboard.TryGetTextAsync()` or formats, builds a `ClipboardItem`, checks if it is distinct from the latest in `History`, and adds it at index 0. Enforces max 50 items.

- [ ] **Step 3: Commit**
```bash
git add .
git commit -m "feat: implement clipboard monitor service"
```

---

### Task 3: Integrate Clipboard History into UI

**Files:**
- Modify: `clients/desktop/src/TelePick.Desktop/App.axaml.cs`
- Modify: `clients/desktop/src/TelePick.Desktop/ViewModels/MainWindowViewModel.cs`
- Modify: `clients/desktop/src/TelePick.Desktop/Views/MainWindow.axaml`

**Interfaces:**
- Consumes: Models and Services in `TelePick.Desktop`

- [ ] **Step 1: Register in DI & ViewModel**
Register `IClipboardMonitorService` in DI. Inject it into `MainWindowViewModel` and bind the `History` collection. Expose a command to set an item as the active clipboard text. Start monitoring using the main window's clipboard.

- [ ] **Step 2: Update `MainWindow.axaml`**
Add a "History" tab to the `TabControl`. Display an `ItemsControl` or `ListBox` bound to the history, with a `DataTemplate` for `ClipboardItem`.

- [ ] **Step 3: Commit**
```bash
git add .
git commit -m "feat: add clipboard history UI"
```

---

### Task 4: Global Hotkey via SharpHook

**Files:**
- Modify: `clients/desktop/src/TelePick.Desktop/TelePick.Desktop.csproj`
- Create: `clients/desktop/src/TelePick.Desktop/Services/IGlobalHotkeyService.cs`
- Create: `clients/desktop/src/TelePick.Desktop/Services/SharpHookGlobalHotkeyService.cs`

**Interfaces:**
- Produces: Background hotkey triggering `SendToTelegramAsync` directly.

- [ ] **Step 1: Add SharpHook package**
```bash
cd clients/desktop/src/TelePick.Desktop
dotnet add package SharpHook -v 5.3.0
```

- [ ] **Step 2: Implement Hotkey Service**
Create `IGlobalHotkeyService` and implement it using `TaskPoolGlobalHook` to detect e.g. `Ctrl+Shift+T` and invoke an `Action`.

- [ ] **Step 3: Update App Initialization**
Register `IGlobalHotkeyService`. On startup, hook it up to execute the exact same flow as the `SendToTelegramCommand` (reading from standard clipboard or History).

- [ ] **Step 4: Commit**
```bash
git add .
git commit -m "feat: implement global OS hotkey"
```
