# InstaDM for Windows — Active Handoff

1. **Last updated:** 2026-07-22 05:35 (UTC+6)
2. **Phase / milestone / task:** Public safety hardenings; CI watch next
3. **Current objective:** Commit and push public-safety fixes; confirm
   windows-latest CI; then M10.4 / M11 on Windows.
4. **Repository state:** branch `main`, public `rquader/InstaDM-Windows`,
   secret scanning + push protection enabled.
5. **Working tree:** safety fixes uncommitted (README, SECURITY.md, cookie
   probe, external-links default, popup scheme gate, ADR-002, scanner).
6. **Files changed:** see pending commit.
7. **Completed since last checkpoint:** Repo made **public** by user request;
   public-safety audit fixes prepared; Core tests 248 pass.
8. **Recent commands:** `gh repo edit … --visibility public`; Core tests PASSED.
9. **Test/build status:** Core 248 pass; App/CI pending confirmation.
10. **Decisions:** ADR-002 updated — public visibility; honest pre-release
    network claims in README/SECURITY.md.
11. **Privacy status:** no personal paths in history scan; cookie probe fails
    closed without propagating exceptions; external-browser handoff disabled
    until safe.
12. **Known blockers:** M11 runtime audit still required before "only
    Instagram traffic" release claim.
13. **Next action:** commit + push safety fixes; `gh run watch` CI.
14. **Recovery:** `gh repo view rquader/InstaDM-Windows --json visibility`.
15. **Next commit boundary:** now —
    `security: harden public-repo privacy claims and cookie probe`.
16. **Handoff synchronization status:** sync after this write.
