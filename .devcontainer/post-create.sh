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
for d in "$HOME/.claude" "$HOME/.config/gh" /commandhistory "$HOME/.nuget/packages"; do
  if [ -d "$d" ] && [ "$(stat -c '%U' "$d")" != "$USER_NAME" ]; then
    sudo chown -R "$USER_NAME:$USER_NAME" "$d"
    log "  chowned $d"
  fi
done
chmod 700 "$HOME/.claude" "$HOME/.config/gh" 2>/dev/null || true

# CLAUDE_CONFIG_DIR (set in devcontainer.json) moves ~/.claude.json onto the
# volume. Carry an existing file over the first time, so per-project state is
# not silently reset. Only ever runs when the old file outlived the new one.
if [ -f "$HOME/.claude.json" ] && [ ! -f "$HOME/.claude/.claude.json" ]; then
  mv "$HOME/.claude.json" "$HOME/.claude/.claude.json"
  log "Migrated ~/.claude.json onto the .claude volume"
fi

# Everything below installs into ~/.local/bin, which is overlayfs and therefore
# empty again after a rebuild. Put it on PATH first so the `command -v` guards
# see what a previous run left behind and skip the reinstall.
export PATH="$HOME/.local/bin:$PATH"

# The VS Code extension runs its own bundled binary by absolute path and never
# puts `claude` on PATH, so the terminal needs the standalone CLI.
# Both read the ~/.claude volume above, so one login covers extension and CLI.
if ! command -v claude >/dev/null 2>&1; then
  log "Installing Claude Code CLI..."
  curl -fsSL https://claude.ai/install.sh | bash || warn "claude CLI install failed"
fi

# spec-kit is a Python CLI, and uv is how it is installed without bringing a
# Python toolchain into a repo that has none. Roughly 6s combined, so it is not
# worth persisting either of them on a volume.
if ! command -v uv >/dev/null 2>&1; then
  log "Installing uv..."
  curl -LsSf https://astral.sh/uv/install.sh | sh || warn "uv install failed"
fi
if ! command -v specify >/dev/null 2>&1; then
  log "Installing spec-kit CLI..."
  "$HOME/.local/bin/uv" tool install specify-cli \
    --from git+https://github.com/github/spec-kit.git@v1.0.4 || warn "specify install failed"
fi

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
