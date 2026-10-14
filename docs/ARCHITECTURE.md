# Architecture — code kaise organised hai

Ye document un logon ke liye hai jo code me change karna chahte hain (ya samajhna chahte hain).

---

## 1. Layers

```
                    ┌──────────────────────────────────────────────┐
   Browser (Chrome) │  templates/*.html  +  static/css  +  static/js│
                    └───────────────┬──────────────────────────────┘
                                    │  HTML pages + JSON API  (/api/*)
                    ┌───────────────▼──────────────────────────────┐
   Flask app        │  app.py                                      │
   (routes, CSRF,   │  - pages  : /, /upload, /records, /review,   │
    headers, PIN)   │             /history, /reports, /settings,   │
                    │             /portal, /help, /lock            │
                    │  - api    : /api/session/*, /api/records/*,  │
                    │             /api/queue/*, /api/reports/*,    │
                    │             /api/register/*, /api/portal/*   │
                    └───┬─────────┬─────────┬─────────┬────────────┘
                        │         │         │         │
        ┌───────────────▼──┐ ┌────▼──────┐ ┌▼───────────────┐ ┌▼──────────────┐
        │ excel_handler.py │ │valid.py  │ │matching_engine │ │progress_      │
        │ read/preview/map │ │rules     │ │ID>name+DOB>    │ │manager.py     │
        │ date order       │ │"no guess"│ │field>fuzzy     │ │status machine │
        └────────┬─────────┘ └──────────┘ └────────────────┘ └───────┬───────┘
                 │                                                  │
        ┌────────▼──────────────────────────────────────────────────▼───────┐
        │ database.py   (SQLite: sessions, records, official_records,        │
        │                queue_state, events, reports, settings, meta)       │
        └────────┬───────────────────────────────────────────────────────────┘
                 │
        ┌────────▼───────────────┐        ┌────────────────────────────────┐
        │ report_generator.py    │        │ uwin_integration.py            │
        │ multi-sheet xlsx/csv   │        │ browser open + RAM credentials │
        └────────────────────────┘        │ + approved-API adapter         │
                                          └────────────────────────────────┘
```

Rule: **upar wali layer niche wali ko call karti hai, kabhi ulta nahi.**
`validation.py`, `matching_engine.py`, `database.py` Flask ko import nahi karte —
isliye tests aur CLI (`python app.py --check`) aasani se chalte hain.

---

## 2. Module-wise kaam

| Module | Zimmedari | Kya import nahi karta |
|--------|-----------|------------------------|
| `config.py` | Paths, portal URL, thresholds, settings defaults + validation, secret key | Flask |
| `database.py` | SQLite schema + queries + settings + events | Flask, pandas |
| `validation.py` | Date/phone/gender/dose rules, record warnings, duplicates, mapping checks | Flask, pandas |
| `excel_handler.py` | Magic-byte sniffing, sheet picking, header detection, auto-mapping, extraction, xlsx/csv writing | Flask |
| `matching_engine.py` | Register index, priority matching, fuzzy scoring, ambiguity | Flask, database |
| `progress_manager.py` | Queue (pause/resume/stop), process/approve/edit/reject/skip, save markers, recovery, statistics | Flask |
| `report_generator.py` | Multi-sheet reports, CSV export, privacy column list | Flask |
| `uwin_integration.py` | Browser open, RAM credential buffer, guided login, adapter client, save decision | Flask |
| `app.py` | HTTP routes, CSRF, security headers, error handlers, PIN lock, CLI | — |
| `samples.py` | Example Excel generator | Flask |

---

## 3. Database schema (short)

```
sessions(id, original_filename, stored_filename, file_hash, sheet_name, header_row,
         total_rows, empty_rows, duplicate_rows, missing_cells, mappings_json,
         columns_json, warnings_json, status, created_at, updated_at)
records(id, session_id, row_number, status, name, dob, dob_raw, gender, mobile,
        beneficiary_id, mother_name, address, district, block, facility, vaccine_name,
        dose_number, vaccination_date, vaccination_date_raw, batch_number,
        vaccinator_name, remarks, extras_json, raw_json, edited_json, warnings_json,
        match_json, match_confidence, matched_official_id, decision, decision_note,
        decided_at, save_state, save_reference, save_confirmed_at, save_attempts,
        error_message, duplicate_of, manually_verified, created_at, updated_at)
official_records(id, beneficiary_id, name, dob, gender, mobile, mother_name,
                 extra_json, name_key, dob_key, source_file, imported_at)
queue_state(id=1, paused, stopped, mode, current_record_id, updated_at)
events(id, ts, level, event, session_id, record_id, message, detail_json)
reports(id, session_id, filename, kind, scope, rows_count, size_bytes, created_at)
settings(key, value, updated_at)
meta(key, value)
```

Notes
* `PRAGMA journal_mode=WAL` + `synchronous=NORMAL` → crash par corruption nahi.
* `ON DELETE CASCADE` sessions → records.
* Credentials ke liye **koi column nahi hai** (jaan-boojh kar).

---

## 4. Status machine

