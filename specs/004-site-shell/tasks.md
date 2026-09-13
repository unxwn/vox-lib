---
description: 'Task list for 004-site-shell'
---

# Tasks: Site Shell

**Input**: Design documents from `/specs/004-site-shell/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: Included, and not optional here. The spec asks for them directly (FR-056, FR-057),
the constitution's fifth gate requires every requirement to name what proves it, and
[quickstart.md](./quickstart.md) already maps all twenty-seven success criteria to something
runnable. Every test task below is the one quickstart names.

**Organization**: Grouped by user story, in the priority order the spec sets, so each story can
be built, tested and stopped at on its own.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: Which user story the task serves (US1 to US8)
- Every task names the exact file it touches

## Path Conventions

Web app, as `plan.md` fixes it: `backend/src/VoxLib.{Model,Orchestrator,Dal,Platform,Api}/`,
`backend/tests/VoxLib.Api.Tests/`, `frontend/src/`, `frontend/tests/`. Run every command from the
repository root; the package manager is pnpm.

## Reading the sequence

**Read this first: the catalogue is replaced in Phase 2.** The twenty-seven seeded books are
placeholders, discovered when the cover art this feature needed arrived and matched none of them.
The books the project actually holds are the four in `backend/src/VoxLib.Dal/Seed/content.md`,
with their covers in `covers/source/`. FR-064 to FR-068 fold the replacement into the foundational
phase, because the seed is already being rewritten there and because putting covers on books that
are about to be deleted is work done twice. The consequence runs through every later phase: every
browser test that named a placeholder book, searched for a placeholder term or relied on the
catalogue paging has to be rewritten, and those rewrites are tasks here rather than surprises at
T110.

Three splits are deliberate and are worth knowing before starting, because each one keeps a story
independently testable at the cost of touching a file twice:

- **The header's search form is built in US1, its behaviour in US3.** FR-001 puts the field in
  the banner, and US1's own landmark assertions fail if `/search` still carries a second search
  landmark in its body. So US1 builds the plain GET form and removes the body form; US3 adds the
  suggestions resource, the rotating example and the suggestion links.
- **`IAuthorRepository` is created in US3 and extended in US4.** US3 needs `SampleNamesAsync`
  first; US4 adds the index and detail queries to the same file.
- **The cover wire shape is emptied in Phase 2 and filled in US5.** Renaming `CoverArtUrl` to
  `CoverKey` stops the API compiling, so Phase 2 changes `BookSummary` to carry `Cover? Cover`
  and emits `null`. Every book shows the placeholder until US5, which is a designed state rather
  than a broken one, and the state the whole catalogue sits in between Phase 2 and US5.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: The container gains object storage. Do this first so nobody rebuilds twice.

- [X] T001 Add `minio` and a `minio-setup` sidecar to `.devcontainer/docker-compose.yml`, both with `network_mode: service:db` and a `vox-lib-minio` named volume for the data directory; the sidecar runs `mc alias set`, `mc mb --ignore-existing vox-lib-covers` and `mc anonymous set download`, per the table in [contracts/cover-storage.md](./contracts/cover-storage.md). Bucket creation and the public-read policy stay out of application code (R4)
- [X] T002 [P] Add `AWSSDK.S3` version 4.0.103.2 as a `PackageReference` in `backend/src/VoxLib.Platform/VoxLib.Platform.csproj`
- [X] T003 [P] Add the `Covers` configuration section (`ServiceUrl`, `Bucket`, `PublicBaseUrl`, `AccessKey`, `SecretKey`, `ForcePathStyle`) to `backend/src/VoxLib.Api/appsettings.Development.json`, with the development values from [contracts/cover-storage.md](./contracts/cover-storage.md), and the non-secret defaults in `backend/src/VoxLib.Api/appsettings.json`
- [X] T004 Rebuild the dev container, then verify from a terminal that `curl -s -o /dev/null -w '%{http_code}\n' localhost:9000/minio/health/live` answers `200` and that the `vox-lib-covers` bucket exists

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: One migration, one seed rework, and the catalogue's real content. Nothing above this
line compiles without it, because `Book.CoverArtUrl` is renamed and `Author` gains two required
columns.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T005 [P] Create `backend/src/VoxLib.Model/Book/AuthorSlug.cs` with a pure `From(string name)` performing the KMU 2010 transliteration, lowercasing and collapsing non-alphanumerics to single hyphens. It must reproduce the real book slugs exactly, which is how R3 chose it: the four cover files in `Seed/covers/source/` were named before the function was written, so `Щоденник сотника Устима. Як козаки Кавказ воювали` must give `shchodennyk-sotnyka-ustyma-yak-kozaky-kavkaz-voiuvaly`. Keep a unit case for the Ґ and Г distinction (`Ґудзик` to `gudzyk`, `Гайдамаки` to `haidamaky`) even though no real title exercises it, because it is the pair the system is most often got wrong on
- [X] T006 [P] Add required `Slug` and `SortName` to `backend/src/VoxLib.Model/Book/Author.cs`
- [X] T006a [P] Create `backend/src/VoxLib.Model/Book/CreditRole.cs`, an enum of exactly `Author` and `Compiler` (FR-066). Two values, not an open vocabulary: translator is deferred with everything else the spec's assumptions defer, and FR-040's narrator stays a name on the book because it is about a recording rather than about the text
- [X] T006b [P] In `backend/src/VoxLib.Model/Book/Book.cs`, change `Authors` from `IReadOnlyList<Author>` to a list of credits pairing an `Author` with a `CreditRole`, per [data-model.md](./data-model.md). The role belongs to the pairing: on the author it would be wrong the first time someone writes one book and compiles another, and on the book it would be wrong the first time a book has two kinds of credit
- [X] T007 [P] In `backend/src/VoxLib.Model/Book/Book.cs`, replace `CoverArtUrl` with a nullable `CoverKey` (an object-key stem, never a URL), add a nullable `Narrator` and a required `AddedToCatalogue`
- [X] T008 Add `Slug` and `SortName` to `backend/src/VoxLib.Dal/Book/AuthorDao.cs`, and `CoverKey`, `Narrator` and `AddedToCatalogue` to `backend/src/VoxLib.Dal/Book/BookDao.cs`
- [X] T009 Configure the new columns in `backend/src/VoxLib.Dal/Persistence/VoxLibDbContext.cs`: a unique `ix_authors_slug`, `sort_name` under the `uk-UA-x-icu` collation the catalogue already uses, `narrator` nullable at 300, `added_to_catalogue_at` required, `cover_key` nullable at 200, and `role` on the book-to-author join required with a default of `Author`
- [X] T010 Generate the `SiteShell` migration into `backend/src/VoxLib.Dal/Persistence/Migrations/` with `dotnet tool restore && dotnet ef migrations add SiteShell --project backend/src/VoxLib.Dal`, then hand-edit its `Up` to run the five steps in the order [data-model.md](./data-model.md) fixes: slug and sort name added nullable, backfilled, made `NOT NULL`; `narrator` added; `added_to_catalogue_at` added `NOT NULL DEFAULT now()`; `cover_art_url` renamed to `cover_key` with its values rewritten from `/covers/{stem}.svg` to `{stem}`; `book_authors.role` added `NOT NULL DEFAULT 'Author'`. Write it correct against a database that already has rows, including a deployed one. Replacing the catalogue is the seeder's job in T012 and never the migration's
- [X] T011 [P] Create `backend/src/VoxLib.Dal/Seed/authors.json` holding one row per person credited in `content.md`, as `{ name, slug, sortName }`, with sort names hand-written in `Прізвище, Ім'я` form. Four rows: Джоко Віллінк, Ігор Козловський, Дмитро Савченко and Валерій Бобрович. Slugs come from `AuthorSlug.From` and are checked against it rather than typed. Every placeholder author row goes (FR-064)
- [X] T012 [P] Replace `backend/src/VoxLib.Dal/Seed/books.json` with the four books in [content.md](../../backend/src/VoxLib.Dal/Seed/content.md), transcribing title, description and credit for each, and deleting all twenty-seven placeholders (FR-064). Per book: a `slug` matching its cover file's stem in `covers/source/`, a `coverKey` equal to that stem, `language: "uk"`, `publicationState: "Published"`, an `authors` array of `{ name, role }` where Дмитро Савченко on the Konovalets book carries `compiler` and the other three carry `author` (FR-066), and one chapter whose `runningTimeSeconds` is the book's whole running time from `content.md`: 38400, 10620, 27600 and 24000 respectively (FR-065). `content.md` is the human-readable source and stays in the repository as prose; it is never parsed at runtime
- [X] T013 Add `Seed\authors.json` to the `EmbeddedResource` items in `backend/src/VoxLib.Dal/VoxLib.Dal.csproj`
- [X] T014 Rework `backend/src/VoxLib.Dal/Seed/CatalogueSeeder.cs` to build authors from `authors.json` rather than from distinct names in `books.json`, assigning each slug once through `AuthorSlug.From`, throwing on a duplicate slug rather than appending a number (FR-038), reading the per-credit `role` into `CreditRole` (FR-066), and failing loudly when a book credits a name `authors.json` does not hold
- [X] T014a [P] Create `backend/tests/VoxLib.Api.Tests/Catalogue/CatalogueSeedTests.cs` asserting the catalogue the API serves is exactly the books in `content.md`, matched by title in both directions rather than by count, and that each reports `totalRunningTimeSeconds > 0` (SC-028). A count alone would pass against the wrong four
- [X] T014b [P] Create `backend/tests/VoxLib.Api.Tests/Catalogue/AuthorCreditRoleTests.cs` asserting one person credited in two roles across two books resolves to one author row with one page, and a different role on each book (FR-066). Seed the second pairing in the test rather than in `books.json`, which holds only what the project actually has
- [X] T015 Map the new columns in `backend/src/VoxLib.Dal/Book/BookMapping.cs`, which stays the one place a DAO becomes a domain object
- [X] T016 In `backend/src/VoxLib.Api/Book/Contracts/Responses/BookSummary.cs`, replace `string? CoverArtUrl` with `Cover? Cover` and add the `Cover` record (a list of `{ url, width }` sources) per [contracts/catalogue.yaml](./contracts/catalogue.yaml); have `backend/src/VoxLib.Api/Book/BookEndpoints.cs` emit `null` for it until US5 composes real URLs
- [X] T017 Update `backend/tests/VoxLib.Api.Tests/Infrastructure/CatalogueResponses.cs` and any catalogue test reading `coverArtUrl` to the new shape, then run `dotnet format backend/VoxLib.slnx --verify-no-changes` and `dotnet test backend/VoxLib.slnx` and confirm the existing catalogue and account suites pass against the migrated schema
- [X] T017a Rewrite every endpoint test that names a placeholder book, author or search term to the real catalogue. Expect this to be wider than it looks: the existing suite asserts specific titles, specific authors and specific result counts, and four books do not paginate where twenty-seven did. Where an assertion needs more books than the catalogue has, seed them in the fixture rather than in `books.json`

