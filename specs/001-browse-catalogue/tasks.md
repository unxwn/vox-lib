---
description: "Task list for Browse the Book Catalogue"
---

# Tasks: Browse the Book Catalogue

**Input**: Design documents from `specs/001-browse-catalogue/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/catalogue.yaml](./contracts/catalogue.yaml)

**Tests**: Included. Principle III of the constitution requires an endpoint test for every
behaviour change, and Principle I is not satisfied by automation alone, so each story also
carries a manual pass on a real screen reader.

**Organization**: Grouped by user story. Stories are implemented **sequentially**, in
priority order, because all three edit the same repository, orchestrator, endpoint and
generated-types files.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel, different files and no dependency on incomplete work
- **[Story]**: Which user story the task serves
- File paths are exact

## Path Conventions

Web application. Backend under `backend/src/` and `backend/tests/`, frontend under
`frontend/src/`, per the structure decision in `plan.md`.

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 [P] Convert the dev container to Compose in `.devcontainer/docker-compose.yml` with an application service and a `postgres:18` service whose data directory is a named volume, point `.devcontainer/devcontainer.json` at it, and expose the connection string as `VOXLIB_DB_CONNECTION`
- [X] T002 [P] Add a PostgreSQL service to the `frontend (lint + build)` and `backend (build + test)` jobs in `.github/workflows/ci.yml`, and start the API before the frontend build so the prerender has something to read. Do not rename either job: the names are the status check contexts the branch protection ruleset matches
- [X] T003 Create `backend/src/VoxLib.Model/`, `backend/src/VoxLib.Orchestrator/` and `backend/src/VoxLib.Dal/`, register them in `backend/VoxLib.slnx`, and set project references so that `VoxLib.Model` references nothing, `.Orchestrator` and `.Dal` reference `.Model`, and `.Api` references `.Model`, `.Orchestrator`, `.Dal`
- [X] T004 Add `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 and `Microsoft.EntityFrameworkCore.Design` 10.0.12 to `backend/src/VoxLib.Dal/VoxLib.Dal.csproj`
- [X] T005 [P] Add `react-router` and `@react-router/dev` 8.3.1 to `frontend/package.json`, create `frontend/react-router.config.ts` with `ssr: false` and the application directory set to `src`, and register the router plugin in `frontend/vite.config.ts` while keeping the existing proxy
- [X] T006 Add `@playwright/test` 1.63.0 and `@axe-core/playwright` 4.13.0 to `frontend/package.json` with a `test:a11y` script, and configure `frontend/playwright.config.ts`
- [X] T007 Add `openapi-typescript` 7.13.0 to `frontend/package.json` with a `gen:api-types` script that writes `frontend/src/api/schema.d.ts` from the API's OpenAPI document

---

