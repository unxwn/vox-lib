# Feature Specification: Site Shell

**Feature Branch**: `feature/site-shell`

**Created**: 2026-09-11

**Status**: Draft

**Input**: User description: "
The site shell, the routes that hold it together, a search anyone can find, and real cover art.

The site works, passes its accessibility checks and now has a visual identity, but it has no
structure a person can move around in. Every way to get anywhere is a link in the body of a page,
usually below the content. There is no header, no footer and no breadcrumbs. The library's name
appears nowhere a person can read it: the wordmark repeats across the ground as a watermark that is
correctly hidden from assistive technology and correctly unmistakable for content, so the name is
rendered everywhere and readable as a name nowhere. Someone who lands on a book page from a search
engine, or opens the sign in page from a confirmation message, sees a heading and a form and nothing
telling them whose site is asking for their password. Three routes have no way back to the catalogue
at all: forgot password, reset password, and search except in the branch that renders when the
search found nothing. A screen reader's landmark list on every page holds exactly two entries, a
navigation called Обліковий запис and main, with no banner and no contentinfo, so for an audience
that navigates by landmark before anything else the page has no skeleton. Authors are plain text, so
a catalogue that credits Shevchenko on three books and Kobylianska on two cannot be browsed by
author.

Persistent structure. A header banner landmark on every page, holding the wordmark as a link home,
the search field, and the account controls. A footer contentinfo landmark on every page, saying what
vox-lib is and who runs it, linking the about page, and carrying the SIL Open Font License for Fixel
that already ships at frontend/public/fonts/OFL.txt and is currently linked from nowhere.
Breadcrumbs on every page below the top level, marking the current page as current and naming the
trail that leads to it. No burger menu: three or four destinations need a bar that wraps at 320
pixels rather than a disclosure, and a burger is added when the navigation outgrows the bar rather
than in anticipation of it.

Search, rethought. Today it is a sentence in the middle of the catalogue page, a text link reading
Шукати книжку за назвою або автором, leading to the one page on the site that holds a search field.
From a book, from an account screen, from the not found page, there is no route to search at all.
The field itself moves into the header and appears on every page. It keeps the form it already has
and the reasoning behind that form: a real form submitting by GET, so that pressing Enter runs the
search with no pointing device involved and the result gets an address that can be shared,
bookmarked and returned to with the back button; a real visible label rather than a placeholder
standing in for one, because a placeholder vanishes as soon as anything is typed and is not reliably
announced; a submit button for anyone who expects one, which is not the only way to search; and the
same hundred character bound the API already enforces, so a visitor is stopped at the field rather
than by an error page.

What is new is that the field says what there is to search for. Its placeholder carries a real
example drawn from the catalogue itself, in the form напр. Кобзар or напр. Леся Українка, so the
first thing a visitor meets is evidence of what is actually in the library rather than an empty box.
That example changes, and how it is allowed to change is part of the requirement rather than left to
whoever implements it: it changes only while the field is untouched, it freezes the moment the field
takes focus and does not change again for the rest of that visit, it freezes when the visitor has
asked their system for reduced motion, and it is never the only thing saying what the field is for,
because the label does that. Text that moves inside a control is otherwise precisely the kind of
thing that makes a page hostile to read, and the placeholder is supplementary throughout.

Underneath the field sit a small number of search suggestions as real links, each one running the
search it names. This is the half of the idea a keyboard and a screen reader can actually use: a
link can be reached, announced and activated, where a rotating placeholder can only be looked at. It
is also what helps most on the search page before anything has been typed, and on the not found
page, where someone has arrived somewhere that does not exist and a way to look for what they wanted
is worth more than a way back.

The examples and the suggestions come from one place, a small resource the API serves rather than a
list hardcoded in the frontend, and its content is derived from the catalogue so that it stays true
as books are added and needs no maintenance. Serving it as data is also what makes it editable
later: when an administrative feature exists, changing what the field suggests is editing rows that
already exist rather than inventing a new concept. This feature does not build an administrative
screen, and the specification should record that as the intended path rather than a promise.
Suggesting results while someone types is deliberately not part of this either: a combobox that
announces changing results as the visitor types is among the hardest patterns in the accessibility
specification to get right, it needs its own endpoint and its own latency budget, and at a catalogue
of this size it buys very little. It earns its place when the catalogue is large enough that paging
to a title is genuinely slow.

Addresses. The roadmap promises a public landing page and a catalogue as two things, and today the
catalogue is standing in for both. The landing page takes the root address: what vox-lib is, who it
is for, that it is free, why an account is needed, and a way into the catalogue. The catalogue moves
to /books, its pagination to /books/page/:n, and the old /page/:n answers with a permanent redirect
to its new address. Book pages keep the addresses they have at /books/:slug, which is the reason the
catalogue is moving under that prefix rather than to a new word: the hierarchy then matches the
breadcrumb trail instead of contradicting it. Search stays at /search. Authors gain /authors and
/authors/:slug. The about page is a new address. The prerender list, which reads the real API to
discover every catalogue page and every book, follows the move, and so does the one helper that
currently encodes the rule that page one of the catalogue is the root address.

Browsing by author. An author's name becomes a link wherever it is shown, and an author's page lists
the books they are credited on. For a library this is a primary axis of browsing and its absence is
felt long before recommendations or a player would be. The catalogue has an authors table behind it
and no endpoint that exposes one, so this adds a resource rather than a query parameter.

Cover art, and where it lives. Covers are stored in S3 compatible object storage rather than in the
repository. Cloudflare R2 in production, which is the provider the architecture document already
chose for audio, reached through an interface in VoxLib.Model with the implementation in
VoxLib.Platform so that changing provider stays a configuration change. MinIO in the compose file for
development, standing in for R2 the way Mailpit stands in for SMTP, so that the dev container, the
test suite and the accessibility suite need no cloud credentials and no network. Covers are public
catalogue metadata under Principle IV, so they are served public read and are never signed: a cover
has to be cacheable, indexable, and usable as a preview when a link is shared. Covers are stored at
a small number of fixed widths, prepared before upload rather than resized on request, and the
catalogue and book pages choose between them. No runtime image pipeline and no resizing service.

Two defects come with that. The two seeded books that point at /covers/boiarynia.svg and
/covers/eneida.svg render a broken image icon today, because that directory does not exist anywhere
in the repository, and on a navy ground a broken image is more conspicuous than the hatched
placeholder the other twenty four books get. Real covers exist for some books already and go in with
this feature; a book without one keeps the placeholder. Underneath that sits the mechanism that let
it go unnoticed and would hide the next one: the single page fallback answers any unknown path with
200 and an HTML document, so a request for a missing image returns a page the browser cannot decode,
and no status check, uptime monitor or smoke test can see that anything failed. A request for a
static asset that is not there must answer 404. Object storage settles it for covers, because a
missing object returns a real 404 from the bucket, and the fallback rule for the rest of the site
still has to tell an unknown route apart from a missing file.