**Checkpoint**: The schema, the seed, the domain and the real catalogue are in place. User stories
can begin.

---

## Phase 3: User Story 1 - Every page has a skeleton (Priority: P1) 🎯 MVP

**Goal**: A banner and a contentinfo on every page, a named landmark set, a skip link, a
breadcrumb trail below the top level, and account controls that do not move the ground under a
keyboard user when the session resolves.

**Independent Test**: Open every page in `PAGES_IN_SCOPE` with a keyboard. Each presents the
header, the footer, a banner, a main, a contentinfo and distinctly named navigation and search
landmarks; each page below the top level carries a trail whose last item is `aria-current="page"`;
one Tab from the top reaches the skip link.

### Implementation for User Story 1

- [X] T018 [P] [US1] Create `frontend/src/account/SessionProvider.tsx` holding a module-level promise so the session is fetched once per page view however many components ask (FR-009, R8)
- [X] T019 [US1] Rewrite `frontend/src/account/useSession.ts` to read the provider instead of holding per-call-site state, keeping the `Session` union and the `refresh` contract the account screens already use
- [X] T020 [P] [US1] Create `frontend/src/catalogue/breadcrumbs.ts` deriving the trail from the current address against the table in [contracts/site-addresses.md](./contracts/site-addresses.md), returning an empty trail for the landing page and for the not-found page. Derived, never authored per page, which is what makes FR-031 hold by construction
- [X] T021 [P] [US1] Create `frontend/src/components/Breadcrumbs.tsx` rendering `<nav aria-label="Навігаційний ланцюжок">` around an `<ol>`, every item but the last a link, the last not a link and carrying `aria-current="page"` (FR-004)
- [X] T022 [P] [US1] Create `frontend/src/components/HeaderSearch.tsx` as a plain `<form role="search" aria-label="Пошук по каталогу" method="get" action="/search">` with a visible `<label>`, an `input[name=q][type=search][maxlength=100]` carrying the current term from the address, and a submit button (FR-012 to FR-016). The rotating example arrives in US3
- [X] T023 [US1] Rework `frontend/src/components/SessionMenu.tsx` into the banner's account slot: last in source order, reserving its resolved size while loading and containing no focusable element in that state (FR-008, R8)
- [X] T024 [US1] Create `frontend/src/components/SiteHeader.tsx` as `<header>`, a direct child of `<body>`, in the source order [contracts/site-addresses.md](./contracts/site-addresses.md) fixes: skip link to `#main`, `vox-lib` wordmark as readable text linking home, `<nav aria-label="Основна навігація">` with Каталог, Автори and Про проєкт, the search form, then the account slot (FR-001, FR-002, FR-005, FR-007, FR-010)
- [X] T025 [P] [US1] Create `frontend/src/components/SiteFooter.tsx` as `<footer>`, a direct child of `<body>`, saying what vox-lib is and who runs it, linking `/about` and linking `/fonts/OFL.txt`, which ships with the site and is currently linked from nowhere (FR-003)
- [X] T026 [US1] Compose the shell in `frontend/src/root.tsx`: `SessionProvider` around `SiteHeader`, `<main id="main">` holding `Breadcrumbs` and the `Outlet`, then `SiteFooter`, with `Ground` staying the first child of `<body>` as `003-visual-identity` left it
- [X] T027 [US1] Create `frontend/src/styles/shell.css` for the header, the footer, the breadcrumbs and the skip link, importing it from `root.tsx`. The header and the footer must carry fully opaque fills, or every contrast result on the site turns from pass to incomplete and the suite stays green while checking nothing. Every value comes from `tokens.css` (FR-063). The bar wraps down to 320 device-independent pixels with no disclosure control (FR-006, R12)
- [X] T028 [US1] Remove the body search form from `frontend/src/routes/search.tsx` and the in-body search link from `frontend/src/routes/catalogue.tsx`, so no page carries two search inputs or two search landmarks (FR-011)
- [X] T028a [US1] Rewrite the two tests in `frontend/tests/a11y/search.spec.ts` that operate the search form in the body of `/search`, at lines 33 and 50 today, to operate the header field instead. They currently find the field by the accessible name `Назва книжки або ім'я автора`; whatever visible label T022 gives the header field is the name they must use, so fix the label in one place and the selector in the other. Also delete or rewrite `the search is reachable from the catalogue` at line 138, which clicks the in-body link T028 removes and does it from `/`, which is no longer the catalogue
- [X] T029 [US1] Update `frontend/tests/a11y/pages.ts`: point `awaitSessionMenu` at the account slot's resolved state and adjust any `settled` selector the shell changes
- [X] T030 [P] [US1] Create `frontend/tests/a11y/shell.spec.ts` walking `PAGES_IN_SCOPE` and asserting, per page: a banner, a main and a contentinfo landmark; a distinct accessible name on every navigation and search landmark (SC-001); exactly one element whose accessible name is the wordmark, with `.ground` still exposing nothing (SC-003); the breadcrumb trail against the table in [contracts/site-addresses.md](./contracts/site-addresses.md) with the last item `aria-current="page"` and every earlier item resolving (SC-004); one Tab from the top reaching the skip link, and activating it moving focus into `main` (FR-007); and the catalogue, the search field and the about page all reachable from the page itself (SC-002)
- [X] T031 [P] [US1] Create `frontend/tests/a11y/session-chrome.spec.ts` proving the reading of SC-005 that R8 records: with the `/api/account/session` request held, capture every tab stop and every bounding box; release it and assert every tab stop before the account controls is identical and every bounding box is unchanged. In the same spec, count requests to `/api/account/session` on a page with more than one consumer and assert exactly one (SC-006)

