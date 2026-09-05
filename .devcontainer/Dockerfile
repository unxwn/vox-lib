FROM mcr.microsoft.com/devcontainers/dotnet:2-10.0-noble

# Pre-create every directory that will have a named volume mounted over it, owned
# by the container user.
#
# Why this matters: when Docker mounts a *fresh* named volume over a path that
# EXISTS in the image, it seeds the volume from that path and carries its
# ownership across. When the path does NOT exist, Docker creates it owned by
# root, and the non-root user cannot write to it. That is the classic dev
# container failure where Claude Code cannot write ~/.claude/.credentials.json
# and silently loses authentication on every rebuild.
#
# Creating them here means the volumes are born owned by vscode:vscode, so there
# is no window between container start and postCreateCommand in which the
# directory is unwritable. post-create.sh still repairs ownership defensively.
USER root
RUN mkdir -p /home/vscode/.claude \
             /home/vscode/.nuget/packages \
             /home/vscode/.local/share/pnpm \
             /commandhistory \
 && chown -R vscode:vscode /home/vscode/.claude \
                           /home/vscode/.nuget \
                           /home/vscode/.local \
                           /commandhistory \
 && chmod 700 /home/vscode/.claude

# The .NET base image forces polling file watchers, a safe default for bind
# mounts from Windows/macOS where inotify events do not propagate. This
# workspace lives on an ext4 Docker volume where inotify works natively, so
# polling only adds latency and CPU churn. Verified: hot reload picks up a change
# in ~2s with inotify.
RUN sed -i '/DOTNET_USE_POLLING_FILE_WATCHER/d' /etc/environment

USER vscode
