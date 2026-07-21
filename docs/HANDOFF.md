# InstaDM for Windows — Active Handoff

1. **Last updated:** 2026-07-22 05:55 (UTC+6)
2. **Phase / milestone / task:** M12 release packaging — cutting v0.1.0
3. **Current objective:** Ship tag-triggered release workflow + README Install
   steps (macOS-parallel); push `v0.1.0` and confirm `InstaDM-win-x64.zip`.
4. **Repository state:** public `rquader/InstaDM-Windows`, branch `main`.
5. **Working tree:** release workflow/README/CHANGELOG pending commit.
6. **Files changed:** `.github/workflows/release.yml`, `README.md`,
   `CHANGELOG.md`, `AssemblyName=InstaDM`, checklist tweak.
7. **Completed since last checkpoint:** Public safety hardenings; CI green.
8. **Recent commands:** preparing release artifacts via windows-latest.
9. **Test/build status:** prior CI green; release job pending tag push.
10. **Decisions:** First public tag is **v0.1.0 prerelease** (honest about
    open M11 audit); unsigned zip like macOS unsigned .app.
11. **Privacy status:** release uploads only publish output; no session data.
12. **Known blockers:** M11 audit still open for stronger traffic claim.
13. **Next action:** commit, push main, `git tag v0.1.0 && git push origin v0.1.0`,
    watch release workflow.
14. **Recovery:** `gh release list --repo rquader/InstaDM-Windows`.
15. **Next commit boundary:** now — release packaging docs/workflow.
16. **Handoff synchronization status:** sync after commit.