**Checkpoint**: The site has a skeleton on every page. This is the MVP and is worth stopping at.

---

## Phase 4: User Story 2 - A front door, and addresses that match the hierarchy (Priority: P2)

**Goal**: A landing page at the root, the catalogue under `/books`, an about page, and a permanent
redirect from every old numbered catalogue address.

**Independent Test**: The root address explains the product and offers a way into the catalogue;
every address matches its place in its trail; `curl -s -o /dev/null -w '%{http_code} %{redirect_url}' localhost:5173/page/2`
answers `301` and the new location.

### Implementation for User Story 2

- [X] T032 [P] [US2] Create `frontend/src/routes/landing.tsx` saying what vox-lib is, who it is for, that it is free and why an account is needed, with a way into the catalogue (FR-025). All copy in Ukrainian (FR-062)
- [X] T033 [P] [US2] Create `frontend/src/routes/about.tsx`, the page the footer and the header both link
- [X] T034 [US2] Rewrite the address table in `frontend/src/routes.ts` to `/` landing, `/books` catalogue, `/books/page/:page`, `/books/:slug`, `/authors`, `/authors/:slug`, `/search`, `/about`, the five account routes unchanged, and `*`. Author routes may be added here or in US4; R2 records why a static segment ranks above a dynamic one so `/books/page/2` never shadows a book
- [X] T035 [US2] Change `catalogueHref` in `frontend/src/catalogue/page.ts` to `page <= 1 ? '/books' : '/books/page/' + page`, keeping it the only place that rule is written, so the landing page, the header, the breadcrumbs and pagination all lead to one address for page one (FR-030)
- [X] T036 [US2] Update the trail table in `frontend/src/catalogue/breadcrumbs.ts` to the new addresses, adding Про проєкт and leaving the landing page and the not-found page without a trail
- [X] T037 [P] [US2] Create `frontend/site-rules.ts` holding the permanent-redirect table (`/page/1` to `/books`, `/page/:n` to `/books/page/:n`) as the one place those rules are written, for whatever ends up serving the build as much as for the dev server (R1)
- [X] T038 [US2] Create `frontend/vite/site-rules-plugin.ts` installing the redirect table as Connect middleware ahead of the React Router dev middleware, answering `301` with the new `Location`, and register the plugin in `frontend/vite.config.ts`
- [X] T039 [US2] Extract the prerender path discovery from `frontend/react-router.config.ts` into `frontend/scripts/catalogue-paths.mjs`, keeping it reading the API rather than the seed file, and have `react-router.config.ts` import it. It emits `/`, `/books`, `/books/page/:n` and `/books/:slug` at their new addresses (FR-032)
- [X] T040 [US2] Update `frontend/tests/a11y/pages.ts` to the new address list, adding the landing page and the about page and moving the catalogue entries to `/books` and `/books/page/2`, and point `BOOK` at a real slug. This file is also what puts the new pages in scope for `target-size.spec.ts`, `contrast.spec.ts`, `typography.spec.ts` and `high-contrast.spec.ts`, which all read `PAGES_IN_SCOPE`: it is the only thing that discharges SC-024 and SC-025 for them, so do not treat it as bookkeeping
- [X] T040a [US2] Rewrite `frontend/tests/a11y/catalogue.spec.ts` for the new address and the real catalogue. All seven tests open `/`, which is now the landing page, so all seven move to `/books`. The book they reach for by name and the book seeded without cover art both change to real ones, and the three pagination tests move onto the fixture in T040c
- [X] T040b [US2] Rewrite `frontend/tests/a11y/book.spec.ts` for the real books: the five placeholder slugs at lines 16, 79, 89, 124 and 143 become real ones, and `focus lands on the book when it is reached from the catalogue` opens `/books` rather than `/`
- [X] T040c [P] [US2] Create `frontend/tests/a11y/paging-fixture.ts` seeding a catalogue past one page, and move the three pagination assertions in `catalogue.spec.ts` and the paging assertion in `search.spec.ts` onto it (FR-068, SC-031). Four books against `PageRequest.FixedPageSize = 20` means nothing paginates, and without this those four assertions keep passing while testing nothing, which is the same failure mode as the contrast incompletes in `CLAUDE.md`
- [X] T041 [P] [US2] Create `frontend/tests/a11y/addresses.spec.ts` requesting every old catalogue address and asserting `301` with the new `Location` (SC-011), and asserting that every link leading to the catalogue's first page resolves to `/books` (FR-030)
- [X] T042 [P] [US2] Create `frontend/tests/a11y/landing.spec.ts` asserting the landing page says what the site is, who it is for, that it is free and why an account is needed, and offers a way into the catalogue (FR-025), with the axe run the other page specs make
- [X] T043 [US2] Extend `frontend/tests/prerender/prerender.spec.ts` to check the emitted documents for the landing page, the about page and the catalogue at its new addresses against what the API says exists (SC-012)

