# InstaDM for Windows — Decision Log (ADRs)

Format: newest last. Each entry: context, decision, evidence, alternatives,
privacy impact, status.

---

## ADR-001 — Repository bootstrap order and continuity protocol

- **Date:** 2026-07-22
- **Status:** Accepted
- **Context:** Model sessions can stop at any time; private reference material
  (macOS source, Obsidian notes) must never leak into the repo.
- **Decision:** Ignore rules (`.gitignore`, `.cursorignore`) and the ignored
  `.instadm-local.env` (holding personal absolute paths) are created before any
  other artifact. Continuity docs (`AGENTS.md`, Cursor rule, `docs/*`) are
  committed before implementation. Two synchronized handoff copies are
  maintained: `docs/HANDOFF.md` (authoritative, committed) and
  `Windows App/HANDOFF.md` (private Obsidian journal copy).
- **Privacy impact:** Prevents accidental commit/index of secrets, personal
  paths, and runtime user data.

---

## ADR-002 — Repository name and visibility

- **Date:** 2026-07-22
- **Status:** Accepted
- **Context:** Local folder is `InstagramDMOnlyAppForWindows`; the prompt
  suggests `InstaDM-Windows` as the default repository name.
- **Decision:** Future GitHub repository name `InstaDM-Windows`, **private** by
  default. The local folder name is irrelevant to the remote name. No push
  until initial docs, ignore rules, and a secret scan pass. Note: `gh` token on
  this host is currently invalid (keyring); remote creation deferred until the
  user refreshes auth — recorded as a blocker, not a stopping condition.
- **Privacy impact:** Private visibility prevents premature exposure.

---

## ADR-003 — Source evidence hierarchy and local/public reconciliation

- **Date:** 2026-07-22
- **Status:** Accepted
- **Context:** The prompt requires comparing the local macOS source with the
  public GitHub repository and resolving differences.
- **Decision:** Verified the local source working tree is clean at tag
  `v1.0.1` (HEAD `009252f`) and identical to `origin/main` of
  https://github.com/rquader/InstaDM — **no divergence exists**. Evidence
  hierarchy used for `docs/SOURCE_BEHAVIOR.md`: notes = product intent,
  local code at `v1.0.1` = implemented behavior, public README/CHANGELOG/git
  history = supporting evidence.
- **Privacy impact:** none; only sanitized distillations recorded.

---

## ADR-004 — Port behavior, not defects: known macOS bugs fixed by design

- **Date:** 2026-07-22
- **Status:** Accepted
- **Context:** A read-only audit of the macOS source (2026-06-04, recorded in
  the reference notes) verified 17 code findings. The Windows port must not
  faithfully reproduce known defects.
- **Decision:** The Windows implementation fixes by construction: boundary-
  anchored matching everywhere including DM paths and the generated JS guard
  (findings L1/L3); one lowercasing host canonicalizer (M1); popup scheme
  checks (L2); comma-grouped and malformed title parsing (M2/L9); heal
  actions gated to explicit link activations, background prefetches dropped
  silently (H1); guard installs unconditionally with runtime auth stand-down
  rather than skipping installation (L5 — decide finally in M5 with harness
  evidence); weak/cancellable timers and disposal (L6/L7); SharedPosts
  default-disabled at both compile-time and runtime (L11); debug diagnostics
  log redacted paths/enums, never full thread URLs (L12 nit).
- **Privacy impact:** positive — several findings are privacy/containment
  hardening.

## ADR-005 — Architecture: C# + WinUI 3 (Windows App SDK stable) + WebView2, with WPF as fallback

- **Date:** 2026-07-22
- **Status:** Accepted
- **Context:** Compared three options on the required axes (native feel,
  document-start injection, navigation/popup/download/permission interception,
  local cookie persistence, auth reliability, privacy/diagnostics, telemetry
  disablement, lifecycle, notifications/taskbar, packaging, testability
  without a live account, dependency footprint, accessibility/DPI/theme,
  single-developer + small-model maintainability).

  1. **C# + WinUI 3 + Windows App SDK + WebView2** — modern native chrome
     (Fluent, Mica, dark/light), first-party toast notification APIs, current
     stable Windows App SDK is 2.3.x (July 2026; min OS Windows 10 1809);
     WebView2 supplies every needed hook: explicit
     `CoreWebView2Environment.CreateWithOptionsAsync` with a dedicated
     user-data folder, `AddScriptToExecuteOnDocumentCreatedAsync`
     (document-start, before page scripts), `NavigationStarting` /
     `FrameNavigationStarting` / `SourceChanged` / `HistoryChanged` /
     `NewWindowRequested` / `DownloadStarting` / `PermissionRequested` /
     `ProcessFailed`, `CookieManager` (existence-only checks are expressible),
     `Profile.ClearBrowsingDataAsync`. All first-party; zero third-party
     packages needed.
  2. **C# + WPF + WebView2** — same WebView2 privacy posture and hooks; older
     but extremely stable tooling; less native-modern feel (no Mica, manual
     dark-mode title bar work); equal testability. Kept as the documented
     fallback if WinUI 3 tooling friction (packaging, CI) becomes a real
     blocker — the swap cost is deliberately kept low because all policy/state
     logic lives in `InstaDM.Core` and the host layer is thin.
  3. **Alternative engine (CEF/Chromium-embedded, Tauri/WRY, Electron)** —
     rejected. Electron: bundles a second browser engine, large third-party
     surface, contradicts the zero-dependency privacy posture. CEF: huge
     supply-chain and update burden for one developer; no privacy advantage
     (Chromium services still need disabling). Tauri/WRY on Windows wraps the
     same WebView2 runtime with an extra non-first-party layer and a Rust
     toolchain — added surface, no benefit. Reconsidered only if the WebView2
     privacy gate (ADR-006/G1–G2) fails.

