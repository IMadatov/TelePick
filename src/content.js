(function () {
  const HOST_ID = "telepick-root";
  const PREVIEW_MAX = 120;

  let host = null;
  let shadow = null;
  let fab = null;
  let panel = null;
  let currentSelection = null;
  let hideFabTimer = null;

  function getHost() {
    if (host && document.contains(host)) return host;

    host = document.createElement("div");
    host.id = HOST_ID;
    host.style.cssText = "position:fixed;inset:0;width:0;height:0;overflow:visible;pointer-events:none;z-index:2147483645;";
    document.documentElement.appendChild(host);
    shadow = host.attachShadow({ mode: "closed" });

    const link = document.createElement("link");
    link.rel = "stylesheet";
    link.href = chrome.runtime.getURL("src/content.css");
    shadow.appendChild(link);

    return host;
  }

  function getSelectedText() {
    const sel = window.getSelection();
    if (!sel || sel.isCollapsed || sel.rangeCount === 0) return null;
    const text = sel.toString().trim();
    if (!text) return null;
    return { text, range: sel.getRangeAt(0) };
  }

  function positionFab(range) {
    const rect = range.getBoundingClientRect();
    const top = Math.max(8, rect.top - 44);
    const left = Math.min(
      window.innerWidth - 48,
      Math.max(8, rect.right - 20)
    );
    fab.style.top = `${top}px`;
    fab.style.left = `${left}px`;
    fab.style.pointerEvents = "auto";
  }

  function hideFab() {
    if (fab) {
      fab.remove();
      fab = null;
    }
    currentSelection = null;
  }

  function hidePanel() {
    if (panel) {
      panel.remove();
      panel = null;
    }
    hideFab();
  }

  function showFab(selection) {
    getHost();
    hidePanel();

    currentSelection = selection;

    fab = document.createElement("button");
    fab.type = "button";
    fab.className = "telepick-fab";
    fab.title = "Send to Telegram";
    fab.setAttribute("aria-label", "Send to Telegram");
    fab.innerHTML = `<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z"/></svg>`;
    fab.style.pointerEvents = "auto";

    fab.addEventListener("mousedown", (e) => e.preventDefault());
    fab.addEventListener("click", (e) => {
      e.stopPropagation();
      openPanel();
    });

    shadow.appendChild(fab);
    positionFab(selection.range);
  }

  function truncatePreview(text) {
    if (text.length <= PREVIEW_MAX) return text;
    return text.slice(0, PREVIEW_MAX) + "…";
  }

  function openPanel() {
    if (!currentSelection) return;

    hideFab();

    const rect = currentSelection.range.getBoundingClientRect();
    const top = Math.min(
      window.innerHeight - 280,
      Math.max(8, rect.bottom + 8)
    );
    const left = Math.min(
      window.innerWidth - 336,
      Math.max(8, rect.left)
    );

    panel = document.createElement("div");
    panel.className = "telepick-panel";
    panel.style.top = `${top}px`;
    panel.style.left = `${left}px`;
    panel.style.pointerEvents = "auto";

    panel.innerHTML = `
      <div class="telepick-panel-header">TelePick</div>
      <p class="telepick-preview"></p>
      <label class="telepick-label" for="telepick-note">Note (optional)</label>
      <textarea id="telepick-note" class="telepick-textarea" placeholder="Add a note or tag…" rows="3"></textarea>
      <div class="telepick-actions">
        <button type="button" class="telepick-btn telepick-btn-cancel">Cancel</button>
        <button type="button" class="telepick-btn telepick-btn-send">Send</button>
      </div>
    `;

    panel.querySelector(".telepick-preview").textContent = truncatePreview(
      currentSelection.text
    );

    const noteInput = panel.querySelector("#telepick-note");
    const sendBtn = panel.querySelector(".telepick-btn-send");
    const cancelBtn = panel.querySelector(".telepick-btn-cancel");

    cancelBtn.addEventListener("click", () => hidePanel());

    sendBtn.addEventListener("click", async () => {
      sendBtn.disabled = true;
      sendBtn.textContent = "Sending…";

      const payload = {
        text: currentSelection.text,
        description: noteInput.value,
        url: window.location.href,
        title: document.title,
      };

      try {
        const result = await chrome.runtime.sendMessage({
          type: "SEND_NOTE",
          payload,
        });

        hidePanel();

        if (result?.ok) {
          showToast("✅ Sent to Telegram", "success");
        } else {
          const msg = result?.error || "Failed to send";
          showToast(`❌ ${msg}`, "error", 5000);
          if (msg.includes("settings") || msg.includes("required")) {
            chrome.runtime.openOptionsPage?.();
          }
        }
      } catch (err) {
        hidePanel();
        showToast(`❌ ${err.message || "Extension error"}`, "error", 5000);
      }
    });

    shadow.appendChild(panel);
    noteInput.focus();
  }

  function showToast(message, type, duration = 3000) {
    getHost();
    const toast = document.createElement("div");
    toast.className = `telepick-toast telepick-toast-${type}`;
    toast.textContent = message;
    toast.style.pointerEvents = "none";
    shadow.appendChild(toast);
    setTimeout(() => toast.remove(), duration);
  }

  function onSelectionEnd() {
    clearTimeout(hideFabTimer);
    hideFabTimer = setTimeout(() => {
      const sel = getSelectedText();
      if (!sel) {
        if (!panel) hideFab();
        return;
      }
      if (panel) return;
      showFab(sel);
    }, 150);
  }

  document.addEventListener("mouseup", onSelectionEnd);
  document.addEventListener("keyup", (e) => {
    if (e.key === "Escape") hidePanel();
  });

  document.addEventListener("mousedown", (e) => {
    if (!host) return;
    const path = e.composedPath();
    if (path.includes(host)) return;
    if (panel) hidePanel();
    else hideFab();
  });

  document.addEventListener("scroll", () => {
    if (fab && currentSelection) {
      positionFab(currentSelection.range);
    }
  }, true);
})();
