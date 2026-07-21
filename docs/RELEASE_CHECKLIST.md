# InstaDM for Windows — Release Checklist

A release may not ship unless every item is checked with evidence. Privacy
blockers mean **no release**, never a softened claim.

## Build and test

- [ ] Fresh clone builds from documented steps (Debug and Release).
- [ ] All automated tests pass (`InstaDM.Core.Tests`, harness tests) with
      synthetic fixtures only.
- [ ] Windows CI green on the release commit.
- [ ] Windows-environment tests executed on a real Windows machine/VM and
      recorded in `docs/TEST_MATRIX.md` (no `NOT RUN` items in required rows).

## Privacy gates (from `docs/PRIVACY_THREAT_MODEL.md` §5)

- [ ] G1: Runtime network audit (Release) — app-owned processes contact only
      Instagram/Meta endpoints. Evidence in `docs/NETWORK_AUDIT.md`.
- [ ] G2: All WebView2 data-sharing features disabled/proven inert; no
      un-disableable non-Meta egress remains.
- [ ] G3: Static privacy scans pass (`tools/privacy-audit.ps1`,
      `verify-no-forbidden-endpoints.ps1`, `verify-no-sensitive-logging.ps1`).
- [ ] G4: Release config disables DevTools and remote debugging.
- [ ] G5: Storage inventory current; clear-data verified on Windows.
- [ ] G6: Process-tree audit — closing last window ends all app-owned work.
- [ ] G7: No secrets, account data, captures, screenshots, personal paths in
      Git history or artifacts (history scan run).

## Product

- [ ] DM-only containment verified against manual acceptance matrix.
- [ ] Conversation pagination/scroll unaffected by containment.
- [ ] Notifications generic, baseline-safe, stop on exit.
- [ ] Closing last window terminates the process tree; no tray/startup/service.
- [ ] Settings local and non-roaming; documented in storage inventory.

## Distribution

- [ ] Signing requirements documented; no keys in repo.
- [ ] No auto-updater or remote update service present.
- [ ] Release notes accurate about privacy (claims match audit evidence).
- [ ] Repository visibility is intentional (public or private per maintainer).
- [ ] Unsigned SmartScreen / first-launch steps documented in README.

## Continuity

- [ ] `docs/HANDOFF.md` and `Windows App/HANDOFF.md` synchronized and final.
- [ ] `Windows App/Implementation Progress.md` reflects final milestone state.
- [ ] Known limitations documented.
