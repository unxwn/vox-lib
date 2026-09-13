# Quickstart: Verifying the Site Shell

**Feature**: `specs/004-site-shell/` | **Date**: 2026-09-11

How to run this feature and how each requirement is proved. Every success criterion in
[spec.md](./spec.md) maps to something runnable below, except the two that are manual by nature
and are marked as such.

## Prerequisites

Everything runs inside the dev container. Three services must be up before anything is verified,
and all three come from `.devcontainer/docker-compose.yml`: PostgreSQL, Mailpit, and object
storage. The storage service is new in this feature, so a container built before it needs
rebuilding once.

It is SeaweedFS rather than the MinIO this was first written against: MinIO's community edition
was archived in February 2026, its images were pulled from Docker Hub and its binaries from
`dl.min.io`. The swap cost five configuration values and no code, because the adapter talks S3
through `AWSSDK.S3` rather than to any particular implementation — which is the same swap R2 will
be.

```bash
# In VS Code: Dev Containers: Rebuild Container. Once, for object storage.
pnpm dev                     # API on :5080, web on :5173
```

Check the three the API depends on:

```bash
curl -s localhost:5080/health                       # {"status":"ok",...}
curl -s -o /dev/null -w '%{http_code}\n' localhost:8025      # 200, Mailpit
curl -s -o /dev/null -w '%{http_code}\n' localhost:9333/cluster/status       # 200, storage
# The object browser, which is what MinIO's console on 9001 used to be:
#   http://localhost:8888
```

Playwright reuses an already running dev server locally and starts one in CI.

## One-time setup: preparing the covers

Cover art is committed at its prepared widths and is never resized at runtime. This runs once per
cover, and again only when a cover is replaced. It is deliberately not part of the build, for the
same reason the typeface subsetting in `003-visual-identity` is not: it is an input that changes
about never, and putting an image toolchain in the CI image to re-derive it every time would cost
more than it saves.

The sources live in `backend/src/VoxLib.Dal/Seed/covers/source/`, one PNG per book named at the
book's slug. They are committed but **not embedded**: they are 3.7 MB and are input to the step
below, never served and never read at runtime.

Aspect ratio is not enforced and does not need to be. The covers that exist run from 0.640 to
0.753 wide against tall, and FR-067 renders each at its own proportions rather than cropping to a
fixed box, so `-resize "${w}x"` constrains the width and lets the height fall where it falls.

```bash
# Every source, at every prepared width. Run from the repository root.
cd backend/src/VoxLib.Dal/Seed/covers
for f in source/*.png; do
  slug=$(basename "$f" .png)
  for w in 160 320; do
    # ImageMagick 6 is what the container has, so the command is `convert`;
    # `magick` is the ImageMagick 7 spelling and is not present.
    convert "$f" -resize "${w}x" -quality 82 "${slug}-${w}.webp"
  done
done
```

Two widths, not three. Three of the four sources are 507, 517 and 550 pixels wide, so a 640 would
be an upscale carrying no more detail than the 320. Add it back for any source wide enough to
earn it.

The prepared `.webp` files are embedded resources; the `EmbeddedResource` item in
`VoxLib.Dal.csproj` must match `Seed\covers\*.webp` and **not** the `source/` subdirectory, or
every original compiles into the assembly as well. `CoverSeeder` uploads each one the bucket does
not already hold, on every startup, so a fresh container picks them up with no manual step.

Check what reached the bucket:

```bash
curl -s -o /dev/null -w '%{http_code}\n' \
  localhost:9000/vox-lib-covers/covers/stratehiia-i-taktyka-liderstva-320.webp   # 200
```

## Running everything

Two things the container rebuild erases, because `/home/vscode` is overlayfs: Playwright's
browsers **and** the system libraries they link against. `post-create.sh` reinstalls both, so a
fresh container needs no manual step; if the browser suite reports
`libglib-2.0.so.0: cannot open shared object file`, that step did not run and
`pnpm --filter frontend exec playwright install --with-deps chromium` fixes it.

The API rate-limits to 100 requests per client address per 15 minutes, which a full browser run
exceeds. `appsettings.Development.json` raises it so the suite can run repeatedly; the production
default is untouched, and a run against an API started without that setting fails in the account
specs with what looks like a broken flow and is a 429.

