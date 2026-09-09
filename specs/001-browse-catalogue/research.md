# Phase 0 Research: Browse the Book Catalogue

**Feature**: `specs/001-browse-catalogue/` | **Date**: 2026-09-08

Every unknown in the plan's Technical Context is resolved below. Versions were checked
against the public registries on the date above.

## R1. How catalogue pages are rendered

**Decision**: React Router framework mode with `ssr: false` and the `prerender` option in
`react-router.config.ts`. The build emits a real HTML document per catalogue page and per
book page, which then hydrates into the client-side application.

**Rationale**: FR-012 requires the served document to already carry the title, author and
description. React Router 8.3.1 supports exactly this combination, documented as
"pre-rendering with `ssr: false`", and `prerender` accepts an async function so the book
paths can be discovered at build time rather than hard coded. It is first-party to the
router the application needs anyway, it produces static files so no Node process runs in
production, and it leaves in-page navigation intact, which the architecture document
requires so that playback will survive navigation later.

**Alternatives considered**:

- `vite-react-ssg` 0.9.2. Does the same job, but it is a third-party package still below
  1.0 on the critical path of the product's discoverability. Rejected on maintenance risk.
- `vike` 0.4.266. Capable and active, but it is a full framework layer with its own
  routing and rendering conventions. Heavier than the problem.
- A hand-written prerender script over `vite build --ssr`. Full control and no new
  dependency, but it means owning route matching, head tags and asset injection. Rejected
  because the router already solves this and the code would be ours to maintain.
- Next.js. Rejected during clarification, and recorded in `docs/ARCHITECTURE.md`. It would
  put a Node process in production beside the .NET API for no gain at this catalogue size.

## R2. Where the prerender gets its data

**Decision**: The prerender runs against a reachable API, whose origin is supplied by an
environment variable. Continuous integration starts PostgreSQL and the API inside the
existing `frontend (lint + build)` job so that every pull request exercises the prerender.

**Rationale**: The alternative, reading the committed seed file directly at build time,
works only while books arrive by seeding. The moment an administrative feature publishes a
book the seed file stops being the source of truth and the mechanism has to be rebuilt.
Fetching from the API is the same path production uses, so nothing is unpicked later.
Extending the existing continuous integration job rather than adding a new one matters:
the constitution notes that job names are the status-check contexts the branch-protection
ruleset matches, so a new job would silently stop satisfying the rule until the ruleset is
updated.

**Alternatives considered**:

- Prerender from the committed seed file. Simplest, no services in continuous integration,
  but it is scaffolding with a known expiry date.
- Prerender only in the deployment pipeline. Keeps continuous integration untouched, but
  the mechanism is then never exercised by a pull request and breaks silently.

## R3. Database and migration tooling

