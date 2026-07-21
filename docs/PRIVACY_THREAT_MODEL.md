# InstaDM for Windows — Privacy Threat Model

Status: LIVING DOCUMENT — updated at every milestone that changes the data or
network surface. The release checklist references the gates defined here.

## 1. Assets to protect

| ID | Asset | Sensitivity |
| --- | --- | --- |
| A1 | Instagram credentials and 2FA codes | Critical |
| A2 | Session cookies / auth tokens / CSRF values | Critical |
| A3 | Message content, previews, media, reactions | Critical |
| A4 | Contact identity: usernames, sender names, thread titles, avatars | High |
| A5 | Thread identifiers and DM URLs (may identify contacts) | High |
| A6 | Browsing/navigation history inside the app | High |
| A7 | Notification content | High |
| A8 | Local app settings (theme, poll interval, feature flags) | Low, must stay local |
| A9 | WebView user-data folder (cookies DB, cache, local storage) | Critical |
| A10 | Diagnostics/logs produced by the app | Must be sanitized by construction |

## 2. Trust boundaries

```
[User] ⇄ [InstaDM native shell (app-owned)] ⇄ [WebView2 runtime processes (app-owned)]
                                                 ⇅ HTTPS
                                       [Instagram/Meta first-party services]   ← ONLY permitted egress
```

- **B1 — App process ⇄ network:** the only permitted destinations for app-owned
  processes are Instagram/Meta endpoints required by the official web client.
- **B2 — Native code ⇄ web content:** native code may steer navigation and check
  cookie *existence*; it must never read page content, credentials, or cookie values.
- **B3 — App ⇄ disk:** all persistence is local and non-roaming
  (`%LOCALAPPDATA%\InstaDM`). Nothing is written to roaming/cloud-synced locations.
- **B4 — App ⇄ OS services:** the app must not hand Instagram URLs, cookies, or
  content to OS services (SmartScreen, search, share targets) beyond what the
  runtime unavoidably does — and unavoidable runtime egress is a release blocker
  unless disabled (see §5).
- **B5 — Development boundary:** development tools (Cursor/models) never receive
  live account data; only synthetic fixtures and sanitized metadata.

## 3. Threats and mitigations

| ID | Threat | Mitigation | Verified by |
| --- | --- | --- | --- |
| T1 | App code captures credentials/2FA | No credential UI; no password-field observation; no keystroke hooks; injected JS reads no input values | Code review + JS guard tests + static scan |
| T2 | Cookie values leak to logs/settings/handoffs | Existence-only cookie API (`ICookieExistenceChecker`); redaction layer; scanner forbids `sessionid`/`Cookie:` value logging | `tools/verify-no-sensitive-logging.ps1` + unit tests |
| T3 | Message/DOM content exfiltration | Injected JS never reads message DOM, input values, or arbitrary text; web-message protocol is schema-validated and carries only navigation metadata | Harness tests + code review |
| T4 | Third-party telemetry/analytics SDK added | Zero-analytics policy; dependency allowlist; static scan for known SDKs/endpoints | `tools/privacy-audit.ps1` + dependency review |
| T5 | WebView2 runtime phones home (diagnostics, SmartScreen, autofill sync) | Environment configured with SmartScreen off, autofill off, password autosave off, no browser sign-in; runtime network audit in Release; **any un-disableable non-Meta egress from app-owned processes blocks release** | `docs/NETWORK_AUDIT.md` runtime audit |
| T6 | Navigation escape to feed/Explore/Reels/profiles | Canonical native `NavigationPolicy` + document-start guard; popups/frames/downloads policy; adversarial URL tests | `InstaDM.Core.Tests` + SPA harness |
| T7 | Lookalike/malformed URL bypass (unicode host, encoded slash, `/p` vs `/profile`) | Strict canonicalization, https-only, host allowlist, directory-boundary matching, fail-closed on parse failure | Adversarial unit tests |
| T8 | Notification leaks content | Generic text only ("N new messages"); parser reads page *title* only; baseline machine prevents stale announcements | Unit tests |
| T9 | Data persists after "Clear data & sign out" | Supported WebView2 `ClearBrowsingDataAsync` + app-state wipe; documented storage inventory | Manual Windows test + storage inventory |
| T10 | Background process survives window close | No tray, no service, no startup task; lifetime coordinator stops pollers and disposes WebView; process-tree audit | Lifecycle tests + manual audit |
| T11 | Secrets/personal paths committed to Git | `.gitignore` before artifacts; ignored `.instadm-local.env`; pre-commit scan; CI scan | `tools/privacy-audit.ps1` (also runs in CI) |
| T12 | Remote debugging/DevTools exposed in Release | Compile-time gating on build configuration; scanner checks | Release config review + scanner |
| T13 | CI receives live account data | CI runs synthetic tests only; no secrets configured; no artifact upload of runtime data | Workflow review |
| T14 | Crash dumps auto-uploaded | WebView2 crash reporting disabled via environment options; no crash SDK; OS-level WER documented as independent OS activity | Runtime audit + docs |
| T15 | External link handoff leaks referrer/cookies | Default block; optional explicit user-confirmed open in system browser passes bare URL only | Code review + tests |

## 4. Non-threats (out of app-process boundary, documented separately)

- Independent Windows OS activity (Windows Update, time sync, WER) that the app
  neither launches nor supplies with app data. Documented in
  `docs/NETWORK_AUDIT.md`; never used to excuse app-initiated egress.
- Instagram/Meta's own server-side processing of the user's account — inherent
  to using Instagram at all.

## 5. Release-blocking privacy gates

Release is BLOCKED unless all of the following hold (evidence in
`docs/NETWORK_AUDIT.md` and `docs/RELEASE_CHECKLIST.md`):

1. **G1:** Runtime network audit of a Release build shows app-owned processes
   contacting only Instagram/Meta-owned endpoints during normal operation.
2. **G2:** All configurable WebView2 data-sharing features (SmartScreen,
   autofill, password autosave, browser sign-in, tracking-prevention exceptions,
   crash reporting) are disabled or proven inert; any remaining un-disableable
   non-Meta connection from app-owned processes = no release on this runtime.
3. **G3:** Static scans pass: no telemetry SDKs, no forbidden endpoints, no
   sensitive-value logging patterns, no secrets/personal paths staged.
4. **G4:** DevTools and remote debugging disabled in Release configuration.
5. **G5:** Storage inventory complete; "Clear Instagram data and sign out"
   verified to remove the dedicated profile's cookies/cache/site data.
6. **G6:** Process-tree audit: closing the last window terminates all app-owned
   processes, timers, and pollers.
7. **G7:** No real account data, captures, screenshots, or personal paths exist
   in Git history or release artifacts.

## 6. Known open questions

- WebView2's *required* diagnostic data behavior in current stable runtimes must
  be measured, not assumed (drives G1/G2). See M2 spike and M11 audit tasks.
- Windows taskbar badge fidelity vs macOS Dock badge (privacy-neutral; UX only).