```bash
# Backend: format, build, endpoint tests
dotnet format backend/VoxLib.slnx --verify-no-changes
dotnet test backend/VoxLib.slnx

# Frontend: format, lint (oxlint + the token script), typecheck and build
pnpm format:check && pnpm lint && pnpm build

# Browser: accessibility, contrast, targets, prerender, and the new shell specs
pnpm --filter frontend test:a11y
```

`pnpm build` prerenders every catalogue page, every book and every author by reading the running
API, so the API must be up for it. It also writes the sitemap from the same discovery.

## What proves what

### Phase 2: the catalogue's real content

Folded in after the plan was written, when the cover art turned out to belong to a different
catalogue. It lands in the foundational phase because the seed is already being rewritten there.

| Criterion | Proved by |
| --- | --- |
| SC-028 the catalogue holds the real books and zero placeholders | `CatalogueSeedTests` in the endpoint suite: every book the API lists appears in `content.md`, and every book in `content.md` is listed. Not a count, which would pass against the wrong four |
| SC-028 every book has a running time above zero | the same test, asserting `totalRunningTimeSeconds > 0` for each, which is what the provisional single chapter of FR-065 buys |
| SC-029 every credit says how the person is credited | `AuthorEndpointTests`, asserting every entry in a book's `authors` array carries a role, and `tests/a11y/book.spec.ts` that the role is visible text beside the name |
| SC-029 the index lists everyone credited | `AuthorIndexOrderTests`, extended: the compiler of the Konovalets book appears in `/api/authors` with `roles` containing `compiler` |
| FR-066 the role is on the pairing | `AuthorCreditRoleTests`: one person credited in two roles across two books resolves to one author row, one page, and a different role on each book |

### US1 — every page has a skeleton

| Criterion | Proved by |
| --- | --- |
| SC-001 landmarks on every page | `tests/a11y/shell.spec.ts`, walking `PAGES_IN_SCOPE` and asserting banner, main, contentinfo and a distinct accessible name on every navigation and search landmark |
| SC-002 catalogue, search and about reachable from any page by keyboard | the same spec, tabbing from the top of each page and asserting all three are reached without leaving it |
| SC-003 the name readable and announced once | the same spec: exactly one accessible element whose name is the wordmark, and `.ground` still exposing nothing |
| SC-004 breadcrumb trails | `shell.spec.ts` against the table in [contracts/site-addresses.md](./contracts/site-addresses.md); the last item carries `aria-current="page"` and every earlier one resolves |
| SC-005 no shift when the session resolves | `tests/a11y/session-chrome.spec.ts`: capture tab stops and bounding boxes with the session request held, release it, compare. R8 records the reading being measured |
| SC-006 the session read once | the same spec, counting requests to `/api/account/session` on a page with more than one consumer |
| FR-007 skip link | `shell.spec.ts`: one Tab from the top of every page reaches it, and activating it moves focus into `main` |
| SC-025 reflow at 320 and at 200% | `tests/a11y/target-size.spec.ts` and the existing reflow assertions, extended to the new pages |

### US2 — a front door, and addresses that match the hierarchy

| Criterion | Proved by |
| --- | --- |
| SC-011 every old catalogue address redirects permanently | `tests/a11y/addresses.spec.ts`, requesting each old address and asserting `301` and the new `Location` |
| SC-012 a document for every address | `tests/prerender/prerender.spec.ts`, extended to authors and the landing page, checking the emitted files against what the API says exists |
| FR-025 the landing page says what the site is | `tests/a11y/landing.spec.ts` |
| FR-030 one address for page one | `pnpm lint` — `check-design-tokens.mjs` is unrelated, so this is an assertion in `addresses.spec.ts`: every link that leads to the catalogue's first page resolves to `/books` |

```bash
# By hand, the same thing:
curl -s -o /dev/null -w '%{http_code} %{redirect_url}\n' localhost:5173/page/2
# 301 http://localhost:5173/books/page/2
```

### US3 — search from anywhere

