# InstaDM for Windows — Implementation Plan

Status legend: `NOT STARTED` | `IN PROGRESS` | `BLOCKED` | `DONE`
`FALLBACK-SAFE: yes` means a smaller model can execute the task from its
description and acceptance criteria alone.

This plan is refined after Phase 1 (source archaeology) and Phase 2
(architecture spike). Task details below M2 are provisional until then and are
finalized in place.

---

## M0 — Safe bootstrap and agent continuity — `IN PROGRESS`

| Task | Description | Status |
| --- | --- | --- |
| M0.1 | Verify target dir safety; init git on `main` | DONE |
| M0.2 | `.gitignore` + `.cursorignore` before any artifact | DONE |
| M0.3 | Ignored `.instadm-local.env` with source paths | DONE |
| M0.4 | `AGENTS.md` + Cursor rule | DONE |
| M0.5 | Continuity docs (`docs/*`) skeletons | IN PROGRESS |
| M0.6 | `Windows App/` Obsidian subtree + first synchronized handoff | NOT STARTED |
| M0.7 | Baseline secret/privacy scan + initial commit | NOT STARTED |

Exit: a new model can resume from the repo alone; no personal path staged;
clean initial commit. Commit: `chore: establish private development and handoff safeguards`
FALLBACK-SAFE: no (sets project rules).

---

## M1 — Source-derived product specification — `NOT STARTED`

| Task | Description | FALLBACK-SAFE |
| --- | --- | --- |
| M1.1 | Inventory Obsidian notes (sorted relative paths; read all relevant) | no |
| M1.2 | Inventory local macOS source (all Swift files, URL policy, scripts, watcher, notifications, lifecycle) | no |
| M1.3 | Compare with public GitHub repo; record meaningful differences | no |
| M1.4 | Write `docs/SOURCE_BEHAVIOR.md` behavior table (intent, evidence, macOS impl, edge cases, Windows disposition, confidence) | no |
| M1.5 | Extract hard-won regression list (login loops, SPA containment, pagination, notifications) | no |
| M1.6 | Freeze first-release scope and non-goals | no |

Exit: every major behavior has evidence + Windows disposition; conflicts in
`docs/DECISIONS.md`. Commit: `docs: derive Windows requirements from InstaDM sources`

---

## M2 — Architecture and privacy spike — `NOT STARTED`

| Task | Description | FALLBACK-SAFE |
| --- | --- | --- |
| M2.1 | Compare WinUI 3 + WebView2 vs WPF + WebView2 vs credible alternative on the 14 evaluation axes | no |
| M2.2 | WebView2 privacy/diagnostics research (SmartScreen, autofill, crash reporting, SSO, user-data folder, DevTools) | no |
| M2.3 | ADR with evidence, rejected alternatives, caveats, fallback plan | no |
| M2.4 | Initial network-boundary model into threat model / network audit doc | no |
| M2.5 | Packaging + Windows-version strategy | no |

Exit: chosen stack satisfies requirements or has explicit accepted caveats; no
ignored privacy blocker. Commit: `docs: select Windows architecture and privacy boundary`

---

## M3 — Buildable solution scaffold — `NOT STARTED`

| Task | Description | FALLBACK-SAFE |
| --- | --- | --- |
| M3.1 | Create solution: `InstaDM.Core` (netstandard-compatible TFM), `InstaDM.App` (Windows), `InstaDM.Core.Tests` | no |
| M3.2 | Local host tooling: install/pin .NET SDK (macOS host builds Core+Tests only) | yes |
| M3.3 | Windows CI workflow (`windows-latest`): restore, build, test synthetic only, no secrets | no |
| M3.4 | Clean clone/build docs in README | yes |

Exit: `dotnet build` + Core tests pass locally; App project builds in Windows CI.
Commit: `build: scaffold native Windows solution and tests`

---

## M4 — Pure navigation policy and adversarial tests — `NOT STARTED`