**Checkpoint**: The hierarchy is real, the trails tell the truth, and old bookmarks land correctly.

---

## Phase 5: User Story 3 - Search from anywhere, with an example (Priority: P3)

**Goal**: One resource supplying both the rotating example inside the field and the suggestion
links beneath it, drawn fresh from the published catalogue on every request.

**Independent Test**: Run a search by keyboard alone from every page and return to the result from
its address in a fresh session; confirm the example and the suggestions come from the catalogue,
and that every suggestion, searched, returns at least one result.

### Implementation for User Story 3

- [X] T044 [P] [US3] Create `backend/src/VoxLib.Model/Book/ICatalogueSuggestions.cs` and the `SearchSuggestion` domain type (`term`, `kind` of `title` or `author`) in `backend/src/VoxLib.Model/Book/`
- [X] T045 [P] [US3] Create `backend/src/VoxLib.Model/Book/IAuthorRepository.cs` with `SampleNamesAsync`. US4 extends this same interface with the index and detail queries
- [X] T046 [US3] Add `SampleTitlesAsync` to `backend/src/VoxLib.Model/Book/IBookRepository.cs` and implement it in `backend/src/VoxLib.Dal/Book/BookRepository.cs`, sampling published books only
- [X] T047 [US3] Create `backend/src/VoxLib.Dal/Book/AuthorRepository.cs` implementing `SampleNamesAsync` over authors credited on at least one published book
- [X] T048 [US3] Create `backend/src/VoxLib.Orchestrator/Book/CatalogueSuggestions.cs` holding the sampling rule: at most five, split half titles and half author names rounded towards titles, published rows only, taken verbatim with nothing composed, shortened or prettified, because that verbatim copy is the whole of FR-023's guarantee (R6). The rule is a business rule and belongs here rather than in the handler
- [X] T049 [P] [US3] Create `backend/src/VoxLib.Api/Search/Contracts/Responses/SearchSuggestion.cs` matching [contracts/catalogue.yaml](./contracts/catalogue.yaml)
- [X] T050 [US3] Create `backend/src/VoxLib.Api/Search/SearchEndpoints.cs` serving `GET /api/search-suggestions` with `Cache-Control: no-store`, which is load-bearing rather than tidy: a cached response freezes the sample per visitor while it stays fresh per request on the server (R6)
- [X] T051 [US3] Register `IAuthorRepository`, `ICatalogueSuggestions` and `MapSearchEndpoints()` in `backend/src/VoxLib.Api/Program.cs`
- [X] T052 [P] [US3] Create `backend/tests/VoxLib.Api.Tests/Catalogue/BookSearchSuggestionsTests.cs` taking every term the endpoint returns, searching for it through `/api/books?q=`, and asserting at least one result (SC-010); assert the shape rather than the content, as the clarification settled, plus the five-item cap and the `no-store` header
- [X] T053 [P] [US3] Create `backend/tests/VoxLib.Api.Tests/Catalogue/SearchSuggestionsEmptyCatalogueTests.cs` against `EmptyCatalogueApiFixture`, asserting an empty list rather than an error
- [X] T054 [US3] Regenerate `frontend/src/api/schema.d.ts` with `pnpm --filter frontend gen:api-types` against the running API
- [X] T055 [P] [US3] Create `frontend/src/search/useSuggestions.ts` fetching the resource once per page view and sharing the answer, and returning an empty list rather than failing when it cannot be read (the edge case the spec names: search is never blocked by the thing that decorates it)
- [X] T056 [US3] Add the rotating example to `frontend/src/components/HeaderSearch.tsx`: the placeholder cycles the suggestions every six seconds in the form `напр. Кобзар`, freezes permanently the first time the field takes focus, never starts when `matchMedia('(prefers-reduced-motion: reduce)')` matches, and falls back to a fixed neutral placeholder when no suggestion is available. The visible label names the field in every one of those states (FR-017 to FR-019, R7)
- [X] T056a [US3] Rewrite `the field has a real label, not a placeholder standing in for one` in `frontend/tests/a11y/search.spec.ts`. It asserts `not.toHaveAttribute('placeholder', /.+/)` on the search field, which T056 makes false by design. What FR-019 actually protects is that the label, not the example, says what the field is for, so assert instead: a visible `<label>` associated with the field, an accessible name that comes from it and not from the placeholder, and that the name is unchanged while the example rotates. Do not simply delete the test; it is the guard that stops the placeholder becoming the label again
- [X] T057 [P] [US3] Create `frontend/src/components/SearchSuggestions.tsx` rendering the suggestions as ordinary links that run the search they name. Links, not buttons and not a listbox, because a link can be reached, announced and activated by keyboard and screen reader (FR-020)
- [X] T058 [US3] Render the suggestions in `frontend/src/routes/search.tsx` before anything has been typed, in place of the results, and in `frontend/src/routes/not-found.tsx` (FR-021). Nothing suggests results while a visitor types (FR-024)
- [X] T059 [P] [US3] Create `frontend/tests/a11y/header-search.spec.ts`: type and press Enter on each page in scope, then reload the resulting address in a fresh context (SC-007); focus the field, wait past two intervals and assert the placeholder is unchanged, then repeat under `emulateMedia({ reducedMotion: 'reduce' })` asserting zero changes from the start (SC-008); assert every suggestion is a keyboard-reachable link announced as a link, on `/search` and on the not-found page (SC-009); and assert exactly one `role="search"` landmark and one `input[name=q]` per page (FR-011)

**Checkpoint**: Search works from anywhere, by keyboard, with real examples from the catalogue.

---

## Phase 6: User Story 4 - Browse by author (Priority: P4)

**Goal**: Authors become a resource with an index and a page each, every credited name is a link,
and the index orders by surname.

**Independent Test**: Open a book credited to an author who appears on more than one book, follow
their name, and confirm the page lists every published book they are credited on and none they
are not.

### Implementation for User Story 4

