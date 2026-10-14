/* =========================================================================
   Settings screen: save settings, PIN, cleanup, wipe, diagnostics
   ========================================================================= */
(function () {
  "use strict";

  const DEFAULTS = {
    theme: "light",
    max_upload_mb: 5,
    date_format: "auto",
    rows_per_page: 20,
    auto_recovery: true,
    cleanup_days: 0,
    delete_session_on_complete: false,
    report_format: "xlsx",
    report_include_warnings: true,
    confirm_before_save: true,
    keep_original_file: true,
  };

  const form = document.getElementById("settingsForm");

  function collect() {
    const values = {};
    UWIN.qsa("input[name], select[name]", form).forEach((field) => {
      if (field.type === "checkbox") values[field.name] = field.checked;
      else if (field.type === "number") values[field.name] = Number(field.value || 0);
      else values[field.name] = field.value;
    });
    return values;
  }

  if (form) {
    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      const button = form.querySelector("button[type=submit]");
      UWIN.setBusy(button, true, "Saving…");
      try {
        const data = await UWIN.api("/api/settings", { method: "POST", body: { settings: collect() } });
        if (data.ok) {
          const state = document.getElementById("saveState");
          if (state) { state.textContent = "Saved " + new Date().toLocaleTimeString(); state.className = "chip"; }
          UWIN.toast("success", data.message, "Settings");
          const theme = collect().theme;
          if (theme) {
            try { localStorage.setItem("uwin-theme", theme); } catch (error) { /* ignore */ }
            if (theme !== "system") document.documentElement.setAttribute("data-theme", theme);
          }
          setTimeout(() => window.location.reload(), 900);
        }
      } finally {
        UWIN.setBusy(button, false);
      }
    });

    const resetBtn = document.getElementById("resetBtn");
    if (resetBtn) {
      UWIN.onClick(resetBtn, async () => {
        const ok = await UWIN.confirm({
          title: "Reset settings to defaults?",
          text: "Your records and reports are not touched — only the settings on this page.",
          confirmText: "Reset",
        });
        if (!ok) return;
        UWIN.qsa("input[name], select[name]", form).forEach((field) => {
          if (!(field.name in DEFAULTS)) return;
          if (field.type === "checkbox") field.checked = DEFAULTS[field.name];
          else field.value = DEFAULTS[field.name];
        });
        UWIN.toast("info", "Defaults filled in. Press Save settings to keep them.", "Reset");
      });
    }
  }

  /* ---------------- PIN ---------------- */
  UWIN.onClick(document.getElementById("pinSave"), async () => {
    const pin = document.getElementById("pinInput").value.trim();
    const current = document.getElementById("pinCurrent").value.trim();
    if (!/^\d{4,8}$/.test(pin)) {
      UWIN.toast("warn", "PIN must be 4 to 8 digits.", "Check the PIN");
      return;
    }
    const data = await UWIN.api("/api/pin/set", { method: "POST", body: { pin: pin, current_pin: current } });
    if (data.ok) {
      document.getElementById("pinInput").value = "";
      document.getElementById("pinCurrent").value = "";
      UWIN.toast("success", data.message, "PIN saved");
      setTimeout(() => window.location.reload(), 900);
    }
  }, "Saving…");

  UWIN.onClick(document.getElementById("pinRemove"), async () => {
    const pin = await UWIN.prompt({
      title: "Remove the PIN lock",
      text: "Enter the current PIN to remove it.",
      confirmText: "Remove PIN",
      danger: true,
      inputmode: "numeric",
    });
    if (!pin) return;
    const data = await UWIN.api("/api/pin/remove", { method: "POST", body: { pin: pin } });
    if (data.ok) {
      UWIN.toast("success", data.message, "PIN removed");
      setTimeout(() => window.location.reload(), 900);
    }
  });

  /* ---------------- cleanup / wipe ---------------- */
  UWIN.onClick(document.getElementById("cleanupBtn"), async () => {
    const data = await UWIN.api("/api/settings/cleanup", { method: "POST" });
    if (data.ok) UWIN.toast("success", data.message, "Cleanup");
  }, "Cleaning…");

  UWIN.onClick(document.getElementById("purgeUploadsBtn"), async () => {
    const ok = await UWIN.confirm({
      title: "Remove leftover upload files?",
      text: "Only files whose session was already deleted are removed.",
      confirmText: "Remove files",
    });
    if (!ok) return;
    const data = await UWIN.api("/api/session/purge_uploads", { method: "POST" });
    if (data.ok) UWIN.toast("success", data.message, "Cleaned");
  });

  UWIN.onClick(document.getElementById("wipeBtn"), async () => {
    const first = await UWIN.confirm({
      title: "Delete ALL local data?",
      text: "Every session, record, the imported register, all reports, uploaded files and the activity log will be erased " +
            "from this phone. This cannot be undone. Download your reports first.",
      confirmText: "I understand, continue",
      danger: true,
    });
    if (!first) return;

    const typed = await UWIN.prompt({
      title: "Type DELETE to confirm",
      text: "This is the last confirmation.",
      placeholder: "DELETE",
      confirmText: "Delete everything",
      danger: true,
    });
    if (typed !== "DELETE") {
      UWIN.toast("info", "Nothing was deleted (the word DELETE was not typed).", "Cancelled");
      return;
    }

    const data = await UWIN.api("/api/data/wipe", { method: "POST", body: { confirm: "DELETE" } });
    if (data.ok) {
      UWIN.toast("success", data.message, "All data deleted");
      setTimeout(() => (window.location.href = "/"), 1200);
    }
  }, "Deleting…");

  /* ---------------- adapter test ---------------- */
  UWIN.onClick(document.getElementById("testAdapter"), async () => {
    const url = document.getElementById("portal_adapter_url").value.trim();
    const token = document.getElementById("adapterToken").value.trim();
    if (!url) {
      UWIN.toast("warn", "Enter the approved integration URL first, or keep using assisted mode.", "No URL");
      return;
    }
    const data = await UWIN.api("/api/portal/test-adapter", { method: "POST", body: { url: url, token: token } });
    if (data.ok) {
      document.getElementById("adapterToken").value = "";
      UWIN.toast("success", data.message, "Approved API connected");
      setTimeout(() => window.location.reload(), 1200);
    }
  }, "Testing…");

  UWIN.onClick(document.getElementById("disableAdapter"), async () => {
    const data = await UWIN.api("/api/portal/disable-adapter", { method: "POST" });
    if (data.ok) {
      UWIN.toast("success", data.message, "Assisted mode");
      setTimeout(() => window.location.reload(), 900);
    }
  });

  /* ---------------- diagnostics ---------------- */
  UWIN.onClick(document.getElementById("runCheck"), async () => {
    const host = document.getElementById("checkResult");
    const data = await UWIN.api("/api/diagnostics", { silent: true });
    if (!data.ok) return;
    const d = data.diagnostics;
    host.innerHTML =
      "<div>Python " + UWIN.escape(d.python) + " · pandas " + UWIN.escape(d.pandas) +
      " · openpyxl " + UWIN.escape(d.openpyxl) + " · xlrd " + UWIN.escape(d.xlrd) + "</div>" +
      "<div>Database: " + UWIN.escape(UWIN.formatBytes(d.database_size)) +
      " · Register rows: " + UWIN.escape(d.register.count) + "</div>";
    UWIN.toast("success", "Diagnostics collected below the button.", "Check complete");
  }, "Checking…");
})();