| Task | Description | FALLBACK-SAFE |
| --- | --- | --- |
| M4.1 | `InstagramSurface` taxonomy + canonical surface model | no |
| M4.2 | `UrlCanonicalizer`: scheme/host/port/path normalization, punycode/unicode fail-closed | no |
| M4.3 | `PathMatcher` with directory-boundary semantics | no |
| M4.4 | `NavigationPolicy` decisions with explicit `NavigationDecision` reasons | no |
| M4.5 | Feature gates: FollowRequests (default off), SharedPosts (default off, source-gated) | no |
| M4.6 | `PolicyScriptBuilder`: single-source policy serialization for the JS guard | no |
| M4.7 | Adversarial test table (hosts, lookalikes, schemes, `/direct*`, auth paths, boundaries, encodings, malformed, punycode) | yes (adding cases) |

Exit: pure, deterministic, tested before WebView integration; C#/JS cannot drift.
Commit: `feat: implement tested Instagram navigation policy`

---

## M5 — Local SPA harness and document-start guard — `NOT STARTED`

| Task | Description | FALLBACK-SAFE |
| --- | --- | --- |
| M5.1 | Local SPA fixture (links, capture/bubble handlers, pushState/replaceState/popstate, frames, popups, pagination-like fetches) | yes |
| M5.2 | `containment-guard.js`: capture-phase click/auxclick, History API wrap, auth stand-down, fail-closed, zero content reads | no |
| M5.3 | Schema-validated web-message protocol (navigation metadata only) | no |
| M5.4 | Repeatable harness tests (Node-based, no network) | yes (adding cases) |

Exit: disallowed transitions blocked before fixture handlers; no DOM scraping;
no Internet needed. Commit: `test: add local SPA harness for early navigation containment`

---

## M6 — Privacy-hardened WebView host — `NOT STARTED`

Explicit environment creation; dedicated local user-data folder; privacy
settings before navigation; documented event wiring; document-start script
registration; Debug-only sanitized diagnostics; clear-browsing-data; process
failure handling. Windows CI compile; interactive behavior marked
`NOT RUN - WINDOWS ENVIRONMENT REQUIRED`.
Commit: `feat: add privacy-hardened embedded Instagram host` — FALLBACK-SAFE: no

## M7 — Authentication and session lifecycle — `NOT STARTED`

Explicit state machine; narrow auth-surface policy; existence-only cookie
watcher with cancellation/backoff; synthetic adapter tests.
Commit: `feat: implement private authentication state machine` — FALLBACK-SAFE: no

## M8 — Navigation recovery and messaging reliability — `NOT STARTED`

Last-valid-DM tracking; top-level escape cancellation without Stop()/reload
damage; same-thread suppression; cooldown/debounce; popup/frame/download/
permission policies; harness regression tests.
Commit: `feat: contain Instagram escapes without breaking chat history` — FALLBACK-SAFE: no

## M9 — Native shell, settings, and optional surfaces — `NOT STARTED`

Messages-first window; native Settings; system/light/dark; Sage accent; local
settings store; Requests surface off by default; accessibility.
Commit: `feat: add focused native Windows shell and local settings` — FALLBACK-SAFE: partial (view polish yes)

## M10 — Privacy-first notifications — `NOT STARTED`

Title parser; baseline/increase state machine; poll scheduling/cancellation;
generic toasts; foreground suppression; taskbar indicator; permission-denied
fallback. Commit: `feat: add local generic unread notifications` — FALLBACK-SAFE: partial (parser test cases yes)

## M11 — Network/privacy audit and hardening — `NOT STARTED`

Dependency/license review; static scanners; runtime audit method + docs;
storage inventory; process-tree audit. Runtime audit itself requires Windows.
Commit: `security: complete local data and network boundary audit` — FALLBACK-SAFE: no

## M12 — Packaging, CI, release, documentation — `NOT STARTED`

Reproducible builds; distribution strategy; signing docs (no keys committed);
README/architecture/privacy/build/test/release docs; final handoff.
Commit: `release: prepare private Windows preview build` — FALLBACK-SAFE: partial (doc cleanup yes)

---

## Known host constraints

- Development host is macOS: `InstaDM.Core` and its tests build/run locally;
  Windows-targeted projects validate via `windows-latest` GitHub Actions.
- `dotnet` is not yet installed on this host (M3.2). `gh` auth token currently
  invalid — remote/CI work blocked until user refreshes (`gh auth refresh`).
- Interactive WebView2/packaging/notification testing requires a real Windows
  machine; recorded honestly in `docs/TEST_MATRIX.md`.