- [X] T060 [P] [US4] Create `backend/src/VoxLib.Model/Book/IAuthorCatalogue.cs` and the author read models it returns, beside `IBookCatalogue` rather than introducing a new pattern
- [X] T061 [US4] Extend `backend/src/VoxLib.Dal/Book/AuthorRepository.cs` with the index query (authors with at least one published book, ordered by `sort_name` under the Ukrainian collation and settled by slug so the order is total and stable) and the detail query (one author by slug with every published book they are credited on)
- [X] T062 [US4] Create `backend/src/VoxLib.Orchestrator/Book/AuthorCatalogue.cs` holding the published-only and ordering rules, so an author with no published books is absent from the index and not found at their address (FR-037)
- [X] T063 [P] [US4] Create `AuthorIdentity.cs`, `AuthorReference.cs`, `AuthorSummary.cs` and `AuthorDetail.cs` in `backend/src/VoxLib.Api/Author/Contracts/Responses/`, matching [contracts/catalogue.yaml](./contracts/catalogue.yaml). The split matters: `AuthorReference` is a credit on one book and carries a role; `AuthorSummary` is an index entry and carries the set of roles that person holds across their published books, because one person can be an author on one and a compiler on another and the index cannot claim a single role for them
- [X] T064 [US4] Create `backend/src/VoxLib.Api/Author/AuthorEndpoints.cs` serving `GET /api/authors` (unpaged) and `GET /api/authors/{slug}`, answering `404` for an absent slug and for an author with no published books alike
- [X] T065 [US4] In `backend/src/VoxLib.Api/Book/Contracts/Responses/BookSummary.cs` and `BookDetail.cs`, change `authors` from bare strings to `AuthorReference`, carrying slug and role, so a name can be a link that says how the person is credited without a second request (FR-033, FR-066), and add `narrator` to `BookDetail` (FR-040); update `backend/src/VoxLib.Api/Book/BookEndpoints.cs` accordingly
- [X] T066 [US4] Register `IAuthorCatalogue` and `MapAuthorEndpoints()` in `backend/src/VoxLib.Api/Program.cs`
- [X] T067 [P] [US4] Create `backend/tests/VoxLib.Api.Tests/Catalogue/AuthorEndpointTests.cs` asserting every author on a book detail resolves at their own address, and that their page's book list matches a direct query of what they are credited on (SC-013)
- [X] T068 [P] [US4] Create `backend/tests/VoxLib.Api.Tests/Catalogue/AuthorIndexOrderTests.cs` asserting the index against the expected Ukrainian order rather than against insertion order (SC-014), and that it lists everyone credited on a published book whatever their role, so Дмитро Савченко appears with `roles` containing `compiler` (SC-029, FR-035)
- [X] T069 [P] [US4] Create `backend/tests/VoxLib.Api.Tests/Catalogue/AuthorNotFoundTests.cs` covering both an absent slug and an author with no published books (FR-037)
- [X] T070 [P] [US4] Create `backend/tests/VoxLib.Api.Tests/Catalogue/AuthorSlugTests.cs` asserting the seeder throws on a duplicate slug rather than appending a number (FR-038), and that `AuthorSlug.From` reproduces the four real book slugs exactly, including `Щоденник сотника Устима. Як козаки Кавказ воювали` to `shchodennyk-sotnyka-ustyma-yak-kozaky-kavkaz-voiuvaly`. The collision case is constructed in the test: R3 found one in the placeholder catalogue, that catalogue is gone, and the rule is worth proving on its own merit rather than because the seed happened to violate it
- [X] T071 [US4] Update `backend/tests/VoxLib.Api.Tests/Infrastructure/CatalogueResponses.cs` and the existing catalogue tests to the version 2 author shape
- [X] T072 [US4] Regenerate `frontend/src/api/schema.d.ts` against the running API
- [X] T073 [P] [US4] Create `frontend/src/components/AuthorLink.tsx`, the one place a credited name becomes a link, showing the role beside the name for anything other than an author (FR-033, FR-066). Ukrainian copy per FR-062: `упорядник` for a compiler. An author needs no label, because it is what a reader assumes; a compiler does, because assuming it there would be wrong
- [X] T074 [P] [US4] Create `frontend/src/routes/authors.tsx`, the index, listing everyone credited on at least one published book in the order the API returns, with how they are credited (SC-029)
- [X] T075 [P] [US4] Create `frontend/src/routes/author.tsx`, one author with every published book they are credited on and a link to each
- [X] T076 [US4] Add `/authors` and `/authors/:slug` to `frontend/src/routes.ts` if not already present, their trails to `frontend/src/catalogue/breadcrumbs.ts`, author paths to `frontend/scripts/catalogue-paths.mjs` so every author gets a document (FR-032), and both pages to `frontend/tests/a11y/pages.ts`
- [X] T077 [US4] Render credited names through `AuthorLink` in `frontend/src/components/BookList.tsx` and `frontend/src/routes/book.tsx`, and show the narrator on the book page when one is recorded (FR-040). Know going in that no book has one: `content.md` records no озвучувач for any of the four, so this branch renders for nothing in the seeded catalogue and only T077a proves it works at all. That is the honest state of FR-040 in this feature, and it is why the field is worth adding now rather than pretending it is exercised
- [X] T077a [P] [US4] Add to `backend/tests/VoxLib.Api.Tests/Catalogue/` an assertion that a book seeded with a narrator returns it on `BookDetail` and a book without one returns null (FR-040). Seed the value in the fixture, since the real catalogue has none
- [X] T078 [US4] Extend `frontend/src/styles/catalogue.css` for the author index and the author page, taking every value from `tokens.css`
- [X] T079 [P] [US4] Create `frontend/tests/a11y/authors.spec.ts` asserting a book reaches its author in one step, the author page lists exactly the published books they are credited on, the index is complete and names how each person is credited, and an unknown author address renders the not-found page with a way to search (SC-013, SC-029, FR-037)

**Checkpoint**: Authors are a resource. Books, authors and the index all reach each other.

---

## Phase 7: User Story 5 - Real covers, and a missing file that says it is missing (Priority: P5)

**Goal**: Covers served from object storage at three prepared widths, no broken image anywhere,
and a request for a file the site does not have answering `404` instead of a page.

**Independent Test**: Open the catalogue and every book page and confirm no broken image; then
`curl -s -o /dev/null -w '%{http_code} %{content_type}\n' localhost:5173/covers/nothing.svg`
answers `404` while `/no-such-page` still answers `200 text/html`.

### Implementation for User Story 5