## Phase 2: Foundational (Blocking Prerequisites)

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T008 Create the domain entities in `backend/src/VoxLib.Model/Book/`: `Book`, `Author`, `Chapter`, `PublicationState`. `Book` carries a slug and a language tag per `data-model.md`. Put the rules on the entity itself: `IsListed` true only when published, `TotalRunningTime` summing chapters and zero when there are none, and `ChapterCount`
- [X] T009 [P] Create `PagedResult<T>` and `PageRequest` in `backend/src/VoxLib.Model/Common/`, with `PageRequest` rejecting a page number below 1 and a fixed page size of 20
- [X] T010 Declare `IBookRepository` and `IBookCatalogue` in `backend/src/VoxLib.Model/Book/`, covering a page of books, a page of search results, and one book by slug
- [X] T011 Create `VoxLibDbContext` and the data access objects in `backend/src/VoxLib.Dal/Persistence/` and `backend/src/VoxLib.Dal/Book/`, applying the deterministic collation `uk-UA-x-icu` to the book title and author name columns, a unique index on the slug, an index on publication state with title and slug so the sort order is total, and a unique index on book with chapter position
- [X] T012 Generate the initial migration into `backend/src/VoxLib.Dal/Persistence/Migrations/` and confirm the generated SQL carries the collation and every index from T011
- [X] T013 Add the seed dataset `backend/src/VoxLib.Dal/Seed/books.json` and `CatalogueSeeder`, covering at least: Ukrainian titles that exercise collation ordering, two books whose titles sort identically, a book whose language tag is not Ukrainian, a book with no cover art, a book with no description, a book with no chapters, and an unpublished book. Seed only when the catalogue is empty
- [X] T014 Wire the composition root in `backend/src/VoxLib.Api/Program.cs`: read `VOXLIB_DB_CONNECTION`, register the context, the repository and the orchestrator, apply pending migrations and run the seeder at startup. Reference `.Dal` here only, never from an endpoint
- [X] T015 Create the test harness in `backend/tests/VoxLib.Api.Tests/Infrastructure/`: a fixture that creates a uniquely named database per run, applies migrations, seeds it, exposes a `WebApplicationFactory<Program>` bound to it, and drops the database afterwards
- [X] T016 [P] Create the frontend shell in `frontend/src/root.tsx` and `frontend/src/routes.ts`, declaring the document language as Ukrainian, and delete the weather forecast markup from `frontend/src/App.tsx`
- [X] T017 [P] Create the fetch wrapper in `frontend/src/api/client.ts` that calls relative paths only and surfaces failures as typed results

**Checkpoint**: Foundation ready. Start User Story 1.

---

## Phase 3: User Story 1 - Browse the list of books (Priority: P1) 🎯 MVP

**Goal**: A visitor sees every published book, page by page, and reaches all of them with a screen reader or the keyboard alone.

**Independent Test**: Seed the catalogue, open it, and confirm with a screen reader and with the keyboard that every book is reachable and correctly identified. No audio and no account involved.

### Tests for User Story 1

- [X] T018 [P] [US1] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Catalogue/BookListEndpointTests.cs` for the ordering of Ukrainian titles, the page size of 20, and the page, page count and total count fields
- [X] T019 [P] [US1] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Catalogue/BookListPagingTests.cs`: a page below the first and a page beyond the last both return 404 with a problem details body per FR-020, page 1 of an empty catalogue returns 200 with an empty list, and paging across a boundary with two identically titled books neither repeats nor skips
- [X] T020 [P] [US1] Endpoint test in `backend/tests/VoxLib.Api.Tests/Catalogue/NoAudioExposureTests.cs` asserting that no list response contains an audio location, storage key or any field resembling one
- [X] T021 [P] [US1] Endpoint test in `backend/tests/VoxLib.Api.Tests/Catalogue/AnonymousAccessTests.cs` asserting that the catalogue is readable with no credentials of any kind, per FR-016

### Implementation for User Story 1

