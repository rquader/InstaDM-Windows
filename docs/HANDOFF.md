# InstaDM for Windows — Active Handoff

1. **Last updated:** 2026-07-22 03:55 (UTC+6)
2. **Phase / milestone / task:** M7 (Authentication) complete → next M8 recovery
3. **Current objective:** Commit the private authentication state machine, then
   implement M8 navigation recovery (last-valid-DM, escape cancellation without
   Stop()/reload thrash, same-thread suppression, cooldown).
4. **Repository state:** branch `main`, HEAD `497074d` (M6), no remote yet.
5. **Working tree:** M7 files intentional, commit pending.
6. **Files changed:** `src/InstaDM.Core/Authentication/*` (pure state machine +
   existence-only cookie watcher), `src/InstaDM.App/Services/
   WebViewSessionCookieProbe.cs` (name-only cookie check),
   `src/InstaDM.App/Controls/InstagramWebViewHost.xaml.cs` (auth wiring,
   process-failure routed through machine, clear-data + unload stop pollers),
   `tests/InstaDM.Core.Tests/AuthenticationStateMachineTests.cs`,
   `AuthSessionWatcherTests.cs`, plan/handoff/ADR updates.
7. **Completed since last checkpoint:** M6 already committed (`497074d`); M7
   implemented and tested; handoff reconciled (prior copy was stale at M6).
8. **Recent commands:** `dotnet test …Core.Tests…` — **217/217 PASSED**;
   Node harness — **25/25 PASSED**; Python privacy audit — **PASSED**;
   `bash tools/sync-handoff.sh` — used after this write.
9. **Test/build status:** Core 217 pass (macOS); JS harness 25 pass;
   `InstaDM.App`: NOT RUN - WINDOWS ENVIRONMENT REQUIRED.
10. **Decisions:** ADR-007 — auth completion is cookie EXISTENCE only; clear-
    data escapes Fatal; process-failure budget owned by the state machine.
11. **Privacy status:** clean. Cookie probe enumerates names only; no values
    logged/retained. Guard messages remain coarse categories.
12. **Known blockers:** `gh` auth token invalid on this host (keyring) —
    private remote + Windows CI deferred until user refreshes
    `gh auth refresh -h github.com`. Obsidian/Windows notes reachable.
13. **Next action:** commit M7 (`feat: implement private authentication state
    machine`), then start M8 `NavigationRecoveryCoordinator` in Core with
    table-driven tests before wiring the host.
14. **Recovery:** if interrupted mid-commit, `git status` — M7 files should be
    staged or uncommitted; re-run Core tests before committing.
15. **Next commit boundary:** now. Message:
    `feat: implement private authentication state machine`
16. **Handoff synchronization status:** will be IN SYNC after
    `tools/sync-handoff.sh` following this write.
