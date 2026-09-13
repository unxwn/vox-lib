# Phase 0 Research: Site Shell

**Feature**: `specs/004-site-shell/` | **Date**: 2026-09-11

Every unknown in the plan's Technical Context is resolved below. Four findings were taken from
the running system rather than from documentation, because the documentation does not say what
this repository actually does: the single-page fallback's behaviour on a missing file (R1), the
author slug collision already sitting in the seed data (R3), Mailpit's message API as the
container actually serves it (R10), and the colour arithmetic behind the primary action role
(R9). Colour values were computed, never judged.

## R1. How a static site answers 404 and 301 when it has no server

**Decision**: One Vite plugin, `frontend/vite/site-rules.ts`, reading a single table in
`frontend/site-rules.ts`. It installs two pieces of Connect middleware ahead of the React Router
dev middleware: a permanent-redirect table for the addresses this feature moves, and a rule that
a request whose final path segment contains a dot, and which no file satisfies, is answered
`404` instead of falling through to the single-page document. Production hosting does not exist
yet; the plan records that whatever serves the build has to be configured from the same table,
and names it as the one place those two rules are written down.

**Rationale**: The defect FR-048 describes was reproduced rather than assumed. Against the dev
server as it stands today:

```
/covers/boiarynia.svg        200 text/html 2896b
/fonts/nope.woff2            200 text/html 2888b
/some.deep/path.png          200 text/html 2892b
/no-such-page                200 text/html 2880b
```

Every one of those is the not-found route rendered as a document with a success status. That is
why two seeded books have shown a broken image icon since `001-browse-catalogue` and no test,
status check or uptime monitor could see it: the response was a valid HTML page, and the only
thing that failed was the browser's attempt to decode it as SVG. The last line is the behaviour
that must survive, and it is the reason the rule keys on the path and not on the status: an
address that matches no *route* still has to render the page that says so, with the header, the
footer and a way to search.

Keying on a dot in the last segment is the same rule every static host uses to tell an asset
request from a route, and it is correct for this site's actual address space: no route this
feature adds contains a dot, and every static asset does. A book slug cannot contain one either,
because slugs are transliterated and hyphenated (R3).

The permanent redirect is a harder problem than it looks, and it is worth stating why the obvious
answers were rejected. With `ssr: false` there is no process in production to emit a `301`, so
the two in-application options are a route at `/page/:n` that navigates on mount, and a
prerendered document carrying `<meta http-equiv="refresh">`. Both are a `200` followed by a
client-side move. SC-011 asks for a permanent redirect, and a crawler that receives `200` learns
the old address is still good and keeps it forever, which is precisely the outcome FR-027 exists
to prevent. So the redirect has to be served by whatever answers the request, and in this
repository, today, that is the dev server the Playwright suite already runs against.

**Alternatives considered**:

- *A `_redirects` file in the build output.* This is the Cloudflare Pages and Netlify spelling of
  the same table, and it is two lines. It was rejected for now because no host has been chosen,
  the file would be verified by nothing, and the 404 half of the problem is not expressible in
  that format: `_redirects` matches a splat, not "a path with an extension", so a site with a
  `/* /index.html 200` catch-all answers every missing asset with a page again. Writing a file
  that solves one half and silently loses the other is worse than writing the rule down once and
  configuring the host from it when there is a host.
- *Serving the frontend from `VoxLib.Api` with `UseStaticFiles` plus a fallback.* This is the
  arrangement `docs/ARCHITECTURE.md` implies, and it would make both rules ordinary ASP.NET Core
  middleware. It is also a deployment change with no deployment behind it, it would put the
  build output on the API's path in development where Vite already serves it, and it is a
  decision the deployment feature should make with its host in front of it.
- *Prerendering the account routes so the fallback can be removed entirely.* It would let every
  unmatched path answer `404` with no extension rule at all. Rejected: `002-user-accounts`
  deliberately does not generate documents for pages personal to one person, and reversing that
  to buy a simpler 404 rule trades a stated privacy decision for a tidier middleware.

## R2. The address move, and the two route-ranking traps in it

**Decision**: The route table becomes `/` landing, `/books` catalogue, `/books/page/:page`,
`/books/:slug`, `/authors`, `/authors/:slug`, `/search`, `/about`, the five account routes
unchanged, and `*`. `catalogueHref` in `frontend/src/catalogue/page.ts` becomes
`page <= 1 ? '/books' : '/books/page/' + page`, and stays the only place that rule is written.
The prerender path discovery moves into `frontend/scripts/catalogue-paths.mjs`, imported by both
`react-router.config.ts` and the sitemap writer (R11).

