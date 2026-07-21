# InstaDM for Windows

A focused, calm, private **Instagram direct-messages-only** client for
Windows, inspired by [InstaDM for macOS](https://github.com/rquader/InstaDM).

The app embeds Instagram's own authenticated web experience in WebView2 and
contains everything that isn't messaging: no feed, no Explore, no Reels, no
Stories, no profile browsing. It is a messaging utility, not an Instagram
browser.

## Privacy invariant (absolute)

Everything the app owns — settings, state, diagnostics, storage — stays on
this machine. App-owned code never adds analytics, telemetry, crash upload,
remote logging, cloud sync, remote configuration, a push backend, an
auto-updater, or a developer server — and never will. See
`docs/PRIVACY_THREAT_MODEL.md`.

**Runtime network claim:** we intend that app-owned processes contact only
first-party Instagram/Meta endpoints required by Instagram’s official web
client. That stronger claim is **not** asserted for a release until
`docs/NETWORK_AUDIT.md` proves it for the shipped WebView2 configuration.
Until then, treat the app as pre-release with an open audit gate.

## Building

Prerequisites: .NET SDK per `global.json` (currently 10.0.3xx LTS).
The core library and its tests build on any OS; the app itself targets
Windows.

```bash
# Any host: core logic + tests
dotnet test tests/InstaDM.Core.Tests/InstaDM.Core.Tests.csproj

# Windows: full app (x64). Core/Tests use AnyCPU; do not pass Platform=x64
# to the .slnx — it has no Release|x64 mapping.
dotnet build src/InstaDM.App/InstaDM.App.csproj -c Release -p:Platform=x64
dotnet test tests/InstaDM.Core.Tests/InstaDM.Core.Tests.csproj -c Release
```

CI (`.github/workflows/windows-ci.yml`) restores, builds, and tests on
`windows-latest` with synthetic fixtures only — no secrets, no live account.

## Repository map

| Path | Purpose |
| --- | --- |
| `src/InstaDM.Core` | Pure policy/state machines (navigation, auth, notifications, privacy) — cross-platform, fully unit-tested |
| `src/InstaDM.App` | WinUI 3 shell + privacy-hardened WebView2 host (Windows-only) |
| `tests/` | Synthetic tests; no live Instagram anything |
| `tools/` | Privacy scanners and handoff sync |
| `docs/` | Authoritative project record — start with `docs/HANDOFF.md` |

## For agents / contributors

Read `AGENTS.md` first. It defines the session-resume procedure, the privacy
rules, and the documentation map.

## Status

Pre-release; under active development. See `docs/IMPLEMENTATION_PLAN.md` for
milestone status. Report security/privacy issues per `SECURITY.md` — never
paste live account data into GitHub.

## Disclaimer

Not affiliated with Instagram or Meta. Use at your own risk; the embedded
web client may change without notice. Users are responsible for compliance
with Instagram's Terms of Service.
