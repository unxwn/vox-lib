# Implementation Plan: Site Shell

**Branch**: `feature/site-shell` | **Date**: 2026-09-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-site-shell/spec.md`

## Summary

Give the site a structure a person can move around in. A banner and a contentinfo on every page,
breadcrumbs below the top level, the search field moved into the header and given real examples
drawn from the catalogue, a landing page at the root with the catalogue moved under `/books`,
authors promoted from plain text to a resource with an index and a page each, and cover art moved
out of a directory that never existed into S3-compatible object storage at three prepared widths.

Four things were established in Phase 0 that shape the plan more than the requirement list does.

**The single-page fallback answers a missing file with a page.** Reproduced, not assumed: today
`/covers/boiarynia.svg`, `/fonts/nope.woff2` and `/some.deep/path.png` all come back `200
text/html`. That is why two seeded books have shown a broken image icon since the catalogue
shipped and no test, status check or monitor could see it — the response was a valid document and
the only thing that failed was the browser decoding it as an image. It is also the harder half of
FR-027, because a static site has no process to emit a `301` either. Both rules come from one
table and are installed as dev-server middleware; R1 records why every in-application alternative
answers `200` first and so teaches a crawler the wrong thing.

**The catalogue this was planned against is placeholder data.** Discovered after Phase 1, when the
cover art FR-046 needed arrived: four images, four books, and not one of them in the catalogue.
The twenty-seven seeded books are mocks, and the books the project holds are recorded in
`backend/src/VoxLib.Dal/Seed/content.md` with a title, one credit, a description and a total
running time each. FR-064 to FR-068 fold the replacement into Phase 2, because the seed is already
being rewritten there for authors and cover keys, and because putting covers on books that are
about to be deleted is work done twice.

What that costs the plan is recorded honestly: the duplicate author this document used to open on,
`Taras Shevchenko` against `Тарас Шевченко`, was a defect in the mock data and goes with it. The
KMU 2010 transliteration survives the change and is confirmed by it, since the four cover files
are already named at exactly what it produces (`Щоденник сотника Устима` to
`shchodennyk-sotnyka-ustyma`). The uniqueness rule and its test survive too, on their own merit
rather than because the seed happened to violate them.

**The session cannot resolve before the page loads, so SC-005 needs a stated reading.** The
catalogue pages are prerendered and the cookie is `HttpOnly`, and the anonymous and signed-in
variants have different numbers of controls, so the count of tab stops in the document
necessarily changes. What FR-008 actually protects — "no tab stop they have already passed moves
and no element shifts under them" — is achievable and testable, by putting the account slot last
in the banner and reserving its box. R8 records this as a resolved reading, in the same way
`003-visual-identity` resolved FR-007's region edges.

**The primary action colour was computed, not chosen.** `--action: #2aa37d` measures 4.58:1
against `--surface`, 3.75:1 against `--surface-raised`, and its label at `--on-action: #070d22`
measures 6.09:1 on it. One number needs explaining rather than enforcing: `--focus` against
`--action` is 2.06:1 and that is not a violation, because the indicator is an outline at a 2px
offset and what abuts the ring is the region behind the button.

## Technical Context

**Language/Version**: C# on .NET 10 for `backend/`; TypeScript 7.0 on Node 24 for `frontend/`.
Both halves change in this feature, which is the first time since `001-browse-catalogue`.

**Primary Dependencies**: ASP.NET Core minimal APIs, EF Core and Npgsql, unchanged. React 19.2,
React Router 8.3.1 and Vite 8.2, unchanged. One new package: `AWSSDK.S3` 4.0.103.2 in
`VoxLib.Platform`, chosen over the MinIO .NET client because it is the SDK Cloudflare's own R2
documentation targets and it makes R2 the production case rather than the awkward one (R4). One
new container: `minio/minio` plus a `minio/mc` sidecar in `docker-compose.yml`.

**Storage**: PostgreSQL, one migration adding `authors.slug`, `authors.sort_name`,
`books.narrator` and `books.added_to_catalogue_at`, and renaming `books.cover_art_url` to
`books.cover_key`. Plus one S3 bucket, `vox-lib-covers`, public read, holding three prepared
widths per cover. The bucket is reached through `ICoverStorage` in `VoxLib.Model`, implemented in
`VoxLib.Platform`.

