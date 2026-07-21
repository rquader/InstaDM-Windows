# Changelog

All notable changes to this project are documented here. Dates use
`YYYY-MM-DD`.

## [Unreleased]

## [1.0.0] - 2026-07-22

First public release of InstaDM for Windows — a focused Instagram
direct-messages client using WinUI 3 + WebView2. (Supersedes the brief
`v0.1.0` tag.)

### Highlights

- **DM-only navigation** — C# `NavigationPolicy` is the single source of
  truth; a document-start JavaScript guard blocks SPA click/`pushState`
  leaks. Cosmetic CSS hides left-rail distractions (not a security layer).
- **Privacy-hardened WebView2 host** — dedicated local user-data folder,
  SmartScreen/reputation off for the app process, password autosave and
  autofill off, extensions and OS SSO off, DevTools Debug-only, no remote
  debugging.
- **Authentication state machine** — login completion uses session-cookie
  *existence* only (never the value); clear-data escapes fatal runtime
  failure.
- **Recovery without Stop()/reload** — incidental prefetches cancel
  silently; user escapes rebound to the last settled DM URL.
- **Native shell** — Messages / Settings; optional Follow Requests (off by
  default, applies on next launch); Sage accent on native chrome; local
  JSON settings under `%LOCALAPPDATA%\InstaDM`.
- **Unread notification core** — title parser + baseline state machine
  (WinUI toast/taskbar host wiring still pending).
- **Distribution** — unsigned `InstaDM-win-x64.zip` via GitHub Releases
  (self-contained, unpackaged). Runs as a normal user (`asInvoker`). No
  auto-updater, no installer, no startup task, no tray resident.

### Intentional behavior / limitations

- Closing the last window quits the app (no tray, no startup task).
- External-browser handoff for non-Instagram links is **disabled** until it
  can open a bare URL with no cookies/headers/referrer.
- Shared Posts navigation stays unavailable; inline shared media in threads
  still works when Instagram renders it.
- Runtime network audit (`docs/NETWORK_AUDIT.md`) remains the gate for the
  stronger “only Instagram/Meta traffic from app-owned processes” claim —
  WebView2/OS components may still have platform diagnostics outside app
  control until that audit is finished.

### Requirements

- Windows 10 version 1809+ or Windows 11 (x64)
- WebView2 Runtime (Evergreen; typically already installed on Windows 11)

### Safety notes for downloaders

- The zip contains only the app and its self-contained .NET / Windows App
  SDK runtime files — no cookies, credentials, or account data.
- Debug symbol (`.pdb`) files are stripped from Release zips.
- First launch may show SmartScreen because the build is **unsigned** (same
  class of warning as the unsigned macOS `.app`). That is not malware; it
  means Windows has not seen a Microsoft-trusted signature yet.

---

## Pre-release development

Milestones M0–M9 and M10 core landed before the v1.0.0 tag. See
`docs/IMPLEMENTATION_PLAN.md` and `docs/HANDOFF.md`.