- [X] T022 [US1] Implement the paged read in `backend/src/VoxLib.Dal/Book/BookRepository.cs`, filtering to published books, ordering by title under the collation and then by slug so the order is total, and mapping data access objects to domain entities
- [X] T023 [US1] Implement listing in `backend/src/VoxLib.Orchestrator/Book/BookCatalogue.cs`, owning page bounds and distinguishing an empty catalogue from a page past the end per FR-020
- [X] T024 [US1] Add `PagedBooks` and `BookSummary` to `backend/src/VoxLib.Api/Book/Contracts/Responses/`, matching `contracts/catalogue.yaml`
- [X] T025 [US1] Add `GET /api/books` in `backend/src/VoxLib.Api/Book/BookEndpoints.cs`, parsing the page, delegating to the orchestrator and shaping the response. No business rule in the handler
- [X] T026 [US1] Run `pnpm --filter frontend gen:api-types` and commit `frontend/src/api/schema.d.ts`
- [X] T027 [P] [US1] Build the catalogue route in `frontend/src/routes/catalogue.tsx`, with Ukrainian interface text, rendering each book as one link naming the title and author, and a heading structure a screen reader can navigate
- [X] T028 [P] [US1] Build the cover art component in `frontend/src/components/CoverArt.tsx`, showing a placeholder when art is absent and contributing nothing to the entry's accessible name
- [X] T029 [P] [US1] Build the empty catalogue state in `frontend/src/components/EmptyState.tsx` in Ukrainian, naming why nothing is listed and offering a route onward, leaving the page navigable
- [X] T030 [P] [US1] Build the loading and failure states in `frontend/src/components/LoadingState.tsx` and `frontend/src/components/ErrorState.tsx` in Ukrainian, each announced to a screen reader, satisfying FR-017 and the dropped-connection edge case
- [X] T031 [P] [US1] Handle overlong titles and descriptions in `frontend/src/styles/catalogue.css` so that text wraps or truncates without clipping and without hiding content from assistive technology
- [X] T032 [US1] Build numbered pagination in `frontend/src/components/Pagination.tsx` as a labelled navigation whose links carry real addresses, stating the current page, the page count and which books the page covers
- [X] T033 [US1] On a page change in `frontend/src/routes/catalogue.tsx`, move focus to the results heading and announce both the new position and the number of results through a polite live region, satisfying FR-014 and FR-019
- [X] T034 [US1] Add catalogue page addresses to the prerender list in `frontend/react-router.config.ts`, discovering the page count from the API at build time
- [X] T035 [US1] Point the root route at the catalogue in `frontend/src/routes.ts` and remove the remaining weather forecast demo files from `frontend/src/`
- [X] T036 [P] [US1] Automated accessibility run in `frontend/tests/a11y/catalogue.spec.ts`: zero critical and serious violations, every book reachable by keyboard with a visible focus indicator and no trap, and focus landing on the results heading after a page change
- [ ] T037 [US1] Manually work through the catalogue with VoiceOver on iOS and TalkBack on Android: every book announced by title and author, the position and result count announced on a page change, no unreachable content. Record the outcome and the versions tested in `specs/001-browse-catalogue/manual-verification.md`

**Checkpoint**: The catalogue is browsable, indexable and operable without sight or a mouse. This is the MVP.

---

## Phase 4: User Story 2 - Read about one book (Priority: P2)

**Goal**: A visitor opens a book and reads its description, author and chapter structure.

**Independent Test**: Open a seeded book directly by its address and confirm every piece of metadata is present, correctly structured for a screen reader, and that no audio is offered.

### Tests for User Story 2

- [X] T038 [P] [US2] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Catalogue/BookDetailEndpointTests.cs` for title, authors, description, cover, total running time, chapter count, and chapters returned in position order
- [X] T039 [P] [US2] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Catalogue/BookDetailNotFoundTests.cs`: an unknown slug and an unpublished slug both return 404 and are indistinguishable, and a book with no chapters, no description or no cover still returns 200
- [X] T040 [P] [US2] Extend `backend/tests/VoxLib.Api.Tests/Catalogue/NoAudioExposureTests.cs` to assert the same absence of audio on the detail response

### Implementation for User Story 2

- [X] T041 [US2] Implement the read by slug in `backend/src/VoxLib.Dal/Book/BookRepository.cs`, loading chapters in position order and authors in a defined order
- [X] T042 [US2] Implement the single book read in `backend/src/VoxLib.Orchestrator/Book/BookCatalogue.cs`, returning nothing for a book that is not listed so that unpublished and absent behave alike
- [X] T043 [US2] Add `BookDetail` and `Chapter` to `backend/src/VoxLib.Api/Book/Contracts/Responses/`, matching `contracts/catalogue.yaml`
- [X] T044 [US2] Add `GET /api/books/{slug}` in `backend/src/VoxLib.Api/Book/BookEndpoints.cs`, returning problem details on 404
- [X] T045 [US2] Regenerate `frontend/src/api/schema.d.ts` and commit the result
- [X] T046 [US2] Build the book route in `frontend/src/routes/book.tsx` with Ukrainian interface text, a heading structure, focus landing on the book's content after navigation, the statement that listening requires an account with no playback control, and the page language declared from the book's own language tag per FR-018
- [X] T047 [P] [US2] Build the chapter list in `frontend/src/components/ChapterList.tsx` as a list with a known item count and per-chapter running times, showing a plain message rather than an empty region when chapters are absent
- [X] T048 [US2] Build the not-found page in `frontend/src/routes/not-found.tsx`, naming what could not be found and carrying a route back to the catalogue
- [X] T049 [US2] Add every published book's address to the prerender list in `frontend/react-router.config.ts`, discovered from the API at build time
- [X] T050 [P] [US2] Automated accessibility run in `frontend/tests/a11y/book.spec.ts`: zero critical and serious violations, correct heading structure, and the chapter list announced as a list with its count
- [ ] T051 [US2] Manually complete the journey from the catalogue to a book's chapter list with VoiceOver on iOS and TalkBack on Android, without sighted assistance, which is the whole of SC-001. Record the outcome in `specs/001-browse-catalogue/manual-verification.md`

