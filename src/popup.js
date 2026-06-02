const statusEl = document.getElementById("status");
const settingsBtn = document.getElementById("open-settings");

settingsBtn.addEventListener("click", () => {
  chrome.runtime.openOptionsPage();
});

chrome.runtime.sendMessage({ type: "GET_CONFIG_STATUS" }, (response) => {
  if (chrome.runtime.lastError) {
    statusEl.textContent = "Extension error. Try reloading.";
    statusEl.className = "status warn";
    return;
  }

  if (response?.configured) {
    statusEl.textContent = "✓ Bot configured. Select text on a page to send.";
    statusEl.className = "status ok";
  } else {
    statusEl.textContent = "⚠ Set up your Bot Token and Chat ID in settings.";
    statusEl.className = "status warn";
  }
});