**Testing**: xunit with `WebApplicationFactory<Program>` for the API, extended to authors,
suggestions, the new book fields and a storage outage. Playwright 1.63 with
`@axe-core/playwright` 4.13 for the browser, extended with five new specs and, for the first
time, a signed-in fixture that registers through the real form and reads the confirmation link
out of Mailpit's HTTP API. `frontend/scripts/check-design-tokens.mjs` gains three contrast rows.

**Target Platform**: The same one origin serving prerendered documents and a single-page
fallback, plus one new origin the browser reaches directly for covers. VoiceOver on iOS and
TalkBack on Android remain the platforms that decide whether this is done.

**Performance Goals**: No new goal, one budget defended. `001-browse-catalogue` committed the
catalogue's first page to being readable within 3 seconds on 1.6 Mbps with 150 ms of latency, and
SC-018 carries it forward across a cover that goes from 56×80 to 128 wide. The defence is that the
covers are WebP at prepared widths on a separate origin and `loading="lazy"`, so a 1280 viewport
fetches at 160 and only a two-times display reaches for 320. It is a weaker defence to test than
it was written as: the real catalogue is four books, so the first page holds four covers and the
budget has little to strain against until the catalogue grows.

**Constraints**: Every page must keep zero critical and zero serious automated violations in both
the anonymous and the signed-in state. The header and the footer must carry fully opaque fills or
every contrast result on the site silently turns from pass to incomplete. No new visual value may
be written outside `tokens.css`. All new copy is Ukrainian; the wordmark stays in latin letters.
No audio, no playback, no signed URL, and no disabled play control anywhere.

**Scale/Scope**: Fourteen addresses where there were ten, of which four are new pages. Three new
API endpoints and three changed response shapes. One migration. Five new frontend components and
one new provider. Five new browser specs and roughly six new endpoint test classes. Two new
compose services. Twenty-seven placeholder books and twenty placeholder author rows replaced by
four real books and four real credits, one of them a compiler rather than an author.

No unresolved unknowns remain. Phase 0 is recorded in [research.md](./research.md).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluated against the six gates in `.specify/memory/constitution.md`.

**1. Accessibility.** PASS, and it is most of what this feature is. The site currently offers a
screen reader two landmarks on every page, a navigation called `Обліковий запис` and a main, and
three routes with no way back to the catalogue at all. This feature takes that to six named
landmarks, a skip link, and a breadcrumb trail on every page below the top level.

The three risks it introduces each have a defence with a test rather than an assurance. A banner
that inserts focusable controls after the session resolves would move the ground under a keyboard
user: the account slot goes last and reserves its box, and the measurement is spelled out in R8.
A placeholder that changes inside a control is ordinarily a defect: it freezes on focus, freezes
for the visit, never starts under `prefers-reduced-motion`, and is supplementary to a real visible
label in every state — and the half of the idea a keyboard can use is the suggestion links, not
the rotation. And a header carrying navigation, search and account controls before the content on
every page is exactly the condition that makes a skip link worth having, which is why FR-007 adds
one that the spec's description did not ask for.

Media session does not apply: there is no playback. FR-055 is the accessibility rule that looks
like a product decision — no disabled play control anywhere, because a disabled control still
appears in a screen reader's list and invites a listener to hunt for a way to enable it.

**2. Dependency rule.** PASS. `VoxLib.Model` gains `Storage/ICoverStorage.cs`,
`Book/IAuthorRepository.cs`, `Book/IAuthorCatalogue.cs`, `Book/ICatalogueSuggestions.cs`,
`Book/AuthorSlug.cs` and `Book/CreditRole.cs`, and references nothing. The credit role is a
domain vocabulary rather than a string on a DAO, which is what keeps "author or compiler" from
being decided in a mapping file. `.Orchestrator` implements the catalogue interfaces,
`.Dal` implements the repositories, `.Platform` implements the storage, and `.Api` names `.Dal`
and `.Platform` only in `Program.cs`.

Two rules are deliberately placed outside the handlers. The suggestion sampling rule — half
titles, half author names, published only, verbatim — is in `CatalogueSuggestions` in
`.Orchestrator`, because FR-023's guarantee that a suggestion always finds something is a
property of that rule. And the author slug is a pure function on the domain in `.Model`, called
once at creation, rather than a string helper in the seeder.

