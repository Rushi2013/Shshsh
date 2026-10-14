/* =========================================================================
   Upload page: choose file -> preview -> column mapping -> import
   ========================================================================= */
(function () {
  "use strict";

  const state = { sessionId: null, preview: null, mapping: {}, file: null };

  const el = {
    dropzone: document.getElementById("dropzone"),
    fileInput: document.getElementById("fileInput"),
    dzTitle: document.getElementById("dzTitle"),
    dzHint: document.getElementById("dzHint"),
    resetBtn: document.getElementById("resetBtn"),
    fileCard: document.getElementById("fileCard"),
    fileName: document.getElementById("fileName"),
    fileMeta: document.getElementById("fileMeta"),
    fileRows: document.getElementById("fileRows"),
    statRows: document.getElementById("statRows"),
    statEmpty: document.getElementById("statEmpty"),
    statDup: document.getElementById("statDup"),
    statMissing: document.getElementById("statMissing"),
    fileWarnings: document.getElementById("fileWarnings"),
    sheetSelect: document.getElementById("sheetSelect"),
    headerRowInput: document.getElementById("headerRowInput"),
    reloadPreview: document.getElementById("reloadPreview"),
    previewCard: document.getElementById("previewCard"),
    previewTable: document.getElementById("previewTable"),
    mappingCard: document.getElementById("mappingCard"),
    mappingRows: document.getElementById("mappingRows"),
    mappingCount: document.getElementById("mappingCount"),
    importBtn: document.getElementById("importBtn"),
    autoMapBtn: document.getElementById("autoMapBtn"),
    resultCard: document.getElementById("resultCard"),
    resultMessage: document.getElementById("resultMessage"),
  };

  /* ---------------- helpers ---------------- */
  function warningHtml(item) {
    const icon = item.severity === "error" ? "alert" : (item.severity === "warn" ? "alert" : "info");
    return (
      '<div class="warn-item w-' + UWIN.escape(item.severity) + '">' +
      UWIN.icon(icon) +
      '<div><strong class="small">' + UWIN.escape(item.field || "file") + ":</strong> " +
      UWIN.escape(item.message) +
      (item.hint ? '<div class="w-hint">' + UWIN.escape(item.hint) + "</div>" : "") +
      "</div></div>"
    );
  }

  function renderWarnings(host, warnings) {
    if (!host) return;
    if (!warnings || !warnings.length) { host.innerHTML = ""; return; }
    host.innerHTML =
      '<div class="stack" style="margin-bottom:6px">' + warnings.map(warningHtml).join("") + "</div>";
  }

  function renderPreview(preview) {
    const head = "<tr><th>#</th>" + preview.columns.map((c) => "<th>" + UWIN.escape(c) + "</th>").join("") + "</tr>";
    const body = preview.rows
      .map((row, index) => {
        const cells = row.map((cell) => {
          const empty = cell === "" || cell === null || cell === undefined;
          return "<td class='" + (empty ? "cell-missing" : "") + "'>" + (empty ? "—" : UWIN.escape(cell)) + "</td>";
        }).join("");
        return "<tr><td class='num muted'>" + (index + 1) + "</td>" + cells + "</tr>";
      })
      .join("");
    el.previewTable.innerHTML = "<table class='data'><thead>" + head + "</thead><tbody>" + body + "</tbody></table>";

    el.statRows.textContent = preview.data_rows;
    el.statEmpty.textContent = preview.empty_rows;
    el.statDup.textContent = preview.duplicate_rows;
    el.statMissing.textContent = preview.missing_cells;
    el.fileRows.textContent = preview.data_rows + " rows";
    el.fileMeta.textContent =
      "Sheet “" + preview.sheet_name + "” · header row " + preview.header_row + " · reader: " + preview.backend +
      " · " + preview.columns.length + " columns";
    el.fileName.textContent = state.file ? state.file.name : "uploaded file";
    renderWarnings(el.fileWarnings, preview.warnings);

    const sheets = preview.sheets && preview.sheets.length ? preview.sheets : [preview.sheet_name];
    el.sheetSelect.innerHTML = sheets
      .map((name) => "<option value='" + UWIN.escape(name) + "'" + (name === preview.sheet_name ? " selected" : "") + ">" +
        UWIN.escape(name) + "</option>")
      .join("");
    el.headerRowInput.value = preview.header_row;

    el.previewCard.hidden = false;
    el.mappingCard.hidden = false;
  }

  function renderMapping() {
    const preview = state.preview;
    if (!preview) return;
    const columns = preview.columns;
    const used = {};

    el.mappingRows.innerHTML = preview.fields
      .map((field) => {
        const current = state.mapping[field.key] || "";
        const options = ['<option value="">— not in my file —</option>']
          .concat(columns.map((column) =>
            "<option value='" + UWIN.escape(column) + "'" + (column === current ? " selected" : "") + ">" +
            UWIN.escape(column) + "</option>"))
          .join("");
        if (current) {
          used[current] = (used[current] || 0) + 1;
        }
        return (
          "<div class='map-row' data-field='" + field.key + "'>" +
          "<div><div class='map-label'>" + UWIN.escape(field.label) +
          (field.required ? " <span class='req'>required</span>" : "") + "</div>" +
          "<div class='map-help'>" + UWIN.escape(field.help) + "</div></div>" +
          "<select class='select js-map' data-field='" + field.key + "'>" + options + "</select>" +
          "</div>"
        );
      })
      .join("");

    UWIN.qsa(".js-map", el.mappingRows).forEach((select) => {
      select.addEventListener("change", () => {
        state.mapping[select.dataset.field] = select.value;
        renderMapping();
      });
    });

    // mark duplicated choices so the worker notices
    UWIN.qsa(".map-row", el.mappingRows).forEach((row) => {
      const select = row.querySelector("select");
      const column = select.value;
      const duplicated = column && used[column] > 1;
      row.classList.toggle("is-duplicate", Boolean(duplicated));
      const help = row.querySelector(".map-help");
      if (help) {
        const base = preview.fields.find((f) => f.key === row.dataset.field).help;
        help.textContent = duplicated ? "This column is used twice — check it." : base;
      }
    });

    const mapped = Object.values(state.mapping).filter(Boolean).length;
    el.mappingCount.textContent = mapped + " mapped";
    el.mappingCount.className = "badge " + (state.mapping.name && state.mapping.vaccine_name ? "s-matched" : "s-needs-review");
  }

  /* ---------------- upload ---------------- */
  async function uploadFile(file) {
    if (!file) return;
    const name = file.name.toLowerCase();
    if (!name.endsWith(".xlsx") && !name.endsWith(".xls")) {
      UWIN.toast("error", "Please choose an .xlsx or .xls Excel file.", "Unsupported file");
      return;
    }

    state.file = file;
    el.dzTitle.textContent = file.name;
    el.dzHint.textContent = UWIN.formatBytes(file.size) + " · uploading…";
    UWIN.busy(true, "Reading your Excel file…");

    try {
      const form = new FormData();
      form.append("file", file);
      const data = await UWIN.api("/api/session/upload", { method: "POST", body: form });
      if (!data.ok) {
        el.dzHint.textContent = "Upload failed. Please try another file.";
        return;
      }

      state.sessionId = data.session_id;
      state.preview = data.preview;
      state.mapping = Object.assign({}, data.preview.mapping);

      el.dzHint.textContent = UWIN.formatBytes(data.size_bytes) + " · uploaded successfully";
      el.fileCard.hidden = false;
      el.resetBtn.hidden = false;
      el.resultCard.hidden = true;
      renderPreview(data.preview);
      renderMapping();
      UWIN.toast("success", data.message, "File uploaded");
      el.fileCard.scrollIntoView({ behavior: "smooth", block: "start" });
    } finally {
      UWIN.busy(false);
    }
  }

  /* ---------------- wiring ---------------- */
  el.dropzone.addEventListener("click", () => el.fileInput.click());
  el.dropzone.addEventListener("keydown", (event) => {
    if (event.key === "Enter" || event.key === " ") { event.preventDefault(); el.fileInput.click(); }
  });
  el.fileInput.addEventListener("change", () => uploadFile(el.fileInput.files[0]));

  ["dragenter", "dragover"].forEach((type) =>
    el.dropzone.addEventListener(type, (event) => {
      event.preventDefault();
      el.dropzone.classList.add("is-over");
    }));
  ["dragleave", "drop"].forEach((type) =>
    el.dropzone.addEventListener(type, (event) => {
      event.preventDefault();
      el.dropzone.classList.remove("is-over");
    }));
  el.dropzone.addEventListener("drop", (event) => {
    const file = event.dataTransfer && event.dataTransfer.files ? event.dataTransfer.files[0] : null;
    uploadFile(file);
  });

  if (el.resetBtn) {
    el.resetBtn.addEventListener("click", () => {
      state.sessionId = null; state.preview = null; state.mapping = {}; state.file = null;
      el.fileInput.value = "";
      el.dzTitle.textContent = "Choose Excel file (.xlsx / .xls)";
      el.dzHint.textContent = "Maximum size set in Settings — smaller files load faster on a phone";
      el.fileCard.hidden = true;
      el.previewCard.hidden = true;
      el.mappingCard.hidden = true;
      el.resultCard.hidden = true;
      el.resetBtn.hidden = true;
      window.scrollTo({ top: 0, behavior: "smooth" });
    });
  }

  UWIN.onClick(el.reloadPreview, async () => {
    if (!state.sessionId) return;
    const data = await UWIN.api("/api/session/" + state.sessionId + "/preview", {
      method: "POST",
      body: { sheet_name: el.sheetSelect.value, header_row: Number(el.headerRowInput.value || 1) },
    });
    if (!data.ok) return;
    state.preview = data.preview;
    state.mapping = Object.assign({}, data.preview.mapping);
    renderPreview(data.preview);
    renderMapping();
    UWIN.toast("success", "Preview reloaded with the new sheet / header row.", "Updated");
  }, "Reading…");

  UWIN.onClick(el.autoMapBtn, () => {
    if (!state.preview) return;
    state.mapping = Object.assign({}, state.preview.mapping);
    renderMapping();
    UWIN.toast("info", "Auto-detected mapping restored. Please check it once.", "Mapping");
  });

  UWIN.onClick(el.importBtn, async () => {
    if (!state.sessionId) return;
    if (!state.mapping.name) {
      UWIN.toast("error", "Please map the Beneficiary Name column first.", "Mapping incomplete");
      return;
    }
    if (!state.mapping.vaccine_name || !state.mapping.vaccination_date) {
      const ok = await UWIN.confirm({
        title: "Vaccine or date column is not mapped",
        text: "Every row will then need a manual review. Continue anyway?",
        confirmText: "Continue",
      });
      if (!ok) return;
    }

    UWIN.busy(true, "Validating and matching every row…");
    try {
      let data = await UWIN.api("/api/session/" + state.sessionId + "/import", {
        method: "POST",
        body: {
          mapping: state.mapping,
          sheet_name: el.sheetSelect.value,
          header_row: Number(el.headerRowInput.value || 1),
          replace: false,
        },
        silent: true,
      });

      if (data.need_replace) {
        const ok = await UWIN.confirm({
          title: "Import this file again?",
          text: data.message + " Review decisions already taken for these rows will be lost.",
          confirmText: "Import again",
          danger: true,
        });
        if (!ok) return;
        data = await UWIN.api("/api/session/" + state.sessionId + "/import", {
          method: "POST",
          body: {
            mapping: state.mapping,
            sheet_name: el.sheetSelect.value,
            header_row: Number(el.headerRowInput.value || 1),
            replace: true,
          },
        });
      } else if (data.__failed) {
        return;
      }

      if (!data.ok) return;

      el.resultMessage.textContent = data.message;
      el.resultCard.hidden = false;
      const counts = data.counts || {};
      UWIN.qsa("#resultCard [data-stat]").forEach((node) => {
        const map = {
          total: "TOTAL", matched: "MATCHED", "needs-review": "NEEDS REVIEW",
          pending: "PENDING", completed: "COMPLETED", approved: "APPROVED",
        };
        const key = map[node.dataset.stat];
        if (key && counts[key] !== undefined) node.textContent = counts[key];
      });
      el.resultCard.scrollIntoView({ behavior: "smooth", block: "center" });
      UWIN.toast("success", data.message, "Import complete");
      UWIN.refreshStats().catch(() => {});
    } finally {
      UWIN.busy(false);
    }
  }, "Importing…");
})();
