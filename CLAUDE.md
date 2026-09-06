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
  state must go on a named volume (see `.devcontainer/devcontainer.json`) or
  inside `/workspaces/vox-lib`. This includes `~/.config/pnpm/config.yaml`, which
  is why `post-create.sh` re-applies the pnpm `store-dir` on every create.
- **`/workspaces` itself is not persistent**, only `/workspaces/vox-lib` is. The
  repo directory is a bind mount from the host; its parent is the container's
  overlayfs and is wiped on rebuild like the rest of `/`. A path such as
  `/workspaces/.some-cache` looks persistent and is not.
- The pnpm store is at `/workspaces/vox-lib/.pnpm-store` (gitignored), on the
  same filesystem as `node_modules` so pnpm's hardlinks work. A named volume
  would be a different filesystem and would silently turn every install into a
  full copy; so would anywhere under `/workspaces` outside the repo.
- When killing dev servers with `pkill -f`, use a self-excluding pattern such as
  `pgrep -f 'Vox[L]ib'`; a plain `-f VoxLib.Api` also matches the shell running
  the command and kills it.

## What this project is

**vox-lib is an audiobook library website.** It is intended to grow into a real,
production-quality product, not a scratch project. Treat every change with that
in mind: prefer designs that survive growth, write tests for behaviour, and do
not take shortcuts that would need unpicking later. When a quick hack and a
sound approach are close in cost, take the sound one.

Expect the domain to grow toward: a catalogue of books and authors, audio file
storage and streaming, chapter/track structure, playback position sync across
devices, search and browse, user accounts and libraries, and probably
recommendations. Design new code so those additions land cleanly: keep the API
resource-shaped, keep domain logic out of endpoint handlers, and put shared types
where both halves of the stack can reach them.

## Git workflow

**Branching: trunk-based / GitHub Flow.** `main` is always deployable. There is
deliberately **no long-lived `develop` branch**. For a continuously deployed
product it becomes a second `main` to keep in sync, and every change pays the
cost of being merged twice. If a staging gate is ever genuinely needed, add a
`staging` branch that deploys to an environment, rather than reviving `develop`.

Branch names (note the prefix is `feature/`, while the _commit_ type is `feat:`):

```
feature/audio-streaming      new capability
fix/playback-position-drift  bug fix
chore/bump-dotnet-10-0-5     maintenance, deps, tooling
docs/api-authentication      documentation only
```

Keep branches short-lived: a branch that lives longer than a few days is
accumulating merge risk. Rebase onto `main` rather than merging `main` into the
branch, so history stays linear.

**Every change goes through a pull request**, including your own solo work. The
PR is where CI runs and where the change becomes reviewable later; a commit
pushed straight to `main` skips both. `.github/pull_request_template.md` is
filled in automatically.

**Merge by squashing.** A squash merge collapses every commit on the branch into
a single commit on `main`. It means you can commit as messily as you like while
working ("wip", "fix typo", "actually fix it") and `main` still reads as one
clean, revertable change per feature. The PR title becomes that commit's message,
so the _PR title_ must follow Conventional Commits. Revert = one `git revert`.

**Commits: Conventional Commits.** `type(scope): subject`, imperative mood,
lowercase subject, no trailing period. Types: `feat` `fix` `refactor` `perf`
`docs` `test` `build` `ci` `chore`. Scopes in use: `api`, `web`, `devcontainer`,
`deps`.

## CI checks

`.github/workflows/ci.yml` runs on every PR into `main` and must be green to
merge. Two parallel jobs:

| Job        | Steps                                                                                    |
| ---------- | ---------------------------------------------------------------------------------------- |
| `backend`  | restore → `dotnet format --verify-no-changes` → build (Release) → `dotnet test`          |
| `frontend` | `pnpm install --frozen-lockfile` → `format:check` → `lint` → `tsc -b --noEmit` → `build` |

Run the same checks locally before pushing:

```bash
dotnet format backend/VoxLib.slnx --verify-no-changes
dotnet test backend/VoxLib.slnx
pnpm format:check && pnpm lint && pnpm build
```

**Branch protection is not currently enforced.** `vox-lib` is a private repo on
the GitHub Free plan, where both the rulesets API and the Settings → Branches UI
refuse with `Upgrade to GitHub Pro or make this repository public`. CI still runs
and still reports on every PR. It just cannot be made a required check, so
nothing mechanically blocks a direct push to `main`. Until the repo goes public
or the account goes Pro, the PR workflow above holds by discipline.

If that changes, protect `main` with: require a PR, require the
`backend (build + test)` and `frontend (lint + build)` checks (the job `name:`
values in `ci.yml` are the status-check contexts), and require the branch to be
up to date.

## Tests

`backend/tests/VoxLib.Api.Tests/` uses xunit plus `WebApplicationFactory<Program>`,
which boots the real application in-process and exercises endpoints over HTTP.
`Program.cs` ends with `public partial class Program;` purely so the test project
can name that type; do not delete it.

Prefer these end-to-end endpoint tests over mock-heavy unit tests for API
behaviour; add focused unit tests for domain logic once it is non-trivial.