Catalogue metadata gains four fields and no more. An author gains a slug, so that an author has an
address, and a sort name, so that Ukrainian names sort by surname under the collation the catalogue
already uses. A book gains its narrator and the date it entered the catalogue. The narrator is the
person whose voice is on the recording, the озвучувач or читець, and it is a name rather than
anything to do with a file: for recordings made in house that name is the library's own, and for
anything contributed it is whoever read it. It is the field that distinguishes an audiobook from a
book, it is how listeners choose between two recordings of the same title, and nothing in the model
carries it today. The date a book entered the catalogue cannot be backfilled honestly once books
exist, and it is what recently added browsing will need. Genre and tags, series and position in
series, translator, publication year, and a per book record of where a recording came from and on
what basis are all deliberately left to a later content feature.

Audio is not built in this feature, and the shape it will take is written down here so that the
feature which does build it inherits a decision rather than making one under time pressure. A
chapter carries an opaque storage key rather than a URL, so that nothing in any public catalogue
response is ever a location someone could fetch, which is what the chapter model already promises in
writing and what this preserves. At the moment a signed in listener asks to play, the API decides
whether they may and hands back a short lived signed URL, and object storage serves the bytes; the
API never proxies audio, because egress is the dominant cost of streaming and because "who may
listen" and "serving bytes" are different jobs. For the MVP that is one file per chapter answered
with HTTP Range requests and 206 partial content, which is what makes seeking work at all, with HLS
deferred until adaptive bitrate is genuinely needed for listeners on poor connections. The expiry is
what makes the gate real against sharing: a link pasted into a chat stops working, which is the
point. It is worth stating plainly in the specification that this is not copy protection, because
anyone who can listen can capture the file within the window, and the design is an honest limit on
casual redistribution rather than a lock. None of this is implemented here. This feature builds only
the public read cover storage, and the audio storage stays a separate interface rather than a
generalisation of the same one, because covers are public and cacheable while audio is signed and
must never be cached, and one interface serving both would have to be told which it is on every
call.

Three more things the shell has to carry. Session dependent chrome must reserve its own space: the
account controls render nothing while the session is being read and then insert two focusable links
at the top of the document, so a keyboard user who starts moving before that request lands has every
tab stop shift under them. The header is where that either gets solved or gets multiplied, and the
session hook currently holds state per call site with no shared cache, which is fine at one consumer
and stops being fine the moment the header is a second one. The palette has thirteen colour roles
and none of them is the colour of doing something: on the sign in screen the submit button, the
email field and the password field carry the same fill and the same edge, so the one thing a visitor
came to that page to press has no more visual weight than the boxes above it, and a primary action
role belongs in the token file with its contrast pair added to the matrix the token script already
recomputes. The single warm hue is reserved for the focus ring and is not spent twice. And the book
page ends by telling someone they need an account to listen while offering nothing to click, so it
links to registration, still with no play button, disabled or otherwise, for the reason the code
already records: a disabled control still appears in a screen reader's list of controls and invites
a listener to hunt for a way to enable it.

Cover art renders at 56 by 80 pixels in the catalogue today, which is a thumbnail rather than a
browse experience, and it is the strongest visual signal an audiobook library has. It is sized up
now that real covers exist to put in it.

Testing has a gap this feature cannot leave open. No browser test has ever run against a signed in
page, because getting a real session inside one would mean confirming an address from a mailbox, so
every axe run, every contrast measurement, every target size check and every focus assertion in the
suite walks the anonymous variant of the site. This feature puts session dependent chrome on every
page, so it also has to make a signed in session reachable from a test. Mailpit is already in the
compose file and has an HTTP API, and reading the confirmation link out of it is what makes the
signed in half of the product testable.

Lowest priority, and droppable without affecting anything above it: because every catalogue address
changes, the move should be legible from outside. A robots.txt and a sitemap covering the landing
page, the catalogue pages, the book pages and the author pages, and Open Graph and Twitter card
metadata on the landing, book and author pages, so that sharing a book into Telegram or Viber
produces a title, a summary and the cover rather than a bare address. Account routes and search
result pages keep the noindex they already carry.

Assumptions to record. The previous feature froze every route, every behaviour, every word and every
piece of content, and forbade adding a page, a control or a capability. That freeze was scoped to
the restyle and expires with it; this feature relaxes it deliberately, because a header introduces
copy and a footer introduces more. The interface language is Ukrainian and all new copy is
Ukrainian, while the wordmark stays in latin letters as the previous feature settled. The deep blue
ground remains the product's single theme. There is no audio and no playback in this feature, and
none of the new storage code touches audio.

Out of scope, named here so they are not lost, each shipping as its own small pull request rather
than waiting on this specification: running the accessibility and prerender suites on every pull
request, which is the largest structural gap in the repository and the thing that stops all of the
above from recurring, noting that the frontend job's name is the status check context the branch
protection ruleset matches, so it stays exactly as it is or the ruleset changes in the same pull
request; turning on the jsx-a11y plugin that oxlint already ships and that the configuration does not
enable; deleting the weather forecast endpoint, which is still mapped and has propagated into the
generated frontend API types; drawing a favicon from the identity and linking it, the file that
exists being the starter's purple logo referenced by nothing; correcting the README's backend layout,
which lists one project where there are now five; moving chapter running times next to the chapters
they belong to rather than roughly five hundred pixels away across an empty region; settling how
visible the watermark is and whether the ground's luminance band token caps the grain alone or the
watermark as well; the web app manifest and service worker that the platform decision's stated
benefit depends on; an administrative surface of any kind, including the screen that would edit the
search suggestions; suggesting results as the visitor types; an account area behind the session; and
playback.
"

## Clarifications

### Session 2026-09-11
- Q: On the search results page, does the header's search field serve as the only search input,
  or does that page keep a search form in its body as well? A: The header field only, on every
  page. On the search page it carries the current term, and the body of that page holds the
  results, with the suggestion links in place of them before anything has been typed. No page
  presents two search inputs and no page exposes two search landmarks.
- Q: How do cover images get into object storage, given that covers do not live in the
  repository but development must work with no network and no cloud credentials? A: The prepared
  images at their fixed widths are committed with the catalogue's seed data, and the same seeding
  step that creates the books uploads any the configured bucket does not already hold. A fresh
  container, a test run and a deployment all reach the same covers with no manual step; nothing
  is ever served from the repository.
- Q: How does an author get the slug that becomes their address, and what happens when two
  authors would produce the same one? A: Stored per author and unique, a latin transliteration
  of the Ukrainian name, assigned when the author is created and never changed by a later
  correction to the name, the way a book's slug already works. A collision is refused at
  creation so that a human chooses how to distinguish the two, rather than a number being
  appended silently.
- Q: Which rows of the catalogue become the search examples and suggestions, given that the
  resource has to follow the catalogue with nothing edited by hand? A: A random sample of titles
  and author names, drawn fresh on each request, so a returning visitor is not shown the same
  handful forever. The consequence accepted with it is that tests assert the sample's shape
  rather than its content.