`CoverSeeder` living in `.Dal` is a deliberate reading rather than an oversight: `.Dal` may
reference `.Model`, `ICoverStorage` is in `.Model`, and the seed folder is the catalogue's
bootstrap rather than a repository. R5 records why it is a second class and not a second
responsibility on `CatalogueSeeder` — the two have genuinely different guards, because the
database and the bucket are separate volumes with separate lifetimes.

**3. Audio and access.** PASS by construction, and the construction is the point.
`ICoverStorage` has no signing method, no delete and no list, so this feature is incapable of
expressing a private object; the audio interface arrives later as a separate interface rather
than by widening this one. Covers are public catalogue metadata under Principle IV, so they are
served public-read and unsigned deliberately: a cover must be cacheable, indexable and usable as
a preview when a link is shared. No service worker is added. No chapter gains a storage key, a
URL or anything else. The shape audio will eventually take is recorded in the spec's assumptions
so the feature that builds it inherits a decision; none of it is built here.

**4. HTTP.** PASS. Three new endpoints, all resources under `/api/`: `/api/authors`,
`/api/authors/{slug}`, `/api/search-suggestions`. Authors are a resource rather than a filter on
books, which FR-036 asks for and Principle V agrees with. The frontend calls all of them by
relative path.

One thing is not an `/api/` route and should be named rather than assumed: cover images are
fetched by the browser from object storage directly, on its own origin. That is the same division
of labour Principle III draws for audio — the application decides, infrastructure serves bytes —
and the API never proxies an image.

**5. Tests.** PASS. Every requirement names what proves it, and the mapping is in
[quickstart.md](./quickstart.md). The ones carrying the most weight: an endpoint test that takes
every suggestion the API returns and searches for it, so FR-023's guarantee is measured rather
than asserted; a browser spec that requests a file which does not exist and requires a `404`
while requiring an unmatched *route* to still render the not-found page; and a signed-in fixture
that closes the gap SC-021 names, where no browser test has ever run against a signed-in page and
so every axe run, contrast measurement, target-size check and focus assertion in the suite has
walked the anonymous variant of the site.

**6. Abstractions.** PASS. Nothing from Principle VII's deferred list is adopted: no aggregate
root, no domain event, no specification, no CQRS, no MediatR, no HLS. The new interfaces mirror
shapes that already exist — `IAuthorRepository` beside `IBookRepository`, `IAuthorCatalogue`
beside `IBookCatalogue` — rather than introducing a pattern.

Three things are declined that a plan this size would be tempted by: a runtime image pipeline
(the widths are prepared before upload and committed, and the browser picks with `srcset`), a
typeahead endpoint (FR-024 excludes it, and a combobox announcing changing results is among the
hardest patterns in the specification to get right), and a burger menu (three destinations wrap
at 320 pixels; a disclosure is added when the navigation outgrows the bar, not in anticipation).

### Post-design re-check

Re-evaluated after the Phase 1 artifacts were written. All six gates still pass. Three tensions
surfaced during design and are resolved rather than waived; each is recorded where it will be
read rather than only here.

**SC-005 against FR-008.** The success criterion says "zero tab stop changes measured between the
two states". Taken literally it is unachievable — the session is read after the document loads,
and signing in offers one control where being anonymous offers two — so a test written against
the literal words would be either false or vacuous. The requirement it summarises is achievable
and is what is measured: every tab stop before the account controls is identical in both states,
and every element's bounding box is identical. Achieved by ordering the account slot last in the
banner and reserving its resolved size. R8 carries the reasoning; the measurement is in
[quickstart.md](./quickstart.md).

**FR-054 against the contrast matrix.** "The focus indicator stays distinct from the primary
action wherever both appear" reads like a contrast floor and is not one. `--focus` against
`--action` measures 2.06:1, and adding it to the enforced matrix would fail the build for a pair
that never touches: the indicator is an `outline` at `--focus-offset: 2px`, so what abuts the
ring on both sides is the region behind the button, and `--focus` against `--surface` is already
enforced. The requirement is about hue, which amber against teal plainly satisfies. The figure is
recorded in [data-model.md](./data-model.md) with this reasoning, so that nobody adds it later
and finds it failing for the wrong reason.

