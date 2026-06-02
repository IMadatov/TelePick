const form = document.getElementById("settings-form");
const tokenInput = document.getElementById("bot-token");
const chatInput = document.getElementById("chat-id");
const testBtn = document.getElementById("test-btn");
const statusEl = document.getElementById("status");

function showStatus(message, type) {
  statusEl.textContent = message;
  statusEl.className = `status visible ${type}`;
}

function hideStatus() {
  statusEl.className = "status";
  statusEl.textContent = "";
}

async function loadSettings() {
  const { botToken, chatId } = await chrome.storage.sync.get([
    "botToken",
    "chatId",
  ]);
  if (botToken) tokenInput.value = botToken;
  if (chatId) chatInput.value = chatId;
}

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  hideStatus();

  const botToken = tokenInput.value.trim();
  const chatId = chatInput.value.trim();

  if (!botToken || !chatId) {
    showStatus("Please fill in both Bot Token and Chat ID.", "error");
    return;
  }

  await chrome.storage.sync.set({ botToken, chatId });
  showStatus("Settings saved.", "success");
});

testBtn.addEventListener("click", async () => {
  hideStatus();

  const botToken = tokenInput.value.trim();
  const chatId = chatInput.value.trim();

  if (!botToken || !chatId) {
    showStatus("Save your Bot Token and Chat ID before testing.", "error");
    return;
  }

  await chrome.storage.sync.set({ botToken, chatId });

  testBtn.disabled = true;
  showStatus("Sending test message…", "info");

  try {
    const result = await chrome.runtime.sendMessage({
      type: "TEST_CONNECTION",
    });

    if (result?.ok) {
      showStatus("Test message sent. Check your Telegram chat.", "success");
    } else {
      showStatus(result?.error || "Test failed.", "error");
    }
  } catch (err) {
    showStatus(err.message || "Could not reach extension.", "error");
  } finally {
    testBtn.disabled = false;
  }
});

loadSettings();