**Rationale**: FR-028 keeps `/books/:slug` exactly where it is, which is what forces the
catalogue under the same prefix rather than to a new word: with the catalogue at `/catalogue` the
breadcrumb above a book would climb to a page whose address is not a prefix of the book's own,
and FR-031 asks for the opposite.

Two ranking questions had to be settled rather than assumed.

`/books/page/2` against `/books/:slug`: React Router ranks a static segment above a dynamic one,
so the numbered page wins and no book is shadowed by it. The exposure is the reverse — a book
whose slug were literally `page` would be unreachable. It is recorded as an edge case rather
than defended in code, because slugs are transliterated titles and `page` is not a Ukrainian
title; if one ever collides the seed refuses it the same way a duplicate slug is refused (R3).

`/` was the catalogue's first page and becomes the landing page. That is not a redirect: the
address keeps working and starts serving a different page, which is FR-025. What does redirect is
`/page/:n`, including `/page/1`, which was never a real address but is a plausible bookmark; it
lands on `/books`.

**Alternatives considered**: keeping the catalogue at `/` and putting the landing page at
`/about` or `/welcome` was rejected by FR-025 directly — the root address is the one a person is
told about, and a first-time visitor arriving at a list of books is the defect the story names.

## R3. Author slugs, and the collision already in the seed data

**Decision**: Slugs are a KMU 2010 transliteration of the author's name, stored on the row,
unique, assigned once at creation and never recomputed. The transliteration lives in
`VoxLib.Model/Book/AuthorSlug.cs` as a pure function on the domain. A collision is refused: the
seeder throws and the unique index is what settles a race. Sort names are authored by hand rather
than derived. Both arrive through a new seed file, `backend/src/VoxLib.Dal/Seed/authors.json`,
and `books.json` continues to credit authors by name, which is now a reference into it.

**Rationale**: The transliteration system is not a free choice: the slugs already in the
repository were made with KMU 2010, the system Ukrainian passports use, and running it over them
reproduces them exactly, with the Ґ/Г distinction intact. That held for the placeholder titles it
was first checked against, and it holds for the real ones: the four cover files in
`covers/source/` were named before this research existed and are character for character what the
function produces, `Щоденник сотника Устима. Як козаки Кавказ воювали` to
`shchodennyk-sotnyka-ustyma-yak-kozaky-kavkaz-voiuvaly` among them. Picking any other system would
give authors addresses spelled by one rule and books addresses spelled by another.

Running the same function over the twenty author names in the catalogue found exactly one
collision:

```
COLLISION taras-shevchenko    Taras Shevchenko | Тарас Шевченко
```

**This finding is void, and the reason is worth keeping.** Those twenty names belong to the
placeholder catalogue that FR-064 removes. Two rows for one person was a defect in mock data, and
it is deleted rather than fixed. What survives is the transliteration choice itself, and the
replacement confirms it independently: the four real cover files were named before this research
existed, and they are character for character what `AuthorSlug.From` produces, including the
awkward cases (`Щоденник сотника Устима` to `shchodennyk-sotnyka-ustyma`, Щ to `shch`, я to `ia`,
ю to `iu`). FR-038's refusal rule also survives, on its own merit rather than because the seed
happened to violate it: it is what stops the next collision becoming `taras-shevchenko-2` in
silence, and `AuthorSlugTests` proves the rule rather than the defect.

Sort names are stored rather than derived because the derivation that works is a guess. Taking
the last whitespace-separated token as the surname is right by coincidence often enough to be
tempting, and the first mononym, patronymic or reversed name breaks it silently into an ordering
nobody can see is wrong. FR-039 asks for a field, not for an algorithm, and hand-written values
cost less than one wrong one. Against the real catalogue there are four of them.

**Alternatives considered**:

- *Deriving the slug at read time from the name.* Rejected by FR-038 and by the reason book slugs
  are stored: correcting a spelling would silently move an author's address and break every link
  to it.
- *Appending a number on collision (`taras-shevchenko-2`).* Rejected by FR-038 explicitly. The
  collision this research found was in placeholder data that FR-064 deletes, but it is still what
  the requirement is for: an automatic suffix produces two addresses for one person and buries the
  defect instead of stopping on it.