**Checkpoint**: Books are readable and their pages carry their own content in the served document.

---

## Phase 5: User Story 3 - Find a book by name (Priority: P3)

**Goal**: A visitor who knows a title or an author reaches it directly instead of paging.

**Independent Test**: With a seeded catalogue, search for a known title fragment and a known author fragment and confirm the matching books are returned, counted and announced. This story extends the listing built in Story 1.

### Tests for User Story 3

- [X] T052 [P] [US3] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Catalogue/BookSearchEndpointTests.cs`: matching part of a title, matching part of an author name, matching Ukrainian text in either letter case, and results excluding unpublished books
- [X] T053 [P] [US3] Endpoint tests in `backend/tests/VoxLib.Api.Tests/Catalogue/BookSearchEdgeCaseTests.cs`: an empty term, a whitespace-only term, a term above the length bound, a term matching nothing, and results spanning more than one page

### Implementation for User Story 3

- [X] T054 [US3] Implement the search read in `backend/src/VoxLib.Dal/Book/BookRepository.cs` using case-insensitive matching against title and author name, keeping the same total order as the plain listing
- [X] T055 [US3] Implement search in `backend/src/VoxLib.Orchestrator/Book/BookCatalogue.cs`, treating an empty or whitespace-only term as no term, and reusing the paging bounds
- [X] T056 [US3] Add the `q` parameter to `GET /api/books` in `backend/src/VoxLib.Api/Book/BookEndpoints.cs`, validating its length against the contract and returning 400 with problem details when it is malformed
- [X] T057 [US3] Regenerate `frontend/src/api/schema.d.ts` and commit the result
- [X] T058 [US3] Build the search route and form in `frontend/src/routes/search.tsx` with Ukrainian interface text, submittable without a pointing device and reachable by keyboard
- [X] T059 [US3] In `frontend/src/routes/search.tsx`, announce the number of results through a polite live region whenever the displayed set changes without moving focus, and build the empty results state that explains the outcome and offers a route back to the full catalogue
- [X] T060 [US3] Mark search result pages as not indexable in `frontend/src/routes/search.tsx` and exclude them from the prerender list in `frontend/react-router.config.ts`, per the exclusion now stated in FR-012
- [X] T061 [P] [US3] Automated accessibility run in `frontend/tests/a11y/search.spec.ts`: zero critical and serious violations, the result count announced politely, and the form fully operable by keyboard
- [ ] T062 [US3] Manually search with VoiceOver on iOS and TalkBack on Android, confirming the result count is announced without stealing focus and the empty state is reachable. Record the outcome in `specs/001-browse-catalogue/manual-verification.md`

**Checkpoint**: All three stories complete.

---

## Phase 6: Polish and Cross-Cutting Concerns

- [ ] T063 [P] Update `README.md` with the database prerequisite, the dev container rebuild step, and the fact that the frontend build now needs the API running
- [ ] T064 [P] Add a generated types drift check to `.github/workflows/ci.yml` that regenerates `frontend/src/api/schema.d.ts` and fails when it differs from the committed file
- [ ] T065 [P] Add a build output assertion in `frontend/tests/prerender/prerender.spec.ts` that a book page in `frontend/build/client/` contains the book's title and description as text, and that a search result page was not prerendered
- [ ] T066 [P] Measure the catalogue list against SC-006 in `frontend/tests/perf/catalogue-load.spec.ts`, throttled to 1.6 Mbps downlink with 150 ms round-trip latency, failing above 3 seconds to readable
- [ ] T067 Run every scenario in `specs/001-browse-catalogue/quickstart.md` and correct whatever does not match
- [ ] T068 Run the continuous integration parity commands: `dotnet format backend/VoxLib.slnx --verify-no-changes`, `dotnet test backend/VoxLib.slnx`, and `pnpm format:check && pnpm lint && pnpm build`
- [ ] T069 Rebuild the dev container from `.devcontainer/docker-compose.yml` from scratch and confirm the database, the seed and both dev servers come up. Nothing in continuous integration builds the container, so this is the only check on T001

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Setup. Blocks all user stories
- **User Stories (Phases 3 to 5)**: Sequential, in priority order
- **Polish (Phase 6)**: Depends on the stories you intend to ship

### Why the stories are sequential

Stories 1, 2 and 3 all edit `BookRepository.cs`, `BookCatalogue.cs`, `BookEndpoints.cs`,
`react-router.config.ts` and the generated `schema.d.ts`. Running them concurrently would
mean three writers per file. Story 3 also extends the endpoint Story 1 creates, so it cannot
start before Story 1 finishes. Parallelism inside a story is still available and marked.

### Within Setup

- T003 before T004, since the project must exist before packages are added to it
- T005, T006 and T007 all edit `frontend/package.json`, so only T005 is marked parallel. Run the three in sequence, or fold them into one edit of the manifest

### Within Foundational

- T008 before T010 and T011, since both consume the entities
- T011 before T012 and T013
- T012, T013 and T014 before T015, since the harness migrates and seeds

### Within Each Story

- Tests before implementation, and they must fail first
- Repository before orchestrator before endpoint
- Endpoint before type generation before the components that consume the types
- Prerender configuration after the routes exist
- The manual screen reader pass last, once the story is otherwise complete

---

## Parallel Example: User Story 1

```bash
# Four independent endpoint test files:
Task: "Ordering and page metadata in BookListEndpointTests.cs"
Task: "Paging bounds in BookListPagingTests.cs"
Task: "Absence of audio in NoAudioExposureTests.cs"
Task: "Anonymous access in AnonymousAccessTests.cs"

