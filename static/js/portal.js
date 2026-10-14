/* =========================================================================
   U-WIN portal login assistant
   Semi-automated and honest: the app opens the official page, the worker
   types the password and the SMS OTP. Nothing secret is stored anywhere.
   ========================================================================= */
(function () {
  "use strict";

  const el = {
    openPortalBtn: document.getElementById("openPortalBtn"),
    stepOpen: document.getElementById("stepOpen"),
    stepHold: document.getElementById("stepHold"),
    stepOtpStage: document.getElementById("stepOtpStage"),
    stepConfirm: document.getElementById("stepConfirm"),
    credForm: document.getElementById("credForm"),
    mobile: document.getElementById("mobile"),
    password: document.getElementById("password"),
    holdBtn: document.getElementById("holdBtn"),
    wipeBtn: document.getElementById("wipeBtn"),
    logoutBtn: document.getElementById("logoutBtn"),
    otpBanner: document.getElementById("otpBanner"),
    credState: document.getElementById("credState"),
    credMobile: document.getElementById("credMobile"),
    credAge: document.getElementById("credAge"),
    credMessage: document.getElementById("credMessage"),
  };

  function setStepActive(number) {
    UWIN.qsa(".step").forEach((step) => {
      step.classList.toggle("is-active", Number(step.dataset.step) === number);
      step.classList.toggle("is-done", Number(step.dataset.step) < number);
    });
  }

  function paintStatus(status) {
    if (!status) return;
    if (el.credState) el.credState.textContent = status.state.replace("_", " ");
    if (el.credMobile) el.credMobile.textContent = status.mobile_masked || "—";
    if (el.credAge) el.credAge.textContent = status.age_seconds + "s of " + status.ttl_seconds + "s";
    if (el.credMessage) el.credMessage.textContent = status.message || "—";
    if (el.otpBanner) el.otpBanner.hidden = status.state !== "awaiting_otp";
    if (status.state === "awaiting_otp") setStepActive(4);
    if (status.state === "logged_in") setStepActive(6);
  }

  async function openPortal() {
    const data = await UWIN.api("/api/portal/open", { method: "POST", body: {} });
    if (data.ok) {
      UWIN.toast("success", data.message, "Portal opened");
      setStepActive(1);
    } else if (data.url) {
      UWIN.toast("warn", data.message + " Copy this link: " + data.url, "Open manually");
    }
  }

  [el.openPortalBtn, el.stepOpen].forEach((button) => {
    UWIN.onClick(button, openPortal, "Opening…");
  });

  if (el.stepHold) {
    el.stepHold.addEventListener("click", () => {
      el.credForm.scrollIntoView({ behavior: "smooth", block: "center" });
      el.mobile.focus();
      setStepActive(2);
    });
  }

  /* ---------------- hold credentials in RAM ---------------- */
  if (el.credForm) {
    el.credForm.addEventListener("submit", async (event) => {
      event.preventDefault();
      const mobile = el.mobile.value.trim();
      const password = el.password.value;

      if (!/^[6-9]\d{9}$/.test(mobile.replace(/\D/g, "").replace(/^91(?=\d{10}$)/, ""))) {
        UWIN.toast("warn", "Please type the 10 digit mobile number used for U-WIN.", "Check mobile number");
        el.mobile.focus();
        return;
      }

      UWIN.setBusy(el.holdBtn, true, "Holding…");
      try {
        const data = await UWIN.api("/api/portal/credentials", {
          method: "POST",
          body: { mobile: mobile, password: password },
        });
        el.password.value = ""; // never keep it in the DOM
        if (data.ok) {
          paintStatus(data.status);
          setStepActive(3);
          UWIN.toast("success", data.message, "Credentials held in memory");
          const opened = await UWIN.confirm({
            title: "Now log in on the official page",
            text: "Chrome will open the U-WIN login page. Type the mobile number and password there, then wait for the SMS OTP. " +
                  "The app will not type the OTP for you.",
            confirmText: "Open U-WIN login",
          });
          if (opened) await openPortal();
        }
      } finally {
        UWIN.setBusy(el.holdBtn, false);
      }
    });
  }

  /* ---------------- OTP stage ---------------- */
  UWIN.onClick(el.stepOtpStage, async () => {
    const data = await UWIN.api("/api/portal/otp-stage", { method: "POST" });
    if (data.ok) {
      paintStatus(data.status);
      setStepActive(4);
      UWIN.toast("warn", "OTP Required – Enter the SMS OTP in the official portal page.", "Waiting for OTP");
    }
  });

  /* ---------------- confirm login ---------------- */
  UWIN.onClick(el.stepConfirm, async () => {
    const ok = await UWIN.confirm({
      title: "Are you really logged in?",
      text: "Only press Yes if you can see the U-WIN dashboard in Chrome and your name / facility is shown there.",
      confirmText: "Yes, I am logged in",
    });
    if (!ok) return;

    const data = await UWIN.api("/api/portal/confirm-login", { method: "POST", body: {} });
    if (data.ok) {
      paintStatus(data.status);
      setStepActive(6);
      UWIN.toast("success", data.message, "Login confirmed");
      const go = await UWIN.confirm({
        title: "Continue with the Excel workflow?",
        text: "Open the Review Queue now? Your records are already validated and matched on this phone.",
        confirmText: "Open Review Queue",
      });
      if (go) window.location.href = data.next_url || "/review";
    }
  }, "Checking…");

  /* ---------------- wipe / logout ---------------- */
  UWIN.onClick(el.wipeBtn, async () => {
    const data = await UWIN.api("/api/portal/logout", { method: "POST" });
    if (data.ok) {
      paintStatus(data.status);
      UWIN.toast("success", data.message, "Memory wiped");
    }
  });

  UWIN.onClick(el.logoutBtn, async () => {
    const ok = await UWIN.confirm({
      title: "Log out from here?",
      text: "This wipes the login data held in memory. Remember to log out in the browser too if this phone is shared.",
      confirmText: "Wipe local login data",
      danger: true,
    });
    if (!ok) return;
    const data = await UWIN.api("/api/portal/logout", { method: "POST" });
    if (data.ok) {
      paintStatus(data.status);
      setStepActive(1);
      UWIN.toast("success", data.message, "Logged out");
    }
  });

  /* ---------------- keep the status fresh ---------------- */
  document.addEventListener("DOMContentLoaded", () => {
    const paint = (data) => { if (data && data.portal) paintStatus(data.portal.credential_buffer); };
    UWIN.api("/api/portal/status", { silent: true }).then(paint).catch(() => {});
    setInterval(() => UWIN.api("/api/portal/status", { silent: true }).then(paint).catch(() => {}), 15000);
  });

  /* Clear the password box as soon as the tab is hidden (shoulder-surfing safety). */
  document.addEventListener("visibilitychange", () => {
    if (document.hidden && el.password) el.password.value = "";
  });
})();