**FR-027 against a site with no server.** A permanent redirect has to be served by whatever
answers the request, and production hosting does not exist yet. The rule is written once, in
`frontend/site-rules.ts`, and installed as dev-server middleware, which is what the Playwright
suite and every local run go through. The honest limit is recorded in Complexity Tracking below:
SC-011 is proved against the dev server, and configuring the production host from the same table
belongs to the deployment feature. R1 records why a `_redirects` file written now would solve
half the problem and silently lose the other half.

One thing the design surfaced that the plan did not survive intact: the catalogue it was written
against is placeholder data, and FR-064 to FR-068 replace it in Phase 2. Three consequences are
worth naming here rather than only in the requirements. A credit role lands on the link between a
book and a person, which is the one schema addition FR-042 now permits by name. Cover art renders
at the proportions of the source rather than cropped, because the four real covers run from 0.64
to 0.75 and no fixed box suits all of them. And the real catalogue is smaller than one page, so
the browser suite needs a seeded catalogue of its own to keep its paging assertions alive.

## Project Structure

### Documentation (this feature)

```text
specs/004-site-shell/
├── plan.md                          # This file
├── spec.md                          # Feature specification
├── research.md                      # Phase 0 output, twelve decisions
├── data-model.md                    # Phase 1 output, schema, storage layout, colour roles
├── quickstart.md                    # Phase 1 output, how each requirement is verified
├── contracts/
│   ├── catalogue.yaml               # Phase 1 output, the HTTP surface at version 2
│   ├── site-addresses.md            # Phase 1 output, addresses, landmarks, trails
│   └── cover-storage.md             # Phase 1 output, the object storage contract
├── checklists/
│   └── requirements.md              # Spec quality checklist
└── tasks.md                         # Phase 2 output, created by /speckit-tasks
```

### Source Code (repository root)