| Criterion | Proved by |
| --- | --- |
| SC-007 keyboard search from every page | `tests/a11y/header-search.spec.ts`, typing and pressing Enter on each page in scope, then reloading the resulting address in a fresh context |
| SC-008 the example freezes | the same spec: focus the field, wait past two intervals, assert the placeholder is unchanged; then again under `emulateMedia({ reducedMotion: 'reduce' })` asserting zero changes from the start |
| SC-009 every suggestion is a keyboard-reachable link | the same spec, on `/search` with nothing typed and on the not-found page |
| SC-010 suggestions follow the catalogue | `BookSearchSuggestionsTests` in the endpoint suite: every returned term, searched through `/api/books?q=`, returns at least one result. Shape, not content, as the clarification settled |
| FR-011 one search input per page | `header-search.spec.ts`: exactly one `role="search"` landmark and one `input[name=q]` on every page |

```bash
# The resource itself:
curl -s localhost:5080/api/search-suggestions | head -c 300
curl -sI localhost:5080/api/search-suggestions | grep -i cache-control   # no-store
```

### US4 — browse by author

| Criterion | Proved by |
| --- | --- |
| SC-013 one step from a book to its author, and a complete list | `AuthorEndpointTests`: every author on a book detail resolves, and their page's book list matches a direct query of what they are credited on |
| SC-014 ordered by surname | `AuthorIndexOrderTests`, asserting the index against the expected Ukrainian order rather than against insertion order |
| FR-037 an unknown or empty author is not found | `AuthorNotFoundTests`, covering both an absent slug and an author with no published books |
| FR-038 a duplicate slug is refused | `AuthorSlugTests`, asserting the seeder throws rather than appending a number |

```bash
curl -s localhost:5080/api/authors | head -c 400
curl -s -o /dev/null -w '%{http_code}\n' localhost:5080/api/authors/no-such-author   # 404
```

### US5 — real covers, and a missing file that says it is missing

| Criterion | Proved by |
| --- | --- |
| SC-015 no broken image anywhere | `tests/a11y/covers.spec.ts`, checking `naturalWidth > 0` for every `img` on the catalogue, every book page and every author page |
| SC-016 a missing file answers as missing | `tests/a11y/addresses.spec.ts`: a request for a file that does not exist is `404`, and an address that matches no route is still the not-found page |
| SC-017 cover size at 1280 and reflow at 320 | `covers.spec.ts`, measuring the rendered box |
| SC-030 no cover is cropped | `covers.spec.ts`, comparing each rendered `img`'s box ratio against its `naturalWidth / naturalHeight` within a pixel, and asserting the placeholder alone keeps a fixed ratio |
| SC-018 the readability budget survives | `tests/a11y/covers.spec.ts` under a 1.6 Mbps / 150 ms CDP throttle, the same budget `001-browse-catalogue` committed to |
| FR-051 storage unreachable | `CoverStorageOutageTests` in the endpoint suite, with the adapter pointed at a dead endpoint: pages still render and the catalogue still answers |

```bash
# The defect this fixes, before and after:
curl -s -o /dev/null -w '%{http_code} %{content_type}\n' localhost:5173/covers/nothing.svg
# today:  200 text/html     after: 404
curl -s -o /dev/null -w '%{http_code} %{content_type}\n' localhost:5173/no-such-page
# both:   200 text/html     (this one must not change)
```

### US6 — the next step is the obvious one

| Criterion | Proved by |
| --- | --- |
| SC-019 every form's primary action is distinct | `tests/a11y/contrast.spec.ts`, extended: on every form, the submit control's computed fill differs from every other control's on that form |
| SC-020 no play control anywhere | `shell.spec.ts`, asserting no control whose accessible name matches the play vocabulary, disabled or otherwise |
| FR-052 the role is defined once with its pair | `pnpm lint` — `check-design-tokens.mjs` recomputes the three new rows and fails the build below the floor |
| FR-055 the account message links to registration | `tests/a11y/book.spec.ts` |

### US7 — the signed in half of the site can be checked

