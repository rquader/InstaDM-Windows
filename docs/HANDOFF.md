# InstaDM for Windows — Active Handoff

1. **Last updated:** 2026-07-22 02:35 (UTC+6)
2. **Phase / milestone / task:** M3 (solution scaffold) complete → committing; next M4 (navigation policy)
3. **Current objective:** Commit the buildable scaffold, then implement the
   canonical navigation policy in `InstaDM.Core` with the adversarial test
   table (M4).
4. **Repository state:** branch `main`, HEAD `4e45e8e` (M2 commit), no remote.
5. **Working tree:** new solution scaffold, intentional, M3 commit pending.
6. **Files changed:** `InstaDM.Windows.slnx` (new .NET slnx format),
   `global.json` (SDK 10.0.302, rollForward latestFeature),
   `Directory.Build.props` (nullable, warnings-as-errors),
   `src/InstaDM.Core/` (csproj + `Navigation/InstagramSurface.cs` taxonomy),
   `src/InstaDM.App/` (minimal WinUI 3: csproj targeting
   net10.0-windows10.0.26100.0, WindowsAppSDK 2.3.1, unpackaged
   self-contained, app.manifest PerMonitorV2, App/MainWindow placeholder),
   `tests/InstaDM.Core.Tests/` (MSTest, first-party), `.github/workflows/
   windows-ci.yml` (windows-latest, SHA-pinned actions, no secrets, privacy
   scan step), `tools/privacy-audit.ps1` (forbidden packages/endpoints/
   logging/staged-files scanner), `README.md`.
7. **Completed since last checkpoint:** installed .NET SDK 10.0.302 (LTS)
   into ignored `.dotnet/`; scaffolded all projects; local build + test of
   `InstaDM.Core.Tests`: **1/1 PASSED**.
8. **Recent commands:** `dotnet new sln` (produced `.slnx`; references
   updated), `dotnet sln add …`, `dotnet build`, `dotnet test` (pass).
9. **Test/build status:** Core+Tests: PASS locally (macOS). `InstaDM.App`:
   NOT RUN - WINDOWS ENVIRONMENT REQUIRED (validated by CI once a remote
   exists; `EnableWindowsTargeting` set for local restore only).
   `tools/privacy-audit.ps1`: NOT RUN locally (no pwsh on host); runs in CI.
10. **Decisions:** unpackaged self-contained WinAppSDK deployment for the
    private preview (packaging finalized M12); MSTest chosen (first-party).
11. **Privacy status:** clean; CI has no secrets, uploads no artifacts;
    scanner added; `DOTNET_CLI_TELEMETRY_OPTOUT=1` used for local dotnet.
12. **Known blockers:** `gh` token invalid → no remote/CI runs yet;
    pwsh absent locally (scanner CI-only for now).
13. **Next action:** commit M3 (`build: scaffold native Windows solution and
    tests`), then M4: implement `UrlCanonicalizer`, `PathMatcher`,
    `NavigationPolicy`, `NavigationDecision`, feature gates, and
    `PolicyScriptBuilder` in `src/InstaDM.Core/Navigation/` with the
    adversarial test table from `docs/TEST_MATRIX.md` §1.
14. **Recovery:** if a build was interrupted, re-run
    `./.dotnet/dotnet test tests/InstaDM.Core.Tests/InstaDM.Core.Tests.csproj`.
15. **Next commit boundary:** now. Message:
    `build: scaffold native Windows solution and tests`
16. **Handoff synchronization status:** IN SYNC via `tools/sync-handoff.sh`.
