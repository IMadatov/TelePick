# Privacy Policy for TelePick

**Effective date:** June 2, 2026

TelePick ("extension", "we", "our") is a Chrome extension that allows users to send selected webpage text and selected-area screenshots to their own Telegram chat using their own Telegram bot credentials.

## 1. Data We Process

TelePick may process the following data only to provide its core functionality:

- User-provided Telegram Bot Token
- User-provided Telegram Chat ID
- User-selected webpage text
- User-selected screenshot area (image)
- Optional user note entered before sending
- Source page title and URL

## 2. How Data Is Used

Data is used only to:

- save user configuration locally in Chrome storage
- send user-selected content to Telegram via the official Telegram Bot API (`https://api.telegram.org/*`) when the user explicitly clicks Send/Test

TelePick does not use collected data for advertising, profiling, analytics, or any unrelated purpose.

## 3. Data Storage

- Bot Token and Chat ID are stored in `chrome.storage.sync` (or Chrome-managed extension storage).
- TelePick does not operate its own backend server.
- TelePick does not permanently store selected text, screenshots, or notes outside the user's browser and Telegram delivery flow.

## 4. Data Sharing

TelePick does not sell, rent, or transfer user data to third parties.

Data is transmitted only to Telegram's API as requested by the user to deliver messages/photos to the user's configured Telegram chat.

## 5. Permissions Justification

TelePick requests only the permissions needed for its single purpose:

- `storage`: store bot token/chat ID settings
- `activeTab`: interact with the currently active page after user action
- `tabs`: capture visible tab image for screenshot feature
- `contextMenus`: provide right-click "TelePick: Screenshot" menu action
- host permission `https://api.telegram.org/*`: send content through Telegram Bot API

## 6. User Control

Users control what is sent:

- content is sent only after explicit user action
- users can edit/remove settings anytime in extension options
- users can uninstall the extension at any time to stop all processing

## 7. Security

We aim to minimize data access and process only what is required for the extension's core functionality. However, users are responsible for securing their Telegram Bot Token and Telegram account access.

## 8. Children's Privacy

TelePick is not directed to children under 13, and we do not knowingly collect data from children.

## 9. Changes to This Policy

This policy may be updated to reflect feature or compliance changes. The latest version will be made available in the extension listing or project repository.

## 10. Contact

For privacy questions, contact: **[your email here]**