```text
docker-compose.yml                   # + minio, + a minio/mc sidecar; both network_mode: service:db

backend/src/
├── VoxLib.Model/
│   ├── Book/
│   │   ├── Author.cs                # + Slug, + SortName
│   │   ├── AuthorSlug.cs            # NEW, KMU 2010 transliteration, called once at creation
│   │   ├── Book.cs                  # CoverArtUrl -> CoverKey, + Narrator, + AddedToCatalogue
│   │   ├── IAuthorRepository.cs     # NEW
│   │   ├── IAuthorCatalogue.cs      # NEW
│   │   ├── ICatalogueSuggestions.cs # NEW
│   │   └── IBookRepository.cs       # + SampleTitlesAsync
│   └── Storage/
│       └── ICoverStorage.cs         # NEW, public-read only: no signing, no delete, no list
├── VoxLib.Orchestrator/Book/
│   ├── AuthorCatalogue.cs           # NEW, the published-only and ordering rules
│   └── CatalogueSuggestions.cs      # NEW, the sampling rule FR-023 depends on
├── VoxLib.Dal/
│   ├── Book/
│   │   ├── AuthorDao.cs             # + Slug, + SortName
│   │   ├── AuthorRepository.cs      # NEW
│   │   ├── BookDao.cs               # CoverKey, Narrator, AddedToCatalogue
│   │   └── BookMapping.cs           # the one place DAO becomes domain
│   ├── Persistence/
│   │   ├── VoxLibDbContext.cs       # + ix_authors_slug, + sort_name collation
│   │   └── Migrations/*_SiteShell.* # NEW
│   └── Seed/
│       ├── content.md               # the real books, as prose; input to books.json, not read at runtime
│       ├── authors.json             # NEW, name + slug + sortName, one row per person
│       ├── books.json               # REPLACED, four real books, covers by key, credits with roles
│       ├── covers/source/*.png      # the originals; NOT embedded, 3.7 MB, input to the magick step
│       ├── covers/*.webp            # NEW, embedded, two widths per cover
│       ├── CatalogueSeeder.cs       # authors now come from authors.json
│       └── CoverSeeder.cs           # NEW, per-object guard, uploads what the bucket lacks
├── VoxLib.Platform/Storage/
│   ├── S3CoverStorage.cs            # NEW, one implementation, MinIO and R2
│   └── CoverStorageOptions.cs       # NEW
└── VoxLib.Api/
    ├── Program.cs                   # + storage, author and suggestion registrations
    ├── Author/
    │   ├── AuthorEndpoints.cs       # NEW, /api/authors and /api/authors/{slug}
    │   └── Contracts/Responses/     # NEW, AuthorSummary, AuthorDetail, AuthorReference
    ├── Search/
    │   ├── SearchEndpoints.cs       # NEW, /api/search-suggestions
    │   └── Contracts/Responses/     # NEW, SearchSuggestion
    └── Book/
        ├── BookEndpoints.cs         # authors become references, cover becomes a set of widths
        └── Contracts/Responses/     # + Cover, + narrator on BookDetail

frontend/
├── site-rules.ts                    # NEW, the redirect table and the static-file rule, once
├── vite/site-rules-plugin.ts        # NEW, installs both as dev middleware (FR-027, FR-048)
├── vite.config.ts                   # + the plugin
├── react-router.config.ts           # path discovery moves out to scripts/catalogue-paths.mjs
├── public/robots.txt                # NEW, names the sitemap
├── scripts/
│   ├── catalogue-paths.mjs          # NEW, one discovery, shared by prerender and sitemap
│   ├── write-sitemap.mjs            # NEW, post-build
│   └── check-design-tokens.mjs      # + three rows for --action and --on-action
├── src/
│   ├── routes.ts                    # the new address table
│   ├── root.tsx                     # SiteShell: header, main, footer, SessionProvider
│   ├── account/
│   │   ├── SessionProvider.tsx      # NEW, FR-009: one read per page view, shared
│   │   └── useSession.ts            # reads the provider
│   ├── catalogue/
│   │   ├── page.ts                  # catalogueHref -> /books
│   │   ├── breadcrumbs.ts           # NEW, derived from the address, never authored
│   │   └── meta.ts                  # + Open Graph and Twitter card
│   ├── search/
│   │   └── useSuggestions.ts        # NEW, one fetch per page view, shared
│   ├── components/
│   │   ├── SiteHeader.tsx           # NEW, banner: skip link, wordmark, nav, search, account
│   │   ├── SiteFooter.tsx           # NEW, contentinfo: what this is, about, the font licence
│   │   ├── Breadcrumbs.tsx          # NEW
│   │   ├── HeaderSearch.tsx         # NEW, the field, the label, the rotating example
│   │   ├── SearchSuggestions.tsx    # NEW, the links a keyboard can use
│   │   ├── SessionMenu.tsx          # becomes the account slot; reserves its box
│   │   ├── CoverArt.tsx             # srcset over prepared widths, onError -> placeholder
│   │   └── AuthorLink.tsx           # NEW, FR-033, every name is a link
│   ├── routes/
│   │   ├── landing.tsx              # NEW, /
│   │   ├── about.tsx                # NEW, /about
│   │   ├── authors.tsx              # NEW, /authors
│   │   ├── author.tsx               # NEW, /authors/:slug
│   │   ├── catalogue.tsx            # loses the in-body search link; /books
│   │   ├── search.tsx               # loses its body form; suggestions when idle
│   │   ├── not-found.tsx            # gains suggestions, loses nothing
│   │   └── book.tsx                 # author links, narrator, larger cover, register link
│   └── styles/
│       ├── tokens.css               # + --action, + --on-action
│       ├── shell.css                # NEW, header, footer, breadcrumbs, skip link
│       └── catalogue.css            # larger covers, the landing and author pages
└── tests/
    ├── a11y/
    │   ├── pages.ts                 # the new address list, one place
    │   ├── session-fixture.ts       # NEW, register -> Mailpit -> confirm -> sign in
    │   ├── shell.spec.ts            # NEW, landmarks, skip link, breadcrumbs, no play control
    │   ├── header-search.spec.ts    # NEW, keyboard search, the freeze, reduced motion
    │   ├── addresses.spec.ts        # NEW, 301s and the 404 for a missing file
    │   ├── covers.spec.ts           # NEW, no broken image, size, the 3s budget
    │   ├── session-chrome.spec.ts   # NEW, SC-005 and SC-006
    │   ├── signed-in.spec.ts        # NEW, the audits nothing has ever run
    │   ├── paging-fixture.ts        # NEW, FR-068: a catalogue big enough to page
    │   ├── catalogue.spec.ts        # the new addresses and the four real books
    │   ├── search.spec.ts           # new terms, the header field, the placeholder rule
    │   └── book.spec.ts             # the new slugs, credits with roles, the register link
    └── prerender/
        ├── prerender.spec.ts        # + authors and the landing page
        └── sharing.spec.ts          # NEW, Open Graph, sitemap, robots
```

