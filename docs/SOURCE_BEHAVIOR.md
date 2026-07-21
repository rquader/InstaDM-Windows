# InstaDM for Windows — Source-Derived Behavior Specification

Derived 2026-07-22 from three evidence classes:

- **Notes** — the private design/development journal for the macOS app
  (inventoried in full; distilled here, never copied).
- **Local code** — the macOS source working tree, verified clean at tag
  `v1.0.1` (HEAD `009252f`), identical to `origin/main`.
- **Public repo** — https://github.com/rquader/InstaDM (same commit; README,
  CHANGELOG, release history). **No local/public divergence exists**, so no
  conflict resolution was needed (recorded in `docs/DECISIONS.md` ADR-003).

Status values: `preserve exactly` | `adapt natively` | `defer` | `intentionally omit`.

---

## B1 — DM-only product shape

- **Intent:** a calm, private client for Instagram DMs and group chats and
  *nothing else*; intentional friction against feed/Explore/Reels/Stories.
- **Evidence:** notes (philosophy), README, code (`NavigationPolicy`).
- **macOS impl:** WKWebView pinned to Instagram's official web DM UI; layered
  navigation allowlist is the real defense; CSS hiding is cosmetic only.
- **Windows strategy:** same shape — WebView2 hosting instagram.com, canonical
  navigation policy in `InstaDM.Core`, document-start JS guard, cosmetic CSS.
- **Confidence:** high. **Status: preserve exactly.**

## B2 — Layered navigation policy (three distinct questions)

- **Intent:** never conflate "may this URL be requested at all" with "may this
  URL become the visible page".
- **macOS impl:** `isAllowed(url, source:)` (network layer, includes XHR
  `/api`, `/graphql`, `/ajax`, `/static`), `isInAppUserSurface(path, source:)`
  (main-document gate), `isDirectMessagingPath(path)` (narrow DM routes).
- **Hard-won lessons:**
  - Blanket `/accounts` leaked notifications/activity/edit → narrowed to
    explicit auth subpaths (`/accounts/login`, `/onetap`, `/password`,
    `/signup`, `/emailsignup`, `/check_email`, `/logout`, `/confirm`,
    `/access`, `/account_recovery`, `/username`, `/two_factor`).
  - Bare `/direct` is the minimized-messenger shell rendering full IG — must
    NOT be a DM path. Only `/direct/inbox`, `/direct/t/…`, `/direct/new`.
  - Internal endpoints allowed as subresources must be cancelled if they try
    to become the main document.
- **Windows strategy:** reproduce as pure C# `NavigationPolicy` with explicit
  decision reasons; WebView2 `NavigationStarting` (main frame),
  `FrameNavigationStarting`, and `WebResourceRequested` (only if needed) map
  onto the layers.
- **Confidence:** high. **Status: preserve exactly.**

## B3 — Directory-boundary path matching

- **Intent:** `/p` must never match `/profile/`; `/direct` never `/directory`;
  `/api` never `/api-status`; `/accounts/login` never `/accounts/login_aux`.
- **macOS impl:** `pathMatches` requires exact match or `prefix + "/"`. Was a
  critical bug fix (2026-05-16 audit).
- **Known residual defect in source (do NOT port):** `isDirectMessagingPath`
  itself still uses raw `hasPrefix` (audit finding L1); and the JS guard's
  `matchesPrefix` uses `indexOf(...)===0` raw substring (finding L3), so the
  Swift and JS matchers can disagree. **Windows fix:** one boundary-anchored
  matcher semantic in C#, and the generated JS uses the identical rule.
- **Confidence:** high. **Status: adapt natively (fixing known defects).**

## B4 — Host handling

- **macOS impl:** allowlist {`www.instagram.com`, `instagram.com`,
  `accounts.instagram.com`}; `accounts.instagram.com` is auth-only and passes
  on host alone. Known defect M1: host comparison is case-sensitive in some
  paths and lowercased in others → inconsistent (fails closed).
- **Windows strategy:** single canonicalizer — lowercase host, strip trailing
  dot, https-only, reject userinfo@ smuggling, reject non-allowlisted hosts,
  treat punycode/Unicode lookalikes conservatively (fail closed). Adversarial
  tests required (`evil-instagram.com`, `instagram.com.evil.com`,
  `WWW.Instagram.COM`, `xn--…`).
- **Confidence:** high. **Status: adapt natively (fixing known defects).**

## B5 — Auth surfaces and the auth stand-down

- **Intent:** login, one-tap, 2FA, checkpoint/challenge, recovery, logout must
  work with zero interference; containment must stand down there.
