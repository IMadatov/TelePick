# TelePick — Project Review

**Date:** 2026-09-08  
**Scope:** Full monorepo (`clients/extension`, `clients/desktop`, design docs)  
**Verdict:** A clear, privacy-first personal capture tool with a mature Chrome extension and a rapidly growing Avalonia desktop client. The product idea is solid; the main risks are documentation drift, missing automated tests on desktop, and architecture that grew past the original layered design.

---

## What it is

**TelePick** lets you capture selected text (and screenshots) and send them to your own Telegram chat via your own bot — no TelePick backend, no third-party relay.

Core loop:

1. Capture content (browser selection or OS clipboard)
2. Optionally add a note
3. Send HTML-formatted content to Telegram Bot API destinations (chat + optional forum topic)

Two clients share that purpose:

| Client | Stack | Role |
|--------|--------|------|
| **Browser extension** | Chrome MV3 (vanilla JS) | Select text / screenshot on any page → floating UI → Telegram |
| **Desktop app** | .NET (`net10.0`) + Avalonia 12 | Clipboard history, global hotkeys, tray, quick paste → Telegram |

License: MIT. Privacy model: credentials and content stay with the user and Telegram only.

---

## Repository layout

```
TelePick/
  README.md
  clients/
    extension/     # Load-unpacked MV3 extension (self-contained)
    desktop/       # Single Avalonia project + design HTML mocks
  docs/
    superpowers/   # Design specs + implementation plans
```

Primary author activity is concentrated on the desktop client (clipboard history, hotkeys, message styles, launch-on-startup). The extension already covers multi-recipient routing, site-scoped destination prefs, and screenshot capture.

---

## Extension (`clients/extension`)

### Strengths

- **Focused MV3 surface:** `storage`, `activeTab`, `contextMenus`, host permission only for `api.telegram.org`.
- **Real product features:** floating FAB on selection, optional note, source link, multi-recipient + topics, per-site destination/source/note preferences, context-menu screenshot selection.
- **Shadow DOM isolation** for the content UI reduces page CSS conflicts.
- **Privacy policy** documents data flow and permission justification clearly.
- **Zero build step** — easy to load unpacked and ship.

### Structure

- `background.js` — Telegram API (`sendMessage` / photo), config migration, context menu
- `content.js` — selection FAB, send panel, screenshot overlay
- `options.js` / `popup.js` — settings and toolbar entry

### Risks / gaps

- No automated tests or packaging script (lint/bundle/zip for Web Store).
- Large single-file content/background scripts (~1.2k LOC combined) will get harder to maintain without modules.
- Bot token in `chrome.storage.sync` is convenient but syncs a secret across Chrome profiles; worth calling out more loudly in UI/docs.

---

## Desktop (`clients/desktop`)

### What exists today

A **single** project `TelePick.Desktop` (solution: `TelePick.slnx`) targeting **`net10.0`** with Avalonia 12, CommunityToolkit.Mvvm, SharpHook, and DI.

Notable capabilities already implemented:

- Clipboard monitoring (native listeners for Windows / Linux X11 / macOS + fallback)
- In-memory clipboard history with memory budget, pin, search, type filters (text / image / files / links)
- Quick-paste popup at mouse position
- Multiple global hotkeys (quick paste, silent send, search, clear, pause)
- Telegram send for text and photos, multi-destination, legacy chat-id migration
- Message formatting by clipboard item type
- System tray (close-to-tray)
- Launch on startup (Linux `.desktop`, Windows registry; macOS stub)
- Settings UI integrated in the main window

Design work is backed by HTML mocks under `clients/desktop/design/` and written specs/plans under `docs/superpowers/`.

### Strengths

- Clear service interfaces (`ITelegramService`, `IClipboardMonitorService`, `IGlobalHotkeyService`, `IStartupService`, …).
- Platform-specific clipboard and startup code is factored behind factories.
- Specs are detailed and map closely to recent commits (startup, message styles, quick paste).
- Privacy-consistent: local settings file, direct Telegram API.