- *Author objects inline in `books.json`.* Rejected because an author credited on three books
  would carry three copies of their slug and sort name, and the first correction would edit two
  of them.

## R4. Cover storage: one SDK, two providers, and public read configured out of band

**Decision**: `AWSSDK.S3` 4.0.103.2. The interface is `ICoverStorage` in
`VoxLib.Model/Storage/`, implemented once by `S3CoverStorage` in `VoxLib.Platform/Storage/`,
against MinIO in development and Cloudflare R2 in production with nothing but configuration
between them. Two prepared widths, 160 and 320, stored as WebP at
`covers/{slug}-{width}.webp`. Bucket creation and the public-read policy are **not** application
code: a `minio/mc` sidecar in `docker-compose.yml` does it for development, and R2's public
access is configured in Cloudflare.

**Rationale**: One SDK for both providers is the whole of FR-043, and the AWS SDK is the one
Cloudflare's own R2 documentation targets. The MinIO .NET client (7.0.0) would work against R2
as well, but it is the narrower of the two and would make R2 the awkward case rather than the
production case.

Keeping the bucket policy out of the application is the decision that actually protects the
"changing provider is configuration" claim. `PutBucketPolicy` works on MinIO and is not supported
by R2, so an implementation that set the policy itself would have a development-only branch
inside the production adapter — exactly the shape the interface exists to prevent. `ICoverStorage`
therefore does three things and no more: compose a public URL, ask whether an object exists, and
put one. The sidecar is the standard MinIO idiom (`mc alias set`, `mc mb --ignore-existing`,
`mc anonymous set download`) and needs `network_mode: service:db` for the same reason Mailpit
does: every service in this compose file shares one network namespace, and on a bridge network
the bucket would be unreachable from the API.

Covers are public under Principle IV and are never signed, so `ICoverStorage` has no signing
method at all, and `IAudioStorage` remains an interface that does not exist yet rather than a
generalisation of this one. FR-044 and the spec's own reasoning agree: one interface serving both
would have to be told on every call which kind of object it is holding.

The widths come from the sizes FR-049 forces, bounded by the art that exists. The catalogue cover
goes from 56 wide to 128 and the book page from 112 to 192, both at a 1280 viewport, so a
two-times display asks for 256 and 384 device pixels. That argued for 160, 320 and 640.

**640 was dropped when the real covers arrived.** Three of the four source images are 507, 517
and 550 pixels wide, so a 640 object would exist only by upscaling, which is a larger file
carrying no more detail. 160 covers 128 at 1×, and 320 covers 128 at 2× and 192 at 1.67×. The
book page at 192 on a two-times display is therefore served 320 for 384 device pixels, which is
the one place the art runs out; it is a real limit and it is the sources' limit, not the
format's. Replacing a source at 640 or wider is what lifts it, and the third width comes back on
that day.

The same arrival settled the shape. The four covers run 0.640, 0.674, 0.688 and 0.753 wide
against tall, so no fixed box suits them: 2:3 would crop about 11% of the height off the last
one. FR-067 therefore fixes the width and lets the height follow the image. This costs nothing in
layout, because covers appear only as a fixed-width item in two flex rows, `.catalogue__item` and
`.book__summary`, and never in a grid of covers where uneven heights would show. The placeholder
is the one thing that needs an explicit `aspect-ratio`, having no image to take proportions from.

The picker is `srcset` plus `sizes` on an ordinary `<img>`, so the browser chooses and nothing is
resized on request, which is FR-045.

FR-051 needs one piece of care that is easy to miss. Composing a cover URL touches no network, so
an unreachable bucket never fails a catalogue response — but the browser's `<img>` does fail, and
a failed `<img>` is the broken-image icon SC-015 forbids. `CoverArt.tsx` therefore keeps an
`onError` that swaps to the placeholder, which is the same designed state a book with no cover
already gets.

## R5. Seeding covers: two seeders, because they have different guards

**Decision**: `CatalogueSeeder` is unchanged in shape and keeps its "only when the catalogue is
empty" guard. A second class, `CoverSeeder`, in the same `VoxLib.Dal/Seed/` folder, uploads each
embedded cover the bucket does not already hold, guarded per object. Both are called from the
composition root's existing startup scope. The prepared images are embedded resources alongside
`books.json`.

