# InstaDM for Windows — Active Handoff

1. **Last updated:** 2026-07-22 03:20 (UTC+6)
2. **Phase / milestone / task:** M4 (navigation policy) complete → committing; next M5 (SPA harness + guard)
3. **Current objective:** Commit the tested navigation policy, then build the
   local SPA containment harness and the document-start guard (M5).
4. **Repository state:** branch `main`, HEAD `48d11ed` (M3 commit), no remote.
5. **Working tree:** 11 new files (7 policy sources, 4 test suites),
   intentional, M4 commit pending.
6. **Files changed:** `src/InstaDM.Core/Navigation/` — `CanonicalUrl.cs`,
   `UrlCanonicalizer.cs` (https-only, lowercase host, no userinfo/punycode/
   unicode hosts, 443 only, RFC 3986 dot-segment resolution, encoded-slash/
   backslash fail-closed), `PathMatcher.cs` (directory-boundary semantics,
   single matcher shared with JS), `InstagramSurface.cs` (taxonomy, committed
   in M3), `NavigationDecision.cs` (+ `DecisionReason`, `NavigationContext`),
   `PolicyOptions.cs` (FollowRequests/SharedPosts gates, default off),
   `NavigationPolicy.cs` (layered: network request → user surface → helpers;
   `IsIncidentalBlockedPrefetch`, `ShouldRecoverFromMainDocument`),
   `PolicyScriptBuilder.cs` (versioned JSON payload, `__INSTADM_POLICY__`
   placeholder splice, SharedPosts prefixes deliberately never exported).
   `tests/InstaDM.Core.Tests/` — `UrlCanonicalizerTests.cs`,
   `PathMatcherTests.cs`, `NavigationPolicyTests.cs`,
   `PolicyScriptBuilderTests.cs`.
7. **Completed since last checkpoint:** M3 committed (`48d11ed`); full M4
   implementation and adversarial test table.
8. **Recent commands:** `dotnet test tests/InstaDM.Core.Tests/…` —
   **169/169 PASSED** (macOS, net10.0).
9. **Test/build status:** Core tests PASS locally. `InstaDM.App`: NOT RUN -
   WINDOWS ENVIRONMENT REQUIRED. `tools/privacy-audit.ps1`: CI-only (no pwsh
   on host).
10. **Decisions:** dot segments are RFC 3986-resolved (browser-equivalent)
    before judgment rather than rejected — tests pin the resolution; JS guard
    receives no SharedPosts prefixes (source-gating is native-only, guard
    fails closed); MSTest 4 requires `[TestMethod]` on data-driven tests
    (`DataTestMethod` obsolete = build error under warnings-as-errors).
11. **Privacy status:** clean. Policy payload contains only static hostnames/
    path prefixes; test asserts absence of cookie/session/csrf/authorization/
    bearer markers. Decision reasons are coarse enums, never raw URLs.
12. **Known blockers:** `gh` token invalid → no remote/CI runs yet.
13. **Next action:** commit M4 (`feat: implement tested Instagram navigation
    policy`), then M5: create `tests/Fixtures/local-spa-harness/` (links,
    capture/bubble handlers, pushState/replaceState/popstate, frames,
    popups, pagination-like fetches) and
    `src/InstaDM.App/Web/containment-guard.js` consuming the
    `__INSTADM_POLICY__` payload, with Node-based harness tests.
14. **Recovery:** if interrupted, re-run
    `./.dotnet/dotnet test tests/InstaDM.Core.Tests/InstaDM.Core.Tests.csproj`.
15. **Next commit boundary:** now. Message:
    `feat: implement tested Instagram navigation policy`
16. **Handoff synchronization status:** IN SYNC via `tools/sync-handoff.sh`.