- [X] T080 [P] [US5] Create `backend/src/VoxLib.Model/Storage/ICoverStorage.cs` with exactly three members: `PublicUrl`, `ExistsAsync`, `PutAsync`. The absences are the contract: no signing, no delete, no list, no bucket administration and no `GetAsync`, so this feature is incapable of expressing a private object and the audio interface cannot arrive by widening this one
- [X] T081 [P] [US5] Create `backend/src/VoxLib.Platform/Storage/CoverStorageOptions.cs` binding the `Covers` section from T003
- [X] T082 [US5] Create `backend/src/VoxLib.Platform/Storage/S3CoverStorage.cs` on `AWSSDK.S3`, one implementation for MinIO and R2 with nothing but configuration between them. `PublicUrl` is pure composition from `PublicBaseUrl` over `covers/{cover-key}-{width}.webp` and touches no network, which is what keeps a catalogue response from ever failing because of storage (FR-051)
- [X] T083 [US5] Prepare the cover images at widths 160 and 320 into `backend/src/VoxLib.Dal/Seed/covers/` from the four sources already committed in `covers/source/`, following the `magick` loop in [quickstart.md](./quickstart.md). Two widths, not three: three of the four sources are 507, 517 and 550 pixels wide, so a 640 would exist only by upscaling. Add `Seed\covers\*.webp` to the `EmbeddedResource` items in `backend/src/VoxLib.Dal/VoxLib.Dal.csproj`, matching the prepared files only. A glob over `Seed\covers\**` would compile the 3.7 MB of source PNGs into the assembly as well, which is the kind of thing nobody notices until the package is built
- [X] T084 [US5] Create `backend/src/VoxLib.Dal/Seed/CoverSeeder.cs` uploading each embedded cover the bucket does not already hold, guarded per object rather than per run, because the database and the bucket are separate volumes with separate lifetimes (R5, FR-047). An unreachable bucket is logged and startup continues
- [X] T085 [US5] Register `CoverStorageOptions`, `ICoverStorage` and `CoverSeeder` in `backend/src/VoxLib.Api/Program.cs`, and call `CoverSeeder` in the existing startup scope beside `CatalogueSeeder`
- [X] T086 [US5] Compose the `Cover` response from `ICoverStorage` in `backend/src/VoxLib.Api/Book/BookEndpoints.cs`, emitting the three prepared widths for a book with a `CoverKey` and `null` for a book without one
- [X] T087 [P] [US5] Create `backend/tests/VoxLib.Api.Tests/Catalogue/CoverStorageOutageTests.cs` with the adapter pointed at a dead endpoint, asserting the catalogue still answers and every page still renders (FR-051)
- [X] T088 [US5] Regenerate `frontend/src/api/schema.d.ts` against the running API
- [X] T089 [US5] Rework `frontend/src/components/CoverArt.tsx` to take the `Cover` shape, render `srcset` and `sizes` over the prepared widths so the browser picks and nothing is resized on request (FR-045), and keep an `onError` that swaps to the placeholder, since a failed `img` is exactly the broken image icon SC-015 forbids. Set `width` and `height` attributes from the rendered size so the box is reserved before the image loads and nothing shifts under a reader
- [X] T090 [US5] Enlarge the covers in `frontend/src/styles/catalogue.css` to 128 wide in the catalogue and 192 wide on the book page at a 1280 viewport, still reflowing without horizontal scrolling at 320 (FR-049). Fix the width and let the height follow the image: replace the fixed `height` and `object-fit: cover` on `.cover-art` with `height: auto` (FR-067). The four covers run 0.640 to 0.753 wide against tall and a fixed 2:3 box would crop about 11% off the tallest. Nothing ragged results, because covers appear only as a fixed-width item in the two flex rows `.catalogue__item` and `.book__summary`. `.cover-art--placeholder` is the exception and needs an explicit `aspect-ratio`, having no image to take proportions from
- [X] T091 [US5] Add the static-file rule to `frontend/site-rules.ts` (a path whose last segment contains a dot and which no file satisfies) and install it as middleware in `frontend/vite/site-rules-plugin.ts`, answering `404` rather than falling through to the single-page document, while an address matching no route still renders the not-found page (FR-048, R1)
- [X] T092 [P] [US5] Create `frontend/tests/a11y/covers.spec.ts` asserting `naturalWidth > 0` for every `img` on the catalogue, every book page and every author page (SC-015); measuring the rendered box at 1280 and the reflow at 320 (SC-017); asserting no cover is cropped, by comparing each rendered box's ratio against its own `naturalWidth / naturalHeight` within a pixel, with the placeholder the one element allowed a fixed ratio (SC-030); and checking the catalogue's first page is still readable within 3 seconds under a 1.6 Mbps and 150 ms CDP throttle (SC-018). Note in the file that SC-018 has little to strain against at four books, so it is a regression guard for later rather than a measurement of a real load today
- [X] T093 [US5] Extend `frontend/tests/a11y/addresses.spec.ts` with the missing-file assertions: a request for a file that does not exist is `404`, and an address that matches no route is still the not-found page with header, footer and a way to search (SC-016)

**Checkpoint**: Covers are real and a missing file is visible to a status check.

---

## Phase 8: User Story 6 - The next step is the obvious one (Priority: P6)

**Goal**: A primary action colour role, defined once, so the control a visitor came to press does
not look like the fields above it; and a book page that links to registration instead of only
naming what the visitor lacks.

**Independent Test**: Open every form and confirm its submit control is distinguishable from the
fields around it by more than its label; open a book page signed out and confirm the account
message carries a link to registration.

### Implementation for User Story 6

- [X] T094 [US6] Add `--action: #2aa37d` and `--on-action: #070d22` to `frontend/src/styles/tokens.css`, beside the thirteen roles already there (FR-052). No hover role: this site has no hover styles anywhere, and adding one here would make the primary action the only element that responds to a pointer (R9)
- [X] T095 [US6] Add the three constrained rows to `CONTRAST_FLOORS` in `frontend/scripts/check-design-tokens.mjs`: `--action` on `--surface` at 3:1, `--action` on `--surface-raised` at 3:1, `--on-action` on `--action` at 4.5:1. Do not add `--focus` against `--action`; [data-model.md](./data-model.md) records why 2.06:1 there is not a violation
- [X] T096 [US6] Give the submit control of every form the action treatment in `frontend/src/styles/account.css` and `frontend/src/styles/base.css`, so it carries a treatment no other control on that form uses (FR-053)
- [X] T097 [US6] Link the "account needed to listen" message on `frontend/src/routes/book.tsx` to `/register`, and confirm no play control, disabled or otherwise, appears anywhere (FR-055)
- [X] T097a [P] [US6] Add the assertion for FR-055 to `frontend/tests/a11y/book.spec.ts`, which [quickstart.md](./quickstart.md) already names as its proof and which nothing else was going to touch: the message about needing an account carries a link, and that link resolves to `/register`
- [X] T098 [P] [US6] Extend `frontend/tests/a11y/contrast.spec.ts` so that, on every form, the submit control's computed fill differs from every other control's on that form (SC-019)
- [X] T099 [P] [US6] Add to `frontend/tests/a11y/shell.spec.ts` an assertion that no control whose accessible name matches the play vocabulary appears on any page in scope, disabled or otherwise (SC-020)