**Rationale**: The two guards are genuinely different, which is the whole argument for two
classes. The catalogue is seeded once into an empty database and left alone afterwards. Covers
have to be uploaded whenever the bucket lacks them, and the bucket and the database are separate
volumes with separate lifetimes: a rebuilt container with a surviving `pgdata` volume and a fresh
MinIO volume has a full catalogue and no covers, and a `CatalogueSeeder` that returned early
would leave every cover missing. Per-object `HeadObject` before `PutObject` is FR-047's "every
image the configured storage does not already hold", and it makes the step idempotent across a
deployment, a test run and a container rebuild alike.

Putting it in `VoxLib.Dal` is a deliberate reading of the dependency rule rather than an
oversight. `.Dal` may reference `.Model`, and `ICoverStorage` is in `.Model`, so nothing points
outward. The seed folder is the catalogue's bootstrap rather than a repository, and `books.json`
is already embedded there; the covers are the same kind of thing in a different format.

**Alternatives considered**: an `mc cp` step in the compose sidecar was rejected because the
images would then reach development and nothing else — FR-047 asks for one step that a fresh
container, a test run and a deployment all go through.

## R6. Where the search suggestions come from

**Decision**: `GET /api/search-suggestions`, answering at most five suggestions, each a title of
a published book or the name of an author credited on one, sampled fresh per request and served
`Cache-Control: no-store`. The sampling rule lives in `CatalogueSuggestions` in
`VoxLib.Orchestrator/Book/`, behind `ICatalogueSuggestions` in `VoxLib.Model/Book/`, reading
`IBookRepository.SampleTitlesAsync` and `IAuthorRepository.SampleNamesAsync`.

**Rationale**: The clarification settled that the sample is random and drawn per request, and the
consequence — that tests assert its shape rather than its content — is accepted in the spec. What
Phase 0 had to settle is where the rule lives and how the guarantee in FR-023 is kept.

The rule is that a suggestion always finds something. That holds only because every candidate is
drawn from a published row: a title is matched by `ILIKE '%title%'` against itself, and an author
name against the same author, both of which the existing search already does. Nothing is
composed, shortened or prettified on the way out, because any edit to the string is an edit to
what the search will be given. That is a business rule, so it sits in the orchestrator and not in
the handler, per Principle II.

Five, not three, because the same resource serves both halves of FR-022: the links beneath the
field and the rotating example inside it. Three links is within the spec's "three to five", but
three examples rotating on a six-second cycle repeat inside twenty seconds and start reading as a
loop rather than as a sample of a library. The split is half titles, half author names, rounded
towards titles, so a visitor sees both kinds of thing to search for. Fewer than five rows in the
catalogue returns fewer suggestions, and an empty catalogue returns an empty list, which is the
edge case the spec already describes.

`no-store` is load-bearing rather than tidy. A response cached by the browser would hand a
returning visitor the same handful the spec asks not to show them, and the sample would be fresh
per request on the server while being frozen per visitor in practice.

**Alternatives considered**:

- *A curated table of suggestions.* It is what an administrative screen would eventually edit,
  and the spec records that path. Rejected for now by FR-022: a curated list needs maintenance
  from the day a book is added, and this feature does not build the screen that would do it.
- *Extending `/api/books` with a `sample` parameter.* Rejected by Principle V. A suggestion is
  not a book — half of them are author names — and returning a different shape from the same
  address for a different query string is how a resource stops being one.

## R7. The rotating example, and why it is allowed to move at all

**Decision**: The placeholder rotates through the suggestions on a six-second interval, freezes
permanently the first time the field takes focus, and never starts at all when
`matchMedia('(prefers-reduced-motion: reduce)')` matches. Until the suggestions arrive, and
whenever they cannot be fetched, the field carries a fixed neutral placeholder. The visible
`<label>` is what names the field in every one of those states.

**Rationale**: Text that changes inside a control is ordinarily a defect, and the spec is
explicit that the constraints are part of the requirement rather than left to whoever implements
it. Three of them matter for a different reason each. Freezing on focus is about the person who
is typing: a placeholder disappears on first keystroke anyway, and one that changes while they
are deciding what to type is movement in the exact spot their attention is. Freezing for the rest
of the visit, rather than until blur, is what stops the field becoming a thing that resumes
moving whenever they look away. And the reduced-motion case is not a nicety: `prefers-reduced-motion`
is asked for by people for whom moving text is a symptom trigger, not a preference.