- Q: What date do the books already in the catalogue get for the date they entered it, given
  that it cannot be backfilled honestly? A: The date the migration that adds the field runs. The
  field is therefore required rather than optional, and the consequence accepted with it is that
  every book present today shares one arrival date, so the first recently added list built on it
  will show all of them together.

  Overtaken by FR-064: the books present when this was asked are placeholders and are being
  removed, so the backfill applies to rows that do not survive the feature. The real books are
  inserted by the seeder and take the date they are inserted. The accepted consequence is
  unchanged in substance, because all four arrive at once, but it now follows from seeding
  rather than from a migration having nothing honest to write.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Every page has a skeleton (Priority: P1)

A visitor lands anywhere on the site, from a search engine, from a link in a confirmation
message, or by typing an address. Whatever page they land on, the top of it carries the
library's name as readable text that takes them home, the means to search, and the controls
for their account. The bottom of it says what vox-lib is, who runs it, and links the about
page and the font licence. If the page sits below the top level, a trail above the content
names the pages that lead to it and marks the one they are on. A visitor navigating by
landmark finds a banner, a main region, a contentinfo region and named navigation regions
rather than the two entries the site offers today.

**Why this priority**: It is the feature. Without it every route is an island reachable only
through a link buried in the body of another page, three routes have no way back to the
catalogue at all, and the audience that navigates by landmark before anything else gets a page
with no skeleton. Nothing else in this specification is worth much on a site a person cannot
move around in.

**Independent Test**: Open every page the site serves, with a keyboard and with a screen
reader, and confirm each one presents the header, the footer, the landmark set, and, below the
top level, a breadcrumb trail whose last item is marked as the current page.

**Acceptance Scenarios**:

1. **Given** a visitor opens any page on the site, **When** it renders, **Then** a header
   region holds the library's name as a link home, the search field and the account controls,
   and a footer region says what vox-lib is, who runs it, and links the about page and the
   font licence.
2. **Given** a visitor using a screen reader lists the landmarks on any page, **When** they
   read that list, **Then** it holds a banner, a main region, a contentinfo region, and every
   navigation region in it carries a name that distinguishes it from the others.
3. **Given** a visitor opens a book page, an author page, a numbered catalogue page, the
   search page, the about page or any account screen, **When** it renders, **Then** a
   breadcrumb trail names the pages that lead to it and marks the current page as current.
4. **Given** a visitor lands on the forgot password, reset password or search page, **When**
   they look for a way to the catalogue, **Then** the header offers one from the page itself
   rather than from the body of another page.
5. **Given** a keyboard user starts moving through a page before the session has been read,
   **When** the session resolves and the account controls appear, **Then** no tab stop they
   have already passed moves and no element shifts under them.
6. **Given** a keyboard user arrives on any page, **When** they press Tab once, **Then** they
   are offered a way to skip the header and reach the page's content directly.
7. **Given** a viewport 320 device-independent pixels wide, **When** the header renders,
   **Then** its destinations wrap onto further lines rather than collapsing behind a
   disclosure, and nothing on the page scrolls horizontally.
8. **Given** the library's name in the header, **When** a screen reader reads the page,
   **Then** the name is announced once as a link, and the decorative watermark behind the page
   is still announced nowhere.

---

### User Story 2 - A front door, and addresses that match the hierarchy (Priority: P2)

Someone who has been told about vox-lib opens its address and finds a page that explains what
the library is, who it is for, that it is free, and why an account is needed, with a way into
the catalogue. The catalogue itself lives at an address that says it is the books, its
numbered pages sit beneath that address, and a book sits beneath the catalogue, so the trail
above a book matches the address in the location bar. Anyone who bookmarked a numbered
catalogue page before this feature is sent to its new address permanently rather than to a
page that no longer exists.

**Why this priority**: The root address currently promises a landing page and serves a
catalogue, so a first time visitor is dropped into a list of books with nothing telling them
what the site is or why it would ask them to register. The move also has to happen before the
breadcrumbs can tell the truth, which is why it sits directly behind the shell rather than
after it.

**Independent Test**: Open the root address and confirm it explains the product and offers a
way into the catalogue; then walk the catalogue, its numbered pages, a book, the search page,
the about page and the author pages, confirming each address matches its place in the trail;
then request every old catalogue address and confirm each answers a permanent redirect.

**Acceptance Scenarios**:

1. **Given** a first time visitor opens the site's root address, **When** the page renders,
   **Then** it says what vox-lib is, who it is for, that it is free and why an account is
   needed, and offers a way into the catalogue.
2. **Given** a visitor opens the catalogue, **When** they look at the address, **Then** the
   catalogue has its own address under the books prefix and its numbered pages sit beneath it.
3. **Given** a visitor follows a link to a book, **When** the page renders, **Then** its
   address is unchanged from before this feature and the breadcrumb trail above it matches
   that address.
4. **Given** a bookmark or an external link to a numbered catalogue page at its old address,
   **When** it is requested, **Then** it answers with a permanent redirect to the new address
   and the visitor arrives at the same page of the catalogue.
5. **Given** the catalogue's first page, **When** a visitor reaches it from the landing page,
   from a breadcrumb, from pagination or from the header, **Then** every one of those routes
   uses the same single address for it.
6. **Given** the set of documents the site publishes for search engines and for devices that
   have not finished running scripts, **When** the site is built, **Then** it holds a document
   for the landing page, for every catalogue page, for every book and for every author, each
   at its new address.

---

### User Story 3 - Search from anywhere, with an example of what to search for (Priority: P3)

A visitor on a book page, an account screen or an address that matches nothing wants to look
for something. The search field is in the header where they already are. Before they type, the
field shows a real example drawn from the catalogue, so the first thing they meet is evidence
of what the library actually holds rather than an empty box. Below the field, on the pages
where it helps most, a few suggestions sit as ordinary links that run the search they name, so
a keyboard and a screen reader can use the same help the example offers to the eye.

**Why this priority**: Today search is one text link in the middle of the catalogue page, and
from everywhere else on the site there is no route to it at all. Moving the field into the
header is what turns search from a page into a capability. It follows the addresses because
the header that carries it is the thing being built in P1.

**Independent Test**: From every page on the site, run a search using only the keyboard,
confirm the result has an address that can be shared and returned to, and confirm the example
in the field and the suggestions beneath it both come from the catalogue rather than from a
list written into the page.

**Acceptance Scenarios**:

1. **Given** a visitor on any page, **When** they type a term into the header field and press
   Enter, **Then** the search runs with no pointing device involved and the result has an
   address they can share, bookmark and return to with the back button.
2. **Given** a visitor using a screen reader, **When** they reach the search field, **Then** a
   visible label announces what it is for, and the example inside the field is supplementary
   rather than the only thing describing it.
3. **Given** an untouched search field, **When** a visitor watches it, **Then** the example
   inside it changes between real examples drawn from the catalogue.
4. **Given** a visitor puts focus in the field, **When** they do so, **Then** the example
   freezes and does not change again for the rest of that visit.
5. **Given** a visitor whose system asks for reduced motion, **When** any page renders,
   **Then** the example never changes at all.
6. **Given** a visitor types more characters than the catalogue accepts, **When** they do so,
   **Then** they are stopped at the field rather than by an error page.
