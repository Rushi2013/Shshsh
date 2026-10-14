/* =========================================================================
   Review screen: edit, approve, reject, skip, portal save + confirmation
   ========================================================================= */
(function () {
  "use strict";

  const recordId = window.UWIN_RECORD_ID;
  if (!recordId) return;

  const el = {
    statusBadge: document.getElementById("statusBadge"),
    editBtn: document.getElementById("editBtn"),
    editBtn2: document.getElementById("editBtn2"),
    viewMode: document.getElementById("viewMode"),
    editMode: document.getElementById("editMode"),
    cancelEdit: document.getElementById("cancelEdit"),
    approveBtn: document.getElementById("approveBtn"),
    rejectBtn: document.getElementById("rejectBtn"),
    skipBtn: document.getElementById("skipBtn"),
    reopenBtn: document.getElementById("reopenBtn"),
    reprocessBtn: document.getElementById("reprocessBtn"),
    openPortal: document.getElementById("openPortal"),
    prepareSave: document.getElementById("prepareSave"),
    confirmDone: document.getElementById("confirmDone"),
    markUnknown: document.getElementById("markUnknown"),
    saveSteps: document.getElementById("saveSteps"),
  };

  function setStatus(status) {
    if (!el.statusBadge) return;
    el.statusBadge.textContent = status;
    el.statusBadge.className = "badge s-" + UWIN.statusSlug(status);
  }

  function reload(delay) {
    UWIN.refreshStats().catch(() => {});
    setTimeout(() => window.location.reload(), delay || 900);
  }

  /* ---------------- edit ---------------- */
  function showEdit(show) {
    if (!el.editMode) return;
    el.editMode.hidden = !show;
    if (el.viewMode) el.viewMode.hidden = show;
    if (show) el.editMode.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  [el.editBtn, el.editBtn2].forEach((button) => {
    if (button) button.addEventListener("click", () => showEdit(true));
  });
  if (el.cancelEdit) el.cancelEdit.addEventListener("click", () => showEdit(false));

  if (el.editMode) {
    el.editMode.addEventListener("submit", async (event) => {
      event.preventDefault();
      const values = {};
      UWIN.qsa("input[name]", el.editMode).forEach((input) => { values[input.name] = input.value; });
      const note = (document.getElementById("editNote") || {}).value || "";

      const ok = await UWIN.confirm({
        title: "Save these changes?",
        text: "The record is validated and matched again after saving. Old and new values are kept in history.",
        confirmText: "Save changes",
      });
      if (!ok) return;

      const data = await UWIN.api("/api/records/" + recordId + "/edit", {
        method: "POST",
        body: { values: values, note: note },
      });
      if (data.ok) {
        UWIN.toast("success", data.message, "Saved");
        reload(800);
      }
    });
  }

  /* ---------------- decisions ---------------- */
  UWIN.onClick(el.approveBtn, async () => {
    const data = await UWIN.api("/api/records/" + recordId + "/approve", { method: "POST", body: {} });
    if (data.ok) {
      UWIN.toast("success", "Record approved. Now open the portal and confirm the entry there.", "Approved");
      setStatus("APPROVED");
      reload(900);
    } else if (data.warnings) {
      const first = data.warnings.slice(0, 3).map((w) => "• " + w.message).join("\n");
      UWIN.toast("warn", first || data.message, "Cannot approve yet");
    }
  }, "Approving…");

  UWIN.onClick(el.rejectBtn, async () => {
    const reason = await UWIN.prompt({
      title: "Reject this record",
      text: "Write a short reason (kept in history and in the report):",
      placeholder: "e.g. beneficiary not found in portal",
      confirmText: "Reject record",
      danger: true,
    });
    if (reason === null || reason === "") return;
    const data = await UWIN.api("/api/records/" + recordId + "/reject", { method: "POST", body: { reason: reason } });
    if (data.ok) {
      UWIN.toast("warn", data.message, "Rejected");
      reload(800);
    }
  });

  UWIN.onClick(el.skipBtn, async () => {
    const reason = await UWIN.prompt({
      title: "Skip for now",
      text: "Optional note: why are you skipping this record?",
      placeholder: "e.g. family will come tomorrow",
      confirmText: "Skip",
    });
    if (reason === null) return;
    const data = await UWIN.api("/api/records/" + recordId + "/skip", { method: "POST", body: { reason: reason } });
    if (data.ok) {
      UWIN.toast("info", data.message, "Skipped");
      reload(700);
    }
  });

  UWIN.onClick(el.reopenBtn, async () => {
    const data = await UWIN.api("/api/records/" + recordId + "/reopen", { method: "POST", body: {} });
    if (data.ok) {
      UWIN.toast("success", data.message, "Reopened");
      reload(700);
    }
  });

  UWIN.onClick(el.reprocessBtn, async () => {
    const data = await UWIN.api("/api/records/" + recordId + "/process", { method: "POST", body: {} });
    if (data.ok) {
      UWIN.toast("success", data.message, "Re-checked");
      reload(700);
    }
  }, "Matching…");

  /* ---------------- portal ---------------- */
  UWIN.onClick(el.openPortal, async () => {
    const data = await UWIN.api("/api/portal/open", { method: "POST", body: {} });
    if (data.ok) {
      UWIN.toast("success", data.message, "Portal opened");
    } else if (data.url) {
      UWIN.toast("warn", data.message + " Link: " + data.url, "Open manually");
    }
  }, "Opening…");

  function renderSteps(steps) {
    if (!el.saveSteps || !steps) return;
    el.saveSteps.innerHTML =
      '<div class="steps" style="margin-top:12px">' +
      steps.map((step) =>
        '<div class="step"><span class="step-num" style="background:transparent;color:var(--brand-600);border:1px solid var(--border)"></span>' +
        '<div class="step-body"><strong>' + UWIN.escape(step.title) + "</strong><p>" + UWIN.escape(step.text) + "</p></div></div>"
      ).join("") + "</div>" +
      '<p class="tiny muted">The app has not sent anything to the portal. You are doing the entry yourself, safely.</p>';
  }

  UWIN.onClick(el.prepareSave, async () => {
    const ok = await UWIN.confirm({
      title: "Prepare the portal save",
      text: "This app can only send data through an approved API. Otherwise it will show you exactly what to type in the " +
            "official portal and wait for your confirmation. Continue?",
      confirmText: "Continue",
    });
    if (!ok) return;

    const data = await UWIN.api("/api/records/" + recordId + "/save", { method: "POST", body: { confirmed: true } });

    if (data.save_unknown) {
      UWIN.toast("warn", data.message, "SAVE STATUS UNKNOWN");
      setStatus("SAVE STATUS UNKNOWN");
      reload(1200);
      return;
    }
    if (data.next_steps) {
      renderSteps(data.next_steps);
      if (data.portal_url) {
        const open = await UWIN.confirm({
          title: "Open the U-WIN portal now?",
          text: "Chrome will open the official login page. Log in with your mobile number, password and the SMS OTP.",
          confirmText: "Open portal",
        });
        if (open) {
          await UWIN.api("/api/portal/open", { method: "POST", body: { url: data.portal_url } });
        }
      }
      UWIN.toast("info", "Follow the steps shown below, then paste the portal acknowledgement.", "Assisted workflow");
      return;
    }
    if (data.needs_login) {
      UWIN.toast("warn", data.message + " Open the portal login assistant to continue.", "Login needed");
      return;
    }
    if (data.ok) {
      UWIN.toast("success", data.message, "Saved");
      reload(900);
    }
  }, "Preparing…");

  UWIN.onClick(el.confirmDone, async () => {
    const first = await UWIN.confirm({
      title: "Did the portal show a success message?",
      text: "Only continue if you actually saw the portal accept this vaccination entry. A photo/screenshot of the " +
            "acknowledgement is a good idea.",
      confirmText: "Yes, I saw it",
    });
    if (!first) return;

    const reference = await UWIN.prompt({
      title: "Portal confirmation reference",
      text: "Paste the acknowledgement / reference number the portal showed. This is required — without it the record stays as “save status unknown”.",
      placeholder: "e.g. UWIN-ACK-12345",
      confirmText: "Mark as completed",
    });
    if (!reference) return;

    const data = await UWIN.api("/api/records/" + recordId + "/confirm", {
      method: "POST",
      body: { reference: reference, note: "Confirmed by the worker from the portal screen." },
    });
    if (data.ok) {
      UWIN.toast("success", "Record marked COMPLETED with reference " + reference + ".", "Completed");
      setStatus("COMPLETED");
      reload(900);
    }
  }, "Saving…");

  UWIN.onClick(el.markUnknown, async () => {
    const reason = await UWIN.prompt({
      title: "Mark as not confirmed",
      text: "The record becomes SAVE STATUS UNKNOWN and will never be re-sent automatically. What did you see?",
      placeholder: "e.g. portal timed out / could not verify",
      confirmText: "Mark unknown",
      danger: true,
    });
    if (reason === null) return;
    const data = await UWIN.api("/api/records/" + recordId + "/unknown", { method: "POST", body: { reason: reason } });
    if (data.ok || data.warning) {
      UWIN.toast("warn", data.message, "Marked unknown");
      setStatus("SAVE STATUS UNKNOWN");
      reload(900);
    }
  });

  /* ---------------- keyboard shortcuts (desktop nicety) ---------------- */
  document.addEventListener("keydown", (event) => {
    if (event.target.matches("input, textarea, select")) return;
    if (event.key === "a" && el.approveBtn) el.approveBtn.click();
    if (event.key === "s" && el.skipBtn) el.skipBtn.click();
  });
})();
