# Data Model: Site Shell

**Feature**: `specs/004-site-shell/` | **Date**: 2026-09-11

Three kinds of thing are recorded here: what changes in the catalogue's persistent schema, what
the shell's non-persistent structures are, and the two colour roles the palette gains. Every
contrast ratio below was computed with the WCAG 2.x relative luminance formula from the values in
`frontend/src/styles/tokens.css`. None was judged by eye, and
`frontend/scripts/check-design-tokens.mjs` recomputes all of them on every `pnpm lint`.

## Persistent entities

### Author

Gains two stored columns. Nothing else about the row changes.

| Field | Type | Constraints | Why |
| --- | --- | --- | --- |
| `Id` | `Guid` | PK, v7 | unchanged |
| `Name` | `string` | 300, `uk-UA-x-icu`, required | unchanged |
| `Slug` | `string` | 200, **unique**, required | FR-038. The author's address. |
| `SortName` | `string` | 300, `uk-UA-x-icu`, required | FR-039. Orders the index by surname. |

**Slug.** A KMU 2010 transliteration of `Name`, lowercased, non-alphanumerics collapsed to single
hyphens. Computed by `AuthorSlug.From(name)` in `VoxLib.Model/Book/`, called **once**, when the
author row is created. It is never recomputed: FR-038 requires that correcting a spelling leave
the address alone, which is the same rule `Book.Slug` already follows and for the same reason.

Uniqueness is enforced by `ix_authors_slug`, and a collision is an error rather than a resolution:
no number is appended. R3 found one collision in the placeholder catalogue; FR-064 removes that
catalogue, so the rule now ships with nothing violating it and is proved by a constructed case in
`AuthorSlugTests` rather than by the seed. It is worth keeping for the reason it was written: an
automatic suffix turns two rows for one person into two addresses and buries the defect.

**SortName.** Written by hand in `authors.json`, in `Прізвище, Ім'я` form. Not derived; R3
records why. It carries the Ukrainian collation because it is the column the index is ordered by,
and sorting Ukrainian text under the database's default collation is how `Ї` ends up after `Я`.

### Book

Gains two columns and loses one in favour of another. FR-042 forbids anything further except the
credit role below, which is on the link rather than on the book.

| Field | Type | Constraints | Why |
| --- | --- | --- | --- |
| `CoverArtUrl` | `string?` | — | **removed** |
| `CoverKey` | `string?` | 200 | FR-043. The object-key stem, not a URL. |
| `Narrator` | `string?` | 300 | FR-040. The озвучувач, a name. |
| `AddedToCatalogue` | `DateTimeOffset` | required | FR-041. |

**CoverKey replaces CoverArtUrl.** The stored value is the stem a set of prepared objects share —
`stratehiia-i-taktyka-liderstva`, not `/covers/stratehiia.svg` and not a full URL. A stored URL would be a location
baked into the database at seed time, so changing storage provider would be a data migration
rather than a configuration change, which is exactly what FR-043 forbids. The full addresses are
composed at the edge by `ICoverStorage`, in `VoxLib.Platform`, from configuration.

The column stays nullable. A book with no cover is a designed state and gets the placeholder, per
FR-046.

**Narrator** is optional and is a name, not a file reference. A book with none shows none. It is
the field that distinguishes an audiobook from a book, and it is what a listener chooses between
two recordings by.

**AddedToCatalogue** is required, which is a consequence the clarification session accepted
rather than an oversight. The migration adds the column with a default of the moment it runs and
backfills every existing row to that value, so the field is always present. The backfill now
applies to placeholder rows that FR-064 deletes in the same feature, so what matters in practice
is the default: the four real books take the moment the seeder inserts them, and all four share
one arrival date, so the first "recently added" list will show them as one batch. It is surfaced
nowhere in this feature and is therefore deliberately **absent from every wire contract** — it is
recorded now because it cannot be backfilled honestly later.