7. **Given** a visitor on the search page before typing anything, or on a page for an address
   that matches nothing, **When** the page renders, **Then** a small number of suggestions are
   offered as links, each of which runs the search it names when activated by keyboard.
8. **Given** a book is added to or removed from the catalogue, **When** the examples and the
   suggestions are next served, **Then** they reflect the catalogue with nothing edited by
   hand.
9. **Given** a visitor on the search page with a term already searched, **When** the page
   renders, **Then** the header field carries that term and is the only search input on the
   page.

---

### User Story 4 - Browse by author (Priority: P4)

A visitor finishes a book and wants to know what else the person behind it has here. The name on
the book page is a link, and it says how they are credited. It leads to a page for that person
listing every book they are credited on with the role they hold on each, and an index lists
everyone credited, ordered by surname the way a Ukrainian reader expects.

**Why this priority**: For a library this is a primary axis of browsing, and its absence is
felt long before recommendations or a player would be. It sits behind the shell and the
addresses because an author page is one more destination that needs the trail and the header
those stories build.

**Independent Test**: Open a book credited to an author who appears on more than one book,
follow their name, and confirm the page lists every book they are credited on and no book they
are not.

**Acceptance Scenarios**:

1. **Given** a book page, **When** it renders, **Then** every author credited on it is a link
   to that author's page.
2. **Given** an author credited on several books, **When** their page renders, **Then** it
   lists every published book they are credited on and offers a link to each.
3. **Given** the index of authors, **When** it renders, **Then** every author in the catalogue
   is listed, ordered by surname under the collation the catalogue already uses.
4. **Given** an author's page, **When** a visitor looks at its address and its trail, **Then**
   the address names the author and the trail leads back through the index of authors.
5. **Given** an address for an author who does not exist, **When** it is requested, **Then**
   the site says so on a page that offers a way to search rather than failing silently.

---

### User Story 5 - Real covers, and a missing file that says it is missing (Priority: P5)

A visitor opens the catalogue and sees cover art large enough to recognise a book by, drawn
from real covers rather than from files that were never there. Two books that show a broken
image icon today show their cover instead. Books with no cover keep the placeholder, which is
a deliberate design rather than a failure. Behind that, a request for a file the site does not
have answers as missing rather than returning a page, so the next absent file is visible to a
status check instead of hiding until someone notices a broken icon.

**Why this priority**: A broken image on a navy ground is more conspicuous than the placeholder
the other books get, and cover art is the strongest visual signal an audiobook library has. The
mechanism underneath matters more than any one cover: while any unknown path answers with a
page, no monitor or smoke test can see that a file is missing at all.

**Independent Test**: Open the catalogue and every book page and confirm no broken image
anywhere; then request a file that does not exist and confirm the response says it is missing
rather than returning a page.

**Acceptance Scenarios**:

1. **Given** the catalogue, **When** it renders, **Then** every entry shows either a real cover
   or the placeholder, and no entry shows a broken image.
2. **Given** a book whose cover is prepared, **When** its page renders, **Then** it shows that
   cover at the proportions of the source image rather than cropped to a fixed shape.
3. **Given** a book with no cover, **When** its page and its catalogue entry render, **Then**
   the placeholder is shown and the entry is announced exactly as it is today.
4. **Given** a request for a static file the site does not have, **When** it is made, **Then**
   the response says the file is missing rather than returning a page with a success status.
5. **Given** a request for an address that matches no route, **When** it is made, **Then** the
   visitor still gets the page that says so, with the header, the footer and a way to search.
6. **Given** a cover is requested, **When** the response arrives, **Then** it can be cached and
   shared, needs no signature and no account, and is the same for everyone.
7. **Given** the catalogue and a book page, **When** each renders a cover, **Then** it uses one
   of a small number of prepared sizes rather than asking for an image to be resized on request.
8. **Given** the dev container with no cloud credentials and no network access, **When** the
   site, its endpoint tests and its browser tests run, **Then** covers are served and every one
   of those suites passes.

---

### User Story 6 - The next step is the obvious one (Priority: P6)

A visitor reaches the sign in screen and the one thing they came to press looks like the thing
to press rather than like the two boxes above it. A visitor reading a book page they cannot
listen to yet is told they need an account and is given the link that creates one, instead of
being told what they lack and offered nothing to do about it.

**Why this priority**: These are small, but they are where the shell either converts a visitor
or loses one, and both are one step away from work this feature is doing anyway. They sit
behind the structural stories because none of them is a way to get anywhere; they make the way
that exists worth taking.

**Independent Test**: Open every form on the site and confirm its primary action is
distinguishable from the fields around it by more than its label; open a book page while
signed out and confirm the account message carries a link to registration.

**Acceptance Scenarios**:

1. **Given** any form on the site, **When** it renders, **Then** the control that submits it
   carries a visual treatment no other control on that form uses.
2. **Given** the colour that marks the primary action, **When** it is added, **Then** it is
   defined once in the same shared place as every other colour role, with its contrast pair
   recorded alongside the others.
3. **Given** the single warm hue reserved for the focus indicator, **When** the primary action
   colour is chosen, **Then** the focus indicator keeps that hue to itself and stays distinct
   from the primary action everywhere the two appear together.
4. **Given** a signed out visitor on a book page, **When** they reach the message saying an
   account is needed to listen, **Then** it links to registration.
5. **Given** any page in this feature, **When** a screen reader lists the controls on it,
   **Then** no disabled play control appears anywhere.

---

### User Story 7 - The signed in half of the site can be checked (Priority: P7)

Someone maintaining the site runs the browser suite. It registers an account, reads the
confirmation link out of the development mailbox, signs in, and then audits the pages that
carry session dependent chrome as a signed in visitor, not only as an anonymous one.

**Why this priority**: No browser test has ever run against a signed in page, so every
accessibility audit, contrast measurement, target size check and focus assertion in the suite
walks the anonymous variant of the site. This feature puts session dependent chrome on every
page, which doubles the number of pages nothing checks. It is last of the structural stories
because it delivers nothing a visitor sees, and it cannot be dropped because everything above
it is only half tested without it.

**Independent Test**: Run the browser suite with no manual step and confirm it reaches a signed
in session and audits at least one page in that state.

**Acceptance Scenarios**:

1. **Given** the browser suite runs in the dev container, **When** it needs a signed in
   session, **Then** it registers an account, retrieves the confirmation link from the
   development mailbox and signs in, with no human involvement.
2. **Given** a signed in session, **When** the suite audits a page carrying session dependent
   chrome, **Then** it reports the same zero critical and zero serious violations required of
   the anonymous variant.
3. **Given** the suite is run twice in a row, **When** the second run starts, **Then** it
   succeeds without anyone clearing state by hand.

---

### User Story 8 - The move is legible from outside (Priority: P8)

Someone shares a book into a chat and the message shows the title, a summary and the cover
rather than a bare address. A search engine reading the site finds a list of the addresses that
changed, and the account screens and search results stay out of the index as they already do.

**Why this priority**: Lowest, and droppable without affecting anything above it. Every
catalogue address changes in this feature, so telling the outside world about the move is worth
doing at the same time, but nothing in the product depends on it.

