# InstaDM for Windows — Active Handoff

1. **Last updated:** 2026-07-22 03:50 (UTC+6)
2. **Phase / milestone / task:** M5 (SPA harness + guard) complete → committing; next M6 (WebView2 host)
3. **Current objective:** Commit the containment guard and harness, then
   implement the privacy-hardened WebView2 host (M6) in `InstaDM.App`.
4. **Repository state:** branch `main`, HEAD `b615fd2` (M4 commit), no remote.
5. **Working tree:** new guard + harness files and CI/plan updates,
   intentional, M5 commit pending.
6. **Files changed:** `src/InstaDM.App/Web/containment-guard.js`
   (document-start guard: factory pattern, capture-phase click/auxclick,
   history wrap, auth stand-down, fail-closed, `__INSTADM_POLICY__` splice
   point, fixed-schema bridge messages with coarse categories only);
   `tests/Fixtures/local-spa-harness/fake-dom.js` (fake window: capture/
   bubble dispatch, committing History API, nested links, webview bridge);
   `tests/Fixtures/local-spa-harness/policy.default.json` (pinned builder
   output); `tests/InstaDM.WebHarness.Tests/guard.test.js` (25 tests);
   `tests/InstaDM.Core.Tests/PolicyFixtureDriftTests.cs` (+ csproj fixture
   copy); `.github/workflows/windows-ci.yml` (node --test step);
   `docs/IMPLEMENTATION_PLAN.md` (M4/M5 status + notes).
7. **Completed since last checkpoint:** M4 committed (`b615fd2`); full M5.
8. **Recent commands:** `node --test tests/InstaDM.WebHarness.Tests/
   guard.test.js` — **25/25 PASSED**; `dotnet test …Core.Tests…` —
   **170/170 PASSED**.
9. **Test/build status:** C# 170 pass (macOS), JS harness 25 pass (Node 25,
   CI uses runner's Node). `InstaDM.App`: NOT RUN - WINDOWS ENVIRONMENT
   REQUIRED. `tools/privacy-audit.ps1`: CI-only.
10. **Decisions:** harness is a fake-DOM Node module instead of a browser
    HTML fixture (models exactly what the guard touches; zero browser deps;
    real WebView2 validation deferred to M6+ on Windows); popstate not
    intercepted (recovery layer's job); malformed policy payload deactivates
    the guard rather than half-judging (native policy stays authoritative);
    guard never receives SharedPosts prefixes — native-only source-gating.
11. **Privacy status:** clean. Guard reads no DOM text/inputs/cookies; walks
    parentNode chain for `<a href>` only; bridge schema `{v, source, kind,
    surface}` with enum-like strings; tests assert no URLs in reports.
12. **Known blockers:** `gh` token invalid → no remote/CI runs yet.
13. **Next action:** commit M5 (`test: add local SPA harness for early
    navigation containment`), then M6: `src/InstaDM.App/Controls/
    InstagramWebViewHost.xaml(.cs)` + `Services/WebViewHostService` —
    explicit `CoreWebView2Environment` with dedicated `%LOCALAPPDATA%`-style
    user-data folder, privacy settings (SmartScreen/autofill/password-save/
    tracking-prevention/SSO per docs/DECISIONS.md ADR-006), event wiring,
    `AddScriptToExecuteOnDocumentCreatedAsync` with
    `PolicyScriptBuilder.InjectIntoScript`, clear-browsing-data, process-
    failure handling. Core-side logic (host config model) testable on macOS;
    WinUI wiring validated by CI compile.
14. **Recovery:** if interrupted, re-run both test commands from item 8.
15. **Next commit boundary:** now. Message:
    `test: add local SPA harness for early navigation containment`
16. **Handoff synchronization status:** IN SYNC via `tools/sync-handoff.sh`.
