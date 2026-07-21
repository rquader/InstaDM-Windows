#!/usr/bin/env bash
# Synchronize docs/HANDOFF.md -> "$INSTADM_WINDOWS_NOTES_DIR/HANDOFF.md" and verify.
# Reads paths from the ignored .instadm-local.env. Never prints private paths' contents.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ENV_FILE="$REPO_ROOT/.instadm-local.env"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "ERROR: .instadm-local.env not found. Handoff sync FAILED." >&2
  exit 1
fi

# shellcheck disable=SC1090
source "$ENV_FILE"

if [[ -z "${INSTADM_WINDOWS_NOTES_DIR:-}" ]]; then
  echo "ERROR: INSTADM_WINDOWS_NOTES_DIR not set. Handoff sync FAILED." >&2
  exit 1
fi

SRC="$REPO_ROOT/docs/HANDOFF.md"
DST="$INSTADM_WINDOWS_NOTES_DIR/HANDOFF.md"

mkdir -p "$INSTADM_WINDOWS_NOTES_DIR"

# Atomic-ish write: temp file in destination dir, then move into place.
TMP="$(mktemp "$INSTADM_WINDOWS_NOTES_DIR/.handoff.XXXXXX")"
cp "$SRC" "$TMP"
mv "$TMP" "$DST"

if cmp -s "$SRC" "$DST"; then
  echo "Handoff synchronized: repository and Windows App copies match."
else
  echo "ERROR: handoff copies DIFFER after sync. Repair immediately." >&2
  exit 1
fi
