# Changelog

All notable changes to this project are documented here. Dates use
`YYYY-MM-DD`.

## [Unreleased]

## [0.1.0] - 2026-07-22

First public **preview** release of InstaDM for Windows — a focused
Instagram direct-messages client using WinUI 3 + WebView2.

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
  (self-contained, unpackaged). No auto-updater.

### Intentional behavior / limitations

- Closing the last window quits the app (no tray, no startup task).
- External-browser handoff for non-Instagram links is **disabled** until it
  can open a bare URL with no cookies/headers/referrer.
- Shared Posts navigation stays unavailable; inline shared media in threads
  still works when Instagram renders it.
- Runtime network audit (`docs/NETWORK_AUDIT.md`) is **not** complete —
  do not treat this preview as proving Instagram-only traffic yet.

### Requirements

- Windows 10 version 1809+ or Windows 11 (x64)
- WebView2 Runtime (Evergreen; typically already installed on Windows 11)

---

## Pre-release development

Milestones M0–M9 and M10 core landed before the v0.1.0 tag. See
`docs/IMPLEMENTATION_PLAN.md` and `docs/HANDOFF.md`.