- **macOS impl:** `authSurfacePathPrefixes` = auth account subpaths +
  `/challenge` + `/auth_platform`. On auth surfaces: policy allows everything,
  the JS guard does not install at all (pristine JS environment), and the
  cookie watcher is armed.
- **Hard-won lessons (critical):**
  - **`/auth_platform` missing broke fresh login entirely** — the policy
    cancelled `/auth_platform/recaptcha/` (error −999) and the spinner hung
    forever. If Instagram adds a new top-level auth path, the same failure
    recurs; the remedy is adding the prefix, never re-broadening `/accounts`.
  - Guessing post-login redirects broke login repeatedly. Final model:
    **during auth, don't interfere at all; poll the cookie store; when a
    `sessionid` cookie exists and we're not on a DM surface, load the inbox.**
    Generation-guarded polling (0.4 s, capped ~6 min), idempotent arm/stop.
  - Post-login redirect through `/` (feed) must be tolerated briefly so
    cookies commit; never load the inbox before Set-Cookie lands or the user
    bounces back to login.
  - Instagram treated an incomplete UA as an automated client (login spinner);
    macOS appends Safari version tokens. Windows: WebView2's default UA is a
    real Edge/Chrome UA, so this specific hazard likely doesn't apply — verify
    during live testing; never fake an unusual UA.
- **Windows strategy:** explicit authentication state machine in Core
  (`Initializing / Unauthenticated / Authenticating / ChallengeInProgress /
  AuthenticatedPendingInbox / AuthenticatedInMessages / SessionExpired /
  Recovering / FatalWebRuntimeFailure`), cookie-existence watcher via
  WebView2 `CookieManager` (existence only, value never read into logs/state;
  the API surface must make value access impossible from policy code).
- **Confidence:** high. **Status: preserve behavior, adapt natively.**

## B6 — SPA containment: document-start JS guard

- **Intent:** Instagram is a SPA; profile clicks while the messenger is
  minimized call `preventDefault()` + `history.pushState()` themselves — **no
  navigation event ever reaches native code**. URL policy alone cannot block
  this, in principle.
- **macOS impl:** document-start user script (main frame only): capture-phase
  `click`/`auxclick` listener blocks link-like targets resolving to
  non-allowed paths before React's delegated handler; wraps
  `history.pushState`/`replaceState` to drop disallowed SPA transitions;
  same-host + http(s)-only resolution; stands down entirely on auth surfaces;
  re-installs on every new document. Side effect (accepted): messenger
  "minimize" button becomes a no-op, which preserves DM-only intent.
  Deliberate scope: `location.assign/replace/href` and `window.open` are NOT
  wrapped — they fire real navigation events handled natively.
- **Hard-won lessons:** guard must precede Instagram's bundle (document
  start); guard must NOT install on auth pages (wrapping history on the login
  page stalled login); per-tab allowlists (Requests tab cannot click into DMs
  or feed); JS and native allowlists must be generated from one source.
- **Windows strategy:** `AddScriptToExecuteOnDocumentCreatedAsync` with a
  script whose policy data is generated by `PolicyScriptBuilder` from the
  canonical C# policy. Test against a local synthetic SPA harness.
- **Confidence:** high. **Status: preserve exactly (single-sourced).**

## B7 — Blocked-navigation handling and recovery

- **Intent:** contain escapes without breaking normal messaging.
- **Hard-won lessons (the oscillation war, must not be relearned):**
  - `stopLoading()` is global — it aborts thread-history pagination XHR, not
    just the blocked navigation. Never call it for blocked incidental
    navigations while a DM surface is visible.
  - Instagram fires incidental background hops (`/`, explore, account-link
    prefetches, profile *prefetches*) while the user sits on DMs — these must
    be cancelled silently, with no recovery action, or the app thrashes
    (heal-thrash audit finding H1: heal actions must run only for
    `linkActivated`-class navigations, background prefetches drop silently).
  - Same-thread re-navigations (`.other` type on the same `/direct/t/…`)
    reload and snap scroll to bottom — suppress, but only after the first
    successful settle (`hasSettledOnUserSurface`), never during cold launch
    (caused white-screen-on-launch).
  - Recovery reload must be debounced/coalesced; bounce cooldown and a
    nil-URL loop counter prevent infinite reload loops when the home URL
    itself 302s into a blocked URL before anything commits.
  - Track last-valid DM URL; rebound there, not always to the inbox root.
  - Open external browser only for explicit link clicks, never for `.other`
    (fast scrolling opened random Safari tabs — user hated it).
  - In-page chrome-dismiss (synthetic Escape/click Close/back) is a narrow
    best-effort layer, rate-limited; became largely redundant after the JS
    guard existed. Windows: implement recovery ladder without it initially;
    add only if live testing shows stuck overlays.
