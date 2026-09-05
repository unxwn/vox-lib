## What

<!-- What does this change do, in one or two sentences? -->

## Why

<!-- The problem being solved. Link the issue: Closes #123 -->

## How

<!-- Anything a reviewer needs to know to read the diff: approach, tradeoffs,
     anything deliberately left out. -->

## Checklist

- [ ] Branch name follows `feature/…`, `fix/…`, `chore/…`
- [ ] Commits follow Conventional Commits (`feat:`, `fix:`, `refactor:`, …)
- [ ] `pnpm build` and `dotnet test backend/VoxLib.slnx` pass locally
- [ ] Tests added or updated for behaviour changes
- [ ] No secrets, tokens or `.env` files in the diff
