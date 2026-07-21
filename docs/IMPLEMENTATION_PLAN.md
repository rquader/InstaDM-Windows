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

## M4 — Pure navigation policy and adversarial tests — `DONE`

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

Status notes (2026-07-22): all tasks complete, 169/169 tests pass on macOS.
Implementation details worth knowing before M5/M6:
- Dot segments (literal and %2e-encoded) are RFC 3986-resolved by `Uri`
  before judgment — same resolution the browser applies — and tests pin it.
- Encoded slashes, backslashes, userinfo, punycode/unicode hosts, non-443
  ports, and non-https schemes all fail canonicalization (blocked).
- SharedPosts prefixes are never exported to the JS guard payload; the guard
  fails closed on posts/reels and the native layer alone grants the
  DM-sourced exception (`NavigationContext.FromDm`).
- `IsIncidentalBlockedPrefetch` encodes the macOS heal-thrash lesson: feed/
  explore/reels/notifications/profile prefetches are cancelled silently.
- Wire payload is versioned (`PolicyScriptBuilder.PayloadVersion`).

---

## M5 — Local SPA harness and document-start guard — `DONE`

| Task | Description | FALLBACK-SAFE |
| --- | --- | --- |
| M5.1 | Local SPA fixture (links, capture/bubble handlers, pushState/replaceState/popstate, frames, popups, pagination-like fetches) | yes |
| M5.2 | `containment-guard.js`: capture-phase click/auxclick, History API wrap, auth stand-down, fail-closed, zero content reads | no |
| M5.3 | Schema-validated web-message protocol (navigation metadata only) | no |
| M5.4 | Repeatable harness tests (Node-based, no network) | yes (adding cases) |

Exit: disallowed transitions blocked before fixture handlers; no DOM scraping;
no Internet needed. Commit: `test: add local SPA harness for early navigation containment`

Status notes (2026-07-22): complete — 25 Node tests + C# fixture-drift test.
- `src/InstaDM.App/Web/containment-guard.js`: factory (`createGuard(g,policy)`)
  so tests install into a fake window; production path self-invokes against
  the real `window`. Mirrors UrlCanonicalizer/PathMatcher exactly; capture-
  phase click/auxclick on window; pushState/replaceState wrapped; popstate
  deliberately untouched (native recovery watches committed locations);
  full stand-down while the CURRENT page is an auth surface; fail closed on
  judgment errors; malformed payload deactivates the guard (native layer
  remains authoritative).
- Bridge messages: fixed schema `{v, source, kind, surface}` — coarse
  category strings only, never URLs/content (tests assert this).
- Harness is a fake-DOM Node module (`tests/Fixtures/local-spa-harness/`)
  rather than a browser HTML fixture: models capture/bubble ordering,
  stopImmediatePropagation, nested link targets, and a committing History
  API — everything the guard touches — with zero browser dependency. A real
  WebView2 run remains part of M6+ Windows validation.
- `policy.default.json` fixture is pinned to `PolicyScriptBuilder` output by
  `PolicyFixtureDriftTests` (C#) and consumed by the Node tests, so the
  C# policy, the fixture, and the JS guard cannot drift pairwise.

---

## M6 — Privacy-hardened WebView host — `DONE`

Status notes (2026-07-22): committed (`497074d`). Windows CI validation still
pending (no remote yet; `gh` auth keyring invalid on this host).
- `src/InstaDM.Core/WebHost/WebViewHostConfiguration.cs`: ADR-006 as data —
  every privacy value is a pinned, tested constant (SmartScreen off at
  environment level, custom crash reporting on = dumps stay local, password
  autosave/autofill/extensions/SSO off, tracking prevention on, DevTools
  Debug-only, dedicated `%LOCALAPPDATA%\InstaDM\WebView2` user-data folder,
  Chromium background-service switches). Tests fail if any value changes.
- `src/InstaDM.Core/WebHost/GuardMessage.cs`: exact-schema web-message
  parser (security boundary — page script is untrusted); rejects extra keys,
  wrong types/version/source, unknown enum values, oversized payloads.
- `src/InstaDM.App/Controls/InstagramWebViewHost.xaml(.cs)`: ordered init
  (environment → settings → document-start guard splice → events → first
  navigation); handlers each enforce one named invariant: main/frame
  navigation policy, popup no-bypass (allowed targets navigate in-view),
  permissions default-deny, downloads cancelled, process-failure recovery
  with cap + reset-on-success, schema-validated guard reports re-raised as
  a typed event for the M8 recovery coordinator.
- macOS local build of InstaDM.App fails in the XAML compiler (expected,
  Windows required); Core tests 192/192 pass. Windows CI validates the App
  compile once the remote exists.

Explicit environment creation; dedicated local user-data folder; privacy
settings before navigation; documented event wiring; document-start script
registration; Debug-only sanitized diagnostics; clear-browsing-data; process
failure handling. Windows CI compile; interactive behavior marked
`NOT RUN - WINDOWS ENVIRONMENT REQUIRED`.
Commit: `feat: add privacy-hardened embedded Instagram host` — FALLBACK-SAFE: no

## M7 — Authentication and session lifecycle — `DONE`

| Task | Description | Status | FALLBACK-SAFE |
| --- | --- | --- | --- |
| M7.1 | `AuthenticationState` / `AuthenticationEvent` enums | DONE | no |
| M7.2 | Pure `AuthenticationStateMachine` (idempotent, capped process failure) | DONE | no |
| M7.3 | `AuthSessionWatcher` + `ISessionCookieProbe` (existence only, backoff, single-flight) | DONE | no |
| M7.4 | `WebViewSessionCookieProbe` (name check only; never `.Value`) | DONE | no |
| M7.5 | Host wiring: surface commits, clear-data, unload stops pollers | DONE | no |
| M7.6 | Synthetic adapter tests (login/2FA/challenge/expiry/logout/clear/fatal) | DONE | yes (adding cases) |

Status notes (2026-07-22): 217 Core tests pass. Clear-data escapes Fatal and
resets the failure budget (ADR-007). Process-failure recovery is owned by the
state machine; the host no longer keeps a parallel counter.
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
