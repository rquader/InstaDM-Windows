# InstaDM for Windows — Active Handoff

1. **Last updated:** 2026-07-22 04:40 (UTC+6)
2. **Phase / milestone / task:** M6 (WebView2 host) implemented → committing; then remote + first CI run
3. **Current objective:** Commit the privacy-hardened host, create the
   private GitHub remote (`gh` auth now valid), push, and use windows-latest
   CI to validate the `InstaDM.App` compile.
4. **Repository state:** branch `main`, HEAD `f694247` (M5 commit), no remote yet.
5. **Working tree:** M6 files, intentional, commit pending.
6. **Files changed:** `src/InstaDM.Core/WebHost/WebViewHostConfiguration.cs`
   (ADR-006 as pinned, tested data), `src/InstaDM.Core/WebHost/
   GuardMessage.cs` (exact-schema bridge parser — security boundary),
   `src/InstaDM.App/Controls/InstagramWebViewHost.xaml(.cs)` (ordered init:
   environment → settings → guard splice → events → navigate; one named
   invariant per handler; process-failure recovery capped with
   reset-on-success), `MainWindow.xaml` (hosts the control),
   `InstaDM.App.csproj` (guard template as Content),
   `tests/InstaDM.Core.Tests/WebViewHostConfigurationTests.cs`,
   `GuardMessageTests.cs`, `docs/IMPLEMENTATION_PLAN.md` (M6 IN PROGRESS).
7. **Completed since last checkpoint:** M5 committed (`f694247`); M6
   implementation + tests.
8. **Recent commands:** `dotnet test …Core.Tests…` — **192/192 PASSED**;
   `dotnet build src/InstaDM.App…` on macOS — fails in XamlCompiler
   (EXPECTED: Windows required; C# analysis clean up to that point);
   `gh auth status` — **valid** (account rquader, repo scope).
9. **Test/build status:** Core 192 pass (macOS); JS harness 25 pass;
   `InstaDM.App`: NOT RUN - WINDOWS ENVIRONMENT REQUIRED (next: CI).
10. **Decisions:** popups never spawn windows — allowed targets navigate
    in-view, rest dropped; permissions default-deny (`Handled = true`);
    downloads cancelled silently; context menus + zoom stay enabled
    (chat UX/accessibility); browser accelerator keys disabled.
11. **Privacy status:** clean. All ADR-006 values pinned by tests; no cookie
    value, URL, or content is read by the host; web messages pass an
    exact-schema parser before anything consumes them.
12. **Known blockers:** none — `gh` auth restored.
13. **Next action:** commit M6 (`feat: add privacy-hardened embedded
    Instagram host`), then `gh repo create InstaDM-Windows --private
    --source . --push`, then `gh run watch` the Windows CI; fix any App
    compile errors it finds (likely WinRT event-signature details in
    `InstagramWebViewHost.xaml.cs`).
14. **Recovery:** if interrupted mid-push, `git remote -v` then
    `gh run list --limit 3` to see where things stopped.
15. **Next commit boundary:** now. Message:
    `feat: add privacy-hardened embedded Instagram host`
16. **Handoff synchronization status:** IN SYNC via `tools/sync-handoff.sh`.
