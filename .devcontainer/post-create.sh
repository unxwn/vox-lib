#!/usr/bin/env bash
# Runs once, after the dev container is created. Must be idempotent, because a
# rebuild re-runs it against volumes that may already be populated.
set -euo pipefail

log() { printf '\033[1;34m[post-create]\033[0m %s\n' "$*"; }
warn() { printf '\033[1;33m[post-create]\033[0m %s\n' "$*"; }

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
USER_NAME="$(id -un)"

# Docker creates a fresh named volume root-owned; the container user needs it.
log "Fixing volume ownership..."
for d in "$HOME/.claude" /commandhistory "$HOME/.nuget/packages"; do
  if [ -d "$d" ] && [ "$(stat -c '%U' "$d")" != "$USER_NAME" ]; then
    sudo chown -R "$USER_NAME:$USER_NAME" "$d"
    log "  chowned $d"
  fi
done
chmod 700 "$HOME/.claude" 2>/dev/null || true

# Persistent shell history.
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

# pnpm writes store-dir to ~/.config/pnpm/config.yaml, which is on overlayfs and
# is lost on rebuild, hence re-applying it every create. The store must sit on
# the same filesystem as node_modules, because pnpm hardlinks out of it, so it
# lives inside the workspace bind mount, not under /workspaces (which is the
# container's overlayfs, not a volume, despite the path looking persistent).
STORE="$REPO_ROOT/.pnpm-store"
mkdir -p "$STORE"
pnpm config set store-dir "$STORE" >/dev/null 2>&1 || true
log "pnpm store-dir -> $STORE"

git config --global --add safe.directory "$REPO_ROOT" 2>/dev/null || true
git config --global init.defaultBranch main 2>/dev/null || true
git config --global pull.rebase true 2>/dev/null || true

# The workspace is a bind mount whose files surface as mode 777 inside the
# container, so git sees a spurious 644 -> 755 change on every tracked file.
# Ignoring the mode bit locally keeps `git status` honest; the three scripts
# that genuinely need +x are already recorded as 100755 in the index.
git -C "$REPO_ROOT" config core.filemode false 2>/dev/null || true

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
