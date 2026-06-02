const STORAGE_KEYS = ["botToken", "chatId"];
const SCREENSHOT_CONTEXT_MENU_ID = "telepick-screenshot";

function ensureContextMenu() {
  chrome.contextMenus.removeAll(() => {
    chrome.contextMenus.create({
      id: SCREENSHOT_CONTEXT_MENU_ID,
      title: "TelePick: Screenshot",
      contexts: ["all"],
    });
  });
}

chrome.runtime.onInstalled.addListener(() => {
  ensureContextMenu();
});

chrome.runtime.onStartup.addListener(() => {
  ensureContextMenu();
});

chrome.contextMenus.onClicked.addListener((info, tab) => {
  if (info.menuItemId !== SCREENSHOT_CONTEXT_MENU_ID || !tab?.id) return;
  chrome.tabs.sendMessage(tab.id, { type: "START_SCREENSHOT_SELECTION" });
});

function escapeHtml(text) {
  return text
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function buildMessage(text, description, url, title) {
  const parts = [`"${escapeHtml(text)}"`];

  if (description && description.trim()) {
    parts.push("", `📝 Note: ${escapeHtml(description.trim())}`);
  }

  const sourceLabel = title ? escapeHtml(title) : "Source";
  parts.push("", `🔗 <a href="${escapeHtml(url)}">${sourceLabel}</a>`);

  return parts.join("\n");
}

function buildPhotoCaption(description, url, title) {
  const parts = [];

  if (description && description.trim()) {
    parts.push(`📝 Note: ${escapeHtml(description.trim())}`);
  }

  const sourceLabel = title ? escapeHtml(title) : "Source";
  parts.push(`🔗 <a href="${escapeHtml(url)}">${sourceLabel}</a>`);

  return parts.join("\n");
}

async function getConfig() {
  const data = await chrome.storage.sync.get(STORAGE_KEYS);
  return {
    botToken: (data.botToken || "").trim(),
    chatId: (data.chatId || "").trim(),
  };
}

function dataUrlToBlob(dataUrl) {
  const [meta, base64] = dataUrl.split(",");
  if (!meta || !base64) {
    throw new Error("Invalid image payload");
  }

  const mimeMatch = meta.match(/data:(.*?);base64/);
  const mimeType = mimeMatch ? mimeMatch[1] : "image/png";
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);

  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i);
  }

  return new Blob([bytes], { type: mimeType });
}

async function sendToTelegram({ text, description, url, title }) {
  const { botToken, chatId } = await getConfig();

  if (!botToken || !chatId) {
    return {
      ok: false,
      error: "Bot token and Chat ID are required. Open extension settings to configure.",
    };
  }

  const message = buildMessage(text, description, url, title);
  const apiUrl = `https://api.telegram.org/bot${botToken}/sendMessage`;

  try {
    const response = await fetch(apiUrl, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        chat_id: chatId,
        text: message,
        parse_mode: "HTML",
        disable_web_page_preview: false,
      }),
    });

    const data = await response.json();

    if (!response.ok || !data.ok) {
      const errMsg =
        data.description || `HTTP ${response.status}: failed to send message`;
      return { ok: false, error: errMsg };
    }

    return { ok: true };
  } catch (err) {
    return { ok: false, error: err.message || "Network error" };
  }
}

async function sendPhotoToTelegram({ imageDataUrl, description, url, title }) {
  const { botToken, chatId } = await getConfig();

  if (!botToken || !chatId) {
    return {
      ok: false,
      error: "Bot token and Chat ID are required. Open extension settings to configure.",
    };
  }

  if (!imageDataUrl) {
    return { ok: false, error: "Screenshot image is missing." };
  }

  const caption = buildPhotoCaption(description, url, title);
  const apiUrl = `https://api.telegram.org/bot${botToken}/sendPhoto`;

  try {
    const photoBlob = dataUrlToBlob(imageDataUrl);
    const formData = new FormData();
    formData.append("chat_id", chatId);
    formData.append("caption", caption);
    formData.append("parse_mode", "HTML");
    formData.append("photo", photoBlob, "telepick-screenshot.png");

    const response = await fetch(apiUrl, {
      method: "POST",
      body: formData,
    });
    const data = await response.json();

    if (!response.ok || !data.ok) {
      const errMsg =
        data.description || `HTTP ${response.status}: failed to send screenshot`;
      return { ok: false, error: errMsg };
    }

    return { ok: true };
  } catch (err) {
    return { ok: false, error: err.message || "Screenshot send failed" };
  }
}

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  if (message.type === "SEND_NOTE") {
    sendToTelegram(message.payload)
      .then(sendResponse)
      .catch((err) => sendResponse({ ok: false, error: err.message }));
    return true;
  }

  if (message.type === "GET_CONFIG_STATUS") {
    getConfig()
      .then(({ botToken, chatId }) =>
        sendResponse({ configured: Boolean(botToken && chatId) })
      )
      .catch(() => sendResponse({ configured: false }));
    return true;
  }

  if (message.type === "CAPTURE_SCREENSHOT") {
    chrome.tabs.captureVisibleTab(
      undefined,
      { format: "png" },
      (dataUrl) => {
        if (chrome.runtime.lastError) {
          sendResponse({
            ok: false,
            error: chrome.runtime.lastError.message || "Could not capture tab.",
          });
          return;
        }
        sendResponse({ ok: true, dataUrl });
      }
    );
    return true;
  }

  if (message.type === "SEND_SCREENSHOT") {
    sendPhotoToTelegram(message.payload)
      .then(sendResponse)
      .catch((err) => sendResponse({ ok: false, error: err.message }));
    return true;
  }

  if (message.type === "TEST_CONNECTION") {
    const testText = "TelePick test message — your bot is configured correctly.";
    sendToTelegram({
      text: testText,
      description: "",
      url: "https://github.com/IMadatov/TelePick",
      title: "TelePick",
    })
      .then(sendResponse)
      .catch((err) => sendResponse({ ok: false, error: err.message }));
    return true;
  }
});