The placeholder is supplementary throughout, which is why FR-019 can be satisfied by doing
nothing when the fetch fails. A screen reader user never depends on it: the label names the
field, and the suggestions are links beneath it. That asymmetry is the point of having both —
the rotating example is help that only the eye can use, and the links are the same help in a form
a keyboard and a screen reader can use.

Six seconds was chosen against the spec's "slowly enough to be read rather than watched". At five
suggestions that is a thirty-second cycle, so a visitor reading a catalogue page sees two or
three examples and never the same one twice.

## R8. Reading the session once, and what "zero tab stop changes" can mean

**Decision**: `useSession` moves behind a `SessionProvider` rendered in `root.tsx`, with a
module-level promise so that one request is made per page view however many components ask. The
account controls are the **last** group in the banner's tab order, and the slot they occupy is
reserved at its resolved size while the session is being read, containing no focusable element.
SC-005 is measured as: every tab stop before the account controls is identical in both states,
and every element's bounding box is identical in both states.

**Rationale**: The sharing half is unambiguous and is FR-009. Today `useSession` holds state per
call site with no cache, which is correct at one consumer and wrong at two; the header is the
second one, and the book page's registration link will be a third.

The reservation half needs a stated reading, because the literal one is not achievable and
pretending otherwise would produce a test that is either false or vacuous. The session is read
after the document loads — it cannot be otherwise, since the catalogue pages are prerendered and
the cookie is `HttpOnly` — and the anonymous and signed-in variants have different numbers of
controls: two links to sign in or register, against one button to sign out. No arrangement makes
those the same count, so the *number* of tab stops in the document necessarily changes when the
session resolves.

What FR-008 actually protects is a person who has already started moving: "no tab stop they have
already passed moves and no element shifts under them". Both halves of that are achievable and
both are testable. Ordering the account controls last in the banner means the skip link, the
wordmark, the navigation and the search field — everything a visitor reaches in the first few
seconds — keep their positions exactly. Reserving the slot's box means nothing below it moves on
the page. The measurement follows the requirement rather than the success criterion's shorter
phrasing, and the plan records that as a resolved reading rather than a waiver, in the same way
`003-visual-identity` resolved FR-007's region edges.

**Alternatives considered**: rendering the anonymous variant optimistically and correcting it was
rejected outright — it tells a signed-in person they are signed out, which is the shared-device
case `002-user-accounts` built this component for. Rendering both variants and hiding one with
CSS was rejected because a hidden link is still in the accessibility tree unless it is removed
from it, and removing it is the same state change with extra steps.

## R9. The primary action colour, computed

**Decision**: `--action: #2aa37d` with `--on-action: #070d22`. Two new roles, no hover role. The
contrast matrix in `frontend/scripts/check-design-tokens.mjs` gains three rows.

**Rationale**: The palette has thirteen roles and none of them is the colour of doing something,
so on the sign-in screen the submit button carries `--surface-raised` and `--border-control` —
exactly what the email and password fields carry. The button is therefore the least distinguished
thing on the page a visitor came to press.

The value was computed against the constraints rather than chosen and then checked. The fill has
to read as a distinct element on both region colours, so it is held to the 3:1 that
`--border-control` is already held to; the label on it has to clear 4.5:1; and the hue has to be
unmistakably not the focus amber. `#2aa37d` measures 4.58:1 against `--surface`, 3.75:1 against
`--surface-raised`, 6.09:1 against `--ground`, and its label at `#070d22` measures 6.09:1 against
the fill. Every margin is comfortable, which matters because a token whose pair sits at 3.02:1
fails the build on the next small adjustment.

One number has to be explained rather than enforced. `--focus` against `--action` is 2.06:1, and
that is not a violation. The indicator is an `outline` at `--focus-offset: 2px`, so what sits
against the ring on both sides is the region behind the button, not the button's own fill, and
`--focus` against `--surface` and `--surface-raised` is already in the enforced matrix. FR-054
asks that the two stay distinct, which amber against teal plainly is; it does not ask for a ratio
between a control and a ring that never touch it. The figure is recorded in
[data-model.md](./data-model.md) with this reasoning so that nobody adds it to the matrix later
and finds it failing.

No hover role is added, because this site has no hover styles anywhere — `grep -n hover
frontend/src/styles/*.css` returns nothing. Adding one for the primary action alone would make it
the only element on the site that responds to a pointer, which is a design decision this feature
was not asked to make.

## R10. Reaching a signed-in session from the browser suite

