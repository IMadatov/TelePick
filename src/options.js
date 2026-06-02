const form = document.getElementById("settings-form");
const tokenInput = document.getElementById("bot-token");
const testBtn = document.getElementById("test-btn");
const statusEl = document.getElementById("status");
const addRecipientBtn = document.getElementById("add-recipient-btn");
const recipientsList = document.getElementById("recipients-list");

const MAX_RECIPIENTS = 20;
let recipients = [];

function makeId(prefix) {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function createRecipient() {
  return {
    id: makeId("recipient"),
    label: "",
    chatId: "",
    topics: [],
  };
}

function createTopic() {
  return {
    id: makeId("topic"),
    topicId: "",
    label: "",
  };
}

function showStatus(message, type) {
  statusEl.textContent = message;
  statusEl.className = `status visible ${type}`;
}

function hideStatus() {
  statusEl.className = "status";
  statusEl.textContent = "";
}

function isNumericId(value) {
  return /^-?\d+$/.test(value);
}

function normalizeRecipients(inputRecipients) {
  return (Array.isArray(inputRecipients) ? inputRecipients : [])
    .slice(0, MAX_RECIPIENTS)
    .map((recipient) => ({
      id: recipient.id || makeId("recipient"),
      label: String(recipient.label || "").trim(),
      chatId: String(recipient.chatId || "").trim(),
      topics: (Array.isArray(recipient.topics) ? recipient.topics : [])
        .map((topic) => ({
          id: topic.id || makeId("topic"),
          topicId: String(topic.topicId || "").trim(),
          label: String(topic.label || "").trim(),
        }))
        .filter((topic) => topic.topicId),
    }))
    .filter((recipient) => recipient.chatId || recipient.label || recipient.topics.length);
}

function validateSettings(botToken, sourceRecipients) {
  if (!botToken) {
    return { ok: false, error: "Bot Token is required." };
  }

  if (!sourceRecipients.length) {
    return { ok: false, error: "Add at least one recipient." };
  }

  if (sourceRecipients.length > MAX_RECIPIENTS) {
    return { ok: false, error: `Maximum ${MAX_RECIPIENTS} recipients allowed.` };
  }

  for (const recipient of sourceRecipients) {
    if (!recipient.chatId) {
      return { ok: false, error: "Each recipient must have a Chat ID." };
    }
    if (!isNumericId(recipient.chatId)) {
      return {
        ok: false,
        error: `Invalid Chat ID: ${recipient.chatId}. Use numeric values only.`,
      };
    }
    for (const topic of recipient.topics) {
      if (!isNumericId(topic.topicId)) {
        return {
          ok: false,
          error: `Invalid topic ID: ${topic.topicId}. Use numeric values only.`,
        };
      }
    }
  }

  return { ok: true };
}

function updateRecipientField(recipientId, field, value) {
  recipients = recipients.map((recipient) =>
    recipient.id === recipientId ? { ...recipient, [field]: value } : recipient
  );
}

function updateTopicField(recipientId, topicId, field, value) {
  recipients = recipients.map((recipient) => {
    if (recipient.id !== recipientId) return recipient;
    return {
      ...recipient,
      topics: recipient.topics.map((topic) =>
        topic.id === topicId ? { ...topic, [field]: value } : topic
      ),
    };
  });
}

function removeRecipient(recipientId) {
  recipients = recipients.filter((recipient) => recipient.id !== recipientId);
  renderRecipients();
}

function addTopic(recipientId) {
  recipients = recipients.map((recipient) =>
    recipient.id === recipientId
      ? { ...recipient, topics: [...recipient.topics, createTopic()] }
      : recipient
  );
  renderRecipients();
}

function removeTopic(recipientId, topicId) {
  recipients = recipients.map((recipient) => {
    if (recipient.id !== recipientId) return recipient;
    return {
      ...recipient,
      topics: recipient.topics.filter((topic) => topic.id !== topicId),
    };
  });
  renderRecipients();
}

function createRecipientCard(recipient) {
  const card = document.createElement("div");
  card.className = "recipient-card";

  const topicRows = recipient.topics
    .map(
      (topic) => `
      <div class="topic-row" data-topic-id="${topic.id}">
        <input type="text" class="topic-label-input" value="${topic.label}" placeholder="Topic label (optional)" />
        <input type="text" class="topic-id-input" value="${topic.topicId}" placeholder="Topic ID" />
        <button type="button" class="btn btn-danger btn-sm remove-topic-btn">Remove</button>
      </div>
    `
    )
    .join("");

  card.innerHTML = `
    <div class="recipient-grid">
      <div class="recipient-row">
        <input type="text" class="recipient-label-input" value="${recipient.label}" placeholder="Recipient label (optional)" />
        <input type="text" class="recipient-chat-id-input" value="${recipient.chatId}" placeholder="Chat ID" />
        <button type="button" class="btn btn-danger btn-sm remove-recipient-btn">Remove</button>
      </div>
      <div class="topics-wrap">
        <div class="topics-head">
          <span class="muted">Topics (optional, for forum groups)</span>
          <button type="button" class="btn btn-secondary btn-sm add-topic-btn">+ Add topic</button>
        </div>
        <div class="topics-list">${topicRows || '<div class="muted">No topics added</div>'}</div>
      </div>
    </div>
  `;

  card.querySelector(".recipient-label-input").addEventListener("input", (event) => {
    updateRecipientField(recipient.id, "label", event.target.value);
  });
  card.querySelector(".recipient-chat-id-input").addEventListener("input", (event) => {
    updateRecipientField(recipient.id, "chatId", event.target.value);
  });
  card.querySelector(".remove-recipient-btn").addEventListener("click", () => {
    removeRecipient(recipient.id);
  });
  card.querySelector(".add-topic-btn").addEventListener("click", () => {
    addTopic(recipient.id);
  });

  card.querySelectorAll(".topic-row").forEach((topicRow) => {
    const topicId = topicRow.dataset.topicId;
    topicRow.querySelector(".topic-label-input").addEventListener("input", (event) => {
      updateTopicField(recipient.id, topicId, "label", event.target.value);
    });
    topicRow.querySelector(".topic-id-input").addEventListener("input", (event) => {
      updateTopicField(recipient.id, topicId, "topicId", event.target.value);
    });
    topicRow.querySelector(".remove-topic-btn").addEventListener("click", () => {
      removeTopic(recipient.id, topicId);
    });
  });

  return card;
}

function renderRecipients() {
  recipientsList.innerHTML = "";
  if (!recipients.length) {
    const placeholder = document.createElement("p");
    placeholder.className = "muted";
    placeholder.textContent = "No recipients yet. Add at least one.";
    recipientsList.appendChild(placeholder);
    return;
  }

  recipients.forEach((recipient) => {
    recipientsList.appendChild(createRecipientCard(recipient));
  });
}

async function loadSettings() {
  const data = await chrome.storage.sync.get(["botToken", "chatId", "recipients"]);
  if (data.botToken) tokenInput.value = data.botToken;

  if (Array.isArray(data.recipients) && data.recipients.length) {
    recipients = normalizeRecipients(data.recipients);
  } else if (data.chatId) {
    recipients = normalizeRecipients([
      {
        id: makeId("recipient"),
        label: "Default",
        chatId: String(data.chatId).trim(),
        topics: [],
      },
    ]);
    await chrome.storage.sync.set({ recipients });
  }

  if (!recipients.length) {
    recipients = [createRecipient()];
  }
  renderRecipients();
}

async function saveSettings() {
  hideStatus();
  const botToken = tokenInput.value.trim();
  const normalizedRecipients = normalizeRecipients(recipients);
  const validation = validateSettings(botToken, normalizedRecipients);

  if (!validation.ok) {
    showStatus(validation.error, "error");
    return null;
  }

  await chrome.storage.sync.set({
    botToken,
    recipients: normalizedRecipients,
  });
  recipients = normalizedRecipients;
  renderRecipients();
  showStatus("Settings saved.", "success");
  return { botToken, recipients: normalizedRecipients };
}

function buildTestDestinations(sourceRecipients) {
  const destinations = [];
  sourceRecipients.forEach((recipient) => {
    if (recipient.topics.length) {
      recipient.topics.forEach((topic) => {
        destinations.push({ chatId: recipient.chatId, topicId: topic.topicId });
      });
      return;
    }
    destinations.push({ chatId: recipient.chatId });
  });
  return destinations;
}

function destinationInfoLine(sourceRecipients) {
  const lines = [];
  sourceRecipients.forEach((recipient) => {
    const recipientName = recipient.label || recipient.chatId;
    if (recipient.topics.length) {
      recipient.topics.forEach((topic) => {
        const topicName = topic.label || topic.topicId;
        lines.push(`${recipientName} -> ${topicName}`);
      });
      return;
    }
    lines.push(recipientName);
  });
  return lines.join(", ");
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  await saveSettings();
});

