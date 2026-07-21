# InstaDM for Windows — Active Handoff

1. **Last updated:** 2026-07-22 04:40 (UTC+6)
2. **Phase / milestone / task:** M0–M9 done; M10 core done; M10 WinUI toast +
   M11–M12 remain
3. **Current objective:** Wire Windows toast/taskbar (M10.4–5) when a Windows
   environment or CI is available; otherwise polish docs/scanners and keep
   Core green on macOS. User must refresh `gh` auth before private remote/CI.
4. **Repository state:** branch `main`, HEAD `49c94b0`, clean working tree,
   no remote.
5. **Working tree:** clean after M9 + M10.core commits.
6. **Files changed:** none pending.
7. **Completed since last checkpoint:** M8 recovery; M9 shell/settings;
   M10 unread parser/state machine/poller. Core tests **248/248**.
8. **Recent commands:** `dotnet test …Core.Tests…` — PASSED 248;
   commits `8f834e8`, `49c94b0`.
9. **Test/build status:** Core 248 pass (macOS); JS harness 25 pass;
   `InstaDM.App` compile + interactive WebView/toasts/network audit:
   NOT RUN - WINDOWS ENVIRONMENT REQUIRED.
10. **Decisions:** ADR-007 auth; ADR-008 recovery; local settings; no tray.
11. **Privacy status:** clean. Absolute invariant intact in Core + host design.
12. **Known blockers:**
    - `gh` auth token invalid (keyring) → cannot `gh repo create` / CI.
    - Full WebView2 runtime privacy audit (M11) needs Windows Release build.
13. **Next action:** User runs `gh auth refresh -h github.com`, then agent
    creates private `InstaDM-Windows` remote, pushes, and watches
    `windows-latest` CI. Meanwhile fallback-safe work: more notification
    parser cases, README polish, or M10.4 design notes in DECISIONS.
14. **Recovery:** `git log -5 --oneline`; `dotnet test tests/InstaDM.Core.Tests`.
15. **Next commit boundary:** after M10.4 WinUI wiring or after CI fixes.
16. **Handoff synchronization status:** IN SYNC after sync-handoff.sh.