**Structure Decision**: The existing five-project backend and the existing frontend, unchanged in
shape. Three structural additions, each with a reason that is not tidiness.

`VoxLib.Platform` gets its first real implementation. It has held only `Email/` since
`002-user-accounts`, and `Storage/` is the folder `docs/ARCHITECTURE.md` already names in its
layout example. Putting the S3 adapter anywhere else is what would need justifying.

`frontend/site-rules.ts` exists because two rules that must agree — which old addresses redirect
where, and what counts as a static file — would otherwise be written in a Vite plugin and again
in whatever serves the build. One table, imported.

`frontend/scripts/catalogue-paths.mjs` exists for the same reason: FR-032 and FR-058 are the same
list of addresses seen from two sides, and discovering it twice is how they drift. The code
already exists inside `react-router.config.ts` and already reads the API rather than the seed
file; this only moves it.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified

No violations. The plan adopts nothing from Principle VII's deferred list and adds no project.

Six costs are recorded because they are the largest in the plan and the obvious places to push
back:

| Addition | Why needed | What it costs |
| --- | --- | --- |
| Two new compose services, MinIO and an `mc` sidecar | FR-050 requires the site, the endpoint tests and the browser tests to work with no cloud credentials and no network, and FR-043 requires the provider to be a configuration change. A stand-in for R2 in the compose file is what buys both, exactly as Mailpit stands in for SMTP | A container rebuild for everyone, one more service to be up before anything works, and a compose file that now has four services where it had three. Nothing in CI builds it, so it is unverified by a green pull request |
| A dev-server plugin serving `301`s and `404`s | A static site has no process to emit either, and every in-application alternative answers `200` first, which teaches a crawler the old address is still good — the precise outcome FR-027 exists to prevent | SC-011 and SC-016 are proved against the dev server. Configuring the production host from the same table is real work that belongs to a feature that has a host in front of it, and until then the rule is code plus a written obligation |
| Cover art committed at two prepared widths as embedded resources | FR-045 forbids resizing on request, and FR-047 requires one seeding step that a fresh container, a test run and a deployment all go through. Preparing at build time would put an image toolchain in the CI image for inputs that change about never. Two widths rather than three because three of the four source images are narrower than 640 pixels and a 640 would exist only by upscaling | Repository size, and a documented manual step in [quickstart.md](./quickstart.md) that runs again only when a cover is replaced. The source images stay out of the `EmbeddedResource` glob: they are 3.7 MB and belong to the repository, not to the assembly |
| A new seed file and a replaced catalogue | The sort names FR-039 needs cannot be derived without guessing, and FR-064 replaces twenty-seven placeholder books with the four the project actually holds. Both are data problems and are fixed in the data | `books.json` now references authors rather than naming them, so adding a book means checking `authors.json` first, and the seeder fails loudly rather than quietly on a name it does not know. Every browser test that named a placeholder book or searched for a placeholder term has to be rewritten in the same change |
| A seeded paging fixture for the browser suite | Four books against a fixed page size of twenty means nothing paginates, and the three pagination assertions in `catalogue.spec.ts` plus the paging assertion in `search.spec.ts` would pass by having no subject. FR-068 keeps them real | One more seeding path that exists only for tests, and the discipline of keeping it out of the catalogue a visitor sees |
| Response shapes changed rather than extended | A book's cover stops being a URL string and its authors stop being bare names. Extending instead would leave `coverArtUrl` pointing at an address that no longer exists and `authors` duplicating what `AuthorReference` already carries | The generated `frontend/src/api/schema.d.ts` has to be regenerated, and every consumer of those two fields is touched in the same change. The contract is versioned `2.0` to say so |
