# Feature Specification: Browse the Book Catalogue

**Feature Branch**: `feature/browse-catalogue`

**Created**: 2026-09-08

**Status**: Draft

**Input**: User description: "Book catalogue browse. Public catalogue: list books, view a book detail page with author, cover, description and chapter list. No audio, no accounts."

## Clarifications

### Session 2026-09-08

- Q: How should catalogue pages be rendered, given that search engine indexing needs the
  content to be present in the served document? â A: Generate the catalogue and book pages
  as complete documents when content is published, and keep the existing client-rendered
  application for everything after the first load. No server-side rendering process runs in
  production.
- Q: How should a visitor move through a catalogue larger than one view? â A: Numbered
  pages of a fixed size, each reachable by its own address, with focus moved to the results
  heading and the new position announced whenever the page changes.
- Q: Should the search story ship inside this feature or move to its own specification? â
  A: Search ships inside this feature.
- Q: Where does catalogue data come from at runtime, given that administration is out of
  scope? â A: A persistent store, populated from a seed dataset kept under version control
  so that a fresh environment and the automated tests start from identical content.
- Q: Is the single-locale Ukrainian decision in FR-015 confirmed, now that it also fixes
  the sort order of the stored catalogue? â A: Confirmed. Ukrainian interface, Ukrainian
  content, and Ukrainian sorting and matching rules.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse the list of books (Priority: P1)

A visitor arrives at the library and wants to know what it holds. They move through a
list of audiobooks, each identified by its title and author, and they can do this with a
screen reader or with the keyboard alone.

**Why this priority**: It is the entry point to everything else. Nothing in the catalogue
is reachable without it, and it is the first thing both a new listener and a search engine
encounter. On its own it already answers the question "what can I listen to here".

**Independent Test**: Seed a set of books, open the catalogue, and confirm with a screen
reader and with the keyboard that every book in the collection can be reached and
correctly identified. No audio and no account is involved.

**Acceptance Scenarios**:

1. **Given** the library holds books, **When** a visitor opens the catalogue, **Then**
   each book appears with its title, its author, and its cover art.
2. **Given** a visitor using a screen reader, **When** they move through the list,
   **Then** each entry is announced as a single link naming the book and its author, and
   the cover art adds no separate or meaningless announcement.
3. **Given** a visitor using only a keyboard, **When** they move focus forward and
   backward, **Then** focus follows reading order, every entry is reachable, the focused
   entry is visibly marked, and no element traps focus.
4. **Given** the library holds more books than one page shows, **When** the visitor moves
   to another page, **Then** the address changes to that page, focus moves to the heading
   of the results, and they are told which page they are on and which books it covers.
5. **Given** a book with no cover art, **When** it appears in the list, **Then** a
   placeholder stands in its place and the entry is still announced by title and author.
6. **Given** the library holds no books at all, **When** a visitor opens the catalogue,
   **Then** a plainly worded message explains that and the page remains navigable.

---

### User Story 2 - Read about one book (Priority: P2)

A visitor has found a book that interests them and wants to decide whether to listen. They
open it and read the description, see who wrote it, and see how it is divided into
chapters and how long it runs.

**Why this priority**: It is what turns a list entry into a decision, and it carries the
description text that makes a book findable from outside the site. It depends on the list
existing, so it follows P1.

**Independent Test**: Open a seeded book directly by its address and confirm every piece
of metadata is present, correctly structured for a screen reader, and that no audio is
offered.

**Acceptance Scenarios**:

1. **Given** a visitor selects a book from the list, **When** the book opens, **Then**
   they see its title, author, cover, description, total running time, and its chapters in
   order with each chapter's title and running time.
2. **Given** a visitor using a screen reader, **When** they open a book, **Then** the page
   announces its title, exposes a heading structure they can jump through, and announces
   the chapter list as a list with a known number of items.
3. **Given** a visitor using only a keyboard, **When** they move from the list into a
   book, **Then** focus lands at the start of the book's content rather than back at the
   top of the page furniture.
4. **Given** a book whose chapters have not been added yet, **When** a visitor opens it,
   **Then** the page says the chapter list is not available yet instead of showing an
   empty region.