**Decision**: A Playwright fixture registers through the real form, polls Mailpit's HTTP API for
the message addressed to that account, extracts the confirmation link from it, follows it, and
signs in through the real form. Each run uses a fresh address, so no state is cleared between
runs.

**Rationale**: Mailpit's API was read off the running container rather than from its
documentation, because the version in the compose file is what has to work. At v1.31.1,
`GET /api/v1/messages?limit=n` returns `{ messages: [{ ID, To: [{ Address }], Subject, ... }] }`
and `GET /api/v1/message/{ID}` returns the body. Against the messages the API has already sent in
this container, the confirmation link comes back intact:

```
http://localhost:5173/confirm?account=01a091a9-...&token=...
```

Mailpit is reachable at `localhost:8025` from a test because `network_mode: service:db` puts it
in the same network namespace as everything else, which is the arrangement `002-user-accounts`
already relies on.

Registering through the form rather than through the API is deliberate on two counts. It exercises
the antiforgery handshake and the real registration path, so the fixture fails when the product
does; and it means the fixture is written in the same vocabulary as the assertions around it.
Polling for the message addressed to this run's own address, rather than taking the newest
message, is what makes the suite safe under `fullyParallel: true`, where several specs register
at once.

Idempotency across runs, which SC-021 asks about explicitly, comes from `freshEmail()` — already
in `tests/a11y/account-support.ts` — rather than from clearing the database or the mailbox. A run
that deleted messages would break every other spec running beside it.

**Alternatives considered**: seeding a confirmed account directly into the database was rejected
because it proves nothing about the flow a real person walks, and because the test suite would
then need a database connection it does not have today. Reading the SMTP transcript from a
recording sender was rejected for the same reason plus one more: `RecordingEmailSender` exists in
the endpoint tests, not in the application the browser talks to.

## R11. Crawler files and sharing metadata

**Decision**: `robots.txt` is a static file in `frontend/public/`, naming the sitemap. The sitemap
is written after the build by `frontend/scripts/write-sitemap.mjs`, which imports the same
`catalogue-paths.mjs` the prerender uses, so the two cannot disagree. Sharing metadata is Open
Graph and Twitter card tags emitted by each route's existing `meta` export. The absolute origin
comes from `VOXLIB_SITE_ORIGIN`, defaulting to `http://localhost:5173`.

**Rationale**: FR-058 and FR-032 are the same list of addresses seen from two sides — what gets a
document and what gets a sitemap entry — so discovering it twice is how they drift. Extracting
the discovery is a small refactor of code that already exists in `react-router.config.ts` and
already reads the API rather than the seed file, for the reason recorded there.

`robots.txt` is static because its content does not depend on the catalogue: it allows
everything, points at the sitemap, and the pages that must stay out of the index are kept out by
the `noindex` meta they already carry and by the API's `X-Robots-Tag`, not by a disallow rule.
Principle IV is explicit that `robots.txt` is advisory and is never the protection.

Open Graph needs an absolute image URL, which is the one thing the cover work supplies for free:
covers are public, unsigned and already absolute, because they come from object storage rather
than from the site's own origin.

This story is P8 and the spec marks it droppable. Nothing above it depends on it, and the plan
sequences it last.

## R12. The header at 320 pixels, and naming the landmarks

**Decision**: The banner is a wrapping flex row: wordmark, primary navigation, search form,
account slot, in that source order, with the search form allowed to take the full width on its
own line. No disclosure control. The landmark names are `Основна навігація` for the header
navigation, `Обліковий запис` for the account controls (unchanged from today),
`Пошук по каталогу` for the search landmark, `Навігаційний ланцюжок` for the breadcrumbs, and
the pagination keeps the names it already passes.

**Rationale**: The no-burger decision is the spec's and is not revisited. What Phase 0 had to
check is that it survives 320 pixels with a visible label above the field, and it does: three
navigation destinations, a wordmark and an account slot wrap to three or four lines, and the
search field is a full-width row. The cost is vertical space on a small screen, which is the
trade the spec accepts, and it is paid back by the skip link FR-007 adds.

Naming every landmark matters more here than it looks, because this feature takes a page from two
landmarks to six. A screen reader's landmark list with two navigations both announced as
"navigation" is worse than one navigation, and FR-005 is written to forbid it. `<header>` and
`<footer>` as direct children of `<body>` are `banner` and `contentinfo` without an explicit
role; `<form role="search">` needs its own name because the pagination navs and it will sit in
the same list.
