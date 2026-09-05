# vox-lib

Full-stack app: ASP.NET Core Web API (.NET 10, minimal APIs) + React 19 / Vite / TypeScript.
Everything runs inside the Dev Container defined in `.devcontainer/`.

## Layout

- `backend/VoxLib.slnx`: solution. **.NET 10 uses the XML `.slnx` format, not `.sln`.**
- `backend/src/VoxLib.Api/`: the API. Endpoints are minimal APIs in `Program.cs`.
- `frontend/`: Vite + React + TS, a pnpm workspace package.

## Commands

Run from the repo root; the package manager is **pnpm** (not npm/yarn).

```bash
pnpm dev      # API + web together
pnpm api      # dotnet watch, http://localhost:5080
pnpm web      # vite, http://localhost:5173
pnpm build    # dotnet build + vite build
pnpm lint     # oxlint
```

## Conventions

- The frontend calls the API at **relative** paths (`/api/...`). Vite proxies them
  to port 5080. Do not hardcode `http://localhost:5080` in frontend code; it
  breaks the proxy and reintroduces CORS.
- API routes intended for the browser live under `/api/`. `/health` is the
  liveness probe and stays at the root.
- The frontend lints with **oxlint**, not ESLint (that is what the current Vite
  template ships). Config is `frontend/.oxlintrc.json`.
- `UseHttpsRedirection` is enabled only outside Development, because the `http`
  launch profile has no HTTPS port and would log redirect warnings.

## Gotchas

- `/home/vscode` is **overlayfs** and is erased on container rebuild. Persistent
  state must go on a named volume (see `.devcontainer/devcontainer.json`) or under
  `/workspaces`. This includes `~/.config/pnpm/config.yaml`, which is why
  `post-create.sh` re-applies the pnpm `store-dir` on every create.
- The pnpm store is at `/workspaces/.pnpm-store`, deliberately on the same
  filesystem as the workspace so pnpm's hardlinks work. Moving it to a separate
  Docker volume would silently turn every install into a full copy.
- When killing dev servers with `pkill -f`, use a self-excluding pattern such as
  `pgrep -f 'Vox[L]ib'`; a plain `-f VoxLib.Api` also matches the shell running
  the command and kills it.

## Git conventions

- **Branching: GitHub Flow.** `main` is always deployable. Work happens on
  short-lived branches off `main` (`feat/…`, `fix/…`, `chore/…`), squash-merged
  back. There is deliberately no long-lived `develop` branch; this project ships
  continuously rather than in scheduled releases, so `develop` would just be a
  second `main` to keep in sync.
- **Commits: Conventional Commits.** `type(scope): subject`, imperative mood,
  lowercase subject, no trailing period. Types: `feat` `fix` `refactor` `perf`
  `docs` `test` `build` `ci` `chore`. Scopes used here: `api`, `web`,
  `devcontainer`, `deps`. A template is wired up via `commit.template`.
