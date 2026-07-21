# Security Policy

## Reporting a vulnerability

If you believe you have found a security or privacy issue in InstaDM for
Windows, please open a **private** security advisory on GitHub (Security →
Advisories → New draft advisory) or contact the maintainer through GitHub.

**Do not** include any of the following in issues, advisories, PRs, or logs:

- Instagram credentials or 2FA codes
- Cookie values, session tokens, CSRF tokens, or auth headers
- Message content, sender names, usernames, media, or screenshots of an
  account
- Packet captures, HAR files, or full sensitive URLs
- Contents of `%LOCALAPPDATA%\InstaDM` or the WebView2 user-data folder

Describe the issue with synthetic fixtures and coarse categories only
(for example: “session cookie existence check incorrectly logged a value”).

## Scope

In scope: this repository’s application-owned code, settings storage, WebView2
host configuration, containment guard, CI, and documentation claims.

Out of scope: Instagram/Meta’s own web client behavior, independent Windows
OS services, and WebView2/Edge runtime behavior that the app cannot disable
(tracked in `docs/NETWORK_AUDIT.md` and release-blocking until resolved).

## Privacy promise

Application-owned data must stay on the user’s machine. The only intended
network traffic from app-owned processes is first-party Instagram/Meta traffic
from the embedded official web client. There is no analytics, telemetry SDK,
crash upload, remote logging, cloud sync, remote config, push backend, or
auto-updater in this codebase. See `docs/PRIVACY_THREAT_MODEL.md`.

The stronger claim that **runtime** traffic is only Instagram/Meta is made
only after `docs/NETWORK_AUDIT.md` is completed for a Release build.