**Independent Test**: Request the crawler files and confirm they list the landing page, the
catalogue pages, the book pages and the author pages; paste a book address into a preview and
confirm a title, a summary and a cover appear.

**Acceptance Scenarios**:

1. **Given** a crawler reads the site, **When** it requests the file that lists the addresses,
   **Then** it finds the landing page, every catalogue page, every book page and every author
   page at their new addresses.
2. **Given** a book, an author or the landing page is shared into a messaging application,
   **When** the preview is produced, **Then** it shows a title, a summary and, where one
   exists, the cover.
3. **Given** an account screen or a search result page, **When** a crawler reads it, **Then**
   it is still marked not to be indexed.

---

### Edge Cases

- What happens on the page for an address that matches nothing, which has no place in the
  hierarchy? It carries the header and the footer like every other page, shows no breadcrumb
  trail because there is no trail that leads to it, and offers search suggestions instead.
- What happens to the breadcrumb on the landing page? There is none. It is the top level and
  a trail of one entry pointing at itself is noise.
- What happens when a visitor reaches a numbered catalogue page beyond the last one? It behaves
  as it does today, at its new address, and the redirect from the old address preserves that
  behaviour rather than changing it.
- What happens to a search term already in the address when a visitor moves to another page?
  The header field carries the current term on the search page and is empty everywhere else,
  so nothing silently re-runs a search a visitor has left behind.
- What happens when the resource that supplies the examples and the suggestions cannot be
  read? The field still works, with its label, its button and a neutral placeholder, and no
  suggestions are shown. Search is never blocked by the thing that decorates it.
- What happens to the examples and the suggestions inside a document published for a crawler?
  They are whichever sample was drawn when that document was produced, and the page replaces
  them with a fresh one as soon as it runs. Nothing depends on which sample a visitor sees.
- What happens when the catalogue is empty, so there is no example to draw? The field falls
  back to the same neutral placeholder and shows no suggestions.
- What happens to an author credited on a book that is not published? They are not listed for
  it anywhere, on their own page or in the index, exactly as the catalogue already hides it.
- What happens to an author with no published books at all? They do not appear in the index,
  and their address behaves as an address that matches nothing.
- What happens when a new author's name transliterates to a slug an existing author already
  holds? The author is not created, and whoever is adding them chooses how the two are told
  apart, so no address is ever assigned by a rule nobody chose.
- What happens when two authors sort identically? Both appear; the order between them is
  stable rather than arbitrary from one page load to the next.
- What happens to a very long author name, book title or breadcrumb trail at 320 device
  independent pixels? Everything wraps, nothing is clipped, nothing is hidden from a screen
  reader, and the page still does not scroll horizontally.
- What happens when a cover is stored at widths that do not match the space it renders in? The
  nearest prepared size at or above that space is used, and no request asks for an image to be
  resized.
- What happens when object storage cannot be reached at all? Pages still render with the
  placeholder in place of every cover, and nothing in the catalogue fails.
- What happens to the account controls in the header while the session is still being read?
  They occupy the space they will occupy once it resolves, so nothing moves when it does.
- What happens when several parts of a page need to know whether a visitor is signed in? The
  session is read once and shared, not once per part.
- What happens to a page shared before this feature whose address is now a redirect? The
  redirect is permanent, so a crawler and a browser both learn the new address rather than
  following the old one forever.

## Requirements *(mandatory)*

### Functional Requirements

Persistent structure.

- **FR-001**: System MUST present a header on every page the site serves, exposed as a banner
  landmark, holding the library's name as a link to the landing page, the search field, and the
  account controls.
- **FR-002**: System MUST render the library's name in that header as readable text, announced
  once per page, distinct from the decorative watermark, which stays unannounced.
- **FR-003**: System MUST present a footer on every page, exposed as a contentinfo landmark,
  saying what vox-lib is and who runs it, linking the about page, and linking the SIL Open Font
  License text that ships with the site's typeface.
- **FR-004**: System MUST present a breadcrumb trail on every page below the top level, naming
  each page that leads to the current one as a link, naming the current page, and marking the
  current page as current to assistive technology.
- **FR-005**: System MUST expose, on every page, a banner landmark, a main landmark, a
  contentinfo landmark, and a distinct accessible name on every navigation and search landmark,
  so that no two landmarks on a page are indistinguishable in a landmark list.
- **FR-006**: System MUST present the header's destinations as a bar that wraps onto further
  lines down to a 320 device-independent pixel viewport, and MUST NOT place them behind a
  disclosure control.
- **FR-007**: System MUST offer, as the first focusable element on every page, a way to skip
  the header and move focus to the page's main content.
- **FR-008**: System MUST reserve the space the account controls will occupy while the session
  is being read, so that neither the layout nor the focus order changes when it resolves.
- **FR-009**: System MUST read a visitor's session once per page view however many parts of the
  page depend on it, and MUST share that single answer between them.
- **FR-010**: System MUST keep every part of the header and the footer reachable and operable
  by keyboard, in the order they are read.

Search.

- **FR-011**: System MUST present the search field in the header on every page, and MUST NOT
  present a second search input anywhere on a page that has the header.
- **FR-012**: System MUST run a search as a form submitted by GET, so that pressing Enter in the
  field runs it with no pointing device involved and the result has an address that can be
  shared, bookmarked and returned to with the back button.
- **FR-013**: System MUST give the search field a visible label, and MUST NOT use its
  placeholder in place of that label.
- **FR-014**: System MUST offer a submit control for anyone who expects one, while keeping it
  from being the only way to run a search.
- **FR-015**: System MUST stop a term longer than one hundred characters at the field, matching
  the bound the catalogue already enforces, rather than letting it fail on a later page.
- **FR-016**: System MUST carry the current search term in the header field on the search
  results page, and MUST leave the field empty on every other page.
- **FR-017**: System MUST show, in the untouched field, a placeholder carrying a real example
  drawn from the catalogue, presented as an example rather than as an instruction, in the form
  "напр. Кобзар" or "напр. Леся Українка".
- **FR-018**: System MUST change that example only while the field is untouched, MUST freeze it
  the moment the field takes focus and MUST NOT change it again for the rest of that visit, and
  MUST NOT change it at all when the visitor has asked their system for reduced motion.
- **FR-019**: System MUST NOT let the example be the only thing that says what the field is for,
  and MUST keep the field fully usable when no example can be supplied.
- **FR-020**: System MUST offer a small number of search suggestions as ordinary links, each
  running the search it names, each reachable, announced and activatable by keyboard and by
  screen reader.
- **FR-021**: System MUST offer those suggestions at least on the search page before anything
  has been typed and on the page for an address that matches nothing.
- **FR-022**: System MUST derive the examples and the suggestions from one resource the API
  serves, drawing a fresh random sample of titles and author names from the published catalogue
  on each request, so that neither is a list written into the frontend, neither needs
  maintenance when books are added or removed, and a returning visitor is not shown the same
  handful forever.
- **FR-023**: System MUST draw every example and every suggestion from a published book's title
  or from the name of an author credited on one, so that activating a suggestion always returns
  at least one result.