- **Decision:** WinUI 3 (Windows App SDK, current stable 2.x line) + WebView2,
  packaged app, .NET current LTS; exact SDK/TFM pinned in M3 from installed
  tooling per the working rules. `InstaDM.Core` (policy, auth, notifications,
  recovery state machines) targets plain `net` TFM so it builds and tests on
  the macOS dev host; only `InstaDM.App` is Windows-targeted.
- **Fallback plan:** if WinUI 3 blocks (packaging/CI/API defect), move the
  thin host layer to WPF + WebView2 without touching `InstaDM.Core`. If
  WebView2 itself fails the privacy gate, do not release; re-evaluate engines
  with the audit evidence in hand.
- **Privacy caveats:** carried in ADR-006.

---

## ADR-006 — WebView2 privacy configuration and release gate

- **Date:** 2026-07-22
- **Status:** Accepted (configuration plan); runtime verification pending (M11)
- **Context:** WebView2 is a Chromium/Edge runtime with Microsoft-connected
  features. Microsoft's data-privacy documentation states apps do not control
  *overall* diagnostic data collection (it follows Windows diagnostic-data
  settings), while specific features are app-controllable. The absolute
  privacy invariant forbids app-owned processes from contacting non-
  Instagram/Meta endpoints during normal operation.
- **Decision — configure all of the following before first navigation:**
  - **SmartScreen/reputation:** disabled via BOTH
    `CoreWebView2EnvironmentOptions.AdditionalBrowserArguments =
    "--disable-features=msSmartScreenProtection"` (environment-level, cannot
    be re-enabled at runtime) and `Settings.IsReputationCheckingRequired =
    false` (belt and suspenders). Security tradeoff accepted and documented:
    the app navigates only to Instagram's own site by policy; SmartScreen's
    protection value here is minimal and its URL-reporting cost violates the
    invariant.
  - **Crash reporting:** `CoreWebView2EnvironmentOptions.
    IsCustomCrashReportingEnabled = true` — per Microsoft docs this stops
    Windows sending WebView2 crash dumps to Microsoft; dumps stay local in
    the user-data folder and are documented in the storage inventory.
  - **Password autosave:** `Settings.IsPasswordAutosaveEnabled = false`.
  - **General autofill:** `Settings.IsGeneralAutofillEnabled = false`.
  - **Browser extensions:** `CoreWebView2EnvironmentOptions.
    AreBrowserExtensionsEnabled = false` (default, set explicitly).
  - **OS single sign-on:** `CoreWebView2EnvironmentOptions.
    AllowSingleSignOnUsingOSPrimaryAccount = false` (default, set explicitly).
  - **Tracking prevention:** keep enabled (privacy-positive, local);
    level set explicitly; verify it makes no external calls in the audit.
  - **DevTools / debugging:** `Settings.AreDevToolsEnabled` true only in
    DEBUG; remote debugging never enabled in Release (no
    `--remote-debugging-port`; scanner enforces).
  - **User-data folder:** dedicated `%LOCALAPPDATA%\InstaDM\WebView2` — local,
    non-roaming, app-exclusive, deleted by clear-data flow where supported.
  - **Additional Chromium service hardening (verify each in audit):**
    disable via `AdditionalBrowserArguments` candidates —
    `--disable-domain-reliability`, `--disable-background-networking`,
    `--disable-component-update` — exact final set determined empirically in
    M11 on Windows; flags must not break Instagram function.
- **Release gate (binding):** the M11 runtime network audit of a Release
  build must show **no app-owned process contacting any non-Instagram/Meta
  endpoint** during normal operation. Any remaining Microsoft-bound
  diagnostics/telemetry from WebView2 processes that cannot be eliminated is
  a release blocker on this runtime — not a disclosure footnote. The
  WebView2 Evergreen runtime's own updater (scheduled task/service installed
  with the runtime, runs independently of the app) is categorized as
  independent OS activity: documented separately, never fed app data, and
  never used to excuse app-process egress.
- **Privacy impact:** this ADR is the concrete implementation of threat-model
  gates G1/G2.

---

(Later ADRs — navigation policy shape details, auth state machine, and
notification design — are appended below as they are made.)