addRecipientBtn.addEventListener("click", () => {
  if (recipients.length >= MAX_RECIPIENTS) {
    showStatus(`Maximum ${MAX_RECIPIENTS} recipients allowed.`, "error");
    return;
  }
  hideStatus();
  recipients = [...recipients, createRecipient()];
  renderRecipients();
});

testBtn.addEventListener("click", async () => {
  hideStatus();
  const saved = await saveSettings();
  if (!saved) return;

  const destinations = buildTestDestinations(saved.recipients);
  if (!destinations.length) {
    showStatus("No destinations selected for test.", "error");
    return;
  }

  const destinationInfo = destinationInfoLine(saved.recipients);

  testBtn.disabled = true;
  showStatus(`Sending test message to: ${destinationInfo}`, "info");
  try {
    const result = await chrome.runtime.sendMessage({
      type: "TEST_CONNECTION",
      destinations,
    });

    if (result?.ok) {
      showStatus(
        `Test message sent to ${result.successCount || destinations.length} destination(s): ${destinationInfo}`,
        "success"
      );
    } else {
      showStatus(
        `${result?.error || "Test failed."} Destinations: ${destinationInfo}`,
        "error"
      );
    }
  } catch (err) {
    showStatus(err.message || "Could not reach extension.", "error");
  } finally {
    testBtn.disabled = false;
  }
});

document.addEventListener("keydown", async (event) => {
  if (!(event.ctrlKey && event.key === "Enter")) return;
  event.preventDefault();
  const active = document.activeElement;
  if (active === testBtn) {
    await testBtn.click();
    return;
  }
  form.requestSubmit();
});

loadSettings();
