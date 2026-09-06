#!/usr/bin/env bash
# Runs once, after the dev container is created. Must be idempotent, because a rebuild
# re-runs it against volumes that may already be populated.
set -euo pipefail

log() { printf '\033[1;34m[post-create]\033[0m %s\n' "$*"; }
warn() { printf '\033[1;33m[post-create]\033[0m %s\n' "$*"; }

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
USER_NAME="$(id -un)"
OLD_KEY="-workspaces-dotnet"      # pre-rename Claude project key
NEW_KEY="-workspaces-vox-lib"

# ---------------------------------------------------------------------------
# 1. Fix ownership of freshly-created named volumes (Docker creates them root-owned)
# ---------------------------------------------------------------------------
log "Fixing volume ownership..."
for d in "$HOME/.claude" /commandhistory "$HOME/.nuget/packages"; do
  if [ -d "$d" ] && [ "$(stat -c '%U' "$d")" != "$USER_NAME" ]; then
    sudo chown -R "$USER_NAME:$USER_NAME" "$d"
    log "  chowned $d"
  fi
done
chmod 700 "$HOME/.claude" 2>/dev/null || true

# ---------------------------------------------------------------------------
# 2. Restore Claude history/credentials into a fresh volume, once
# ---------------------------------------------------------------------------
RESTORE_MARKER="$HOME/.claude/.restored-from-backup"
if [ ! -f "$RESTORE_MARKER" ] && [ ! -d "$HOME/.claude/projects" ]; then
  # newest backup produced by backup-claude.sh (or the pre-migration snapshot)
  BACKUP_TAR="$(ls -1t /workspaces/.claude-backup-*/claude-home.tar 2>/dev/null | head -1 || true)"
  if [ -n "$BACKUP_TAR" ]; then
    log "Restoring Claude state from $BACKUP_TAR"
    tmp="$(mktemp -d)"
    tar -C "$tmp" -xf "$BACKUP_TAR"
    # copy contents of the archived .claude/ into the mounted volume
    cp -a "$tmp/.claude/." "$HOME/.claude/"
    rm -rf "$tmp"

    # Migrate the project-history key from the old folder name to the new one so
    # /resume lists past sessions under /workspaces/vox-lib.
    P="$HOME/.claude/projects"
    if [ -d "$P/$OLD_KEY" ]; then
      if [ -d "$P/$NEW_KEY" ]; then
        cp -an "$P/$OLD_KEY/." "$P/$NEW_KEY/" 2>/dev/null || true
        rm -rf "$P/$OLD_KEY"
      else
        mv "$P/$OLD_KEY" "$P/$NEW_KEY"
      fi
      # rewrite the recorded cwd inside transcripts so resumed sessions land right
      find "$P/$NEW_KEY" -name '*.jsonl' -print0 \
        | xargs -0 -r sed -i 's#/workspaces/dotnet#/workspaces/vox-lib#g'
      log "  migrated session history $OLD_KEY -> $NEW_KEY"
    fi
    chmod 600 "$HOME/.claude/.credentials.json" 2>/dev/null || true
    touch "$RESTORE_MARKER"
    log "  Claude state restored."
  else
    warn "No backup found under /workspaces/.claude-backup-*/, starting fresh."
  fi
else
  log "Claude volume already populated, leaving it alone."
fi

# ---------------------------------------------------------------------------
# 3. Persistent shell history
# ---------------------------------------------------------------------------
touch /commandhistory/.bash_history 2>/dev/null || true
if ! grep -q 'commandhistory' "$HOME/.bashrc" 2>/dev/null; then
  cat >> "$HOME/.bashrc" <<'BRC'

# --- persistent shell history (mounted volume) ---
export HISTFILE=/commandhistory/.bash_history
export HISTSIZE=100000
export HISTFILESIZE=200000
export HISTCONTROL=ignoreboth:erasedups
shopt -s histappend
PROMPT_COMMAND="history -a; ${PROMPT_COMMAND:-}"
BRC
  log "Wired persistent bash history into ~/.bashrc"
fi

# ---------------------------------------------------------------------------
# 4. pnpm store on the workspace filesystem (hardlinks cannot cross filesystems)
# ---------------------------------------------------------------------------
STORE=/workspaces/.pnpm-store
mkdir -p "$STORE"
pnpm config set store-dir "$STORE" >/dev/null 2>&1 || true
log "pnpm store-dir -> $STORE (same fs as workspace, so hardlinks work)"
# pnpm writes this to ~/.config/pnpm/config.yaml, which lives on overlayfs and is
# therefore lost on rebuild -- hence re-applying it on every create.

# ---------------------------------------------------------------------------
# 5. git
# ---------------------------------------------------------------------------
git config --global --add safe.directory "$REPO_ROOT" 2>/dev/null || true
git config --global init.defaultBranch main 2>/dev/null || true
git config --global pull.rebase true 2>/dev/null || true

# The workspace is a bind mount whose files surface as mode 777 inside the
# container, so git sees a spurious 644 -> 755 change on every tracked file.
# Ignoring the mode bit locally keeps `git status` honest; the three scripts
# that genuinely need +x are already recorded as 100755 in the index.
git -C "$REPO_ROOT" config core.filemode false 2>/dev/null || true

# ---------------------------------------------------------------------------
# 6. Dependencies + local HTTPS dev certificate
# ---------------------------------------------------------------------------
if [ -f "$REPO_ROOT/backend/VoxLib.slnx" ]; then
  log "dotnet restore..."
  dotnet restore "$REPO_ROOT/backend/VoxLib.slnx" || warn "dotnet restore failed"
fi
dotnet dev-certs https --trust >/dev/null 2>&1 || true

if [ -f "$REPO_ROOT/frontend/package.json" ]; then
  log "pnpm install..."
  (cd "$REPO_ROOT/frontend" && pnpm install) || warn "pnpm install failed"
fi

log "Done. node $(node --version) / pnpm $(pnpm --version) / dotnet $(dotnet --version)"