- **FR-024**: System MUST NOT suggest results while a visitor is typing.

Addresses.

- **FR-025**: System MUST serve a landing page at the site's root address, saying what vox-lib
  is, who it is for, that it is free, and why an account is needed, and offering a way into the
  catalogue.
- **FR-026**: System MUST serve the catalogue at an address under the books prefix, with its
  numbered pages beneath that address.
- **FR-027**: System MUST answer every old numbered catalogue address with a permanent redirect
  to its new address.
- **FR-028**: System MUST keep every book page at the address it has today.
- **FR-029**: System MUST keep the search results page at the address it has today, and MUST add
  addresses for the index of authors, for each author, and for the about page.
- **FR-030**: System MUST give the catalogue's first page exactly one address, and MUST use that
  address in every link that leads to it, including the landing page, the header, the
  breadcrumbs and pagination.
- **FR-031**: System MUST make every breadcrumb trail match the hierarchy of the address it
  describes.
- **FR-032**: System MUST publish a standalone document, readable without running scripts, for
  the landing page, for every catalogue page, for every book and for every author, each at its
  new address, discovering what exists by reading the catalogue rather than from a fixed list.

Browsing by author.

- **FR-033**: System MUST render every credited name as a link to that person's page wherever
  the name is shown, and MUST name the role they are credited in.
- **FR-034**: System MUST serve a page per credited person listing every published book they are
  credited on, with a link to each and the role they hold on it.
- **FR-035**: System MUST serve an index of everyone credited on a published book, whatever
  their role, ordered by sort name under the collation the catalogue already uses.
- **FR-036**: System MUST expose authors as a resource of their own rather than as a filter on
  the books resource.
- **FR-037**: System MUST answer an address for an author that does not exist, or who has no
  published books, with the page that says the address matches nothing.

Catalogue metadata.

- **FR-038**: System MUST give each author a stored slug, unique across authors, being a latin
  transliteration of their name, assigned when the author is created and left unchanged by any
  later correction to that name, and MUST refuse to create an author whose slug already exists
  rather than distinguishing the two automatically.
- **FR-039**: System MUST give each author a sort name, so that Ukrainian names order by
  surname.
- **FR-040**: System MUST give each book an optional narrator, being the name of the person
  whose voice is on the recording, and MUST show it on the book page when one is recorded.
- **FR-041**: System MUST record, for every book, the date it entered the catalogue. Every book
  created from now on records the date it was created; every book already in the catalogue
  records the date the field was added to it, so the value is always present rather than
  optional.
- **FR-042**: System MUST NOT add any other field to a book or an author in this feature, beyond
  the credit role FR-066 places on the link between them.

Cover art.

- **FR-043**: System MUST serve cover art from object storage rather than from the site's own
  static files, and MUST make changing the storage provider a matter of configuration rather
  than of changing how the catalogue behaves.
- **FR-044**: System MUST serve covers publicly, without a signature and without an account, so
  that a cover is cacheable, indexable and usable as a preview when a link is shared.
- **FR-045**: System MUST store each cover at a small number of fixed widths, prepared before
  upload, and MUST let the catalogue and the book page choose between them without asking for an
  image to be resized on request.
- **FR-046**: System MUST show real cover art for every book whose cover is prepared, and MUST
  show the existing placeholder for a book that has none.
- **FR-047**: System MUST commit the prepared images alongside the catalogue's seed data and
  upload, as part of the same seeding step that creates the books, every image the configured
  storage does not already hold, so that a fresh container, a test run and a deployment all
  reach the same covers with no manual step and no upload repeated.
- **FR-048**: System MUST answer a request for a static file it does not have with a not found
  status, while continuing to answer an address that matches no route with the page that says
  so.
- **FR-049**: System MUST render cover art in the catalogue substantially larger than the 56 by
  80 device-independent pixel thumbnail it uses today, at no less than 120 device-independent
  pixels on its shorter side at a 1280 pixel viewport, while still fitting without horizontal
  scrolling at 320.
- **FR-050**: System MUST keep the site, its endpoint tests and its browser tests working in the
  dev container with no cloud credentials and no network access.
- **FR-051**: System MUST keep every page rendering, with the placeholder in place of covers,
  when object storage cannot be reached.

The obvious action.

- **FR-052**: System MUST define a colour role for the primary action, once, in the same shared
  place as every other colour role, with its contrast pair recorded in the same matrix as the
  others.
- **FR-053**: System MUST give the control that submits a form a visual treatment no other
  control on that form carries, so that the action a visitor came for is distinguishable from
  the fields above it by more than its label.
- **FR-054**: System MUST keep the single warm hue reserved for the focus indicator, so that the
  focus indicator stays distinct from the primary action wherever both appear.
- **FR-055**: System MUST link the book page's message about needing an account to listen to the
  registration screen, and MUST NOT present a play control, disabled or otherwise, anywhere.

Testing.

- **FR-056**: System MUST let the browser suite reach a signed in session with no manual step,
  by registering an account, retrieving the confirmation link from the development mailbox and
  signing in.
- **FR-057**: System MUST audit, in that signed in state, the pages that carry session dependent
  chrome, to the same zero critical and zero serious violations required of the anonymous
  variant.

Legibility from outside. Lowest priority, and droppable without affecting anything above.

- **FR-058**: System MUST serve a crawler policy file and a sitemap listing the landing page,
  every catalogue page, every book page and every author page at their new addresses.
- **FR-059**: System MUST supply sharing metadata, being a title, a summary and the cover where
  one exists, on the landing page, every book page and every author page.
- **FR-060**: System MUST keep the account screens and the search results page marked not to be
  indexed, as they already are.

Obligations carried forward.

- **FR-061**: System MUST hold every page it adds or changes to the accessibility obligations
  the site already meets: zero critical and zero serious automated violations, body text at
  4.5:1 and large text at 3:1 measured over the decorated ground, non-text elements that carry
  meaning at 3:1, a visible focus indicator on every interactive element, targets of at least 44
  by 44 device-independent pixels, no meaning carried by colour alone, and no horizontal
  scrolling of the page body down to 320 device-independent pixels or at 200% zoom.
- **FR-062**: System MUST write every new piece of interface copy in Ukrainian, and MUST keep
  the wordmark in latin letters as the previous feature settled.
- **FR-063**: System MUST take every visual decision it introduces from the shared place where
  visual decisions are named, adding a name there when one is missing rather than writing a
  literal value into a page or a component.

The catalogue's real content. Added after the plan was written, when the covers this feature
needed turned out to belong to a different catalogue: the twenty-seven books seeded today are
placeholders, and the books the project actually holds are recorded in
`backend/src/VoxLib.Dal/Seed/content.md`.

- **FR-064**: System MUST replace the placeholder catalogue with the books the project actually
  holds, and MUST remove the placeholders rather than leaving the two sets side by side.
- **FR-065**: System MUST seed each of those books with the chapter structure that is known,
  which today is one chapter carrying the book's whole running time. The real chapters arrive
  with the audio and replace it, so the running time a reader sees is right from the start and
  stays right afterwards without a second field to keep in step with the first.