### Risks / gaps

1. **Documentation is outdated.** Desktop README still describes .NET 8, `TelePick.Core` / `TelePick.Platform`, `dotnet test`, and a minimal MVP. Actual code is a monolithic `net10.0` app with tray, history, and hotkeys — no `tests/` folder, no Core/Platform projects.
2. **No automated tests** despite README claiming `dotnet test` and design plans that assume TDD-style coverage for services.
3. **God ViewModel:** `MainWindowViewModel` owns settings, history filters, send flow, and hotkey-related state (~470 LOC). Will resist change as features grow.
4. **Secrets on disk:** bot token in `~/.config/TelePick/settings.json` plaintext. Roadmap already mentions OS credential store — still open.
5. **Settings vs monitoring drift:** UI exposes `HistoryLimit`; monitor hard-codes `MaxItems = 50`. Easy user confusion.
6. **macOS startup** is a no-op (`DummyStartupService`); clipboard path exists but maturity is unclear vs Linux/Windows.
7. **Fire-and-forget async** in `App` / clipboard monitor (`Task.Run`, `_ = ProcessClipboardAsync()`) — failures can be silent without centralized error reporting.

---

## Cross-cutting comparison

| Concern | Extension | Desktop |
|---------|-----------|---------|
| Capture | DOM selection + area screenshot | OS clipboard (text/image/files) |
| Destinations | Multi-recipient + topics + per-site prefs | Multi-recipient models in service; UI maturity unclear vs extension |
| Settings | `chrome.storage.sync` + local site prefs | `~/.config/TelePick/settings.json` |
| Telegram | `background.js` | `TelegramService` |
| Offline UX | Page-bound | Tray + global hotkeys + history |

Shared product DNA (HTML notes, user-owned bot, no backend) is a strength. Shared logic is **not** extracted — message composition is duplicated in JS and C#. That is acceptable for now; a shared format contract (golden HTML fixtures) would reduce drift.

---

## Architecture assessment

**Intended (docs):** layered Core / Platform / Desktop with tests.  
**Actual:** UI-centric Avalonia app with services living next to views.

That is a normal early-product shape, but the docs and folder story should match reality. Recommended direction:

1. Update README/status tables to `net10.0` / Avalonia 12 and current features.
2. Add a small test project for pure logic first (`TelegramService` formatting/truncation, settings migration, destination resolution).
3. Split ViewModel responsibilities (Settings / ClipboardHistory / Send) before more UI surface area lands.
4. Align `HistoryLimit` setting with `ClipboardMonitorService` eviction.
5. Prefer OS secret storage for the bot token when touching settings again.

---

## Product readiness

| Area | Status |
|------|--------|
| Extension usability | Strong for personal use; Web Store packaging/compliance not evidenced in-repo |
| Desktop Linux MVP | Beyond MVP — history + hotkeys + tray + startup |
| Desktop Windows | Startup + clipboard paths present; needs explicit QA matrix |
| Desktop macOS | Partial (dummy startup) |
| Shared quality bar | Specs yes; automated regression no |
| Security posture | Good network minimalism; weak secret storage |

---

## Recommendations (priority order)

1. **Refresh root + desktop READMEs** so structure, TFM, and features match the repo.
2. **Add `TelePick.Desktop.Tests`** for Telegram message building, truncation, and recipient migration (highest ROI, no UI).
3. **Wire `HistoryLimit`** (and pause/clear) consistently through monitor + settings.
4. **Document secret-handling** expectations (extension sync + desktop JSON) in both READMEs.
5. **Optional later:** extract message-format golden tests shared conceptually with the extension; Web Store zip script; macOS login-item startup.

---

## Summary

TelePick is a **personal “clip → annotate → Telegram”** toolkit with a polished extension and an ambitious desktop twin evolving via clear design specs. The idea and privacy stance are coherent. The highest-value follow-ups are honesty in docs, a minimal test harness for send/settings logic, and tightening settings↔clipboard behavior — not more features until the foundation matches what the code already does.