### Credit role

FR-066. How one person is credited on one book, held on the join between them rather than on
either end.

| Field | Type | Constraints | Why |
| --- | --- | --- | --- |
| `book_authors.role` | `CreditRole` | required, default `Author` | FR-066. `Author` or `Compiler`. |

The placement is the decision. A role on the `authors` row would say that Дмитро Савченко *is* a
compiler, which is true of the one book he compiled and false the moment he writes one. A role on
the `books` row would say a book has one kind of credit, which is false as soon as a book has an
author and a translator. It belongs to the pairing, and the pairing is the join table.

`CreditRole` is an enum in `VoxLib.Model/Book/`, not a string, so "author or compiler" is decided
in the domain rather than in a mapping file or a seed row. It holds exactly two values: translator
and narrator credits are deferred with everything else the spec's assumptions defer to a content
feature, and FR-040's narrator stays a name on the book rather than a third role, because it is
about a recording rather than about the text.

The default is `Author`, so every existing row and every book that says nothing about roles means
what it already meant.

### What is not added

Genre, tags, series, position in series, translator, publication year, and the provenance of a
recording are all named in the spec as deferred to a later content feature. Nothing about audio
is added: no storage key on a chapter, no duration-of-file, no bitrate. `ICoverStorage` has no
signing method, so this feature cannot express a private object at all.

## Migration

One migration, `SiteShell`, doing five things in this order:

1. `authors.slug` and `authors.sort_name` added as nullable, backfilled from the seed's authored
   values, then made `NOT NULL`. `ix_authors_slug` unique.
2. `books.narrator` added, nullable.
3. `books.added_to_catalogue_at` added `NOT NULL` with `DEFAULT now()`, which is what backfills
   the existing rows to the moment of the migration.
4. `books.cover_art_url` renamed to `books.cover_key` and its values rewritten from
   `/covers/{stem}.svg` to `{stem}`.
5. `book_authors.role` added `NOT NULL` with a default of `Author`, so every existing pairing
   keeps the meaning it already had.

Step 1's three-step dance is the only way to add a required unique column to a table that already
has rows. Step 4 is a rename rather than a drop-and-add, which costs nothing and keeps the
migration honest about what happened to the column, though the two rows carrying a value are
placeholder books that FR-064 deletes immediately afterwards. The migration is written to be
correct against a database that already exists, including a deployed one; the catalogue
replacement is the seeder's job and not the migration's.

## Storage layout

Covers live in one bucket, public read, never signed.

| | Development | Production |
| --- | --- | --- |
| Provider | MinIO in `docker-compose.yml` | Cloudflare R2 |
| Endpoint | `http://localhost:9000` | the account's R2 endpoint |
| Bucket | `vox-lib-covers` | `vox-lib-covers` |
| Public base | `http://localhost:9000/vox-lib-covers` | the bucket's public hostname |
| Bucket created by | the `minio/mc` sidecar | Cloudflare, once |
| Public read set by | `mc anonymous set download` | Cloudflare public access |

**Object key**: `covers/{cover-key}-{width}.webp`, for widths 160 and 320.

The widths are not a scale, they are the sizes the rendered boxes ask for, bounded by the sources
that exist. FR-049 moves the catalogue cover to 128 wide and the book page to 192 wide at a 1280
viewport, so a two-times display requests 256 and 384 device pixels. 160 serves 128 at 1×, and 320
serves 128 at 2× and 192 at 1.67×. A 640 was planned and dropped: three of the four source images
are 507, 517 and 550 pixels wide, so it would have existed only by upscaling. The one place the
art runs short is the book page at 192 on a two-times display, which is served 320 for 384 device
pixels. The browser picks with `srcset` and `sizes`; nothing is resized on request, which is
FR-045.

