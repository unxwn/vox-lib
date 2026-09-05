#!/usr/bin/env bash
# Snapshot Claude Code state (sessions, memory, settings, credentials) from the
# named volume onto the workspace volume, so it survives even `docker volume rm`
# / "Clean Up Dev Containers". Runs automatically on container start; safe to run
# by hand any time:  bash .devcontainer/backup-claude.sh
set -euo pipefail

SRC="$HOME/.claude"
DEST_ROOT=/workspaces
KEEP=10

[ -d "$SRC/projects" ] || { echo "[backup-claude] nothing to back up yet"; exit 0; }

STAMP="$(date +%Y%m%d-%H%M%S)"
DEST="$DEST_ROOT/.claude-backup-$STAMP"

# Skip if the newest existing backup is byte-identical in session content.
NEWEST="$(ls -1td "$DEST_ROOT"/.claude-backup-* 2>/dev/null | head -1 || true)"
CUR_SUM="$(find "$SRC/projects" -type f -name '*.jsonl' -exec md5sum {} + 2>/dev/null | sort | md5sum | cut -d' ' -f1)"
if [ -n "$NEWEST" ] && [ -f "$NEWEST/.sessions.md5" ] && [ "$(cat "$NEWEST/.sessions.md5")" = "$CUR_SUM" ]; then
  echo "[backup-claude] no session changes since $NEWEST, skipping"
  exit 0
fi

mkdir -p "$DEST"; chmod 700 "$DEST"
tar -C "$HOME" -cf "$DEST/claude-home.tar" .claude
chmod 600 "$DEST/claude-home.tar"
printf '%s' "$CUR_SUM" > "$DEST/.sessions.md5"
cp /commandhistory/.bash_history "$DEST/bash_history" 2>/dev/null || true
echo "$DEST" > "$DEST_ROOT/.claude-backup-latest"

# rotate
ls -1td "$DEST_ROOT"/.claude-backup-* 2>/dev/null | tail -n +$((KEEP + 1)) | xargs -r rm -rf

echo "[backup-claude] snapshot -> $DEST ($(du -sh "$DEST" | cut -f1))"
