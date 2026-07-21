# InstaDM for Windows — Active Handoff

1. **Last updated:** 2026-07-22 04:35 (UTC+6)
2. **Phase / milestone / task:** M9 shell done; M10 notification core done;
   Windows toast host still pending
3. **Current objective:** Commit M9 + M10.core, then wire WinUI toasts/taskbar
   (needs Windows) or continue docs/scanners/README polish if staying on macOS.
4. **Repository state:** branch `main`, HEAD `529ae24` (M8), M9/M10.core
   uncommitted.
5. **Working tree:** intentional M9 shell + M10 Core notification files.
6. **Files changed:** Settings/Lifecycle/Notifications Core; SettingsView;
   MainWindow nav shell; cosmetic CSS; App lifecycle; tests (248 total).
7. **Completed since last checkpoint:** M8 committed; M9 shell/settings;
   M10 parser/state machine/poller.
8. **Recent commands:** `dotnet test …Core.Tests…` — **248/248 PASSED**.
9. **Test/build status:** Core 248 pass; App/UI toasts:
   NOT RUN - WINDOWS ENVIRONMENT REQUIRED.
10. **Decisions:** settings local-only JSON; Follow Requests next-launch;
    close window → Lifecycle.Shutdown + Exit (no tray).
11. **Privacy status:** clean. Settings JSON forbids secret field names (tested).
12. **Known blockers:** `gh` auth keyring invalid — no private remote/CI yet.
13. **Next action:** commit M9 then M10.core; remaining M10.4–5 need Windows
    or stay documented as pending.
14. **Recovery:** `dotnet test tests/InstaDM.Core.Tests`.
15. **Next commit boundary:** two commits —
    `feat: add focused native Windows shell and local settings` then
    `feat: add local generic unread notification core`
16. **Handoff synchronization status:** sync after this write.