5. **Given** a book that is no longer published, **When** a visitor follows an old link to
   it, **Then** they get a clearly worded not-found page with a route back to the
   catalogue.
6. **Given** any visitor without an account, **When** they open a book, **Then** all of
   its metadata is readable, the page states that listening requires an account, and no
   audio is played or offered.

---

### User Story 3 - Find a book by name (Priority: P3)

A visitor already knows the book or the author they want. They type part of the name and
get to it directly rather than paging through the whole library.

**Why this priority**: Browsing alone stops working as the library grows, and it is the
slowest path for the audience this exists for. It is P3 because a small library is still
fully usable through P1 and P2.

**Independent Test**: With a seeded library, search for a known title fragment and a known
author fragment, and confirm the matching books are returned, counted, and announced.

**Acceptance Scenarios**:

1. **Given** a visitor types part of a title, **When** they submit the search, **Then**
   only books whose title matches are listed.
2. **Given** a visitor types part of an author's name, **When** they submit the search,
   **Then** only books by matching authors are listed.
3. **Given** a visitor using a screen reader, **When** the results change, **Then** the
   number of results is announced politely without stealing their focus.
4. **Given** a search that matches nothing, **When** the results appear, **Then** a
   plainly worded empty state explains it and offers a route back to the full catalogue.
5. **Given** a visitor using only a keyboard, **When** they reach the search field,
   **Then** they can enter a term and submit it without a pointing device.
6. **Given** a book whose title is written in Ukrainian, **When** a visitor searches for
   part of that title in either letter case, **Then** the book matches either way.

---

### Edge Cases

- What happens when the library holds no books, or a search matches none of them?
- What happens when a book has no cover art, no description, or no chapters yet?
- What happens when a visitor follows a link to a book that has since been unpublished?
- What happens when two different authors have written books with the same title?
- What happens when a title or description is far longer than the space for it?
- What happens when the connection drops midway through loading a page of books?
- How does the system order and match a title that is not in Ukrainian, such as a
  translated work listed under its original name?
- How does a screen reader user learn where they are after moving to another page?
- What happens when a visitor asks for a page number beyond the last page, or below the
  first?
- What does a visitor see for a book published after the catalogue pages were last
  generated?
- Are search result pages, whose content depends on what was typed, treated differently
  from catalogue pages for indexing?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST present a browsable list of published books, each showing its
  title, its author, and its cover art.
- **FR-002**: System MUST make every catalogue page fully operable with a screen reader
  alone, including reaching every book and identifying it by title and author.
- **FR-003**: System MUST make every catalogue page fully operable with the keyboard
  alone, with a visible focus indicator and no focus traps.
- **FR-004**: System MUST divide the catalogue, and any set of search results, into
  numbered pages of a fixed size, each reachable by its own address, and MUST state which
  page the visitor is on, how many pages there are, and which books the page covers.
- **FR-005**: System MUST present the catalogue in a stable default order, so that moving
  through the pages never repeats or skips a book.
- **FR-006**: System MUST substitute a placeholder for missing cover art without changing
  how the entry is announced.
- **FR-007**: System MUST show an explanatory message, on a page that remains navigable,
  when the catalogue or a set of results is empty.
- **FR-008**: System MUST provide a detail view for each published book showing its title,
  author, cover, description, total running time, and its chapters in order with each
  chapter's title and running time.
- **FR-009**: System MUST state on the detail view that listening requires an account.
- **FR-010**: System MUST NOT play, offer, or disclose the location of any audio file
  anywhere in the catalogue.
- **FR-011**: System MUST return a clearly worded not-found response, carrying a route
  back to the catalogue, for a book that does not exist or is no longer published.
- **FR-012**: System MUST deliver every catalogue page and book page as a complete
  document that already carries the book's title, author, and description, so that a
  search engine, or a visitor whose device has not finished building the page, still
  receives the content.
- **FR-013**: Users MUST be able to find books by matching text against book titles and
  author names.
- **FR-014**: System MUST announce the number of results whenever the displayed set of
  books changes.
