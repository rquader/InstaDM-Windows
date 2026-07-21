# InstaDM for Windows — Decision Log (ADRs)

Format: newest last. Each entry: context, decision, evidence, alternatives,
privacy impact, status.

---

## ADR-001 — Repository bootstrap order and continuity protocol

- **Date:** 2026-07-22
- **Status:** Accepted
- **Context:** Model sessions can stop at any time; private reference material
  (macOS source, Obsidian notes) must never leak into the repo.
- **Decision:** Ignore rules (`.gitignore`, `.cursorignore`) and the ignored
  `.instadm-local.env` (holding personal absolute paths) are created before any
  other artifact. Continuity docs (`AGENTS.md`, Cursor rule, `docs/*`) are
  committed before implementation. Two synchronized handoff copies are
  maintained: `docs/HANDOFF.md` (authoritative, committed) and
  `Windows App/HANDOFF.md` (private Obsidian journal copy).
- **Privacy impact:** Prevents accidental commit/index of secrets, personal
  paths, and runtime user data.

---

## ADR-002 — Repository name and visibility

- **Date:** 2026-07-22
- **Status:** Accepted
- **Context:** Local folder is `InstagramDMOnlyAppForWindows`; the prompt
  suggests `InstaDM-Windows` as the default repository name.
- **Decision:** Future GitHub repository name `InstaDM-Windows`, **private** by
  default. The local folder name is irrelevant to the remote name. No push
  until initial docs, ignore rules, and a secret scan pass. Note: `gh` token on
  this host is currently invalid (keyring); remote creation deferred until the
  user refreshes auth — recorded as a blocker, not a stopping condition.
- **Privacy impact:** Private visibility prevents premature exposure.

---

(Later ADRs — architecture selection, navigation policy shape, auth state
machine, notification design — are appended below as they are made.)
