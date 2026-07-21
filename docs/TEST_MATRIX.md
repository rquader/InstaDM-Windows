# InstaDM for Windows — Test Matrix

Status legend: `PASS` | `FAIL` | `NOT RUN` | `NOT RUN - WINDOWS ENVIRONMENT REQUIRED` | `MANUAL ONLY`

No test may use a live Instagram account, real DOM, real cookies, or real
message data. All automated tests use synthetic fixtures.

## 1. Automated — Navigation policy (`InstaDM.Core.Tests`) — runs on any host

| Area | Cases | Status |
| --- | --- | --- |
| Allowed hosts | `www.instagram.com`, `instagram.com` normalization; rejected lookalikes (`instagram.com.evil.com`, `xn--` punycode) | NOT RUN |
| Schemes | https allowed; http/javascript/data/file/custom rejected | NOT RUN |
| DM surfaces | `/direct/inbox`, `/direct/t/{id}`, `/direct/new` allowed; bare `/direct` per source disposition | NOT RUN |
| Auth surfaces | login, one-tap, recovery, 2FA, challenge, logout narrow allows; unrelated `/accounts/...` blocked | NOT RUN |
| Path boundaries | `/p` vs `/profile`; trailing slash; repeated slashes | NOT RUN |
| Encodings | encoded slashes/dots, case, ports, fragments, queries | NOT RUN |
| Malformed | unparseable URLs fail closed | NOT RUN |
| Feature gates | Requests/SharedPosts on/off; source-context gating | NOT RUN |
| Policy generation | C# policy → JS payload equivalence | NOT RUN |

## 2. Automated — JS guard via local SPA harness — runs on any host with Node

Capture-phase click; modified/aux click; nested elements in links;
relative/absolute URLs; pushState/replaceState before/after fixture handlers;
popstate; auth stand-down; dynamic links; iframes; popups; **no extraction of
text/input values**. Status: NOT RUN

## 3. Automated — Authentication state machine (fake adapters)

Fresh unauthenticated start; delayed cookie appearance; duplicate events;
challenge flow; success; expiry; logout; clear-data; cancellation; WebView
recreation; shutdown. Status: NOT RUN

## 4. Automated — Recovery and lifecycle

Top-level escape; incidental/subresource denial; same-thread navigation; rapid
repeated blocks; cooldown expiry; simulated pagination; deactivate/reactivate;
window close; process failure; restart. Status: NOT RUN

## 5. Automated — Notifications

Title parsing (known/unknown formats); nil baseline; zero/nonzero transitions;
increase/decrease; re-enable baseline; foreground suppression combinations;
permission denial; duplicate-timer prevention; shutdown cancellation.
Status: NOT RUN

## 6. Automated — Privacy scans (also in CI)

Forbidden SDK/endpoint scan; sensitive-logging pattern scan; staged-secrets
scan; Release DevTools/remote-debugging config check. Status: NOT RUN

Limitations: static scans cannot prove runtime network behavior; that requires
the M11 runtime audit on Windows.

## 7. Windows-environment-required (automated where possible, else manual)

| Area | Status |
| --- | --- |
| WebView2 host initialization, event ordering, script injection order | NOT RUN - WINDOWS ENVIRONMENT REQUIRED |
| Clear-browsing-data verification | NOT RUN - WINDOWS ENVIRONMENT REQUIRED |
| Packaging, title bar, DPI, theme, accessibility | NOT RUN - WINDOWS ENVIRONMENT REQUIRED |
| Toast notifications and taskbar indicator | NOT RUN - WINDOWS ENVIRONMENT REQUIRED |
| Process-tree/lifecycle audit after close | NOT RUN - WINDOWS ENVIRONMENT REQUIRED |
| Runtime network audit (Release) | NOT RUN - WINDOWS ENVIRONMENT REQUIRED |

## 8. Manual live-account acceptance matrix — MANUAL ONLY, never automated

The user enters credentials manually into Instagram's own page; prefer a
non-sensitive test account; never record the session; no screenshots containing
account data.

Fresh login · invalid login presentation · one-tap login · 2FA ·
checkpoint/challenge · password recovery · session persistence across restart ·
session expiry · logout/relogin · inbox load · 1:1 thread · group thread ·
new-thread flow · send/receive · attachment/media where supported · inline
shared post/reel rendering · long-thread upward pagination + scroll
preservation · feed/profile/Explore/Reels/Stories/post escape attempts ·
back/forward · popups/external links · Requests on/off · generic notifications
+ foreground suppression · offline/DNS failure/process failure · clear data +
clean relogin · close-window termination.

All: NOT RUN.
