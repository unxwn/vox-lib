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
So is `/workspaces` itself: only `/workspaces/vox-lib`, the bind mount of the repo
from the host, persists. Anything that must survive is on a named volume or inside
the repo directory:

| What                                      | Where                                             | Survives rebuild |
| ----------------------------------------- | ------------------------------------------------- | ---------------- |
| Claude Code sessions, memory, `CLAUDE.md` | volume `vox-lib-claude` → `~/.claude`             | yes              |
| Shell history                             | volume `vox-lib-bash-history` → `/commandhistory` | yes              |
| NuGet cache                               | volume `vox-lib-nuget` → `~/.nuget/packages`      | yes              |
| pnpm store                                | `/workspaces/vox-lib/.pnpm-store`                 | yes              |
| Source code                               | `/workspaces/vox-lib`                             | yes              |

A rebuild keeps the named volumes; removing them does not. `docker volume rm`,
a `docker volume prune` once the container is gone, or a Docker Desktop reset
takes Claude Code's history and credentials with it. Only the repo directory is
on the host disk.

The pnpm store is deliberately **not** a Docker volume: pnpm hardlinks packages
from the store into `node_modules`, hardlinks cannot cross filesystems, and a
separate volume would silently downgrade every install to full file copies.

## Contributing workflow

`main` is always deployable. Work on short-lived branches and open a PR:

```bash
git switch -c feature/audio-streaming
# ... commit using Conventional Commits: feat(api): stream audio by chapter
git push -u origin feature/audio-streaming
gh pr create            # PR title must follow Conventional Commits. It becomes
                        # the squashed commit message on main
```

CI (`.github/workflows/ci.yml`) must be green: backend build + tests + C#
formatting, frontend format + lint + typecheck + build. See `CLAUDE.md` for the
full convention.

## Scripts

| Command                             | What                                        |
| ----------------------------------- | ------------------------------------------- |
| `pnpm dev`                          | API (hot reload) + Vite dev server together |
| `pnpm api` / `pnpm web`             | one half only                               |
| `pnpm build`                        | `dotnet build` + `vite build`               |
| `pnpm lint`                         | oxlint over the frontend                    |
| `pnpm format` / `pnpm format:check` | Prettier                                    |
| `pnpm lint:cs`                      | `dotnet format --verify-no-changes`         |
| `dotnet test backend/VoxLib.slnx`   | run the API integration tests               |

## Repository

The remote is `github.com/unxwn/vox-lib` (public). Branch protection is enforced
by the `protect main` ruleset: a pull request is required, both CI checks must
pass, the branch must be up to date with `main` before it can merge, and
force-push and deletion are blocked. CI is a merge gate, not a report.

## Licence

Unlicensed and proprietary, all rights reserved. No `LICENSE` file is present by
intent, not omission.

Any licence added later would cover this source only. Audiobook files and cover
art carry their own separate rights.