**Checkpoint**: The action a visitor came for looks like one, everywhere.

---

## Phase 9: User Story 7 - The signed in half of the site can be checked (Priority: P7)

**Goal**: A browser fixture that reaches a signed-in session with no manual step, and the audits
nothing has ever run.

**Independent Test**: Run the browser suite twice in a row with no manual step and confirm it
reaches a signed-in session and audits at least one page in that state both times.

### Implementation for User Story 7

- [X] T100 [US7] Create `frontend/tests/a11y/session-fixture.ts` registering through the real form, polling Mailpit's `GET /api/v1/messages` for the message addressed to this run's own address, reading the confirmation link out of `GET /api/v1/message/{ID}`, following it and signing in through the real form. Idempotency comes from `freshEmail()` in `tests/a11y/account-support.ts`, never from clearing the mailbox or the database, because a run that deleted messages would break every spec running beside it under `fullyParallel` (R10)
- [X] T101 [US7] Create `frontend/tests/a11y/signed-in.spec.ts` running the existing axe, contrast, target-size and focus assertions against the pages carrying session-dependent chrome while signed in, to the same zero critical and zero serious violations required of the anonymous variant (SC-021, SC-022)
- [X] T102 [US7] Run `pnpm --filter frontend test:a11y` twice in a row and confirm the second run passes with nothing cleared by hand

**Checkpoint**: Both states of the site are audited, not just the anonymous one.

---

## Phase 10: User Story 8 - The move is legible from outside (Priority: P8)

**Goal**: Crawler files listing the new addresses, and sharing metadata that produces a title, a
summary and a cover. Droppable without affecting anything above.

**Independent Test**: Request the crawler files and confirm they list the landing page, the
catalogue pages, the book pages and the author pages; read a book's emitted document and confirm
its Open Graph tags.

### Implementation for User Story 8

