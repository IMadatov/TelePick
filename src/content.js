(function () {
  const HOST_ID = "telepick-root";
  const PREVIEW_MAX = 120;
  const FAB_DEBOUNCE_MS = 250;
  const PANEL_WIDTH_PX = 280;
  const PANEL_HEIGHT_LIMIT_PX = 240;
  const MIN_CAPTURE_SIZE_PX = 24;

  let host = null;
  let shadow = null;
  let fab = null;
  let panel = null;
  let currentSelection = null;
  let hideFabTimer = null;
  let screenshotOverlay = null;
  let removeCaptureKeyListener = null;
  let keyboardGuardEnabled = false;

  function stopPageShortcuts(event) {
    if (!host) return;
    const path = typeof event.composedPath === "function" ? event.composedPath() : [];
    if (!path.includes(host)) return;
    // Escape ni panelni yopish uchun ishlatamiz.
    if (event.key === "Escape") return;
    event.stopPropagation();
  }

  function syncKeyboardGuard() {
    const shouldEnable = Boolean(panel || screenshotOverlay);
    if (shouldEnable && !keyboardGuardEnabled) {
      window.addEventListener("keydown", stopPageShortcuts, true);
      window.addEventListener("keyup", stopPageShortcuts, true);
      keyboardGuardEnabled = true;
      return;
    }
    if (!shouldEnable && keyboardGuardEnabled) {
      window.removeEventListener("keydown", stopPageShortcuts, true);
      window.removeEventListener("keyup", stopPageShortcuts, true);
      keyboardGuardEnabled = false;
    }
  }

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
    removeScreenshotOverlay();
    if (hideFabTimer) {
      clearTimeout(hideFabTimer);
      hideFabTimer = null;
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

  function normalizeRect(rect) {
    const width = Math.abs(rect.endX - rect.startX);
    const height = Math.abs(rect.endY - rect.startY);
    const left = Math.min(rect.startX, rect.endX);
    const top = Math.min(rect.startY, rect.endY);
    return { left, top, width, height };
  }

  function removeScreenshotOverlay() {
    if (removeCaptureKeyListener) {
      document.removeEventListener("keydown", removeCaptureKeyListener);
      removeCaptureKeyListener = null;
    }
    if (screenshotOverlay) {
      screenshotOverlay.remove();
      screenshotOverlay = null;
    }
    syncKeyboardGuard();
  }

  async function cropImageDataUrl(dataUrl, rect) {
    const image = new Image();
    image.src = dataUrl;
    await image.decode();

    const scaleX = image.naturalWidth / window.innerWidth;
    const scaleY = image.naturalHeight / window.innerHeight;

    const sx = Math.max(0, Math.floor(rect.left * scaleX));
    const sy = Math.max(0, Math.floor(rect.top * scaleY));
    const sw = Math.max(1, Math.floor(rect.width * scaleX));
    const sh = Math.max(1, Math.floor(rect.height * scaleY));

    const canvas = document.createElement("canvas");
    canvas.width = sw;
    canvas.height = sh;
    const ctx = canvas.getContext("2d");
    ctx.drawImage(image, sx, sy, sw, sh, 0, 0, sw, sh);
    return canvas.toDataURL("image/png");
  }

  function openScreenshotSendPanel(imageDataUrl, sourceUrl, sourceTitle) {
    hidePanel();
    getHost();

    const top = Math.max(8, Math.min(window.innerHeight - 320, 56));
    const left = Math.max(8, Math.min(window.innerWidth - (PANEL_WIDTH_PX + 16), 56));

    panel = document.createElement("div");
    panel.className = "telepick-panel";
    panel.style.top = `${top}px`;
    panel.style.left = `${left}px`;
    panel.style.pointerEvents = "auto";

    panel.innerHTML = `
      <div class="telepick-panel-header">TelePick Screenshot</div>
      <img class="telepick-shot-preview" alt="Screenshot preview" />
      <label class="telepick-label" for="telepick-shot-note">Note (optional)</label>
      <textarea id="telepick-shot-note" class="telepick-textarea" placeholder="Add a note or tag…" rows="2"></textarea>
      <div class="telepick-actions">
        <button type="button" class="telepick-btn telepick-btn-cancel">Cancel</button>
        <button type="button" class="telepick-btn telepick-btn-send">Send</button>
      </div>
    `;

    panel.querySelector(".telepick-shot-preview").src = imageDataUrl;
    const noteInput = panel.querySelector("#telepick-shot-note");
    const sendBtn = panel.querySelector(".telepick-btn-send");
    const cancelBtn = panel.querySelector(".telepick-btn-cancel");

    cancelBtn.addEventListener("click", () => hidePanel());

    sendBtn.addEventListener("click", async () => {
      sendBtn.disabled = true;
      sendBtn.textContent = "Sending…";

      try {
        const result = await chrome.runtime.sendMessage({
          type: "SEND_SCREENSHOT",
          payload: {
            imageDataUrl,
            description: noteInput.value,
            url: sourceUrl,
            title: sourceTitle,
          },
        });

        hidePanel();
        if (result?.ok) {
          showToast("✅ Screenshot sent to Telegram", "success");
        } else {
          const msg = result?.error || "Failed to send screenshot";
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
    syncKeyboardGuard();
    noteInput.focus();
  }

  function startScreenshotSelection() {
    getHost();
    removeScreenshotOverlay();
    hidePanel();

    screenshotOverlay = document.createElement("div");
    screenshotOverlay.className = "telepick-capture-overlay";
    screenshotOverlay.innerHTML = `
      <div class="telepick-capture-hint">Drag to select screenshot area (Esc to cancel)</div>
      <div class="telepick-capture-box"></div>
    `;

    const box = screenshotOverlay.querySelector(".telepick-capture-box");
    const dragRect = { startX: 0, startY: 0, endX: 0, endY: 0 };
    let dragging = false;

    function updateBox() {
      const rect = normalizeRect(dragRect);
      box.style.left = `${rect.left}px`;
      box.style.top = `${rect.top}px`;
      box.style.width = `${rect.width}px`;
      box.style.height = `${rect.height}px`;
    }

    async function finishSelection() {
      const rect = normalizeRect(dragRect);
      removeScreenshotOverlay();

      if (
        rect.width < MIN_CAPTURE_SIZE_PX ||
        rect.height < MIN_CAPTURE_SIZE_PX
      ) {
        showToast("❌ Selected area is too small", "error", 3500);
        return;
      }

      try {
        const captureResult = await chrome.runtime.sendMessage({
          type: "CAPTURE_SCREENSHOT",
        });
        if (!captureResult?.ok || !captureResult?.dataUrl) {
          throw new Error(captureResult?.error || "Could not capture screenshot");
        }

        const croppedDataUrl = await cropImageDataUrl(captureResult.dataUrl, rect);
        openScreenshotSendPanel(croppedDataUrl, window.location.href, document.title);
      } catch (err) {
        showToast(`❌ ${err.message || "Screenshot capture failed"}`, "error", 5000);
      }
    }

    screenshotOverlay.addEventListener("mousedown", (event) => {
      event.preventDefault();
      dragging = true;
      dragRect.startX = event.clientX;
      dragRect.startY = event.clientY;
      dragRect.endX = event.clientX;
      dragRect.endY = event.clientY;
      box.style.display = "block";
      updateBox();
    });

    screenshotOverlay.addEventListener("mousemove", (event) => {
      if (!dragging) return;
      dragRect.endX = event.clientX;
      dragRect.endY = event.clientY;
      updateBox();
    });

    screenshotOverlay.addEventListener("mouseup", async (event) => {
      if (!dragging) return;
      dragRect.endX = event.clientX;
      dragRect.endY = event.clientY;
      dragging = false;
      await finishSelection();
    });

    removeCaptureKeyListener = (event) => {
      if (event.key !== "Escape") return;
      removeScreenshotOverlay();
      showToast("Screenshot capture cancelled", "success", 1500);
    };
    document.addEventListener("keydown", removeCaptureKeyListener);

    shadow.appendChild(screenshotOverlay);
    syncKeyboardGuard();
  }

  function openPanel() {
    if (!currentSelection) return;

    // hideFab() currentSelection-ni null qiladi, shuning uchun snapshot olamiz.
    const selection = currentSelection;
    hideFab();
    currentSelection = selection;

    const rect = currentSelection.range.getBoundingClientRect();
    const top = Math.min(
      window.innerHeight - PANEL_HEIGHT_LIMIT_PX,
      Math.max(8, rect.bottom + 8)
    );
    const left = Math.min(
      window.innerWidth - (PANEL_WIDTH_PX + 16),
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
      <textarea id="telepick-note" class="telepick-textarea" placeholder="Add a note or tag…" rows="2"></textarea>
      <div class="telepick-actions">
        <button type="button" class="telepick-btn telepick-btn-cancel">Cancel</button>
        <button type="button" class="telepick-btn telepick-btn-secondary telepick-btn-shot">Screenshot</button>
        <button type="button" class="telepick-btn telepick-btn-send">Send</button>
      </div>
    `;

    panel.querySelector(".telepick-preview").textContent = truncatePreview(
      selection.text
    );

    const noteInput = panel.querySelector("#telepick-note");
    const sendBtn = panel.querySelector(".telepick-btn-send");
    const cancelBtn = panel.querySelector(".telepick-btn-cancel");
    const screenshotBtn = panel.querySelector(".telepick-btn-shot");

    cancelBtn.addEventListener("click", () => hidePanel());
    screenshotBtn.addEventListener("click", () => startScreenshotSelection());

    sendBtn.addEventListener("click", async () => {
      sendBtn.disabled = true;
      sendBtn.textContent = "Sending…";

      const payload = {
        text: selection.text,
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
    syncKeyboardGuard();
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
    }, FAB_DEBOUNCE_MS);
  }

  document.addEventListener("mouseup", onSelectionEnd);
  document.addEventListener("keyup", (e) => {
    if (e.key === "Escape") {
      removeScreenshotOverlay();
      hidePanel();
    }
  });

  document.addEventListener("mousedown", (e) => {
    if (!host) return;
    const path = e.composedPath();
    if (path.includes(host)) return;
    // Selection "debounce" timer ishlayotgan bo'lishi mumkin, uni o'chiramiz.
    if (hideFabTimer) {
      clearTimeout(hideFabTimer);
      hideFabTimer = null;
    }
    if (panel) hidePanel();
    else hideFab();
  });

  document.addEventListener("scroll", () => {
    if (fab && currentSelection) {
      positionFab(currentSelection.range);
    }
  }, true);

  chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
    if (message?.type !== "START_SCREENSHOT_SELECTION") return;
    startScreenshotSelection();
    sendResponse({ ok: true });
  });
})();
