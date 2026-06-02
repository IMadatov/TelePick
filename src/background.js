const STORAGE_KEYS = ["botToken", "chatId", "recipients"];
const SCREENSHOT_CONTEXT_MENU_ID = "telepick-screenshot";
const MAX_RECIPIENTS = 20;

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

function buildMessage(text, description, url, title, includeSource = true) {
  const parts = [`"${escapeHtml(text)}"`];

  if (description && description.trim()) {
    parts.push("", `📝 Note: ${escapeHtml(description.trim())}`);
  }

  if (includeSource) {
    const sourceLabel = title ? escapeHtml(title) : "Source";
    parts.push("", `🔗 <a href="${escapeHtml(url)}">${sourceLabel}</a>`);
  }

  return parts.join("\n");
}

function buildPhotoCaption(description, url, title, includeSource = true) {
  const parts = [];

  if (description && description.trim()) {
    parts.push(`📝 Note: ${escapeHtml(description.trim())}`);
  }

  if (includeSource) {
    const sourceLabel = title ? escapeHtml(title) : "Source";
    parts.push(`🔗 <a href="${escapeHtml(url)}">${sourceLabel}</a>`);
  }

  return parts.join("\n");
}

async function getConfig() {
  const data = await chrome.storage.sync.get(STORAGE_KEYS);
  let recipients = Array.isArray(data.recipients) ? data.recipients : [];

  if (!recipients.length && data.chatId) {
    recipients = [
      {
        id: "legacy-1",
        label: "Default",
        chatId: String(data.chatId).trim(),
        topics: [],
      },
    ];
    await chrome.storage.sync.set({ recipients });
  }

  recipients = recipients
    .slice(0, MAX_RECIPIENTS)
    .map((recipient, index) => {
      const topics = Array.isArray(recipient.topics)
        ? recipient.topics
            .map((topic, topicIndex) => ({
              id: topic.id || `topic-${index + 1}-${topicIndex + 1}`,
              topicId: String(topic.topicId || "").trim(),
              label: String(topic.label || "").trim(),
            }))
            .filter((topic) => topic.topicId)
        : [];

      return {
        id: recipient.id || `recipient-${index + 1}`,
        label: String(recipient.label || "").trim(),
        chatId: String(recipient.chatId || "").trim(),
        topics,
      };
    })
    .filter((recipient) => recipient.chatId);

  return {
    botToken: (data.botToken || "").trim(),
    recipients,
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

function uniqueDestinations(destinations) {
  const seen = new Set();
  return destinations.filter((destination) => {
    const key = `${destination.chatId}|${destination.topicId || ""}`;
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}

function resolveDestinations(selectedDestinations, recipients) {
  const destinationPool = [];
  for (const recipient of recipients) {
    destinationPool.push({ chatId: recipient.chatId });
    for (const topic of recipient.topics || []) {
      destinationPool.push({ chatId: recipient.chatId, topicId: topic.topicId });
    }
  }

  if (Array.isArray(selectedDestinations) && selectedDestinations.length) {
    return uniqueDestinations(
      selectedDestinations
        .map((destination) => ({
          chatId: String(destination.chatId || "").trim(),
          topicId: destination.topicId ? String(destination.topicId).trim() : "",
        }))
        .filter((destination) => destination.chatId)
    );
  }

  return uniqueDestinations(destinationPool.filter((destination) => destination.chatId));
}

function buildAggregateResult(results) {
  const successCount = results.filter((result) => result.ok).length;
  const failResults = results.filter((result) => !result.ok);
  const failureCount = failResults.length;

  if (!results.length) {
    return {
      ok: false,
      successCount: 0,
      failureCount: 0,
      totalCount: 0,
      error: "No destinations selected.",
      errors: ["No destinations selected."],
    };
  }

  if (!failureCount) {
    return {
      ok: true,
      successCount,
      failureCount: 0,
      totalCount: results.length,
    };
  }

  const errors = failResults.map((result) => result.error).filter(Boolean);
  return {
    ok: false,
    successCount,
    failureCount,
    totalCount: results.length,
    error: errors[0] || "One or more destinations failed.",
    errors,
  };
}

function resolveDestinationLabel(destination, recipients) {
  const recipient = recipients.find(
    (item) => String(item.chatId) === String(destination.chatId)
  );
  if (!recipient) {
    return destination.topicId
      ? `${destination.chatId} / topic ${destination.topicId}`
      : String(destination.chatId);
  }

  const recipientLabel = recipient.label || recipient.chatId;
  if (!destination.topicId) return recipientLabel;

  const topic = (recipient.topics || []).find(
    (item) => String(item.topicId) === String(destination.topicId)
  );
  const topicLabel = topic?.label || destination.topicId;
  return `${recipientLabel} -> ${topicLabel}`;
}

async function sendMessageSingle({ botToken, destination, message }) {
  const apiUrl = `https://api.telegram.org/bot${botToken}/sendMessage`;
  const body = {
    chat_id: destination.chatId,
    text: message,
    parse_mode: "HTML",
    disable_web_page_preview: false,
  };
  if (destination.topicId) {
    body.message_thread_id = Number(destination.topicId);
  }

  try {
    const response = await fetch(apiUrl, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
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

async function sendToTelegram({ text, description, url, title, destinations, includeSource = true }) {
  const { botToken, recipients } = await getConfig();

  if (!botToken || !recipients.length) {
    return {
      ok: false,
      error: "Bot token and at least one recipient are required. Open settings to configure.",
    };
  }

  const message = buildMessage(text, description, url, title, includeSource);
  const resolvedDestinations = resolveDestinations(destinations, recipients);
  if (!resolvedDestinations.length) {
    return { ok: false, error: "No destinations selected." };
  }

  const results = await Promise.all(
    resolvedDestinations.map(async (destination) => {
      const sendResult = await sendMessageSingle({ botToken, destination, message });
      return sendResult.ok
        ? { ok: true }
        : {
            ok: false,
            error: `[${destination.chatId}${destination.topicId ? ` / topic ${destination.topicId}` : ""}] ${sendResult.error}`,
          };
    })
  );
  return buildAggregateResult(results);
}

async function sendPhotoSingle({ botToken, destination, caption, imageDataUrl }) {
  const apiUrl = `https://api.telegram.org/bot${botToken}/sendPhoto`;

  try {
    const photoBlob = dataUrlToBlob(imageDataUrl);
    const formData = new FormData();
    formData.append("chat_id", destination.chatId);
    formData.append("caption", caption);
    formData.append("parse_mode", "HTML");
    if (destination.topicId) {
      formData.append("message_thread_id", String(destination.topicId));
    }
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

async function sendPhotoToTelegram({ imageDataUrl, description, url, title, destinations, includeSource = true }) {
  const { botToken, recipients } = await getConfig();

  if (!botToken || !recipients.length) {
    return {
      ok: false,
      error: "Bot token and at least one recipient are required. Open settings to configure.",
    };
  }

  if (!imageDataUrl) {
    return { ok: false, error: "Screenshot image is missing." };
  }

  const caption = buildPhotoCaption(description, url, title, includeSource);
  const resolvedDestinations = resolveDestinations(destinations, recipients);
  if (!resolvedDestinations.length) {
    return { ok: false, error: "No destinations selected." };
  }

  const results = await Promise.all(
    resolvedDestinations.map(async (destination) => {
      const sendResult = await sendPhotoSingle({
        botToken,
        destination,
        caption,
        imageDataUrl,
      });
      return sendResult.ok
        ? { ok: true }
        : {
            ok: false,
            error: `[${destination.chatId}${destination.topicId ? ` / topic ${destination.topicId}` : ""}] ${sendResult.error}`,
          };
    })
  );

  return buildAggregateResult(results);
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
      .then(({ botToken, recipients }) =>
        sendResponse({ configured: Boolean(botToken && recipients.length) })
      )
      .catch(() => sendResponse({ configured: false }));
    return true;
  }

  if (message.type === "GET_RECIPIENTS") {
    getConfig()
      .then(({ recipients }) => sendResponse({ ok: true, recipients }))
      .catch((err) => sendResponse({ ok: false, error: err.message }));
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
    getConfig()
      .then(async ({ botToken, recipients }) => {
        if (!botToken || !recipients.length) {
          sendResponse({
            ok: false,
            error:
              "Bot token and at least one recipient are required. Open settings to configure.",
          });
          return;
        }

        const destinations = resolveDestinations(message.destinations || [], recipients);
        if (!destinations.length) {
          sendResponse({ ok: false, error: "No destinations selected." });
          return;
        }

        const results = await Promise.all(
          destinations.map(async (destination) => {
            const targetLabel = resolveDestinationLabel(destination, recipients);
            const testText = `TelePick test message — target: ${targetLabel}`;
            const formattedMessage = buildMessage(
              testText,
              "",
              "https://github.com/IMadatov/TelePick",
              "TelePick"
            );
            const sendResult = await sendMessageSingle({
              botToken,
              destination,
              message: formattedMessage,
            });
            return sendResult.ok
              ? { ok: true }
              : {
                  ok: false,
                  error: `[${targetLabel}] ${sendResult.error}`,
                };
          })
        );

        sendResponse(buildAggregateResult(results));
      })
      .catch((err) => sendResponse({ ok: false, error: err.message }));
    return true;
  }
});