- **FR-066**: System MUST record how each person is credited on each book, being an author or a
  compiler, on the link between the two rather than on the person, so that the same person can
  be an author on one book and a compiler on another.
- **FR-067**: System MUST render cover art at the proportions of the image rather than cropping
  it to a fixed shape, and MUST keep a fixed shape for the placeholder, which has no proportions
  of its own.
- **FR-068**: System MUST let the browser suite exercise paging against a catalogue large enough
  to page, because the real catalogue is smaller than one page and would otherwise quietly
  retire every paging assertion the site already holds.

### Key Entities

- **Landing page**: The site's front door at the root address. What vox-lib is, who it is for,
  that it is free, why an account is needed, and the way into the catalogue. It replaces the
  catalogue in that position rather than being added alongside it.
- **Header**: The banner every page carries. Holds the library's name as a link home, the search
  field and the account controls. Its account controls are the only part that depends on who is
  looking.
- **Footer**: The contentinfo every page carries. What vox-lib is, who runs it, the about page,
  and the typeface licence that ships with the site and is currently linked from nowhere.
- **Breadcrumb trail**: The ordered list of pages leading to the current one, with the current
  one marked as current. Exists on every page below the top level and matches the address.
- **Search suggestion**: One phrase that names a search worth running, drawn from the catalogue.
  The same phrase serves twice: as an example inside the untouched field, and as a link that
  runs the search it names. Served as data by the API, so that editing it later is editing rows
  rather than inventing a concept.
- **Author**: Someone credited on a book, gaining a stored, unique slug so that they have an
  address that survives a corrected spelling, and a sort name so that Ukrainian names order by
  surname. Related to the books they are credited on, and
  now a resource with an index and a page of their own.
- **Credit role**: How one person is credited on one book, being an author or a compiler. It
  belongs to the link between them rather than to either end, because the same person can write
  one book and compile another, and a role stored on the person would be wrong the first time
  that happens.
- **Book's arrival date**: The date a book entered the catalogue, recorded for every book and
  shown nowhere yet. Present rather than optional: books created from now on carry the date they
  were created, and the books already here carry the date the field was added to them.
- **Narrator**: The name of the person whose voice is on a recording, the озвучувач or читець.
  A name on a book rather than anything to do with a file. It is what distinguishes an audiobook
  from a book and how a listener chooses between two recordings of the same title.
- **Cover art**: A book's cover, held in object storage at a small number of fixed widths,
  public and cacheable, chosen between by whoever renders it. Rendered at its own proportions,
  because the covers the project holds are not one shape. Absent for some books, where the
  placeholder stands in.
- **Primary action colour role**: The colour of doing something, added to the thirteen roles the
  palette already names, with its contrast pair recorded alongside them. Distinct from the warm
  hue the focus indicator keeps to itself.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every page the site serves presents a banner, a main and a contentinfo landmark,
  and every navigation and search landmark on it carries a distinct name: 100% of pages, with
  zero pages left with the two landmarks the site offers today.
- **SC-002**: From any page on the site, a visitor using only a keyboard can reach the
  catalogue, the search field and the about page without going through another page first: 100%
  of pages, the forgot password, reset password and search pages included.
- **SC-003**: The library's name is readable as text on 100% of pages, and is announced exactly
  once per page by a screen reader.
- **SC-004**: Every page below the top level shows a breadcrumb trail whose last item is marked
  as the current page and whose earlier items each lead to the page they name: 100% of such
  pages, with zero broken trails.
- **SC-005**: A keyboard user who begins moving through a page before the session resolves loses
  no position and sees no element shift: zero tab stop changes measured between the two states,
  on every page.
- **SC-006**: However many parts of a page depend on who is signed in, the answer is established
  once per page view rather than once per part, measured on the pages that have more than one
  such part.
- **SC-007**: A search can be started and completed with the keyboard alone from 100% of pages,
  and the result is reachable again from its address in a new browser session.
- **SC-008**: The example inside the search field changes zero times after the field has taken
  focus, and zero times in a session that asks for reduced motion.
- **SC-009**: Every search suggestion can be reached and activated by keyboard and is announced
  as a link: 100% of suggestions shown.
- **SC-010**: Adding or removing a book changes the examples and the suggestions with zero files
  edited by hand, and every suggestion shown names something the catalogue actually holds, so
  activating it returns at least one result: 100% of the samples checked.
- **SC-011**: Every catalogue address that existed before this feature answers with a permanent
  redirect to its new address: 100%, with zero addresses that answer as missing.
- **SC-012**: The site publishes a document readable without running scripts for the landing
  page, every catalogue page, every book and every author: 100% coverage, verified against what
  the catalogue actually holds.
- **SC-013**: Every author credited on a published book is reachable from that book in one step,
  and their page lists 100% of the published books they are credited on and none they are not.
- **SC-014**: The index of authors orders names by surname under Ukrainian collation, with zero
  names ordered by given name.
- **SC-015**: No page in the site shows a broken image: zero across the catalogue, the book
  pages and the author pages.
- **SC-016**: A request for a static file the site does not have answers as missing in 100% of
  cases, and zero such requests return a page with a success status.
- **SC-017**: Cover art in the catalogue measures at least 120 device-independent pixels on its
  shorter side at a 1280 pixel viewport, and the catalogue still reflows without horizontal
  scrolling at 320.
- **SC-018**: The catalogue's first page still becomes readable within 3 seconds on a connection
  limited to 1.6 Mbps downlink with 150 ms of round-trip latency, with the larger covers in
  place, so the readability budget committed to in `001-browse-catalogue` survives the change.
- **SC-019**: Every form on the site renders its primary action in a treatment no other control
  on that form uses: 100% of forms.
- **SC-020**: Zero play controls, disabled or otherwise, appear in a screen reader's list of
  controls on any page.
- **SC-021**: The browser suite reaches a signed in session with zero manual steps, and audits at
  least the pages carrying session dependent chrome in that state.
- **SC-022**: The automated accessibility audit reports zero critical and zero serious
  violations on every page in scope, in both the anonymous and the signed in state.
- **SC-023**: Body text measures at least 4.5:1 and large text at least 3:1 against what is
  actually rendered behind it, across 100% of the text sampled on every page, header, footer and
  breadcrumbs included.
- **SC-024**: Every interactive element the feature adds presents a target of at least 44 by 44
  device-independent pixels, at 100% coverage.
- **SC-025**: Every page reflows without horizontal scrolling of the page body at a 320
  device-independent pixel viewport and at 200% zoom: 100% of pages.
- **SC-026**: Sharing a book, an author or the landing page into a messaging application produces
  a title, a summary and, where one exists, a cover, rather than a bare address.
- **SC-027**: The account screens and the search results page remain marked not to be indexed:
  100%, unchanged from before the feature.
- **SC-028**: The catalogue holds exactly the books the project actually has and zero
  placeholders, and every one of them shows a running time greater than zero.
- **SC-029**: Every credited name on a book page says how that person is credited, and the index
  lists 100% of the people credited on a published book whatever their role.