```
                    ┌─────────┐
       import ────► │ PENDING │ ────► (matching) ────► MATCHED ──┐
                    └─────────┘                             │
                         │ validation problem               │ worker
                         ▼                                  ▼
                   ┌──────────────┐  approve        ┌──────────────┐
                   │ NEEDS REVIEW │ ───────────────►│   APPROVED   │
                   └──────────────┘                 └──────┬───────┘
                         │ reject                          │ portal save
                         ▼                                 │
                    ┌────────┐                             │ confirmed ──► COMPLETED
                    │ FAILED │◄── worker reject           │
                    └────────┘                             │ not confirmed
                                                           ▼
                                                    SAVE STATUS UNKNOWN
                                        (kabhi automatic retry nahi)

   worker skip kisi bhi open status se ─────► SKIPPED ──► reopen ──► PENDING/NEEDS REVIEW
```

`COMPLETED` aur `SAVE STATUS UNKNOWN` **final** hain: `process_record()` inhe chhuta nahi
(jab tak worker `force` na kare), aur `recover_state()` unhe dobara queue me nahi daalta.

---

## 5. JSON API (dashboard ke liye)

| Endpoint | Method | Kaam |
|----------|--------|------|
| `/api/status` | GET | counts, queue, session, register, credential state |
| `/api/statistics` | GET | sirf counts |
| `/api/session/upload` | POST | file upload + preview + auto mapping |
| `/api/session/<id>/preview` | POST | sheet/header badal ke dobara preview |
| `/api/session/<id>/import` | POST | mapping save + records import + auto match |
| `/api/session/<id>/activate` \| `/delete` \| `/reprocess` | POST | session actions |
| `/api/queue/pause` \| `resume` \| `stop` \| `next` \| `batch` \| `retry_failed` \| `mode` | POST | queue |
| `/api/records/<id>` | GET | ek record + warnings + match |
| `/api/records/<id>/process` \| `approve` \| `edit` \| `reject` \| `skip` \| `reopen` | POST | decisions |
| `/api/records/<id>/save` \| `confirm` \| `unknown` | POST | portal save + confirmation |
| `/api/register/import` \| `/clear` \| `/sample` | POST/GET | authorised register |
| `/api/reports/session` \| `/overview` \| `/export` | POST | report banana |
| `/api/reports` \| `/api/reports/<id>/delete` | GET/POST | report list/delete |
| `/reports/download/<file>` | GET | report download (traversal protected) |
| `/api/portal/status` \| `open` \| `credentials` \| `otp-stage` \| `otp` \| `confirm-login` \| `logout` \| `test-adapter` \| `disable-adapter` | GET/POST | portal assistant |
| `/api/settings` \| `/api/settings/theme` \| `/api/settings/cleanup` | POST | settings |
| `/api/pin/set` \| `remove` \| `unlock` | POST | PIN lock |
| `/api/data/wipe` \| `/api/session/purge_uploads` | POST | privacy |
| `/api/sample-file` \| `/api/register/sample` | GET | example files |

Sab write endpoints CSRF token maangte hain (`X-CSRF-Token` header).

---

## 6. Frontend structure

| File | Kaam |
|------|------|
| `static/css/style.css` | design tokens (light/dark), layout, components — koi framework nahi |
| `static/js/app.js` | `UWIN` namespace: api(), toast, confirm/prompt dialog, theme, sidebar, live stats polling, copy |
| `static/js/upload.js` | file → preview → mapping → import |
| `static/js/dashboard.js` | session open/delete, queue buttons, register import |
| `static/js/review.js` | edit form, approve/reject/skip, assisted save + steps, confirm reference |
| `static/js/settings.js` | settings save, PIN, cleanup, wipe, diagnostics |
| `static/js/portal.js` | guided login, credential form, OTP banner, session state |

Accessibility: skip link, `aria-label`/`aria-expanded`, native `<dialog>` (focus trapping),
keyboard shortcuts (`a` approve, `s` skip) jab kisi input me focus na ho.

---

## 7. Testing

```bash
python -m pytest -q            # 91 tests
python -m pytest tests/test_workflow.py::TestPortalSafety -v
```

| Test file | Kya cover karta hai |
|-----------|---------------------|
| `tests/test_validation.py` | date parsing, ambiguity, mobile/gender/dose, "no guess" policy, duplicates |
| `tests/test_excel.py` | sniffing, sheet picking, header detection, mapping, extraction stats, writers |
| `tests/test_matching.py` | priority order, ambiguity, fuzzy-never-auto, conflicts, summary |
| `tests/test_workflow.py` | poora HTTP flow, CSRF, report download, PIN, wipe, portal safety |

Tests ek temporary data folder (`UWIN_DATA_ROOT`) use karte hain — aapka asli data safe rehta hai.

---

## 8. Extension points (aage kya jodna aasan hai)

1. **Official U-WIN API** → `AdapterClient` ke 3 methods + `_record_payload()` (dekho `docs/UWIN_INTEGRATION.md` §4.3).
2. **Naya Excel field** → `excel_handler.FIELD_DEFINITIONS` + `SYNONYMS` me add karo,
   `database.py` ke `records` table me column, `progress_manager.EDITABLE_FIELDS`, phir
   `report_generator.REPORT_COLUMNS`. (Chaaron jagah ek hi naam rakhna.)
3. **Naya status** → `config.ALL_STATUSES` + `STATUS_COUNT_KEYS` + badge CSS class `.s-<slug>`.
4. **Naya report** → `report_generator.py` me ek function jo `excel_handler.write_xlsx()` use kare.
5. **Doosri language (UI text)** → templates me strings; ya Jinja `i18n` extension jodna.
