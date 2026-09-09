# Implementation Plan: Browse the Book Catalogue

**Branch**: `feature/browse-catalogue` | **Date**: 2026-09-08 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-browse-catalogue/spec.md`

## Summary

Deliver the public catalogue: a paged list of published books, a page per book showing its
description and chapters, and search by title or author name. Everything is readable without
an account and nothing touches audio.

The approach follows from the clarifications. Catalogue and book pages are generated as real
documents at build time by React Router's prerender, so a search engine and a slow device
both receive the content rather than an empty root element. Content comes from PostgreSQL,
introduced here along with `VoxLib.Model`, `VoxLib.Orchestrator` and `VoxLib.Dal`, seeded from
a dataset committed to the repository. Titles sort under a Ukrainian collation and search
matches without regard to letter case, which is why the database arrives in this feature
rather than the next one.

This is a large first feature. It stands up the domain projects, the database, the routing
and rendering strategy, and the accessibility test harness, in addition to the user-facing
behaviour. That was the accepted consequence of keeping search in scope and of choosing a
real database over a fixture.

## Technical Context

**Language/Version**: C# on .NET 10, SDK 10.0.400. TypeScript 7.0 on Node 24.

**Primary Dependencies**: ASP.NET Core minimal APIs; Entity Framework Core 10 with
`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 and `Microsoft.EntityFrameworkCore.Design`
10.0.12; React 19.2; React Router 8.3.1 with `@react-router/dev`; Vite 8.2;
`openapi-typescript` 7.13 for generated wire types.

**Storage**: PostgreSQL 18. Deterministic ICU collation `uk-UA-x-icu` on the text columns
that are sorted or matched. Case-insensitive search via `ILIKE`.

**Testing**: xunit with `WebApplicationFactory<Program>` over real HTTP against a real
database created and dropped per run. Playwright 1.63 with `@axe-core/playwright` 4.13 for
the accessibility outcomes.

**Target Platform**: Prerendered static pages plus the .NET API behind a single origin.
Browsers running VoiceOver on iOS and TalkBack on Android are the platforms that matter.

**Project Type**: Web application. Backend and frontend in one repository.

**Performance Goals**: The catalogue list is readable within 3 seconds on a typical mobile
connection, which is SC-006. Prerendered documents are what make that achievable on a slow
device, since the first paint does not wait on a round trip for data.

**Constraints**: No audio field, link or storage key in any response or page. The served
document must carry title, author and description without client rendering. One locale,
Ukrainian, fixed when the database is created. Page size fixed at 20.

**Scale/Scope**: A catalogue in the low thousands of books. Three routes, two endpoints,
three domain entities.

No unresolved unknowns remain. Phase 0 is recorded in [research.md](./research.md).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluated against the six gates in `.specify/memory/constitution.md`.

**1. Accessibility.** PASS. All three user stories carry screen reader and keyboard-only
acceptance scenarios. The design adds focus movement to the results heading on a page change
(FR-019), a polite live region for result counts (FR-014), the document language declared as
Ukrainian (FR-018), and native elements throughout. Media session does not apply: this
feature has no playback. SC-002, SC-003 and SC-004 are verified automatically by the
accessibility run described in R7 rather than by inspection.

**2. Dependency rule.** PASS. `VoxLib.Model` references no other VoxLib project and declares
`IBookRepository` and `IBookCatalogue`. `VoxLib.Dal` and `VoxLib.Orchestrator` implement them.
`VoxLib.Api` depends on `.Model` and `.Orchestrator`, and touches `.Dal` only at the
composition root to register implementations. The rules that exist live on the entity:
`Book.IsListed` decides whether a book may appear anywhere, so no endpoint or service can
leak an unpublished book by forgetting a filter. Endpoint handlers parse, delegate and shape.

**3. Audio and access.** PASS, and mostly by construction. There is no audio in this feature,
so no path moves audio bytes, hands out a URL or caches anything. The domain model carries no
audio field at all, which is stronger than stripping one in the mapping layer. No service
worker is introduced here; when the progressive web app work lands it must bypass audio URLs,
as the constitution requires.

**4. HTTP.** PASS. Two resource-shaped routes under `/api/`: `GET /api/books` and
`GET /api/books/{slug}`. `/health` is untouched. The frontend calls relative paths only, and
the prerender reaches the API through a configured origin rather than a hardcoded one.

