# InstaDM for Windows — Active Handoff

1. **Last updated:** 2026-07-22 04:15 (UTC+6)
2. **Phase / milestone / task:** M8 (Navigation recovery) complete → next M9 shell
3. **Current objective:** Commit M8 recovery coordinator, then implement M9
   native shell (Messages/Settings, local settings store, theme, lifecycle quit).
4. **Repository state:** branch `main`, HEAD `47e9480` (M7), M8 uncommitted.
5. **Working tree:** M8 files intentional, commit pending.
6. **Files changed:** `NavigationRecoveryCoordinator.cs` + tests; host wiring
   for initiator/recovery/guard absorb/reset; ADR-008; plan/handoff.
7. **Completed since last checkpoint:** M7 committed; M8 implemented (231 Core
   tests pass).
8. **Recent commands:** `dotnet test …Core.Tests…` — **231/231 PASSED**.
9. **Test/build status:** Core 231 pass; JS harness unchanged 25 pass;
   `InstaDM.App`: NOT RUN - WINDOWS ENVIRONMENT REQUIRED.
10. **Decisions:** ADR-008 — recovery without Stop()/reload; user-initiated
    escapes rebound; Other initiator cancels silently.
11. **Privacy status:** clean. Last-valid DM URL in memory only for now.
12. **Known blockers:** `gh` auth keyring invalid — private remote + Windows CI
    deferred until `gh auth refresh -h github.com`.
13. **Next action:** commit M8, then scaffold M9 settings store + Settings view.
14. **Recovery:** re-run Core tests; ensure no WebView `Stop()` on blocked nav.
15. **Next commit boundary:** now. Message:
    `feat: contain Instagram escapes without breaking chat history`
16. **Handoff synchronization status:** will be IN SYNC after sync-handoff.sh.
