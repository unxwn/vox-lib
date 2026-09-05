# vox-lib

ASP.NET Core Web API (.NET 10) + React 19 / Vite frontend, developed inside a Dev Container.

## Layout

```
backend/
  VoxLib.slnx            # .NET 10 uses the XML .slnx solution format
  src/VoxLib.Api/        # minimal-API web project
frontend/                # Vite + React + TypeScript (pnpm workspace package)
.devcontainer/           # container definition + lifecycle scripts
```

## Running

From the repo root:

```bash
pnpm dev        # API (hot reload) + Vite dev server together
pnpm api        # API only  -> http://localhost:5080
pnpm web        # frontend only -> http://localhost:5173
pnpm build      # dotnet build + vite production build
pnpm lint       # oxlint over the frontend
```

The Vite dev server proxies `/api`, `/health` and `/openapi` to the API on port
5080, so the browser only ever talks to one origin and there are no CORS
preflights in development.

| URL                                       | What                                |
| ----------------------------------------- | ----------------------------------- |
| http://localhost:5173                     | React app                           |
| http://localhost:5080/health              | API liveness probe                  |
| http://localhost:5080/api/weatherforecast | Sample endpoint the React app calls |
| http://localhost:5080/openapi/v1.json     | OpenAPI 3.1 document                |

`backend/src/VoxLib.Api/VoxLib.Api.http` has ready-made requests (REST Client extension).

## Toolchain

| Tool     | Version            | Installed by                |
| -------- | ------------------ | --------------------------- |
| .NET SDK | 10.0.400           | base image                  |
| Node.js  | 24 LTS ("Krypton") | `node` devcontainer feature |
| pnpm     | latest             | `node` devcontainer feature |

## Persistence across rebuilds

`/home/vscode` is on **overlayfs** and is wiped whenever the container is rebuilt.
Anything that must survive is on a named volume or on the workspace volume:

| What                                      | Where                                             | Survives rebuild |
| ----------------------------------------- | ------------------------------------------------- | ---------------- |
| Claude Code sessions, memory, credentials | volume `vox-lib-claude` → `~/.claude`             | yes              |
| Shell history                             | volume `vox-lib-bash-history` → `/commandhistory` | yes              |
| NuGet cache                               | volume `vox-lib-nuget` → `~/.nuget/packages`      | yes              |
| pnpm store                                | `/workspaces/.pnpm-store`                         | yes              |
| Source code                               | `/workspaces/vox-lib`                             | yes              |

On top of the volume, `.devcontainer/backup-claude.sh` snapshots Claude state to
`/workspaces/.claude-backup-<timestamp>/` every time the container starts (keeping
the last 10), so history survives even a `docker volume rm` or a
"Clean Up Dev Containers" sweep. Run it manually any time with `pnpm backup:claude`.

The pnpm store is deliberately **not** a Docker volume: pnpm hardlinks packages
from the store into `node_modules`, hardlinks cannot cross filesystems, and a
separate volume would silently downgrade every install to full file copies.

## Contributing workflow

`main` is always deployable. Work on short-lived branches and open a PR:

```bash
git switch -c feature/audio-streaming
# ... commit using Conventional Commits: feat(api): stream audio by chapter
git push -u origin feature/audio-streaming
gh pr create            # PR title must follow Conventional Commits; it becomes
                        # the squashed commit message on main
```

CI (`.github/workflows/ci.yml`) must be green: backend build + tests + C#
formatting, frontend format + lint + typecheck + build. See `CLAUDE.md` for the
full convention.

## Scripts

| Command                             | What                                                    |
| ----------------------------------- | ------------------------------------------------------- |
| `pnpm dev`                          | API (hot reload) + Vite dev server together             |
| `pnpm api` / `pnpm web`             | one half only                                           |
| `pnpm build`                        | `dotnet build` + `vite build`                           |
| `pnpm lint`                         | oxlint over the frontend                                |
| `pnpm format` / `pnpm format:check` | Prettier                                                |
| `pnpm lint:cs`                      | `dotnet format --verify-no-changes`                     |
| `pnpm backup:claude`                | snapshot Claude state to `/workspaces/.claude-backup-*` |
| `dotnet test backend/VoxLib.slnx`   | run the API integration tests                           |

## Creating the GitHub repository

```bash
gh repo create vox-lib --private --source=. --remote=origin --push
bash .github/setup-branch-protection.sh   # requires PR + green CI on main
```

There is deliberately **no `LICENSE` file**. Under copyright law, code published
with no licence is "all rights reserved": nobody may copy, modify or
redistribute it. For a product you intend to run commercially that is the
correct default. Add a licence only if you decide to open-source, and then
choose deliberately: MIT (maximally permissive), Apache-2.0 (permissive plus an
explicit patent grant), or AGPL-3.0 (copyleft that also covers running the code
as a network service, the usual choice for stopping someone hosting your
product as a competing SaaS).

Note that a licence covers **your code only**. Audiobook files and cover art
carry their own separate rights and are not affected by this repository's
licence.