| Criterion | Proved by |
| --- | --- |
| SC-021 a signed-in session with no manual step | `tests/a11y/signed-in.spec.ts` and the fixture in `tests/a11y/session-fixture.ts` |
| SC-022 zero critical and serious in both states | the same spec, running the existing axe assertions against the pages that carry session-dependent chrome while signed in |

The fixture registers through the real form, polls Mailpit for the message addressed to this
run's own address, follows the confirmation link and signs in. It clears nothing, because each run
uses a fresh address; R10 records why that matters under `fullyParallel`.

```bash
# What the fixture reads, by hand:
curl -s 'localhost:8025/api/v1/messages?limit=1' | head -c 200
ID=$(curl -s 'localhost:8025/api/v1/messages?limit=1' | grep -o '"ID":"[^"]*"' | head -1 | cut -d'"' -f4)
curl -s "localhost:8025/api/v1/message/$ID" | grep -o 'http://localhost:5173/confirm[^"\\ ]*' | head -1
```

### US8 — the move is legible from outside

| Criterion | Proved by |
| --- | --- |
| SC-026 sharing produces a title, a summary and a cover | `tests/prerender/sharing.spec.ts`, reading the emitted documents for the Open Graph tags |
| SC-027 account screens and search stay out of the index | the same spec, asserting the `robots` meta on each is unchanged |
| FR-058 the crawler files | the same spec, against `build/client/sitemap.xml` and `robots.txt` |

### Carried forward

| Criterion | Proved by |
| --- | --- |
| SC-022 zero critical and serious, anonymous | the existing specs, with the new pages added to `PAGES_IN_SCOPE` |
| SC-023 contrast over the decorated ground | `contrast.spec.ts`, unchanged in method. **The header and the footer must carry a fully opaque fill**, or every contrast result on every page turns from pass to incomplete and the suite stays green while checking nothing |
| SC-024 targets at 44×44 | `target-size.spec.ts`, which walks every interactive element and so covers the new ones without being changed |
| SC-031 the paging assertions stay alive | `tests/a11y/paging-fixture.ts` seeds a catalogue past one page, and the three pagination assertions in `catalogue.spec.ts` plus the paging assertion in `search.spec.ts` run against it. Without it four books at a fixed page size of twenty leave them passing with no subject |

Note on SC-024 and SC-025: neither is proved by a spec written for it. Both ride on
`PAGES_IN_SCOPE` in `tests/a11y/pages.ts`, which `target-size.spec.ts`, `contrast.spec.ts`,
`typography.spec.ts` and `high-contrast.spec.ts` all read, so adding the new addresses there is
what puts the new pages in scope. That is real coverage and it is invisible, which is why it is
written down here.

## Manual checks

Two things no automated suite decides, both listed in `manual-verification.md` when tasks are
generated:

1. **A real screen reader.** VoiceOver on iOS and TalkBack on Android, walking the landmark list
   on a book page and on the search page. Automated landmark assertions prove the roles and names
   exist; only a real screen reader says whether the resulting list is usable.
2. **The rotating example, watched.** Whether six seconds reads as "changing slowly enough to be
   read rather than watched" is a judgement, and a test can only prove it changes and then stops.

## Troubleshooting

- **Covers are all placeholders.** Storage is down or the bucket is missing. Check
  `localhost:9333/cluster/status`, then that the `storage-setup` sidecar ran; it creates the bucket and
  sets it public-read on every `docker compose up`.
- **Every cover 403s.** The bucket exists but is not anonymous-readable. Re-run the sidecar, or
  `mc anonymous set download local/vox-lib-covers`.
- **A Playwright run reports CSS rules that are already correct on disk.** The `react-router dev`
  server caches `critical.css` and does not invalidate it when a stylesheet changes. Confirm with
  `curl -s "http://localhost:5173/@react-router/critical.css?pathname=/" | grep <rule>`, then
  restart the dev server. Touching the file does not help.
- **`pnpm build` fails saying the catalogue could not be read.** The API is not up. The prerender
  and the sitemap both read it on purpose; the seed file stops being the truth the moment a book
  is published any other way.
- **The signed-in spec times out waiting for mail.** Mailpit is on the `db` network namespace;
  if it was started on a bridge network the API's send fails with connection refused and no
  message ever arrives.
