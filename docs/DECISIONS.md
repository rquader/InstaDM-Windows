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

## ADR-003 — Source evidence hierarchy and local/public reconciliation

- **Date:** 2026-07-22
- **Status:** Accepted
- **Context:** The prompt requires comparing the local macOS source with the
  public GitHub repository and resolving differences.
- **Decision:** Verified the local source working tree is clean at tag
  `v1.0.1` (HEAD `009252f`) and identical to `origin/main` of
  https://github.com/rquader/InstaDM — **no divergence exists**. Evidence
  hierarchy used for `docs/SOURCE_BEHAVIOR.md`: notes = product intent,
  local code at `v1.0.1` = implemented behavior, public README/CHANGELOG/git
  history = supporting evidence.
- **Privacy impact:** none; only sanitized distillations recorded.

---

## ADR-004 — Port behavior, not defects: known macOS bugs fixed by design

- **Date:** 2026-07-22
- **Status:** Accepted
- **Context:** A read-only audit of the macOS source (2026-06-04, recorded in
  the reference notes) verified 17 code findings. The Windows port must not
  faithfully reproduce known defects.
- **Decision:** The Windows implementation fixes by construction: boundary-
  anchored matching everywhere including DM paths and the generated JS guard
  (findings L1/L3); one lowercasing host canonicalizer (M1); popup scheme
  checks (L2); comma-grouped and malformed title parsing (M2/L9); heal
  actions gated to explicit link activations, background prefetches dropped
  silently (H1); guard installs unconditionally with runtime auth stand-down
  rather than skipping installation (L5 — decide finally in M5 with harness
  evidence); weak/cancellable timers and disposal (L6/L7); SharedPosts
  default-disabled at both compile-time and runtime (L11); debug diagnostics
  log redacted paths/enums, never full thread URLs (L12 nit).
- **Privacy impact:** positive — several findings are privacy/containment
  hardening.

(Later ADRs — architecture selection, navigation policy shape, auth state
machine, notification design — are appended below as they are made.)