- [X] T103 [P] [US8] Create `frontend/public/robots.txt` allowing everything and naming `/sitemap.xml`. It is static because its content does not depend on the catalogue, and it is never the protection: the pages that stay out of the index are kept out by the `robots` meta they already carry (FR-058, Principle IV)
- [X] T104 [US8] Create `frontend/scripts/write-sitemap.mjs` importing `catalogue-paths.mjs` so the sitemap and the prerender cannot disagree, and run it after the build from `frontend/package.json`
- [X] T105 [US8] Add `og:title`, `og:description`, `og:url`, `og:type`, `twitter:card` and `og:image` (the cover's largest prepared width, where one exists) to the `meta` exports used by `/`, `/books/:slug` and `/authors/:slug`, through `frontend/src/catalogue/meta.ts`, with absolute URLs from `VOXLIB_SITE_ORIGIN` defaulting to `http://localhost:5173` (FR-059)
- [X] T106 [P] [US8] Create `frontend/tests/prerender/sharing.spec.ts` reading the emitted documents for the Open Graph tags (SC-026), asserting the `robots` meta on the account screens and the search page is unchanged (SC-027), and checking `build/client/sitemap.xml` and `robots.txt` against what the catalogue holds (FR-058)

**Checkpoint**: Every story is complete.

---

## Phase 11: Polish & Cross-Cutting Concerns

- [X] T107 [P] Write `specs/004-site-shell/manual-verification.md` with the checks no suite decides, as [quickstart.md](./quickstart.md) names them: a real screen reader (VoiceOver on iOS, TalkBack on Android) walking the landmark list on a book page and the search page; whether six seconds reads as slow enough to be read rather than watched; and FR-054, the focus indicator staying distinct from the primary action, which is the one requirement in this feature with no automated proof. T095 deliberately refuses the only mechanical check available, because `--focus` against `--action` measures 2.06:1 for a pair that never touches, so this is decided by eye on a focused submit button and recorded here rather than left to nothing
- [X] T108 [P] Update `docs/ARCHITECTURE.md` for `VoxLib.Platform/Storage/`, the author resource and the two dev-server rules
- [X] T109 [P] Add to `CLAUDE.md` the gotchas this feature creates: MinIO must be up before covers appear; the `mc` sidecar owns bucket creation and the public-read policy; `frontend/site-rules.ts` is the one place the redirect and static-file rules are written, so the production host has to be configured from it; `backend/src/VoxLib.Dal/Seed/content.md` is the human-readable source for `books.json` and adding a book means editing both; the `EmbeddedResource` glob for covers must match `*.webp` only, or the source PNGs compile into the assembly; and the catalogue is smaller than one page, so paging is proved only by `paging-fixture.ts` and a change to `PageRequest.FixedPageSize` would silently retire it
- [X] T110 Run the full CI parity set from the repository root: `dotnet format backend/VoxLib.slnx --verify-no-changes`, `dotnet test backend/VoxLib.slnx`, `pnpm format:check && pnpm lint && pnpm build`
- [X] T111 Run `pnpm --filter frontend test:a11y` in full and confirm zero critical and zero serious violations and zero `color-contrast` incompletes in both the anonymous and the signed-in state (SC-022, SC-023)
- [X] T112 Walk [quickstart.md](./quickstart.md) end to end on a freshly rebuilt container, including the `curl` checks, and correct anything it gets wrong

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies. T004 needs a container rebuild, so start it early
- **Foundational (Phase 2)**: Depends on Setup only for T002 and T003; the schema work can begin immediately. **Blocks every user story**, because the `CoverArtUrl` rename stops the API compiling until Phase 2 is finished
- **US1 (Phase 3)**: Foundational only. It is frontend-only and is the MVP
- **US2 (Phase 4)**: Foundational. Best after US1, because the breadcrumb table US1 creates is what US2 edits
- **US3 (Phase 5)**: Foundational. Needs US1's `HeaderSearch.tsx` to exist, which is why the header form is built there
- **US4 (Phase 6)**: Foundational. Extends the `IAuthorRepository` US3 creates; if US3 is skipped, T045 moves into this phase
- **US5 (Phase 7)**: Foundational and Setup (T001 to T004). Extends the `site-rules.ts` US2 creates; if US2 is skipped, T037 moves into this phase
- **US6 (Phase 8)**: Foundational. Independent of every other story
- **US7 (Phase 9)**: Needs US1, because the session-dependent chrome it audits is what US1 builds
- **US8 (Phase 10)**: Needs US2's `catalogue-paths.mjs`, and US5 for the cover in `og:image`
- **Polish (Phase 11)**: Everything above it

### Within Each User Story

- Backend: model interfaces, then repositories, then orchestrator, then endpoints, then registration in `Program.cs`, then tests
- Frontend: regenerate `schema.d.ts` after any API shape change, before the components that read it
- Styles last within a story, because what they style has to exist

### Parallel Opportunities

- **Setup**: T002 and T003 together, while T001 is written
- **Foundational**: T005, T006, T006a and T007 together (four files in `.Model`); T011 and T012 together (two seed files); T014a and T014b together (two test files). T006b follows T006a, and T008 onward is sequential, because the migration has to see the finished configuration
- **US1**: T018, T020, T021, T022 and T025 together; then T030 and T031 together
- **US2**: T032, T033 and T037 together; then T041, T042 and T040c together. T040a and T040b follow T040, which is what tells them the new addresses
- **US3**: T044, T045 and T049 together; T052 and T053 together; T055 and T057 together
- **US4**: T060 and T063 together; T067, T068, T069, T070 and T077a together (five test files); T073, T074 and T075 together
- **US5**: T080 and T081 together
- **US6**: T098, T099 and T097a together
- **Polish**: T107, T108 and T109 together
- Once Foundational is done, US1, US2, US6 and the backend halves of US3 and US4 can be worked by different people at once

---

## Parallel Example: User Story 1

```bash
# The five independent frontend pieces of the shell, together:
Task: "Create frontend/src/account/SessionProvider.tsx"
Task: "Create frontend/src/catalogue/breadcrumbs.ts"
Task: "Create frontend/src/components/Breadcrumbs.tsx"
Task: "Create frontend/src/components/HeaderSearch.tsx"
Task: "Create frontend/src/components/SiteFooter.tsx"

# Then, once the shell is composed, the two specs together:
Task: "Create frontend/tests/a11y/shell.spec.ts"
Task: "Create frontend/tests/a11y/session-chrome.spec.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1: Setup, including the container rebuild
2. Phase 2: Foundational, which blocks everything
3. Phase 3: US1
4. **Stop and validate**: `pnpm --filter frontend test:a11y` with `shell.spec.ts` and
   `session-chrome.spec.ts` green. Every page now has a banner, a main, a contentinfo, named
   navigation landmarks, a skip link and a trail
5. That alone is the feature's stated value: nothing else in the spec is worth much on a site a
   person cannot move around in

### Incremental Delivery

Each story below the MVP is deployable on its own, in priority order:

1. US1, the shell, is the MVP
2. US2 moves the addresses and adds the front door
3. US3 makes search a capability rather than a page
4. US4 adds the second axis of browsing
5. US5 fixes the covers and makes a missing file visible
6. US6 makes the next step obvious
7. US7 doubles what the suite actually checks
8. US8 tells the outside world, and is droppable

### Where to push back

Six costs are recorded in the plan's Complexity Tracking as the obvious places to argue. Two are
worth re-reading before starting.

The dev-server plugin (T038, T091): SC-011 and SC-016 are proved against the dev server only, and
configuring the production host from the same table is work that belongs to a feature with a host
in front of it.

And the catalogue replacement (T011, T012, T017a, T040a, T040b), which was folded into this
feature after the plan was written rather than specified with it. The argument for folding it in
is that the seed is already being rewritten here and that covering books about to be deleted is
work done twice. The argument against is that it puts a content change and a schema change inside
a feature specified as structural, and it is the reason five test files are rewritten rather than
extended. If this feature starts to feel too large to review in one pull request, this is the part
that was added last and would come out most cleanly.

---

## Notes

- `[P]` means a different file and no dependency on an unfinished task
- Commit after each task or logical group; the branch is squashed on merge, so commit as messily
  as the work actually goes
- Every new piece of interface copy is Ukrainian; the wordmark stays in latin letters (FR-062)
- No visual value is written outside `tokens.css` (FR-063)
- The header and the footer must carry fully opaque fills, or every contrast result on the site
  turns from pass to incomplete and the suite stays green while checking nothing
- After adding an import, Vite can answer `504 (Outdated Optimize Dep)`, and `react-router dev`
  caches `critical.css` across stylesheet changes. Both are a dev-server restart, not a bug in
  the code you just wrote

---

## Phase 12: Convergence

Appended by `/speckit-converge` after the implementation pass. Each item traces to the
requirement it closes and says how the code currently falls short.

Two of these exist because T031 and T092 were marked complete without the files being
written. Nothing failed, because a test that does not exist cannot fail — which is the
whole reason this pass looks at the code rather than at the checkboxes.

- [ ] T113 Create `frontend/tests/a11y/covers.spec.ts` per SC-015, SC-017, SC-018 and SC-030 (missing). `tasks.md` names the file at T092 and it was never written, so four success criteria have no browser proof: that every `img` on the catalogue, the book pages and the author pages reports `naturalWidth > 0` (SC-015, the broken-image icon that went unseen for months); that a cover renders 128 wide in the listing and 192 on a book page at a 1280 viewport and still reflows at 320 (SC-017); that no cover is cropped, by comparing each rendered box's ratio against its own `naturalWidth / naturalHeight` within a pixel, with the placeholder the one element allowed a fixed ratio (SC-030); and that the catalogue's first page is readable within 3 seconds under a 1.6 Mbps, 150 ms CDP throttle (SC-018). Note in the file that SC-018 has little to strain against at four books, so it is a regression guard for later rather than a measurement of a real load today
- [ ] T114 Create `frontend/tests/a11y/session-chrome.spec.ts` per SC-005 and SC-006 (missing). Named at T031 and never written, so the reading of SC-005 that R8 records is asserted nowhere: with the `/api/account/session` request held, capture every tab stop and every bounding box; release it and assert every tab stop before the account controls is identical and every bounding box is unchanged. The account slot's reserved box and its position last in the banner exist precisely to make that true, and nothing currently checks that they do. In the same spec, count requests to `/api/account/session` on a page with more than one consumer and assert exactly one (SC-006) — the shared promise in `SessionProvider` is what makes that hold, and it is the thing a future refactor would quietly undo
- [ ] T115 Resolve `VoxLib.Api/Account/HttpAccountSession.cs` naming `VoxLib.Dal.Account` outside the composition root, per Constitution II and the Constitution Check in [plan.md](./plan.md) (contradicts). The file imports `VoxLib.Dal.Account` for `SignInManager<AccountDao>`, while the plan states that `.Api` names `.Dal` and `.Platform` only in `Program.cs`. It predates this feature (commit `93b71d2`) and the file argues its own case: issuing a cookie needs the request it belongs to, so the type doing it belongs to the web framework. Either give the identity user an abstraction in `.Model` so the import goes, or record the exception in the constitution — what is not acceptable is a principle whose stated scope and the code disagree, because the next reader cannot tell which is wrong
- [ ] T116 Track restoring `loader` on `frontend/src/routes/catalogue-page.tsx` when the catalogue outgrows one page, per FR-032 (partial). It is a `clientLoader` today because React Router refuses a `loader` on a route prerendering emits no document for, and with four books against a page size of twenty no address matches the pattern. FR-032 is therefore satisfied right now — `/books` is prerendered and page two does not exist — and is lost silently the moment somebody adds the twenty-first book, because page two would then exist and be client-rendered. The obligation currently lives in a code comment and in `CLAUDE.md` and in nothing that fails