**Shape**: the width is fixed and the height follows the image, per FR-067. The four covers run
0.640, 0.674, 0.688 and 0.753 wide against tall, so a fixed 2:3 box would crop roughly 11% off the
tallest one. This costs no layout: covers appear only as a fixed-width item inside two flex rows,
`.catalogue__item` and `.book__summary`, never in a grid where uneven heights would show. The
placeholder is the exception and keeps an explicit `aspect-ratio`, having no image to take
proportions from.

## Non-persistent structures

These are the shell's own shapes. None of them is stored.

### Breadcrumb trail

An ordered list of `{ label, href }` ending in the current page, which carries no href and is
marked `aria-current="page"`. Derived from the address, never authored per page, so FR-031 holds
by construction. The full table is in [contracts/site-addresses.md](./contracts/site-addresses.md).

The landing page and the not-found page have no trail. The first is the top level and a trail of
one entry pointing at itself is noise; the second has no place in the hierarchy, so it offers
search suggestions instead.

### Search suggestion

One phrase drawn from the published catalogue, serving twice: as a link that runs the search it
names, and as one frame of the rotating example inside the untouched field.

| Field | Type | Notes |
| --- | --- | --- |
| `term` | `string` | Verbatim. A published book's title or a credited author's name. |
| `kind` | `"title" \| "author"` | What it was drawn from. Lets the link be announced precisely. |

At most five per response, split half and half rounded towards titles, sampled fresh per request,
`Cache-Control: no-store`. FR-023's guarantee — activating one always returns at least one
result — holds only because the string is never edited on the way out; R6 records why.

### Session

Unchanged in shape. What changes is that it is read once per page view and shared, rather than
once per consumer. `{ state: 'loading' } | { state: 'anonymous' } | { state: 'signedIn', email,
isVerifiedBeneficiary }`, now supplied by a provider in `root.tsx`.

## Colour roles

Two roles added to the thirteen. Every value below is measured, and the three constrained pairs
join the enforced matrix in `check-design-tokens.mjs`.

| Token | Value | What it is for |
| --- | --- | --- |
| `--action` | `#2aa37d` | The fill of the control that submits a form. Nothing else. |
| `--on-action` | `#070d22` | Text and any icon on that fill. |

### Constrained pairs

| Foreground | Background | Floor | Measured | Requirement |
| --- | --- | --- | --- | --- |
| `--action` | `--surface` | 3:1 | **4.58:1** | FR-052, the action reading as a distinct control |
| `--action` | `--surface-raised` | 3:1 | **3.75:1** | FR-052, the same on a nested region |
| `--on-action` | `--action` | 4.5:1 | **6.09:1** | FR-053, its label |

### Measured but not constrained

| Pair | Measured | Why it is not a floor |
| --- | --- | --- |
| `--action` / `--ground` | 6.09:1 | A primary action always sits on a region, never on the bare ground. Recorded because the landing page's call to action is the first thing that would be tempted otherwise. |
| `--focus` / `--action` | 2.06:1 | **Not a violation.** The indicator is an `outline` at `--focus-offset: 2px`, so what abuts the ring on both sides is the region behind the button, and `--focus` against `--surface` (already enforced) is the pair that decides whether the ring is visible. FR-054 asks that the two stay distinct in hue, which amber against teal plainly is; it does not ask for a ratio between a control and a ring that never touches it. Recorded here so that nobody adds it to the matrix later and finds it failing for the wrong reason. |
| `--action` / `--link` | 1.81:1 | Both are interactive, and a button and a link are told apart by shape and by role, not by colour. |

`--on-action` currently holds the same value as `--ground`. That is a coincidence, not a
relationship, and it is a separate token because a colour role names what a colour is *for*: if
the ground moves, the label on a teal button should not move with it.

### The single warm hue

`--focus` remains the only amber on the site and is spent on nothing else, which is FR-054. The
whole value of the focus indicator is that nothing else is that colour, so a ring is unmistakable
wherever it lands; a primary action in the same family would cost exactly that.
