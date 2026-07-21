# InstaDM for Windows

A native Windows app that's meant to give you Instagram **messaging and
nothing else** — direct messages and group chats, without the feed, reels,
explore, or stories. Inspired by
[InstaDM for macOS](https://github.com/rquader/InstaDM).

## What it is

A small WinUI 3 app that embeds Instagram's web client in **WebView2** and
enforces a **layered** navigation policy: a C# URL allowlist in
`NavigationPolicy` plus a document-start JavaScript guard that blocks SPA
click/`history.pushState` leaks Instagram's React bundle would otherwise
slip past native navigation events. Anything outside direct-messaging paths
(plus login / challenge / internal AJAX) stays blocked. Posts and reels
shared in your messages still render inline in the thread when Instagram
itself shows them there.

- **Privacy**: everything the app owns stays on your PC. The WebView profile
  lives under `%LOCALAPPDATA%\InstaDM\WebView2`; settings live in
  `%LOCALAPPDATA%\InstaDM\settings.json`. No analytics, no telemetry SDKs, no
  crash upload, no cloud sync, no auto-updater.
- **Dependencies**: Windows App SDK + WebView2 only (first-party). No
  third-party NuGet packages in the shipped app.
- **Lifecycle**: standard Windows app. Closing the window quits. Nothing
  runs in the background.

## Requirements

- Windows 10 (1809+) or Windows 11, **x64**
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)
  (Evergreen — usually already present on Windows 11)

## Install

1. Download **`InstaDM-win-x64.zip`** from the
   [**latest release**](https://github.com/rquader/InstaDM-Windows/releases/latest).
2. Unzip the folder somewhere convenient (for example
   `%LOCALAPPDATA%\Programs\InstaDM`). Replace the previous folder if you
   are upgrading.
3. Run **`InstaDM.exe`**.
4. **First launch only:** Windows SmartScreen may warn because the build is
   **unsigned**. Choose **More info** → **Run anyway**.
5. Log in with your Instagram credentials the first time; the session
   persists across launches in this app's private WebView profile.

There is no installer and no auto-updater — download a newer release zip when
you want to update.

## Features

- **DM-only navigation**: layered C# policy + document-start JS guard.
  Cosmetic CSS hides left-rail Home / Explore / Reels / Notifications links
  so you aren't tempted toward surfaces the policy blocks — that CSS is not
  a security layer and will drift when Instagram reshuffles its DOM.
- **Allowed-surfaces toggle** (opt-in): Follow Requests in Settings (off by
  default; applies the next time the app starts). Shared Posts navigation
  remains unavailable in this preview.
- **Appearance**: Sage accent on native chrome. System / Light / Dark for
  the Windows shell only — Instagram's page keeps its own theme.
- **Settings**: local-only preferences; Clear Instagram data and sign out;
  Reset app settings.
- **No background process**: closing the window fully quits the app.

### Intentional quirks

- **Messenger minimize is a no-op** when it would leave the DM surface — the
  same DM-only intent as the macOS app.
- **External non-Instagram links** are blocked for now (safer default until a
  cookie-free browser handoff is wired).

## Configuration

Open **Settings** in the left navigation:

- **Appearance**: System / Light / Dark (native chrome).
- **Notifications**: Off / Taskbar indicator / Generic banner — levels are
  stored now; WinUI toast/taskbar host wiring is still completing.
- **Surfaces**: Follow Requests opt-in.
- **Data**: clear this app's Instagram profile, or reset app settings.

All settings are stored under `%LOCALAPPDATA%\InstaDM\` on your machine and
never leave it.

## Caveats

- Instagram's web client is the source of truth. URL redesigns may require
  updates to the navigation policy, JS guard, or cosmetic CSS.
- This is a **preview** release. The stronger claim that app-owned processes
  contact only Instagram/Meta endpoints is **not** asserted until
  `docs/NETWORK_AUDIT.md` is completed for a Release build.
- For personal use. Do not use with multiple accounts in parallel.
- Unsigned builds will trip SmartScreen until code signing is added.

## Disclaimer

Not affiliated with Instagram or Meta. The "Instagram" name is used
descriptively only. "InstaDM" is descriptive of what the app does. Use at
your own risk. The app may break when Instagram changes its web client;
users are responsible for compliance with Instagram's Terms of Service.

---

## For developers

Everything below is for people who want to build from source or contribute.
End users should use the
[Releases](https://github.com/rquader/InstaDM-Windows/releases) download
described in **Install** above.

### Build from source

Requirements: Windows 10/11 x64 and .NET SDK per `global.json`
(currently 10.0.3xx).

```bash
dotnet test tests/InstaDM.Core.Tests/InstaDM.Core.Tests.csproj -c Release
dotnet publish src/InstaDM.App/InstaDM.App.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64
```

The Core library and its tests also build on macOS/Linux; the WinUI app
requires Windows (or the `windows-latest` CI).

### Continuous integration

- `.github/workflows/windows-ci.yml` — restore, Release build, synthetic
  tests, guard harness, privacy scan on every push/PR.
- `.github/workflows/release.yml` — on `v*` tags, publish
  `InstaDM-win-x64.zip` to a GitHub Release.

### Project map

| Path | Purpose |
| --- | --- |
| `src/InstaDM.Core` | Pure policy/state machines — cross-platform, unit-tested |
| `src/InstaDM.App` | WinUI 3 shell + privacy-hardened WebView2 host |
| `tests/` | Synthetic tests only (no live Instagram account) |
| `tools/` | Privacy scanners and handoff sync |
| `docs/` | Authoritative project record — start with `docs/HANDOFF.md` |
| `AGENTS.md` | Rules for automated contributors |

Report security/privacy issues per `SECURITY.md` — never paste live account
data into GitHub.