**Decision**: PostgreSQL 18 with Entity Framework Core code-first migrations, via
`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 and `Microsoft.EntityFrameworkCore.Design`
10.0.12. Note that Entity Framework Core 11 exists only as a preview and this project
targets .NET 10, so the 10.x line is the correct one.

**Rationale**: This settles the open question left in `docs/ARCHITECTURE.md`. Code-first
migrations keep the schema in the same language and the same review as the model that
defines it, and the tooling is already in the .NET SDK. Liquibase is stronger when several
services or several languages share one schema, which is not the situation here, and it
would add a second toolchain and a second place where schema truth lives.

**Alternatives considered**:

- Liquibase with hand-written change sets. Better for polyglot teams and for schemas that
  outlive one application. Rejected as unearned for a single .NET service.
- SQLite for the first feature. Rejected because it cannot provide the Ukrainian collation
  that FR-015 requires, so the ordering behaviour would differ between tests and
  production, which is the worst possible outcome for the thing being tested.

## R4. Ukrainian collation and case-insensitive matching

**Decision**: Apply the deterministic collation `uk-UA-x-icu` to the book title and author
name columns through Entity Framework's per-property collation. Perform case-insensitive
substring search separately with `ILIKE`, exposed by Npgsql as `EF.Functions.ILike`.

**Rationale**: Ordering and matching are two different jobs and PostgreSQL treats them
that way. A deterministic collation gives the sort order FR-015 asks for while leaving
pattern matching available. A non-deterministic collation would fold case automatically
but PostgreSQL restricts pattern matching against non-deterministic collations, which
would break the substring search in FR-013. Per-column collation is also reachable from a
migration, whereas a database-level collation is fixed by `CREATE DATABASE` and therefore
sits outside the tooling.

**Alternatives considered**:

- Non-deterministic `uk-UA-x-icu` with case folding built in. Elegant for equality, but it
  collides with substring search.
- The `citext` extension. Solves case-insensitivity only, still needs a collation for
  ordering, and adds an extension to install.
- Sorting in the application after loading. Rejected because it cannot work with paging.

## R5. PostgreSQL in the dev container and in continuous integration

**Decision**: Convert `.devcontainer` from a single Dockerfile to a Compose file with an
application service and a `postgres:18` service, with the data directory on a named
volume. Continuous integration uses a PostgreSQL service container in the workflow.

**Rationale**: There is no database in the container today. Compose is the supported way to
add one to a dev container, and a named volume matches the persistence rule in `CLAUDE.md`,
since anything on the container filesystem is erased on rebuild. A service container is the
ordinary approach in GitHub Actions and needs no Docker-in-Docker.

**Risk worth stating**: `CLAUDE.md` records that nothing in continuous integration builds
the dev container, so this change is not verified by a green pull request. It has to be
checked by rebuilding the container locally before merge.

**Alternatives considered**:

- A community dev container feature that installs PostgreSQL inside the application
  container. Smaller diff, but the data directory then sits on the container filesystem and
  is destroyed on rebuild unless separately mounted, and the feature is not first-party.
- Testcontainers for tests plus a manually installed local database. Rejected because the
  container has no Docker socket, so Testcontainers cannot start anything from inside it.

## R6. Testing endpoints against a real database

**Decision**: Keep xunit with `WebApplicationFactory<Program>`. The fixture creates a
uniquely named database per test run, applies migrations, seeds a known dataset, and drops
the database afterwards. The connection string comes from an environment variable with a
local default.

**Rationale**: The constitution requires endpoint tests over real HTTP, and Principle III
rules out substituting mock-heavy unit tests. Ordering under Ukrainian collation and
case-insensitive matching are database behaviours, so an in-memory provider would test
something the product does not do. A database per run keeps parallel runs from colliding
without adding Testcontainers, which the container cannot run.

**Alternatives considered**:

- The Entity Framework in-memory provider. Rejected outright: it cannot express collation
  or `ILIKE`, so the tests that matter most would be meaningless.
- A shared test database with per-test cleanup. Cheaper to start, but cleanup order becomes
  a source of flaky tests.

## R7. Verifying accessibility

**Decision**: Playwright 1.63.0 with `@axe-core/playwright` 4.13.0, running the catalogue
list, a book page and a search result page. Assert zero critical and zero serious
violations, that focus moves to the results heading when the page changes, and that a
keyboard traversal reaches every book and hits no trap.

**Rationale**: Principle I is non-negotiable and SC-002, SC-003 and SC-004 are stated as
measurable outcomes, so something has to measure them. Focus movement and live regions only
behave correctly in a real browser, so a simulated document environment would give false
confidence.

**Cost, stated plainly**: this adds a browser download and a meaningful amount of time to
continuous integration. It is the largest single piece of new tooling in this feature and
the most obvious candidate if the plan has to be trimmed.

**Alternatives considered**:

- Vitest with a simulated document and `jest-axe`. Fast, but it models neither focus nor
  assistive-technology announcement, so it cannot verify most of what Principle I demands.
- Manual audits only. Rejected because a success criterion nothing checks automatically
  regresses silently.

## R8. How a book is addressed

**Decision**: Each book carries a stored, unique, lowercase slug. The page address is
`/books/{slug}` and the resource is `GET /api/books/{slug}`.

**Rationale**: A slug in the address is what carries the title into search results, and a
stored slug is stable when a title is later corrected. Storing rather than deriving it also
avoids writing a Cyrillic transliteration routine in this feature; the seed data supplies
slugs, and the future administrative feature will generate them.

**Alternatives considered**:

- A generated identifier in the address. Stable and trivial, but it throws away the words
  that make the page findable.
- Deriving the slug from the title at request time. Needs transliteration now, and any
  later change to the rules silently breaks every existing link.

## R9. Paging contract and out-of-range pages

**Decision**: A fixed page size of 20. `GET /api/books?page=n` returns the page, the page
size, the total count and the page count. A page number below 1, or above the last page
when the catalogue is not empty, returns 404 with a problem details body. Page 1 of an
empty catalogue returns 200 with an empty list.

**Rationale**: This closes the edge case the specification raises. Returning 200 with an
empty list for a page past the end would create pages that look valid to a crawler and
contain nothing, which search engines treat as low quality. Page 1 of a genuinely empty
catalogue is a real page and carries the empty state that FR-007 requires.

**Alternatives considered**:

- Redirect an out-of-range page to the last page. Friendly in a browser, but it invents a
  second address for the same content, which is bad for indexing.
- Clamp silently to the valid range. Hides mistakes and produces duplicate content.

## R10. How the catalogue is populated

**Decision**: A seed dataset committed to the repository as JSON inside `VoxLib.Dal`, applied
by a seeder that runs at startup when the catalogue is empty, after pending migrations are
applied.

**Rationale**: The clarification settled that content comes from a versioned seed dataset so
that a fresh container and the test suite start from identical content. Seeding only when
empty makes the operation safe to repeat.

**Known tradeoff**: applying migrations at startup is convenient here and is normally
replaced by an explicit deployment step once more than one instance runs. Worth revisiting
before the first real deployment rather than now.

## R11. Ukrainian interface strings

**Decision**: Write interface text in Ukrainian directly in the components. Set the document
language to `uk`. Add no translation library.

**Rationale**: The clarification fixed a single locale and explicitly deferred translation
machinery until a second locale genuinely exists, which is Principle VII applied directly.
FR-018 needs the document language declared so that a screen reader selects a Ukrainian
voice; that is an attribute, not a framework.

**Alternatives considered**:

- Adding a translation library now against a future second locale. Rejected as the textbook
  unearned abstraction, and it makes every string indirect for no present benefit.

## R12. Keeping the wire contract true on both sides

**Decision**: Generate the frontend's request and response types from the API's own OpenAPI
document using `openapi-typescript`, commit the generated file, and fail continuous
integration when regenerating it produces a diff.

**Rationale**: The constitution requires that a type crossing the API boundary has a single
source of truth reachable by both halves of the stack. The API already publishes an OpenAPI
document, so the C# contracts in `VoxLib.Api/Book/Contracts/` are that source and the
TypeScript is derived. Committing the output keeps the frontend build independent of a
running API, and the drift check is what stops the two sides quietly diverging.

**Alternatives considered**:

- Hand-written TypeScript interfaces mirroring the C# contracts. This is the status quo in
  the scaffold and it is exactly the duplication the constitution rules out.
- A shared schema package consumed by both. There is no language both halves share, so any
  such package is itself generated, which adds a step without removing one.
