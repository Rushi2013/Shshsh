/* =========================================================================
   U-WIN Smart Assistant - shared front-end helpers
   Plain vanilla JavaScript. No frameworks, no CDN, no tracking.
   ========================================================================= */
(function () {
  "use strict";

  const UWIN = {};

  /* ---------------- basic helpers ---------------- */
  UWIN.qs = (sel, root) => (root || document).querySelector(sel);
  UWIN.qsa = (sel, root) => Array.from((root || document).querySelectorAll(sel));

  UWIN.escape = function (value) {
    return String(value === null || value === undefined ? "" : value)
      .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;").replace(/'/g, "&#39;");
  };

  UWIN.formatBytes = function (bytes) {
    const value = Number(bytes || 0);
    if (!value) return "0 B";
    const units = ["B", "KB", "MB", "GB"];
    const index = Math.min(Math.floor(Math.log(value) / Math.log(1024)), units.length - 1);
    return (value / Math.pow(1024, index)).toFixed(index === 0 ? 0 : 1) + " " + units[index];
  };

  UWIN.statusSlug = function (status) {
    return String(status || "none").toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
  };

  UWIN.badge = function (status) {
    return '<span class="badge s-' + UWIN.statusSlug(status) + '">' + UWIN.escape(status) + "</span>";
  };

  UWIN.icon = function (name, cls) {
    return '<svg class="icon ' + (cls || "") + '" aria-hidden="true"><use href="#i-' + name + '"></use></svg>';
  };

  /* ---------------- CSRF + API ---------------- */
  UWIN.csrf = function () {
    const meta = document.querySelector('meta[name="csrf-token"]');
    return meta ? meta.getAttribute("content") : "";
  };

  UWIN.api = async function (url, options) {
    const opts = Object.assign({ method: "GET" }, options || {});
    opts.headers = Object.assign(
      { "X-CSRF-Token": UWIN.csrf(), "X-Requested-With": "fetch" },
      opts.headers || {}
    );
    if (opts.body && !(opts.body instanceof FormData) && typeof opts.body !== "string") {
      opts.headers["Content-Type"] = "application/json";
      opts.body = JSON.stringify(opts.body);
    }

    let response;
    try {
      response = await fetch(url, opts);
    } catch (error) {
      UWIN.toast("error", "Network problem. Is the server still running in Termux?", "Connection failed");
      throw error;
    }

    let data = {};
    try {
      data = await response.json();
    } catch (error) {
      data = { ok: false, message: "The server sent an unexpected reply." };
    }

    if (response.status === 401 && data.locked) {
      window.location.href = "/lock";
      return data;
    }
    if (!response.ok || data.ok === false) {
      const message = data.message || "Request failed (" + response.status + ")";
      if (!opts.silent) UWIN.toast("error", message, "Could not complete");
      data.__failed = true;
    }
    return data;
  };

  /* ---------------- toasts ---------------- */
  const TOAST_ICONS = { success: "check-circle", error: "alert", warn: "alert", info: "info" };

  UWIN.toast = function (type, message, title, timeout) {
    const host = document.getElementById("toasts");
    if (!host) return;
    const kind = TOAST_ICONS[type] ? type : "info";
    const node = document.createElement("div");
    node.className = "toast t-" + kind;
    node.innerHTML =
      UWIN.icon(TOAST_ICONS[kind]) +
      '<div class="toast-body">' +
      (title ? "<strong>" + UWIN.escape(title) + "</strong>" : "") +
      "<span>" + UWIN.escape(message) + "</span></div>" +
      '<button class="toast-close" aria-label="Dismiss">' + UWIN.icon("x", "icon-sm") + "</button>";

    const close = () => {
      node.classList.add("out");
      setTimeout(() => node.remove(), 220);
    };
    node.querySelector(".toast-close").addEventListener("click", close);
    host.appendChild(node);
    setTimeout(close, timeout || (kind === "error" ? 9000 : 5000));
  };

  /* ---------------- dialogs ---------------- */
  UWIN.confirm = function (options) {
    const opts = typeof options === "string" ? { text: options } : (options || {});
    const dialog = document.getElementById("confirmDialog");
    if (!dialog || typeof dialog.showModal !== "function") {
      return Promise.resolve(window.confirm(opts.text || "Are you sure?"));
    }

    UWIN.qs("#confirmTitle").textContent = opts.title || "Please confirm";
    UWIN.qs("#confirmText").textContent = opts.text || "";
    const icon = UWIN.qs("#confirmIcon");
    const okBtn = UWIN.qs("#confirmOk");
    const cancelBtn = UWIN.qs("#confirmCancel");
    okBtn.textContent = opts.confirmText || "Confirm";
    okBtn.className = "btn " + (opts.danger ? "btn-danger" : "btn-primary");
    cancelBtn.textContent = opts.cancelText || "Cancel";
    icon.style.color = opts.danger ? "var(--bad)" : "var(--warn)";

    const extra = UWIN.qs("#confirmExtra");
    extra.innerHTML = "";
    let input = null;
    if (opts.input) {
      input = document.createElement("input");
      input.className = "input";
      input.type = opts.input.type || "text";
      input.placeholder = opts.input.placeholder || "";
      if (opts.input.inputmode) input.inputMode = opts.input.inputmode;
      if (opts.input.maxlength) input.maxLength = opts.input.maxlength;
      input.setAttribute("aria-label", opts.input.label || "Value");
      extra.appendChild(input);
    }

    return new Promise((resolve) => {
      const onClose = () => {
        dialog.removeEventListener("close", onClose);
        if (dialog.returnValue === "ok") {
          if (opts.input) resolve(input ? input.value.trim() : "");
          else resolve(true);
        } else {
          resolve(opts.input ? null : false);
        }
      };
      dialog.returnValue = "cancel";
      dialog.addEventListener("close", onClose);
      dialog.showModal();
      if (input) setTimeout(() => input.focus(), 60);
      else setTimeout(() => okBtn.focus(), 60);
    });
  };

  UWIN.prompt = function (options) {
    const opts = options || {};
    return UWIN.confirm({
      title: opts.title || "Enter details",
      text: opts.text || "",
      confirmText: opts.confirmText || "Save",
      danger: opts.danger,
      input: { placeholder: opts.placeholder || "", label: opts.label || "Value", type: opts.type || "text", inputmode: opts.inputmode },
    });
  };

  /* ---------------- busy / loading ---------------- */
  UWIN.busy = function (isBusy, text) {
    const overlay = document.getElementById("busyOverlay");
    if (!overlay) return;
    if (text) UWIN.qs("#busyText").textContent = text;
    overlay.hidden = !isBusy;
  };

  UWIN.setBusy = function (button, isBusy, busyLabel) {
    if (!button) return;
    if (isBusy) {
      button.dataset.originalHtml = button.innerHTML;
      button.classList.add("is-busy");
      button.disabled = true;
      button.innerHTML = '<span class="spinner sm"></span><span>' + UWIN.escape(busyLabel || "Working…") + "</span>";
    } else {
      button.classList.remove("is-busy");
      button.disabled = false;
      if (button.dataset.originalHtml) button.innerHTML = button.dataset.originalHtml;
    }
  };

  /** Wrap an async click handler with automatic button-busy state + errors. */
  UWIN.onClick = function (element, handler, busyLabel) {
    if (!element) return;
    element.addEventListener("click", async (event) => {
      event.preventDefault();
      UWIN.setBusy(element, true, busyLabel);
      try {
        await handler(event);
      } catch (error) {
        console.error(error);
        UWIN.toast("error", "Unexpected problem. Please try again.", "Error");
      } finally {
        UWIN.setBusy(element, false);
      }
    });
  };

  /* ---------------- theme ---------------- */
  function applyTheme(theme) {
    const resolved = theme === "system"
      ? (window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light")
      : theme;
    document.documentElement.setAttribute("data-theme", resolved === "dark" ? "dark" : "light");
    try { localStorage.setItem("uwin-theme", theme); } catch (error) { /* private mode */ }
  }

  UWIN.toggleTheme = function () {
    const current = document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
    const next = current === "dark" ? "light" : "dark";
    applyTheme(next);
    UWIN.api("/api/settings/theme", { method: "POST", body: { theme: next }, silent: true }).catch(() => {});
    UWIN.toast("info", (next === "dark" ? "Dark" : "Light") + " theme enabled.", "Theme");
  };

  /* ---------------- sidebar ---------------- */
  function setupSidebar() {
    const sidebar = document.getElementById("sidebar");
    const scrim = document.getElementById("scrim");
    const button = document.getElementById("menuBtn");
    if (!sidebar || !button) return;

    const open = () => {
      sidebar.classList.add("is-open");
      if (scrim) scrim.hidden = false;
      button.setAttribute("aria-expanded", "true");
    };
    const close = () => {
      sidebar.classList.remove("is-open");
      if (scrim) scrim.hidden = true;
      button.setAttribute("aria-expanded", "false");
    };

    button.addEventListener("click", () => (sidebar.classList.contains("is-open") ? close() : open()));
    if (scrim) scrim.addEventListener("click", close);
    document.addEventListener("keydown", (event) => { if (event.key === "Escape") close(); });
    UWIN.qsa(".sidebar-nav a").forEach((link) => link.addEventListener("click", close));
  }

  /* ---------------- live statistics ---------------- */
  const STAT_KEYS = {
    total: "TOTAL",
    pending: "PENDING",
    matched: "MATCHED",
    "needs-review": "NEEDS REVIEW",
    approved: "APPROVED",
    completed: "COMPLETED",
    failed: "FAILED",
    skipped: "SKIPPED",
    "save-status-unknown": "SAVE STATUS UNKNOWN",
    progress: "PROGRESS",
    open: "OPEN",
    attention: "ATTENTION",
  };

  function updateStats(counts) {
    UWIN.qsa("[data-stat]").forEach((node) => {
      const key = STAT_KEYS[node.dataset.stat];
      if (!key || counts[key] === undefined) return;
      const value = counts[key];
      node.textContent = node.dataset.stat === "progress" ? value + "%" : value;
    });
    UWIN.qsa("[data-progress-bar]").forEach((bar) => {
      bar.style.width = (counts.PROGRESS || 0) + "%";
    });
    UWIN.qsa("[data-count-badge]").forEach((node) => {
      const key = STAT_KEYS[node.dataset.countBadge];
      if (key && counts[key] !== undefined) {
        node.textContent = counts[key];
        node.hidden = !counts[key];
      }
    });
  }

  UWIN.refreshStats = async function (options) {
    const data = await UWIN.api("/api/status", { silent: true });
    if (data && data.counts) updateStats(data.counts);
    if (data && data.queue) {
      const state = data.credentials && data.credentials.state;
      UWIN.qsa("[data-queue-state]").forEach((node) => {
        node.textContent = data.queue.stopped ? "Stopped" : (data.queue.paused ? "Paused" : "Ready");
        node.className = "badge " + (data.queue.stopped || data.queue.paused ? "s-needs-review" : "s-completed");
      });
      if (state) UWIN.qsa("[data-login-state]").forEach((node) => { node.textContent = state; });
    }
    if (options && options.callback) options.callback(data);
    return data;
  };

  function startPolling() {
    if (window.__uwinPolling) return;
    window.__uwinPolling = true;
    setInterval(() => {
      if (document.hidden) return;
      if (document.querySelector("[data-stat], [data-progress-bar]")) {
        UWIN.refreshStats().catch(() => {});
      }
    }, 10000);
  }

  /* ---------------- generic confirm attributes ---------------- */
  function setupAutoConfirm() {
    document.addEventListener("click", async (event) => {
      const target = event.target.closest("[data-confirm]");
      if (!target) return;
      if (target.dataset.confirmed === "1") {
        delete target.dataset.confirmed;
        return;
      }
      event.preventDefault();
      event.stopPropagation();
      const ok = await UWIN.confirm({
        title: target.dataset.confirmTitle || "Please confirm",
        text: target.dataset.confirm,
        confirmText: target.dataset.confirmButton || "Yes, continue",
        danger: target.dataset.confirmDanger === "1",
      });
      if (!ok) return;
      target.dataset.confirmed = "1";
      target.click();
    }, true);
  }

  /* ---------------- clipboard ---------------- */
  UWIN.copy = async function (text, label) {
    try {
      await navigator.clipboard.writeText(text);
      UWIN.toast("success", (label || "Copied") + " copied. Paste it in the U-WIN portal.", "Copied");
    } catch (error) {
      const area = document.createElement("textarea");
      area.value = text;
      document.body.appendChild(area);
      area.select();
      try { document.execCommand("copy"); UWIN.toast("success", "Copied.", "Copied"); }
      catch (inner) { UWIN.toast("warn", "Copy did not work. Please select the text and copy manually.", "Copy"); }
      area.remove();
    }
  };

  /* ---------------- boot ---------------- */
  document.addEventListener("DOMContentLoaded", function () {
    let stored = null;
    try { stored = localStorage.getItem("uwin-theme"); } catch (error) { stored = null; }
    if (stored && stored !== document.documentElement.getAttribute("data-theme")) {
      applyTheme(stored === "system" ? "system" : stored);
    }

    const themeBtn = document.getElementById("themeBtn");
    if (themeBtn) themeBtn.addEventListener("click", UWIN.toggleTheme);

    setupSidebar();
    setupAutoConfirm();
    startPolling();

    UWIN.qsa("[data-flash]").forEach((node) => {
      const kind = node.dataset.flash;
      UWIN.toast(kind === "error" ? "error" : (kind === "success" ? "success" : "info"), node.textContent.trim());
    });

    UWIN.qsa("[data-copy]").forEach((button) => {
      button.addEventListener("click", () => UWIN.copy(button.dataset.copy, button.dataset.copyLabel));
    });
  });

  window.UWIN = UWIN;
})();
