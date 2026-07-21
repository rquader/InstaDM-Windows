# InstaDM for Windows — Active Handoff

1. **Last updated:** 2026-07-22 06:00 (UTC+6)
2. **Phase / milestone / task:** Retag as v1.0.0 + final safety strip (no PDBs)
3. **Current objective:** Commit safety hardenings; publish `v1.0.0`; delete
   superseded `v0.1.0`.
4. **Repository state:** public `rquader/InstaDM-Windows`, branch `main`.
5. **Working tree:** version rename + manifest asInvoker + PDB strip + SHA256.
6. **Files changed:** CHANGELOG, release.yml, app.manifest, csproj Version,
   README caveats, HANDOFF.
7. **Completed since last checkpoint:** Audited v0.1.0 zip — found shippable
   `.pdb` files (local path leak risk); stripping in release pipeline.
8. **Recent commands:** downloaded/inspected `InstaDM-win-x64.zip`; Core tests.
9. **Test/build status:** Core tests pending this commit; release job on tag.
10. **Decisions:** Public tag is **v1.0.0** (parity with macOS naming); not a
    prerelease flag; honest NETWORK_AUDIT caveat remains.
11. **Privacy/safety status:** no admin elevation; no HttpClient/telemetry in
    app code; storage LocalAppData only; PDBs stripped from zips.
12. **Known residual risks (documented, not ignored):** unsigned SmartScreen
    warning; Instagram/Meta traffic via WebView2 (intended); platform
    WebView2/OS diagnostics until M11 audit closes.
13. **Next action:** commit, push, tag `v1.0.0`, delete `v0.1.0`, watch release.
14. **Recovery:** `gh release list --repo rquader/InstaDM-Windows`.
15. **Next commit boundary:** now.
16. **Handoff synchronization status:** sync after commit.