- **SC-030**: Zero covers are cropped: each renders at the proportions of its source image, and
  the placeholder keeps one fixed shape.
- **SC-031**: Every paging assertion the browser suite held before this feature still runs and
  still passes, against a catalogue seeded large enough to page.

## Assumptions

- The freeze the previous feature imposed, that no route, behaviour, word, page, control or
  capability could change, was scoped to the restyle and expires with it. This feature relaxes
  it deliberately: a header introduces copy and a footer introduces more.
- The interface language is Ukrainian and all new copy is Ukrainian. The wordmark stays in latin
  letters, as `003-visual-identity` settled, and is now readable text in the header as well as
  decorative texture behind the page.
- The deep blue ground remains the product's single theme, and every new surface is designed on
  it. No light theme, no theme switch.
- There is no audio and no playback in this feature, and none of the new storage code touches
  audio.
- The search field moves into the header rather than being duplicated there, as the
  clarification session settled. The search results page loses the form in its body and the
  header field carries the current term, so a visitor never meets two fields that do the same
  thing and a screen reader never meets two search landmarks on one page.
- A small number of suggestions means three to five. Fewer stops being help, more becomes a
  second navigation.
- The rotating example changes slowly enough to be read rather than watched, on the order of
  several seconds per example, and it is supplementary throughout: the label, not the example,
  says what the field is for.
- The resource that supplies the examples and the suggestions is a random sample of the
  catalogue drawn on each request rather than a curated list, so it needs no maintenance. Its
  content cannot be asserted, so tests assert its shape: how many suggestions are offered, that
  each names a title or an author the catalogue holds, and that activating one returns at least
  one result. Serving it as data is what
  makes it editable later: when an administrative feature exists, changing what the field
  suggests is editing rows that already exist rather than inventing a new concept. This feature
  does not build that screen, and records the path rather than promising it.
- Suggesting results while someone types is deliberately excluded. A combobox that announces
  changing results as a visitor types is among the hardest patterns in the accessibility
  specification to get right, it needs its own endpoint and its own latency budget, and at this
  catalogue size it buys very little. It earns its place when paging to a title is genuinely
  slow.
- No burger menu. Three or four destinations need a bar that wraps at 320 pixels rather than a
  disclosure, and a burger is added when the navigation outgrows the bar rather than in
  anticipation of it.
- The skip link in FR-007 is new here rather than asked for in the description. A banner
  carrying navigation, search and account controls now sits before the content on every page,
  which is exactly the condition that makes skipping it worth offering; leaving it out would
  make every keyboard visitor pay for the header on every page.
- An author's page lists every book they are credited on without paging, because no author in
  the catalogue is close to a page's worth of books. If one ever is, it adopts the catalogue's
  existing paging convention rather than inventing another.
- A book has at most one narrator, recorded as a name. For recordings made in house that name is
  the library's own, and for anything contributed it is whoever read it. A book that has none
  simply shows none.
- The date a book entered the catalogue is recorded but surfaced nowhere in this feature. It
  cannot be backfilled honestly once books exist, which is why it is added now, and recently
  added browsing is what will use it. The books already in the catalogue take the date the field
  was added to them, so the value is always present; the accepted consequence is that all of them
  share one arrival date, and the first recently added list built on this field will show them as
  one batch rather than in the order they really arrived.
- Genre and tags, series and position in series, translator, publication year, and a per book
  record of where a recording came from and on what basis are all deliberately left to a later
  content feature.
- Real cover art exists and goes in with this feature, committed as seed input at its prepared
  widths and uploaded by the seeding step. It belongs to the real books FR-064 brings in, not to
  the placeholders, which is how the mismatch was found: the covers arrived and matched nothing
  in the catalogue. A book without one keeps the placeholder, which is a designed state rather
  than a failure.
- The source images are not one shape. They run from 0.64 to 0.75 wide against tall, and three
  of the four are narrower than 640 pixels, which is why the prepared widths are 160 and 320
  rather than the three first planned and why FR-067 renders each at its own proportions. Both
  follow from the art that exists rather than from a preference.
- Cloudflare R2 in production, which is the provider the architecture document already chose for
  audio, and MinIO in the compose file for development, standing in for R2 the way Mailpit
  stands in for SMTP, so that the dev container, the endpoint tests and the accessibility suite
  need no cloud credentials and no network.
- Covers are public catalogue metadata under Principle IV of the constitution, so they are
  served public read and are never signed. A cover has to be cacheable, indexable, and usable as
  a preview when a link is shared.
- The audio storage stays a separate interface rather than a generalisation of the cover one,
  because covers are public and cacheable while audio is signed and must never be cached, and
  one interface serving both would have to be told which it is on every call.
- The shape audio will take is recorded here so that the feature which builds it inherits a
  decision rather than making one under time pressure, and none of it is implemented now. A
  chapter carries an opaque storage key rather than a URL, so that nothing in a public catalogue
  response is ever a location someone could fetch. At the moment a signed in listener asks to
  play, the API decides whether they may and hands back a short lived signed URL, and object
  storage serves the bytes; the API never proxies audio, because egress is the dominant cost of
  streaming and because deciding who may listen and serving bytes are different jobs. For the
  MVP that is one file per chapter answered with HTTP Range requests and partial content, which
  is what makes seeking work, with HLS deferred until adaptive bitrate is genuinely needed. The
  expiry is what makes the gate real against sharing: a link pasted into a chat stops working.
  It is worth stating plainly that this is not copy protection, because anyone who can listen
  can capture the file within the window; it is an honest limit on casual redistribution rather
  than a lock.
- Mailpit is already in the compose file and has an HTTP API, and reading the confirmation link
  out of it is what makes the signed in half of the product testable. No production mail flow
  changes.
- The existing accessibility audit, endpoint tests and prerender tests remain the regression net.
  This feature extends them to the new pages, to the signed in state, and to the requirement that
  a missing file answers as missing.

## Out of Scope

Named here so they are not lost. Each ships as its own small pull request rather than waiting on
this specification.

- Running the accessibility and prerender suites on every pull request. This is the largest
  structural gap in the repository and the thing that stops all of the above from recurring. The
  frontend job's name is the status check context the branch protection ruleset matches, so it
  stays exactly as it is or the ruleset changes in the same pull request.
- Turning on the jsx-a11y plugin that the linter already ships and the configuration does not
  enable.
- Deleting the weather forecast endpoint, which is still mapped and has propagated into the
  generated frontend API types.
- Drawing a favicon from the identity and linking it. The file that exists is the starter's
  purple logo, referenced by nothing.
- Correcting the README's backend layout, which lists one project where there are now five.
- Moving chapter running times next to the chapters they belong to rather than roughly five
  hundred pixels away across an empty region.
- Settling how visible the watermark is, and whether the ground's luminance band token caps the
  grain alone or the watermark as well.
- The web app manifest and service worker that the platform decision's stated benefit depends on.
- An administrative surface of any kind, including the screen that would edit the search
  suggestions.
- Suggesting results as the visitor types.
- An account area behind the session.
- Playback, and any audio storage, streaming or signing.