# Five independent frontend files, once the types exist:
Task: "Catalogue route in frontend/src/routes/catalogue.tsx"
Task: "Cover art in frontend/src/components/CoverArt.tsx"
Task: "Empty state in frontend/src/components/EmptyState.tsx"
Task: "Loading and error states in frontend/src/components/"
Task: "Overlong text handling in frontend/src/styles/catalogue.css"
```

---

## Implementation Strategy

### MVP First

1. Phase 1 Setup
2. Phase 2 Foundational
3. Phase 3, User Story 1, ending with the manual screen reader pass in T037
4. Stop and validate. This is a shippable increment on its own

### Incremental Delivery

Setup and Foundational, then Story 1, then Story 2, then Story 3, validating each before the
next. Each adds value without breaking what came before.

### If the feature is too large

Moving User Story 3 to its own specification removes Phase 5 entirely and is the cleanest
trim. The automated accessibility runs in T036, T050, T061 and T065 could also go, but the
manual passes in T037, T051 and T062 cannot: Principle I is non-negotiable and a feature that
is not operable with a screen reader is not done.

---

## Notes

- `[P]` means different files and no dependency on incomplete work
- Every behaviour change carries the endpoint test that proves it, as Principle III requires
- Automated checks catch violations and regressions; only the manual passes prove the product
  is usable by the people it exists for
- Commit after each task or logical group. Squash merging means commit granularity on the
  branch does not matter
- Stop at any checkpoint to validate a story
