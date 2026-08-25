# Quick Paste UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the glassmorphic Quick Paste popup UI based on the Tailwind HTML design for the TelePick Avalonia desktop client.

**Architecture:** We will define global color and font resources in `App.axaml`. We will then completely overhaul `Views/ClipboardPopupWindow.axaml` (or create it if it's currently a stub) to use a borderless window, custom search TextBox, filter toggle buttons, and a customized ListBox for clipboard items.

**Tech Stack:** .NET 8, Avalonia UI, C#, XAML.

## Global Constraints

- Must use native Avalonia XAML styling.
- Do not add new heavy UI framework dependencies (e.g. SukiUI or Material.Avalonia) unless absolutely necessary.
- Do not write tests unless explicitly requested by the user.

---

### Task 1: Global Resources and Colors

**Files:**
- Modify: `clients/desktop/src/TelePick.Desktop/App.axaml`

**Interfaces:**
- Consumes: Nothing
- Produces: Global color brushes (`Background`, `Primary`, `SurfaceContainerHigh`, etc.) and font settings.

- [ ] **Step 1: Add Tailwind color palette to App.axaml resources**
Define the SolidColorBrush resources inside `<Application.Resources>` matching the design spec (e.g. `Background="#111317"`, `Primary="#2AABEE"`).

- [ ] **Step 2: Add Font configurations to App.axaml**
Set the default font family for the application and add a definition for a monospaced font (JetBrains Mono).

### Task 2: Window Configuration and Layout Structure

**Files:**
- Modify: `clients/desktop/src/TelePick.Desktop/Views/ClipboardPopupWindow.axaml`

**Interfaces:**
- Consumes: Global resources from Task 1.
- Produces: A borderless, glassmorphic window structure with Header, List, and Status Bar areas.

- [ ] **Step 1: Configure Window properties**
Set `SystemDecorations="None"`, `TransparencyLevelHint="Mica, AcrylicBlur"`, `Background="Transparent"`, `Width="480"`, and `Height="600"`.

- [ ] **Step 2: Define main Grid layout**
Create a master `Border` with `CornerRadius="12"`, `Background="{DynamicResource SurfaceContainerHigh}"` (with some transparency), and inside it a `Grid` with 3 rows: Header (Auto), List (Star), and Footer (Auto).

### Task 3: Header (Search and Filters)

**Files:**
- Modify: `clients/desktop/src/TelePick.Desktop/Views/ClipboardPopupWindow.axaml`

**Interfaces:**
- Consumes: Window Layout from Task 2.
- Produces: The custom search TextBox and the horizontal filter ToggleButtons.

- [ ] **Step 1: Implement custom Search TextBox**
Add a `TextBox` in the Header row. Add a customized `Styles` section to override its default border/background to match the transparent glass look and blue focus border.

- [ ] **Step 2: Implement Filter Chips**
Below the search box, add a horizontal `ScrollViewer` or `StackPanel`. Add `RadioButton`s styled as chips (e.g. "All Items", "Text", "Images") with rounded borders.

### Task 4: Clipboard Item List and Hover Actions

**Files:**
- Modify: `clients/desktop/src/TelePick.Desktop/Views/ClipboardPopupWindow.axaml`
- Modify: `clients/desktop/src/TelePick.Desktop/ViewModels/MainWindowViewModel.cs` (or `ClipboardPopupViewModel.cs` if it exists)

**Interfaces:**
- Consumes: Layout structure from Task 2.
- Produces: The styled `ListBox` that renders clipboard items.

- [ ] **Step 1: Ensure ViewModel has sample/real properties**
Verify the ViewModel has a collection of `ClipboardItem` objects with properties for Type, Title, PreviewText, and Timestamp.

- [ ] **Step 2: Build the ListBox ItemTemplate**
Create a `DataTemplate` for the `ListBox`. It should contain a `Grid` with the Type icon on the left, the text/preview in the middle.

- [ ] **Step 3: Implement Quick Actions Panel**
Inside the ItemTemplate, add a right-aligned `StackPanel` containing the Pin, Share, and Delete buttons. Set its initial opacity to 0.

- [ ] **Step 4: Add Hover Interaction**
Use Avalonia Styles within the `ListBox` to set the Quick Actions panel opacity to 1 when the `ListBoxItem` has the `:pointerover` pseudo-class.
