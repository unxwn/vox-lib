# Contract: Addresses, landmarks and trails

**Feature**: `specs/004-site-shell/` | **Date**: 2026-09-11

The site's own interface: what address serves what, what a request that matches nothing gets,
what landmarks a screen reader finds, and what trail sits above each page. It is a contract
rather than a description because three test suites read it — the browser suite walks the address
table, the prerender suite checks the document list against it, and the redirect spec checks the
status codes.

## Addresses

| Address | Page | Document | Indexed | Trail |
| --- | --- | --- | --- | --- |
| `/` | Landing | prerendered | yes | none |
| `/books` | Catalogue, page 1 | prerendered | yes | Головна › Каталог |
| `/books/page/:n` | Catalogue, page n | prerendered, one per page | yes | Головна › Каталог › Сторінка n |
| `/books/:slug` | Book | prerendered, one per book | yes | Головна › Каталог › *title* |
| `/authors` | Index of authors | prerendered | yes | Головна › Автори |
| `/authors/:slug` | Author | prerendered, one per author | yes | Головна › Автори › *name* |
| `/search` | Search results | fallback | **no** | Головна › Пошук |
| `/about` | About | prerendered | yes | Головна › Про проєкт |
| `/register` | Registration | fallback | **no** | none |
| `/confirm` | Confirming an address | fallback | **no** | none |
| `/sign-in` | Sign in | fallback | **no** | none |
| `/forgot-password` | Forgotten password | fallback | **no** | none |
| `/reset-password` | New password | fallback | **no** | none |
| `*` | Not found | fallback | **no** | none |

"Document" says whether the address is emitted as a standalone HTML file at build time (FR-032)
or answered by the single-page fallback. The account addresses stay out of the prerender list for
the reason `002-user-accounts` recorded: a page that was never generated cannot be indexed, and
these are personal to one person. `/search` stays out because its content depends on a query
string.

"Indexed" is carried by the `robots` meta the page already emits, not by `robots.txt`, which is
advisory and is never the protection (Principle IV). FR-060 requires these to be unchanged from
today.

### Changed addresses

| Old | New | Answer |
| --- | --- | --- |
| `/` | `/books` | **not** a redirect. `/` keeps working and serves the landing page. |
| `/page/1` | `/books` | `301` |
| `/page/:n` | `/books/page/:n` | `301` |

`301` rather than `302` or a client-side navigation: a permanent redirect is what teaches a
crawler and a browser the new address, and anything that answers `200` first teaches them the old
one is still good. R1 records why this has to be served by the host rather than by the
application, and where the table lives.

### Static files

| Request | Answer |
| --- | --- |
| a path whose last segment contains a dot, and a file exists | the file, `200` |
| a path whose last segment contains a dot, and no file exists | **`404`**, not a document |
| any other unmatched path | the not-found page, with header, footer and search suggestions |

The second row is FR-048 and is the mechanism that makes SC-016 measurable. Today every one of
these answers `200 text/html`, which is why two books have shown a broken image icon since the
catalogue shipped and nothing could see it.

## Landmarks

Every page carries exactly these, and every navigation and search landmark carries a name that
distinguishes it (FR-005).

| Landmark | Element | Accessible name |
| --- | --- | --- |
| `banner` | `<header>`, child of `<body>` | — |
| `navigation` | `<nav>` in the header | `Основна навігація` |
| `search` | `<form role="search">` in the header | `Пошук по каталогу` |
| `navigation` | `<nav>` holding the account controls | `Обліковий запис` |
| `navigation` | `<nav>` holding the breadcrumbs | `Навігаційний ланцюжок` |
| `main` | `<main id="main">` | — |
| `navigation` | pagination, where a page has it | the label it already passes |
| `contentinfo` | `<footer>`, child of `<body>` | — |

Exactly one `search` landmark per page (FR-011). The search results page loses the form in its
body; the header field carries the current term and is the only search input anywhere.

## The header

Source order, which is also tab order:

1. **Skip link** — first focusable element on every page, moves focus to `#main` (FR-007).
2. **Wordmark** — `vox-lib` as readable text, a link to `/`, announced once (FR-002).
3. **Navigation** — `Каталог` → `/books`, `Автори` → `/authors`, `Про проєкт` → `/about`.
4. **Search** — a visible `<label>`, a `type="search"` input bounded at 100 characters, a submit
   button. `method="get"`, `action="/search"`, field named `q`.
5. **Account slot** — last, and the only part that depends on who is looking.

The slot is last so that nothing a visitor has already tabbed past moves when the session
resolves (FR-008); it reserves its resolved size while loading and contains no focusable element
in that state. R8 records the reading of SC-005 this implies.

Down to a 320 device-independent pixel viewport the bar wraps onto further lines. There is no
disclosure control and no burger (FR-006).

## The footer

A `contentinfo` landmark on every page, holding:

- what vox-lib is and who runs it, in a sentence;
- a link to `/about`;
- a link to `/fonts/OFL.txt`, the SIL Open Font License for Fixel, which ships with the site and
  is currently linked from nowhere (FR-003).

## Breadcrumbs

`<nav aria-label="Навігаційний ланцюжок">` holding an `<ol>`. Every item except the last is a
link; the last is the current page, is not a link, and carries `aria-current="page"` (FR-004).

The trail is derived from the address, which is what makes FR-031 hold by construction rather
than by review. The landing page and the not-found page have none.

## Search suggestions in the page

Rendered as ordinary links, each running the search it names, on:

- `/search`, before anything has been typed, in place of the results;
- the not-found page, where a way to look for what was wanted is worth more than a way back.

They are links, not buttons and not a listbox, because a link can be reached, announced and
activated by a keyboard and a screen reader, which the rotating example inside the field cannot
(FR-020). Nothing suggests results while a visitor types (FR-024).

## The rotating example

| Condition | Behaviour |
| --- | --- |
| field untouched, motion allowed | the placeholder cycles through the suggestions every 6s |
| field takes focus | freezes, and does not change again for the rest of the visit |
| `prefers-reduced-motion: reduce` | never changes at all |
| suggestions unavailable | a fixed neutral placeholder; the field stays fully usable |

Form: `напр. Кобзар`, `напр. Леся Українка`. It is supplementary in every state — the visible
label, never the placeholder, is what says what the field is for (FR-013, FR-019).

## Sharing metadata

On `/`, every `/books/:slug` and every `/authors/:slug` (FR-059): `og:title`, `og:description`,
`og:url`, `og:type`, `twitter:card`, and `og:image` pointing at the cover's largest prepared
width where one exists. Absolute URLs, from `VOXLIB_SITE_ORIGIN` for the page and from object
storage for the image.

`robots.txt` allows everything and names `/sitemap.xml`. The sitemap lists the landing page,
every catalogue page, every book and every author, discovered from the API by the same code the
prerender uses (FR-058).
