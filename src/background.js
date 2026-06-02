const STORAGE_KEYS = ["botToken", "chatId"];

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

async function getConfig() {
  const data = await chrome.storage.sync.get(STORAGE_KEYS);
  return {
    botToken: (data.botToken || "").trim(),
    chatId: (data.chatId || "").trim(),
  };
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

  if (message.type === "TEST_CONNECTION") {
    const testText = "TelePick test message — your bot is configured correctly.";
    sendToTelegram({
      text: testText,
      description: "",
      url: "https://github.com",
      title: "TelePick",
    })
      .then(sendResponse)
      .catch((err) => sendResponse({ ok: false, error: err.message }));
    return true;
  }
});
