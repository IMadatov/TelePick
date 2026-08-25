# TelePick Desktop - Quick Paste UI Design Specification

## Overview
This document outlines the architectural and UI design for implementing the "Quick Paste Popup" in the TelePick Desktop Avalonia application. The design is based on a provided HTML/Tailwind mockup (`design/quick_paste_popup.html`) and aims to reproduce the modern, glassmorphic UI natively in Avalonia XAML.

## Approach
We will use **Native Avalonia XAML** to build the UI components. This ensures maximum performance and seamless integration with the existing MVVM architecture.

## Global Styles and Resources (`App.axaml`)
To match the Tailwind CSS design, we will define global resources:
- **Colors:**
  - `Background`: `#111317`
  - `SurfaceContainerHigh`: `#282a2d`
  - `Primary`: `#2AABEE` / `#89ceff`
  - `OnSurface`: `#e2e2e6`
  - `OnSurfaceVariant`: `#bec8d2`
- **Fonts:**
  - Integrate `Inter` for standard text and `JetBrains Mono` for monospaced snippets.
- **Icons:**
  - Utilize Material Symbols (using a font file or existing Avalonia Material Icon packages).

## Window Configuration (`MainWindow.axaml` or `QuickPasteWindow.axaml`)
- `SystemDecorations="None"`: Removes standard OS chrome for a custom borderless look.
- `TransparencyLevelHint="Mica"` or `"AcrylicBlur"`: Enables native OS glassmorphism backing.
- `Background="Transparent"`: Allows the glass effect to show through.
- Dimensions: Approximately `480px` width and `600px` height.
- `CornerRadius="12"` on the main root border.

## Component Breakdown

### 1. Header & Search Area
- **Search Bar:** A custom `TextBox`.
  - `ControlTemplate`: Customized to have a transparent background by default, and a visible border + darker background on focus (mimicking the `focus-within` tailwind state).
  - Prefix icon: Material Search icon.
  - Suffix icon: A small visual hint for the `Cmd+K` shortcut.
- **Filters:** A horizontal list of `RadioButton`s (styled to look like chips) for "All Items", "Text", "Images", "Files". A `ScrollViewer` ensures horizontal scrolling if they overflow.

### 2. Clipboard History List
- **Main Control:** `ListBox` bound to an `ObservableCollection<ClipboardItem>`.
- **ItemTemplate (DataTemplate):**
  - A `Grid` layout representing a single row.
  - **Left (Icon):** Fixed width/height box with a background and an icon denoting the item type (Text, Image, Link, Color, Code).
  - **Middle (Content):** Title, timestamp, and a truncated preview of the content.
  - **Right (Quick Actions):** A small floating action bar (Pin, Share, Delete).
- **Hover Interactions:**
  - The Quick Actions panel will have `Opacity="0"` by default.
  - Using Avalonia's `:pointerover` pseudo-class (or interaction behaviors), the panel's opacity will transition to `1` when hovering over the `ListBoxItem`.
  - The `ListBoxItem` background will subtly highlight on hover and display a border when selected.

### 3. Status Bar (Footer)
- Positioned at the bottom using a `Grid` or `DockPanel` (docked to bottom).
- Contains application version/sync status on the left and a Settings gear icon on the right.
- Separated from the list by a subtle top border and slightly darker background.

## ViewModels and Logic
- The existing MVVM setup will be expanded:
  - `MainViewModel` will hold the `ObservableCollection<ClipboardItem>`.
  - Commands for filtering, searching, and quick actions (Pin, Delete, Share) will be added to the ViewModel.
  - The `ClipboardService` (from `TelePick.Platform`) will be responsible for fetching and populating real clipboard data into the list (future implementation step).

## Spec Self-Review Checklist
- [x] Placeholders resolved? Yes, concrete colors and controls are mapped.
- [x] Internal consistency? Yes, matches the Avalonia UI capabilities and HTML structure.
- [x] Scope check? Focused entirely on styling the main clipboard window.
- [x] Ambiguity check? Clarified that native Avalonia XAML will be used instead of a webview.

## Next Steps
Upon user approval of this spec, transition to the `writing-plans` skill to generate a step-by-step implementation plan.
