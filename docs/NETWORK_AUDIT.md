# InstaDM for Windows — Network Audit

Status: METHOD DEFINED — no audit executed yet. Findings are appended in M11.
The privacy claim "only Instagram traffic" is NOT made until this audit proves
it for the released runtime and configuration.

## Method (repeatable, privacy-safe)

1. Clean dedicated WebView2 profile and clean app data directory.
2. Release build with final runtime settings.
3. Observe process tree and outbound **destination metadata only** using local
   Windows tooling (e.g. `Get-NetTCPConnection` correlated to PIDs, Packet
   Monitor / ETW for destination hosts). Never record payloads, cookies,
   headers, full private URLs, or message content.
4. Exercise the local synthetic harness first; then a manually controlled
   Instagram session only if the user is present and consents.
5. Record destination hostnames/categories, process names, purpose,
   coarse timestamps.
6. Categorize every destination (lists below). Investigate anything unexpected
   before continuing.
7. Confirm no developer-controlled endpoint and no third-party SDK traffic.
8. Confirm close-window ends app-owned connections and timers.
9. Delete captures/traces after extracting sanitized findings; never commit them.

## Destination categories (findings appended here in M11)

### A. Top-level navigable Instagram surfaces
(none recorded yet)

### B. Instagram/Meta subresource/API/media domains required by the official web client
(none recorded yet — expected: `*.instagram.com`, `*.cdninstagram.com`,
`*.fbcdn.net`, `*.facebook.com` auth-platform endpoints; verify empirically)

### C. Web-runtime connections NOT owned by Instagram/Meta — RELEASE BLOCKERS unless eliminated
(none recorded yet — candidates to hunt for: WebView2/Edge diagnostics,
SmartScreen reputation, component update, crash upload, autofill/sync)

### D. Independent Windows/OS activity (not initiated or fed by the app)
(none recorded yet)

### E. Localhost connections used only by synthetic tests
(none recorded yet)

### F. Unexpected / release-blocking destinations
(none recorded yet)

## Runtime configuration under audit

Planned configuration (ADR-006; implemented in M6, verified in M11):

- Environment: `CreateWithOptionsAsync`, dedicated user-data folder
  `%LOCALAPPDATA%\InstaDM\WebView2`.
- `AdditionalBrowserArguments`: `--disable-features=msSmartScreenProtection`
  plus candidates `--disable-domain-reliability`,
  `--disable-background-networking`, `--disable-component-update`
  (final set fixed empirically here).
- `IsCustomCrashReportingEnabled = true` (no crash upload to Microsoft).
- `AreBrowserExtensionsEnabled = false`,
  `AllowSingleSignOnUsingOSPrimaryAccount = false`.
- Settings before first navigation: `IsReputationCheckingRequired = false`,
  `IsPasswordAutosaveEnabled = false`, `IsGeneralAutofillEnabled = false`,
  `AreDevToolsEnabled` = Debug only.
- Known open question to measure: WebView2 "required diagnostic data"
  behavior (Microsoft documents it as following Windows diagnostic settings).
  If app-owned WebView2 processes emit Microsoft-bound telemetry that cannot
  be eliminated, release on this runtime is blocked (gates G1/G2).

## Limitations

- Static scanning cannot prove runtime behavior; only this audit can.
- The audit is a snapshot; re-run after any WebView2 runtime major update and
  before every release (see `docs/RELEASE_CHECKLIST.md`).