- **Windows strategy:** `NavigationCoordinator` in Core implementing the
  decision ladder as a pure, testable state machine; WebView2 host translates
  events. Cooldown/debounce/settled-state guards are explicit named concepts
  with unit tests.
- **Confidence:** high. **Status: preserve behavior, adapt natively.**

## B8 — Popups, external links, downloads, permissions

- **macOS impl:** `createWebViewWith` returns nil (no in-app popups) and
  routes the URL to the default browser, gated by a Settings toggle
  (`openLinksInExternalBrowser`, default ON to match user's chosen behavior);
  auth/challenge flows may always open externally (Facebook/Meta OAuth).
  Known defect L2 (do not port): popup URLs weren't scheme-checked.
- **Windows strategy:** `NewWindowRequested` → mark handled, never create a
  child WebView; external open only for http/https, only per settings toggle,
  bare URL only (no cookies/headers/referrer). Downloads: block by default
  (`DownloadStarting` → cancel). Permissions: default-deny camera, mic,
  geolocation, clipboard-read, notifications, and all device APIs.
- **Confidence:** high. **Status: preserve behavior, adapt natively (fix L2).**

## B9 — Cosmetic shell (CSS hiding)

- **Intent:** remove visible temptation (left rail: Home, Search, Explore,
  Reels, Notifications, Create, Threads links); never a security boundary.
- **macOS impl:** `href`-pattern selectors (durable) + English `aria-label`
  fallbacks; whole-rail `:has()` selector; injected at document end (accepted
  brief flash). Instagram DOM drift expected every few months.
- **Windows strategy:** same CSS approach, injected via document-created
  script; keep minimal; do not restyle message content; expect drift and keep
  the selectors in one versioned file (`Web/cosmetic-shell.css`).
- **Confidence:** high. **Status: preserve exactly.**

## B10 — Notifications and unread state

- **Intent:** privacy-first: generic text only, never sender/preview content.
- **macOS impl:** poll `document.title` every 15/30/60/120 s (default 30);
  parse `"(N) Inbox • Instagram"` → N, any other title containing
  "Instagram" → 0, else nil (leave state alone — prevents false-zero badge
  collapse); baseline is nil on attach/level-change so pre-existing unreads
  never fire a banner; notify only on increase; suppress when app active +
  window key + Messages surface visible; dock badge shows count; permission
  denial silently demotes level to badge-only (no nagging); level Off stops
  the timer and clears delivered notifications; timers die with the window.
  Levels shipped: Off / Badge only / Standard (generic banner). Full-preview
  was designed but intentionally NOT shipped.
- **Known defects (fix on Windows):** M2 comma-grouped counts `"(1,234)"`
  parse to nil (strip separators); L9 `"(3 new…"` without `)` falls through
  to a false 0 (a leading `(` must be numeric-authoritative → nil).
- **Windows strategy:** same title-poll design via `ExecuteScriptAsync
  ("document.title")` or the `DocumentTitle` property + `DocumentTitleChanged`
  event (prefer the event + poll hybrid; decide in M10). Windows App SDK
  toast notifications with generic text; taskbar badge via `TaskbarItemInfo`
  overlay/badge equivalent — research exact Windows fidelity and document
  honestly (macOS Dock badge has no perfect twin).
- **Confidence:** high. **Status: preserve behavior, adapt natively.**

## B11 — Optional surfaces (feature modules)

- **Intent:** every non-DM surface is opt-in, isolated, and cleanly removable.
- **macOS impl:** `FollowRequests` (compile-time `available=true`, runtime
  default OFF; `/accounts/activity` + dedicated tab; tab has its own narrow
  JS-guard allowlist) and `SharedPosts` (compile-time `available=false` —
  disabled because Instagram renders shares inline or via `window.open`, so
  in-app rendering was a no-op; runtime default was `true`, audit L11 says it
  should be `false` before ever re-enabling). SharedPosts is source-gated:
  only from `/direct/*` frames.
- **Windows strategy:** same pattern as Core feature classes with
  `Available` (compile-time) + `Enabled` (settings) + `AllowedPathPrefixes`.
  FollowRequests: available, default off. SharedPosts: **not available** in
  the first release (matches source), source-gating logic implemented and
  tested anyway so enabling is a one-flag change after live verification.
- **Confidence:** high. **Status: preserve exactly.**

## B12 — App lifecycle

- **Intent:** the app runs only while the user wants it: closing the last
  window quits fully; no background process, tray agent, launch-at-login,
  or post-exit notifications.
- **macOS impl:** `applicationShouldTerminateAfterLastWindowClosed = true`;
  polling timer bound to app lifetime.
- **Windows strategy:** single main window; on close, stop pollers, dispose
  WebView2, exit the process; no tray icon, no startup task, no service.
  Verify the WebView2 runtime child processes terminate (process-tree audit).
- **Confidence:** high. **Status: preserve exactly.**

## B13 — Settings and storage

- **macOS impl:** UserDefaults only: notification level, sound, poll
  interval, color-scheme pref, feature toggles, external-browser toggle.
  Cookies in the default persistent WebKit store. Nothing else persisted;
  deliberately no message archive ("let Instagram be the storage" — storing
  content raises the privacy bar sharply).
- **Windows strategy:** JSON settings in `%LOCALAPPDATA%\InstaDM` (local,
  non-roaming); dedicated WebView2 user-data folder under the same root;
  document every persisted value; `Clear Instagram data and sign out` via
  `ClearBrowsingDataAsync` + state reset; separate `Reset app settings`.
- **Confidence:** high. **Status: preserve behavior, adapt natively.**

## B14 — Theming and native chrome

- **macOS impl:** Sage palette only in the shipped app (three-palette system
  was built, then deliberately simplified — "three-theme picking turned out
  to be over-engineered"); System/Light/Dark preference; native controls
  tinted; Instagram's surface never themed; standard title bar; 800×600
  minimum; window title "DMs"; calm anti-doomscroll visual tone; no custom
  fonts, no animations beyond defaults.
- **Windows strategy:** native Windows chrome (Mica/title bar per chosen
  framework), Sage accent for native controls, System/Light/Dark for native
  chrome only, ~800×600 minimum (verify on Windows DPI), no scope expansion.
- **Confidence:** high. **Status: adapt natively.**

## B15 — Privacy posture

- **Intent (verbatim constraint):** nothing leaves the machine except traffic
  to Instagram itself. No telemetry, analytics, crash reporting, cloud sync,
  third-party SDKs, or logging of message content/cookies. Zero third-party
  dependencies is what makes "everything local" verifiable, not aspirational.
- **macOS impl:** Apple frameworks only; audit confirmed zero non-IG egress;
  debug diagnostics compiled out of Release (audit nit: debug logs included
  full thread URLs — Windows must log redacted paths/enums only, even in
  Debug).
- **Windows strategy:** first-party platform only (Windows App SDK/.NET/
  WebView2); the WebView2 runtime's own network behavior becomes the main new
  risk (macOS's WebKit had no analogous phone-home concern) — this drives the
  M2 spike and the M11 release-gating audit.
- **Confidence:** high. **Status: preserve exactly (stricter where needed).**

## B16 — Failed experiments (do NOT retry without new evidence)

- WKContentRuleList-style network-layer document blocking (fought delegate
  policy, broke login timing) → Windows analog: do not build a
  `WebResourceRequested`-based main-document blocker as the primary defense.
- Early-commit abort (`didCommit` stopLoading during launch/auth) → broke
  login and cold launch.
- In-app profile viewing feature (DMProfiles) → full IG chrome in-app;
  profiles go to the external browser or nowhere.
- Blanket `/accounts` or `/direct` allowlists.
- Global allow on nil/synthetic request info.
- Opening the external browser for non-click navigations.

## B17 — Known macOS-only workarounds (not needed on Windows)

- `safeRequest` KVC nullability workaround for macOS 26 WebKit nil requests —
  WebView2's API is C#-native; no analog. Windows equivalent lesson: treat
  event args defensively; navigation events may carry unexpected/empty URIs;
  fail closed but never crash.
- `applicationNameForUserAgent` Safari-token append — WebView2 already sends
  a complete Chromium UA. Do not modify the UA unless live testing proves a
  need.
- `:has()` CSS support gating (macOS 14+) — Chromium supports `:has()`;
  keep the fallback per-item selectors anyway.

## First-release scope freeze

**In scope:** B1–B15 as specified: Messages-first window, auth flows, DM
containment + recovery, generic notifications (Off/Badge-ish/Standard),
Settings (appearance, notifications, allowed surfaces, external links),
FollowRequests optional surface (default off), clear-data, private packaging.

**Out of scope (first release):** SharedPosts in-app rendering (compile-time
unavailable, matching source), full-preview notifications, per-thread mute /
quiet hours / VIP threads, native chat UI, multi-account, iOS/mobile anything,
App Store distribution, auto-update.

**Non-goals (permanent, from source notes):** feed/Explore/Reels/Stories
support, cloud sync, message archive/export, analytics of any kind, posting/
commenting/liking, background/tray operation.
