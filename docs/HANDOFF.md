# InstaDM for Windows — Active Handoff

1. **Last updated:** 2026-07-22 02:05 (UTC+6)
2. **Phase / milestone / task:** M2 (architecture/privacy spike) complete → committing; next M3 (solution scaffold)
3. **Current objective:** Commit the architecture and WebView2 privacy ADRs,
   then scaffold the buildable solution: install .NET SDK locally, create
   `InstaDM.Core` + `InstaDM.App` + `InstaDM.Core.Tests`, and the Windows CI
   workflow.
4. **Repository state:** branch `main`, HEAD `ded4a66`
   ("docs: derive Windows requirements…"), no remote.
5. **Working tree:** modified `docs/DECISIONS.md` (ADR-005 architecture,
   ADR-006 WebView2 privacy), `docs/NETWORK_AUDIT.md` (planned runtime
   config) — intentional, M2 commit pending.
6. **Files changed:** as above, plus Obsidian `Windows App/Architecture and
   Tradeoffs.md` and `Decisions.md` updated with sanitized summaries.
7. **Completed since last checkpoint:** researched current stable tooling
   (Windows App SDK 2.3.1 stable, min OS Win10 1809) and WebView2 privacy
   controls (SmartScreen via environment flag + IsReputationCheckingRequired;
   IsCustomCrashReportingEnabled; autofill/password/extension/SSO toggles;
   documented that overall diagnostic data is not app-controllable → runtime
   audit is the binding gate). Wrote ADR-005 (WinUI 3 chosen, WPF fallback,
   third-party engines rejected) and ADR-006 (full configuration plan +
   release gate).
8. **Recent commands:** web research only; no builds.
9. **Test/build status:** N/A (docs only).
10. **Decisions:** ADR-005, ADR-006 in `docs/DECISIONS.md`.
11. **Privacy status:** clean; the WebView2 required-diagnostics question is
    explicitly tracked as a release-gating open item (G1/G2), not ignored.
12. **Known blockers:** `gh` token invalid (remote + Windows CI runs blocked
    until user refreshes; local work proceeds); `dotnet` not yet installed
    locally (next task M3.2); exact SDK/TFM pinned at M3 from installed
    tooling.
13. **Next action:** commit M2 (`docs: select Windows architecture and privacy
    boundary`), then M3.2: install .NET SDK (prefer current LTS) into
    `.dotnet/` or via system if present, then `dotnet new` the solution
    per the structure in `docs/IMPLEMENTATION_PLAN.md`.
14. **Recovery:** nothing destructive pending; re-run `tools/sync-handoff.sh`.
15. **Next commit boundary:** now. Message:
    `docs: select Windows architecture and privacy boundary`
16. **Handoff synchronization status:** IN SYNC via `tools/sync-handoff.sh`.
