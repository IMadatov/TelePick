# TelePick

**Pick selected text and send it as a note to your Telegram chat.**

TelePick is a Chrome extension (Manifest V3). Select text on any webpage, click the floating button, add an optional note, and send the selection to your Telegram chat via your own bot.

## Features

- Floating action button appears when you select text (similar to translation extensions)
- Optional note/description before sending
- Message includes selected text, note, and source page link
- Configure your own bot token and chat ID — credentials stay in your browser

## Installation

1. Clone or download this repository.
2. Open Chrome and go to `chrome://extensions`.
3. Enable **Developer mode** (top right).
4. Click **Load unpacked** and select the project folder.
5. Open extension **Settings** and enter your Bot Token and Chat ID.

## Setup: Telegram bot

### 1. Create a bot

1. Open [@BotFather](https://t.me/BotFather) in Telegram.
2. Send `/newbot` and follow the prompts.
3. Copy the **bot token** (format: `123456789:ABCdef...`).

### 2. Get your Chat ID

**Personal chat:**

1. Send any message to your new bot.
2. Visit in a browser (replace `YOUR_TOKEN`):

   ```
   https://api.telegram.org/botYOUR_TOKEN/getUpdates
   ```

3. Find `"chat":{"id":123456789}` — that number is your Chat ID.

**Alternative:** use [@GetChatID_IL_BOT](https://t.me/GetChatID_IL_BOT) for your user ID (message the bot first if using a private chat with your bot).

**Groups/channels:** add the bot to the group, send a message, then use `getUpdates`. Group IDs are usually negative (e.g. `-1001234567890`).

### 3. Configure TelePick

1. Click the TelePick icon → **Open Settings**, or right-click the extension → **Options**.
2. Paste **Bot Token** and **Chat ID**.
3. Click **Save**, then **Send test message** to verify.

## Usage

1. On any webpage, **select** the text you want to save.
2. Click the blue **floating button** next to the selection.
3. Optionally add a **note**.
4. Click **Send**.

You should receive a message in Telegram like:

```
"Your selected text"

📝 Note: your optional note
🔗 Page title (link to source)
```

## Project structure

```
manifest.json
src/
  background.js   # Telegram API calls
  content.js      # Selection UI (floating button + panel)
  content.css
  options.html    # Settings page
  options.js
  popup.html      # Toolbar popup
  popup.js
icons/
  icon16.png, icon48.png, icon128.png
```

## Privacy

- Bot token and chat ID are stored in `chrome.storage.sync` (synced across Chrome profiles if sync is enabled).
- Selected text is sent only to Telegram’s API using your bot; no third-party servers are used by this extension.

## License

MIT
