# TelePick - Additional UI Design Briefs

This document contains design requirements and prompts for two additional UI components for TelePick, ready to be used in Stitch or similar UI generation tools.

---

## 1. Shortcut (Hotkey) Setting Modal
*Based on Option 1.B - A dedicated modal window for capturing keyboard shortcuts.*

**Key Elements:**
- **Container**: A small, focused modal window overlaying the main settings.
- **Title**: "Set Global Shortcut"
- **Instruction Text**: "Press the combination of keys you want to use to open TelePick."
- **Capture Area**: A large, visually distinct area in the center. When active, it pulses or glows slightly.
  - Default state: shows current hotkey (e.g., `Ctrl + Shift + V`)
  - Active/Recording state: "Listening for input..."
- **Controls**: "Save" (primary) and "Cancel" (secondary) buttons.

### Prompt for Stitch:
> "Design a small, focused modal window in dark mode for setting a keyboard shortcut. The title should be 'Set Global Shortcut' with a subtext 'Press the key combination you want to use'. In the center, create a large, visually distinct input area that looks like it's actively listening for keyboard input (maybe a subtle glowing border). Inside the area, display a placeholder like 'Listening...'. Below it, include 'Save' and 'Cancel' buttons. Use a clean, modern aesthetic with glassmorphism and a Telegram-blue accent color."

---

## 2. Custom Floating Notifications
*Based on Option 2.C - A custom notification toast that floats on the screen even when the app is hidden.*

**Key Elements:**
- **Container**: A small, elegant toast notification that floats on the screen (usually top-right or bottom-right). It should have no window borders—just a sleek, rounded card.
- **Status Icon**: A clear indicator of success (green checkmark), info (blue info icon), or error (red warning).
- **Content**: 
  - Title (e.g., "Sent to Telegram")
  - Subtitle/Description (e.g., "image_screenshot.png was successfully delivered.")
- **Animations (Implied)**: Needs to look like it slides in smoothly and fades out.
- **Actions (Optional)**: A small 'x' button to dismiss it immediately.

### Prompt for Stitch:
> "Design a custom, floating desktop notification (toast) component in dark mode. It should be a standalone rounded card with a soft drop shadow, intended to float on the user's desktop. Include a success variant with a green checkmark icon, a bold title 'Sent to Telegram', and a smaller subtitle '1 file successfully delivered'. It should look sleek, unobtrusive, and highly polished, similar to macOS system notifications but with a custom premium dark theme."
