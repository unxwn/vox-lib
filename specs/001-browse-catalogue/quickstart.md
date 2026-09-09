# Quickstart: Browse the Book Catalogue

**Feature**: `specs/001-browse-catalogue/` | **Date**: 2026-09-08

How to run the feature and prove it works. Shapes and field names are in
`contracts/catalogue.yaml` and `data-model.md` rather than repeated here.

## Prerequisites

The dev container gains a database service in this feature, so rebuild it once before
anything below will work.

> Dev Containers: Rebuild Container

Confirm the database is reachable and speaks the collation the catalogue needs. The second
command must list a Ukrainian ICU collation; without it, ordering cannot satisfy FR-015.

```bash
psql "$VOXLIB_DB_CONNECTION" -c 'select version();'
psql "$VOXLIB_DB_CONNECTION" -c "select collname from pg_collation where collname like 'uk%';"
```

## Run it

```bash
pnpm dev
```

The API applies pending migrations at startup and seeds the catalogue when it is empty, so
the first run populates the database on its own. The catalogue is at
http://localhost:5173 and the API at http://localhost:5080.

## Prove the API behaves

Each command below maps to a requirement, and each one is also covered by an endpoint test
so that the behaviour is enforced rather than merely observed once.

```bash
# FR-001, FR-005: first page, ordered by title under Ukrainian collation
curl -s localhost:5080/api/books | jq '{page, pageCount, totalCount, titles: [.items[].title]}'

# FR-008: one book with its chapters in order
curl -s localhost:5080/api/books/<slug> | jq '{title, authors, chapterCount, chapters: [.chapters[].position]}'

# FR-013: search by part of a title, then by part of an author's name, in either letter case
curl -s 'localhost:5080/api/books?q=<fragment>' | jq '.totalCount'

# R9: a page past the end is not found, rather than an empty page
curl -s -o /dev/null -w '%{http_code}\n' 'localhost:5080/api/books?page=9999'

# FR-011: an unknown or unpublished book is not found
curl -s -o /dev/null -w '%{http_code}\n' localhost:5080/api/books/no-such-book

# FR-010: no audio anywhere in any response
curl -s localhost:5080/api/books/<slug> | grep -iE 'audio|mp3|opus|storage|signed' && echo 'FAIL' || echo 'clean'
```

## Prove the document carries the content

FR-012 is the reason for prerendering, so it is checked against built output rather than
against a running browser. Build with the API running, then read the generated file.

```bash
pnpm --filter frontend build
grep -o '<title>[^<]*</title>' frontend/build/client/books/<slug>/index.html
grep -c '<meta name="description"' frontend/build/client/books/<slug>/index.html
```

A book page whose title and description appear in the file passes. An empty root element
with no text is the failure this feature exists to prevent.

## Prove it is usable without sight or a mouse

```bash
pnpm --filter frontend test:a11y
```

The run covers the catalogue list, a book page and a search result page, and asserts:

- zero critical and zero serious accessibility violations, which is SC-002
- every book reachable by keyboard with a visible focus indicator and no trap, SC-004
- focus lands on the results heading after a page change, FR-019
- the result count is announced when the displayed set changes, FR-014

## Run what continuous integration runs

```bash
dotnet format backend/VoxLib.slnx --verify-no-changes
dotnet test backend/VoxLib.slnx
pnpm format:check && pnpm lint && pnpm build
```

The test run needs a database. It creates its own uniquely named one, applies migrations,
seeds it, and drops it afterwards, so it neither depends on nor disturbs the development
data.

## Check the two halves still agree

The frontend's types are generated from the API's own OpenAPI document. Regenerate them and
confirm nothing moved; a diff here means a contract changed on one side only.

```bash
pnpm --filter frontend gen:api-types
git diff --exit-code frontend/src/api/schema.d.ts
```

## Known gaps at this stage

- Publishing a book requires regenerating the pages it appears on, so a book added directly
  to the database is not visible in prerendered output until the next build. Recorded in the
  specification and settled by the administrative feature.
- The dev container change is not verified by continuous integration, because nothing in the
  pipeline builds the container. Rebuild locally before merging.
