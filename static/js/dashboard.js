/* =========================================================================
   Dashboard / history helpers: queue buttons, session opening, register import
   ========================================================================= */
(function () {
  "use strict";

  /* ---------------- open a session ---------------- */
  function bindSessionButtons(scope) {
    UWIN.qsa(".js-activate", scope || document).forEach((button) => {
      UWIN.onClick(button, async () => {
        const data = await UWIN.api("/api/session/" + button.dataset.session + "/activate", { method: "POST" });
        if (data.ok) {
          UWIN.toast("success", "Session opened. Loading records…", "Opened");
          window.location.href = "/review";
        }
      }, "Opening…");
    });
  }

  function bindDeleteButtons(scope) {
    UWIN.qsa(".js-delete-session", scope || document).forEach((button) => {
      UWIN.onClick(button, async () => {
        const ok = await UWIN.confirm({
          title: "Delete this uploaded file and its records?",
          text: "“" + (button.dataset.name || "file") + "” and every record/decision inside it will be removed from this phone. " +
                "Create a report first if you need a copy.",
          confirmText: "Delete permanently",
          danger: true,
        });
        if (!ok) return;
        const data = await UWIN.api("/api/session/" + button.dataset.session + "/delete", { method: "POST" });
        if (data.ok) {
          UWIN.toast("success", data.message, "Deleted");
          setTimeout(() => window.location.reload(), 700);
        }
      });
    });
  }

  /* ---------------- queue buttons ---------------- */
  function queueButton(id, action, body, busyLabel, confirmOptions) {
    const button = document.getElementById(id);
    if (!button) return;
    UWIN.onClick(button, async () => {
      if (confirmOptions) {
        const ok = await UWIN.confirm(confirmOptions);
        if (!ok) return;
      }
      const data = await UWIN.api("/api/queue/" + action, { method: "POST", body: body || {} });
      if (data.ok) {
        UWIN.toast(data.warning ? "warn" : "success", data.message || "Done.", "Queue");
        UWIN.refreshStats().catch(() => {});
        if (data.counts) {
          setTimeout(() => window.location.reload(), 800);
        }
      }
    }, busyLabel);
  }

  /* ---------------- register import (shared by dashboard + settings) ---------------- */
  function bindRegister() {
    const drop = document.getElementById("registerDrop");
    const input = document.getElementById("registerInput");
    const importBtn = document.getElementById("registerImport");
    const clearBtn = document.getElementById("registerClear");
    const nameLabel = document.getElementById("registerName");
    if (!drop || !input) return;

    let chosen = null;

    drop.addEventListener("click", () => input.click());
    drop.addEventListener("keydown", (event) => {
      if (event.key === "Enter" || event.key === " ") { event.preventDefault(); input.click(); }
    });
    input.addEventListener("change", () => {
      chosen = input.files[0] || null;
      if (chosen && nameLabel) {
        nameLabel.textContent = chosen.name + " (" + UWIN.formatBytes(chosen.size) + ")";
      }
    });

    UWIN.onClick(importBtn, async () => {
      if (!chosen) {
        UWIN.toast("warn", "Choose the register file first.", "No file selected");
        return;
      }
      const form = new FormData();
      form.append("file", chosen);
      const data = await UWIN.api("/api/register/import", { method: "POST", body: form });
      if (data.ok) {
        UWIN.toast("success", data.message, "Register imported");
        if (data.warnings && data.warnings.length) {
          UWIN.toast("warn", data.warnings.length + " note(s) about the register file. Matching is still active.", "Check the register");
        }
        setTimeout(() => window.location.reload(), 900);
      }
    }, "Importing…");

    UWIN.onClick(clearBtn, async () => {
      const ok = await UWIN.confirm({
        title: "Clear the authorised register?",
        text: "Matching will stop until you import a register again. Your uploaded records are not touched.",
        confirmText: "Clear register",
        danger: true,
      });
      if (!ok) return;
      const data = await UWIN.api("/api/register/clear", { method: "POST" });
      if (data.ok) {
        UWIN.toast("success", data.message, "Cleared");
        setTimeout(() => window.location.reload(), 700);
      }
    });
  }

  document.addEventListener("DOMContentLoaded", () => {
    bindSessionButtons();
    bindDeleteButtons();
    bindRegister();

    queueButton("btnPause", "pause", {}, "Pausing…");
    queueButton("btnResume", "resume", {}, "Resuming…");
    queueButton("btnStop", "stop", {}, "Stopping…", {
      title: "Stop processing?",
      text: "Records already completed stay completed. Unconfirmed saves stay as they are, and are never retried automatically.",
      confirmText: "Stop",
      danger: true,
    });
    queueButton("btnProcessNext", "next", {}, "Checking…");
    queueButton("btnRetry", "retry_failed", {}, "Retrying…", {
      title: "Retry failed validation?",
      text: "Only validation is repeated. No portal save is ever retried automatically.",
      confirmText: "Retry",
    });
  });
})();