**5. Tests.** PASS. Every behaviour names the endpoint test that proves it: ordering under
Ukrainian collation, paging bounds including the out-of-range page, the empty catalogue, book
detail by slug, unknown and unpublished slugs, absence of audio fields, and search matching by
title and by author in either letter case. Accessibility outcomes are covered separately by
the browser run.

**6. Abstractions.** PASS. The plan adopts nothing from the deferred list in Principle VII:
no aggregate roots, domain events, specifications, CQRS, MediatR or HLS. `VoxLib.Platform` is
deliberately not created, because there is no object storage or external provider in this
feature; it arrives with audio. The catalogue is CRUD and stays CRUD.

### Post-design re-check

Re-evaluated after the Phase 1 artifacts were written. All six gates still pass. The data
model keeps behaviour on `Book`, the contract in `contracts/catalogue.yaml` contains no audio
field and no authenticated path, and the generated wire types give the boundary a single
source of truth as Principle II requires.

## Project Structure

### Documentation (this feature)

```text
specs/001-browse-catalogue/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── catalogue.yaml   # Phase 1 output, OpenAPI 3.1
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output, created by /speckit-tasks
```

### Source Code (repository root)

```text
backend/
├── VoxLib.slnx                          # three new projects added here
├── src/
│   ├── VoxLib.Api/
│   │   ├── Program.cs                   # composition root, endpoint registration
│   │   └── Book/
│   │       ├── BookEndpoints.cs
│   │       └── Contracts/
│   │           └── Responses/           # BookSummary, BookDetail, PagedBooks, Chapter
│   ├── VoxLib.Model/                    # references no other VoxLib project
│   │   ├── Book/                        # Book, Author, Chapter, PublicationState,
│   │   │                                # IBookRepository, IBookCatalogue
│   │   └── Common/                      # PagedResult, PageRequest
│   ├── VoxLib.Orchestrator/
│   │   └── Book/BookCatalogue.cs        # paging bounds, published-only, ordering
│   └── VoxLib.Dal/
│       ├── Book/                        # BookDao, AuthorDao, ChapterDao,
│       │                                # BookRepository, mapping to the domain
│       ├── Persistence/                 # VoxLibDbContext, Migrations
│       └── Seed/                        # books.json, CatalogueSeeder
└── tests/VoxLib.Api.Tests/
    ├── Catalogue/                       # list, detail and search endpoint tests
    └── Infrastructure/                  # per-run database, seeded factory

frontend/
├── react-router.config.ts               # ssr: false, prerender paths from the API
├── vite.config.ts                       # existing proxy, plus the router plugin
├── src/
│   ├── root.tsx
│   ├── routes.ts
│   ├── routes/                          # catalogue, book, search
│   ├── components/                      # book list, pagination, search form, cover art
│   └── api/                             # client plus schema.d.ts generated from OpenAPI
└── tests/a11y/                          # Playwright specs with axe

.devcontainer/
├── docker-compose.yml                   # new: app service plus postgres:18 on a volume
├── devcontainer.json                    # points at the compose file
└── Dockerfile                           # unchanged

.github/workflows/ci.yml                 # postgres service; the frontend job starts the
                                         # API so the prerender is exercised on every PR
```

**Structure Decision**: Web application, using the five-project backend shape the
constitution names, minus `VoxLib.Platform` which has nothing to do yet. The frontend keeps
its existing `src/` directory as the router's application directory rather than moving to
`app/`, so the change stays within the current layout. The sample weather forecast page is
replaced, since the catalogue owns the root route; the sample API endpoint is left alone and
should be removed by a separate chore rather than smuggled into this feature.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified

No violations. The plan adopts no deferred pattern from Principle VII, and creates one fewer
project than the constitution's full backend shape because `VoxLib.Platform` has no work in
this feature.

Two additions are new tooling rather than deferred patterns, recorded here because they are
the largest costs in the plan and the obvious places to trim:

| Addition | Why needed | What it costs |
| --- | --- | --- |
| Playwright with axe | SC-002, SC-003 and SC-004 are measurable outcomes about focus, announcements and violations, which only a real browser can verify | A browser download and meaningful time in continuous integration |
| PostgreSQL in the dev container and in the pipeline | FR-015 ordering and FR-013 matching are database behaviours; testing them anywhere else tests something the product does not do | A Compose-based dev container, unverified by continuous integration, and a service container in the workflow |
