# InstaDM for Windows — Active Handoff

1. **Last updated:** 2026-07-22 01:55 (UTC+6)
2. **Phase / milestone / task:** Phase 1 complete → committing M1; next Phase 2 (M2 architecture/privacy spike)
3. **Current objective:** Source archaeology finished. Commit the distilled
   behavior spec, then perform the architecture comparison (WinUI 3 vs WPF vs
   alternative) and the WebView2 privacy research, recording the ADR.
4. **Repository state:** branch `main`, HEAD `244c95a` (initial safeguards
   commit), no remote.
5. **Working tree:** modified `docs/SOURCE_BEHAVIOR.md` (full behavior spec),
   `docs/DECISIONS.md` (ADR-003, ADR-004) — intentional, to be committed as M1.
6. **Files changed:** `docs/SOURCE_BEHAVIOR.md` — 17 behavior entries B1–B17 +
   scope freeze; `docs/DECISIONS.md` — evidence hierarchy + defect-fix policy.
7. **Completed since last checkpoint:** read ALL reference notes (19 root
   notes: philosophy, architecture, privacy, notifications, theming, risks,
   roadmap, and all dated session records incl. the fresh-login/auth_platform
   fix, post-ship regression war, and the 2026-06-04 read-only audit with its
   17 verified findings); read all 10 macOS Swift sources (NavigationPolicy,
   WebView + coordinator + JS guard, NotificationManager, Settings, feature
   modules, ContentView, App, AppDelegate); verified local tree clean at
   v1.0.1 == public origin/main (no divergence).
8. **Recent commands:** `git log/status/remote/tag` in the macOS source
   (read-only) — clean at `009252f`, tags v1.0.0/v1.0.1.
9. **Test/build status:** not applicable (docs only so far).
10. **Decisions:** ADR-003 (evidence hierarchy; no local/public divergence),
    ADR-004 (fix known audited defects by design, do not port them).
11. **Privacy status:** clean. Notes distilled, never copied; no personal
    paths, credentials, or private content in the repo. Public repo URL cited
    (public information).
12. **Known blockers:** `gh` auth token invalid (remote/CI deferred);
    `dotnet` not installed yet (M3.2); Windows-interactive tests need a
    Windows machine.
13. **Next action:** `git add -A && git commit -m "docs: derive Windows
    requirements from InstaDM sources"`, then M2: research current stable
    Windows App SDK / WebView2 privacy configuration (EnvironmentOptions,
    SmartScreen, autofill, crash reporting, telemetry) and write ADR-005
    architecture selection.
14. **Recovery:** nothing destructive pending; re-run `tools/sync-handoff.sh`
    if interrupted.
15. **Next commit boundary:** now. Message:
    `docs: derive Windows requirements from InstaDM sources`
16. **Handoff synchronization status:** IN SYNC after running
    `tools/sync-handoff.sh` (verified byte-identical).