- **FR-015**: System MUST present its interface in Ukrainian, and MUST order titles and
  match search terms under Ukrainian collation rules, so that Cyrillic titles sort in the
  order a Ukrainian reader expects and matching ignores letter case.
- **FR-016**: System MUST NOT require an account to view any catalogue metadata.
- **FR-017**: System MUST convey every state change that a sighted visitor would notice,
  including loading, errors, and result counts, to a screen reader user.
- **FR-018**: System MUST declare the language of every page, and of any passage written in
  a different language, so that a screen reader pronounces each with the correct voice.
- **FR-019**: System MUST move focus to the heading of the results whenever the displayed
  page changes, and MUST announce the new position, so that a screen reader user is not
  returned to the top of the page furniture.

### Key Entities

- **Book**: A published audiobook. Carries a title, a description, cover art, a
  publication state that decides whether it is listed, and a total running time. Credited
  to one or more authors and divided into an ordered set of chapters.
- **Author**: A person credited for one or more books. Carries a name used for both
  display and searching.
- **Chapter**: An ordered division of one book. Carries a title, its position in the book,
  and its running time. It belongs to exactly one book.
- **Cover art**: An image representing a book. Optional, and absent for some books.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A screen reader user reaches a named book's chapter list from the catalogue
  without sighted assistance in 100% of test runs, on both of the screen readers the
  product targets.
- **SC-002**: An accessibility audit of the catalogue list and the book detail page
  reports zero critical and zero serious violations.
- **SC-003**: Every interactive element on catalogue pages carries an accessible name that
  identifies what it does and to which book, verified at 100% coverage.
- **SC-004**: A keyboard-only visitor completes the journey from catalogue to book detail
  and back with no focus traps, in 100% of test runs.
- **SC-005**: A visitor who knows a book by name reaches its detail page within 30 seconds
  of arriving at the site.
- **SC-006**: The catalogue list becomes readable within 3 seconds on a typical mobile
  connection.
- **SC-007**: A catalogue page for a published book appears in public search engine results
  for its title and author within 30 days of publication.
- **SC-008**: No audio file is reachable from any catalogue page without an account, across
  every page the catalogue serves.

## Assumptions

- Books, authors, and chapters already exist in the system. Adding, editing, and
  unpublishing them is an administrative capability specified separately and out of scope
  here.
- Catalogue content is held in a persistent store, populated from a seed dataset kept under
  version control, so that a fresh environment and the automated tests start from identical
  and known content.
- Catalogue and book pages are generated when content is published rather than on every
  request, so a newly published book becomes visible after the next generation rather than
  instantly. The acceptable delay is settled by the administrative feature that publishes
  books.
- Search result pages depend on what the visitor typed and are not required to be
  indexable. Only catalogue pages and book pages carry that requirement.
- User accounts do not exist yet, so every visitor is anonymous. The detail page states
  that listening needs an account without linking to a sign-up flow that has not been
  built.
- Audio playback, streaming, and download are entirely out of scope. This feature never
  plays audio nor reveals where audio lives.
- A landing or marketing page is out of scope. The catalogue is reached directly.
- Only books in a published state are listed or reachable. Unpublished books behave as if
  they do not exist.
- The default catalogue order is alphabetical by title under Ukrainian collation rules,
  chosen because a predictable order lets a returning screen reader user find their place
  again.
- Search matches titles and author names only. Searching descriptions, and any semantic or
  recommendation-driven search, is deferred to a later feature.
- The interface is Ukrainian only. A second interface language is out of scope, and
  translation machinery is deliberately not built until a second locale is actually
  required. The stored catalogue's sort order is fixed to Ukrainian rules when it is
  created, so this decision is expensive to reverse later.
- Book metadata is predominantly Ukrainian. A title in another language is stored and
  displayed as written, and no translated copy of it is held.
- The screen readers the product targets are VoiceOver on iOS and TalkBack on Android, as
  recorded in the architecture document.
- Visitors may be on slow mobile connections and older devices, so pages stay light.
- Cover art is decorative in the list, because the entry is already named by its title and
  author.
