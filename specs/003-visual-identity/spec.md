# Feature Specification: Visual Identity

**Feature Branch**: `feature/visual-design`

**Created**: 2026-09-10

**Status**: Draft

**Input**: User description: "Give the catalogue a visual identity. The site works and passes
its accessibility checks but looks like an unstyled scaffold. This feature restyles the pages
that already exist (catalogue list, book detail, search results, not-found, and the loading,
empty and error states) without changing any behaviour, route, wording or content. The look: a
deep blue ground, close to navy, carrying a fine even grain rather than a flat fill. Across it
the wordmark 'ZALIZNA biblioteka' repeats as a watermark, set diagonally, at a small size, with
even gaps, so it reads as texture rather than as a headline. Content sits above that ground in
clearly delineated regions, so reading never happens directly against the busiest part of the
texture. Requirements that must hold: body text keeps a contrast ratio of at least 4.5:1 and
large text at least 3:1 against what is actually behind it, measured over the decorated ground
rather than over the flat colour. The decorative layers are never exposed to assistive
technology and are never announced. When a visitor asks their system for higher contrast, the
grain and the watermark are dropped in favour of a plain solid ground. The keyboard focus
indicator stays clearly visible against the dark ground on every interactive element. Tap
targets are large enough to hit without precision. The existing automated accessibility checks
continue to report zero violations. Visual decisions live in one shared place and are referenced
by name, so a colour or a spacing step is defined once and changed once rather than repeated per
page. Assumptions to record: the deep blue ground is the product's single theme, and a light
theme is out of scope. The wordmark is set in latin letters as 'ZALIZNA biblioteka' even though
the interface language is Ukrainian, because it is a brand mark and not interface copy. No
commissioned artwork, illustration or photography is involved; every decorative element is
generated. No new pages, no new capabilities, no copy changes."

## Clarifications

### Session 2026-09-10

- Q: Does the restyle cover the account screens, or only the catalogue pages the description
  lists? A: Every page that exists today. Register, confirm, sign in, forgot password and reset
  password are designed to the same requirements as the catalogue pages, and so is the session
  menu that appears on every page. Nothing on the site is left on the old ground.
- Q: Which typeface carries the identity, given that the description settles the ground and the
  wordmark but not the lettering? A: Fixel, by MacPaw, under the SIL Open Font License. Its Text
  style sets running text and its Display style sets headings and the wordmark. It was chosen for
  a complete Ukrainian layout, for open, wide, low contrast letterforms that hold up as light text
  on a dark ground, and for a licence that a shipped product can rely on.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The site looks like a finished product (Priority: P1)

A visitor opens the catalogue and sees a library with a face of its own: a deep blue ground,
close to navy, carrying a fine even grain instead of a flat fill, with the wordmark
"ZALIZNA biblioteka" repeating diagonally across it at a small size and even gaps so that it
registers as texture. The books, a book's details, the search results, the account screens they
register and sign in on, and every short message sit above that ground in clearly delineated
regions. Everything they could do before, they can still do, in exactly the same words.

**Why this priority**: It is the entire point of the feature. A site that reads as an unfinished
scaffold undermines trust in a product that asks people to hand over an address, choose a
password and listen, and the ground is the layer every other visual decision sits on. On its own
it already turns the site from a scaffold into something recognisable. It has to cover every
page at once: a single screen left on the old ground is more obviously broken than a site that
was never styled at all.

**Independent Test**: Open each page in scope and confirm the ground, its grain and its
watermark are present, that content sits in delineated regions above them, and that every
route, every control and every word on the page is identical to what it was before.

**Acceptance Scenarios**:

1. **Given** a visitor opens the catalogue list, **When** the page renders, **Then** the deep
   blue ground carries a fine even grain and the repeated diagonal wordmark, and the list of
   books sits above it in a delineated region.
2. **Given** a visitor moves from the catalogue to a book, to a set of search results, to an
   address that matches nothing, and on to registering and signing in, **When** each page
   renders, **Then** every one of them carries the same ground, the same region treatment and
   the same visual language.
3. **Given** a visitor fills in a form on an account screen, **When** a field is rejected or a
   message reports what happened, **Then** the label, the hint, the field itself and the message
   are all legible against the dark ground, and the reason a field was rejected is still given
   by more than colour.
4. **Given** the catalogue is loading, is empty, or could not be reached, **When** the
   corresponding message appears, **Then** it too sits in a delineated region rather than
   directly on the texture.
5. **Given** any page in scope, **When** a visitor reads its running text, **Then** the grain
   and the wordmark are not perceptible behind that text.
6. **Given** the whole feature is applied, **When** the existing tests run, **Then** no route,
   no behaviour and no piece of wording has changed.
7. **Given** a page whose content is shorter than the viewport, **When** it renders, **Then**
   the ground fills the viewport with no visible seam where the content ends.

---

### User Story 2 - Nothing decorative reaches assistive technology (Priority: P2)

A visitor using a screen reader moves through the same pages they used yesterday. The site now
has a texture and a repeated wordmark behind everything, and they never hear about either. The
page announces exactly what it announced before, in the same order, and nothing decorative can
be focused or landed on.

**Why this priority**: Accessibility is the product, and a decorative layer is the classic way
to break a working screen reader experience: a repeated wordmark that is exposed would be read
out on every page, dozens of times, drowning the catalogue it decorates. This story is what
keeps a purely visual change from costing the site its primary audience.

**Independent Test**: Run the existing automated accessibility audit, which already covers the
catalogue, the book page, search and all five account screens, and a screen reader pass over
each page in scope, then compare the announced output against the same pass before the feature.
Any difference is a failure.

**Acceptance Scenarios**:

1. **Given** a visitor using a screen reader, **When** they read any page in scope from top to
   bottom, **Then** neither the grain nor the wordmark is announced anywhere.
2. **Given** a visitor using only the keyboard, **When** they move focus through a page,
   **Then** no decorative layer is reachable, and focus order is unchanged from before.
3. **Given** any page in scope, **When** the existing automated accessibility audit runs,
   **Then** it reports zero critical and zero serious violations.
4. **Given** a screen reader pass recorded before this feature, **When** the same pass is
   repeated after it, **Then** the announced content is identical, with nothing added and
   nothing lost.

---

### User Story 3 - A visitor who needs more contrast gets a plain ground (Priority: P3)

A visitor has told their system they want increased contrast, or their system supplies its own
colours. The catalogue drops the grain and the wordmark entirely and gives them a plain solid
ground, with the same content, the same regions and the same routes.

**Why this priority**: It is a narrower audience than P1 and P2 but a decisive one: texture
behind text is precisely what a person who asked for higher contrast is asking to be rid of.
It depends on the ground existing, so it follows the stories that create it.

**Independent Test**: Set the platform to request increased contrast, then open each page in
scope and confirm no grain and no wordmark are rendered, that the ground is a plain solid fill,
and that every contrast obligation still holds.

**Acceptance Scenarios**:

1. **Given** a visitor whose system asks for increased contrast, **When** any page in scope
   renders, **Then** the ground is a plain solid fill with no grain and no wordmark.
2. **Given** that same visitor, **When** they read the page, **Then** body and large text still
   meet their contrast thresholds and the focus indicator is still visible.
3. **Given** a visitor whose system supplies its own colours, **When** any page renders,
   **Then** those colours are honoured and no decorative layer overrides them.
4. **Given** a device or browser on which a decorative layer cannot be produced at all,
   **When** a page renders, **Then** it falls back to the plain solid ground and remains fully
   legible.

---

### User Story 4 - One place to change a visual decision (Priority: P4)

Someone maintaining the site is asked to make the ground a shade deeper, or to open up the
space between books in the list. They change one named value in one place, and every page that
uses it follows.

**Why this priority**: It is the difference between a visual identity and a pile of per-page
styling. It delivers no visible change on its own, which is why it is last, but without it the
next visual change costs a sweep of every page and drifts out of alignment.

**Independent Test**: Change one named colour and one named spacing step, then confirm that
exactly one location was edited and that the change appears on every page that uses it.

**Acceptance Scenarios**:

1. **Given** the set of visual decisions, **When** a maintainer looks for the ground colour,
   **Then** it is defined once, under a name that says what it is for.
2. **Given** a named colour is changed in that one place, **When** every page in scope is
   opened, **Then** all of them reflect the change with no further edits.
3. **Given** any page or component styling, **When** it is inspected, **Then** it carries no
   literal colour value and no spacing value invented on the spot.

---

### Edge Cases

- What happens when the grain or the watermark cannot be produced on a given device or browser?
  The page falls back to the plain deep blue ground, and every contrast obligation still holds.
- What happens when a visitor asks for increased contrast **and** their system supplies its own
  colours? Decoration is dropped, the system colours win, and the focus indicator stays visible.
- What happens where the decorated ground is at its lightest under one piece of text and at its
  darkest under another? Both extremes are measured, and the text colour has to clear its
  threshold against the worse of the two.
- What happens to a very long unbroken book title inside a region? It wraps as it does today;
  no region clips it, no fixed height cuts a wrapped line off, and nothing is hidden from a
  screen reader.
- What happens when a region is narrower than one repeat of the wordmark? The texture still
  tiles evenly and a partial mark never reads as content.
- What happens at 200% zoom, where the regions grow and little of the ground is left visible?
  The identity may be almost entirely covered, and every contrast, focus and target obligation
  still holds.
- What happens to a book cover whose own artwork is pale against a dark region? Cover art is
  content, not decoration, and is rendered unchanged.
- What happens on the shortest pages, such as the not-found page and the empty state? They
  still present a delineated region rather than a single line floating on the texture.
- What happens to native controls, such as the search field and the retry button, on a dark
  ground? They render in a scheme that matches the interface rather than in the browser's
  light defaults.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST present every page the site serves today on a single deep blue ground,
  close to navy, carrying a fine even grain rather than a flat fill. Those pages, referred to
  below as the pages in scope, are the catalogue list and its numbered pages, the book detail
  page, the search results page, the not-found page, the register, confirm, sign in, forgot
  password and reset password screens, the session menu that appears on every one of them, and
  the loading, empty and error states any of them can show.
- **FR-002**: System MUST repeat the wordmark "ZALIZNA biblioteka" across that ground as a
  watermark, set diagonally, at a small size, with even gaps.
- **FR-003**: System MUST keep the watermark reading as texture rather than as content: no
  instance of it is the largest text on a page, and no instance is placed or sized so that it
  could be mistaken for a page title or a heading.
- **FR-004**: System MUST place every block of running text on a delineated region whose fill
  subdues the grain and the watermark to imperceptibility behind that text, so that reading
  never happens directly against the busiest part of the ground.
- **FR-005**: System MUST leave the decorated ground visible around and between those regions,
  so the identity is present on every page rather than covered over.
- **FR-006**: System MUST maintain a contrast ratio of at least 4.5:1 for body text and at
  least 3:1 for large text, measured against the lightest and the darkest point that the
  decorated ground and anything layered over it actually produce behind that text, rather than
  against the flat colour alone.
- **FR-007**: System MUST maintain a contrast ratio of at least 3:1 for every non-text element
  that carries meaning, including region edges, control boundaries, and the placeholder that
  stands in for missing cover art, against what is immediately adjacent to it.
- **FR-008**: System MUST NOT expose the grain, the watermark or any other decorative layer to
  assistive technology, and MUST NOT allow any of them to be announced, focused, or placed in
  reading order.
- **FR-009**: System MUST replace the grain and the watermark with a plain solid ground when
  the visitor's system asks for increased contrast or supplies its own colours, while
  continuing to satisfy every other requirement in this specification.
- **FR-010**: System MUST fall back to the plain solid ground, with every contrast obligation
  intact, whenever a decorative layer cannot be produced.
- **FR-011**: System MUST show a focus indicator on every interactive element that is clearly
  visible against the dark ground, at a contrast ratio of at least 3:1 against both the element
  it marks and what surrounds it, and MUST NOT remove or weaken an indicator the platform
  already provides.
- **FR-012**: System MUST give every interactive element a target measuring at least 44 by 44
  device-independent pixels, including its surrounding hit area, so it can be hit without
  precision.
- **FR-013**: System MUST render native controls, including text fields, buttons and
  scrollbars, in a scheme that matches the dark interface rather than in the platform's light
  defaults.
- **FR-014**: System MUST keep the decorative layers static, introducing no motion or
  animation anywhere in this feature.
- **FR-015**: System MUST NOT change any route, any behaviour, any wording or any content on
  any page, and MUST NOT add a page, a control or a capability.
- **FR-016**: System MUST define every visual decision, meaning each colour, spacing step, type
  size, corner radius and elevation, exactly once in one shared place, under a name that says
  what the value is for, and MUST refer to it by that name everywhere it is used.
- **FR-017**: System MUST NOT leave any page-level or component-level styling carrying a
  literal colour value or a spacing value invented on the spot rather than drawn from that
  shared place.
- **FR-018**: System MUST remove the scaffold styling inherited from the project starter, so
  that no page carries a rule whose only purpose was to present the starter's own
  demonstration content.
- **FR-019**: System MUST leave no page, and no part of a page, on the styling it carried before,
  so that nothing mixes the new ground with the old one and no screen is reachable that looks
  like it belongs to a different product.
- **FR-020**: System MUST keep every page readable without horizontal scrolling of the page
  body down to a 320 device-independent pixel viewport and at 200% zoom, and MUST NOT clip or
  hide text when the platform enlarges it.

- **FR-021**: System MUST keep every part of a form legible against the dark ground, including
  field labels, the text a visitor types, placeholder and hint text, the description of the
  password policy, and any message reporting what happened, each at the contrast ratio FR-006
  requires of text of its size.
- **FR-022**: System MUST NOT introduce colour as the only means of conveying that a field was
  rejected, that a field is required, or that an action succeeded or failed, and MUST preserve
  every non-colour signal already carrying that meaning.
- **FR-023**: System MUST style the regions that report status and errors without changing what
  they announce, when they announce it, or how urgently, so that a message that interrupts today
  still interrupts and a message that waits politely still waits.
- **FR-024**: System MUST set light text on the dark ground so that it stays crisp rather than
  appearing to bloom, keeping body text off pure white and setting line spacing and letter
  spacing for reversed text rather than inheriting values chosen for dark text on a light
  ground.

- **FR-025**: System MUST set the interface in Fixel, using its Text style for running text and
  its Display style for headings and for the wordmark, and MUST render every character the
  Ukrainian interface uses, including і, ї, є and ґ, from that typeface rather than from a
  substitute.
- **FR-026**: System MUST NOT depend on a third party to deliver the typeface, and MUST keep text
  readable while the typeface loads, so that no page shows empty space where words should be.

### Key Entities

- **Visual token**: One named visual decision, defined once and referred to by name. A colour
  role, a spacing step, a type size, a corner radius, or an elevation. It is the unit that
  FR-016 says is changed in one place.
- **Colour role**: What a colour is for rather than what it is, for example the ground, a
  region surface, body text, muted text, a heading, a link, a border, the focus indicator. Each
  role carries the contrast obligation that applies to the text or element it serves.
- **Ground**: The composite backdrop of every page: the flat deep blue, the fine even grain
  laid over it, and the repeated diagonal wordmark. Wholly decorative, never announced, and
  reduced to the flat deep blue alone under increased contrast.
- **Region**: A delineated area holding content above the ground, whose fill is what makes text
  readable rather than set against texture.
- **Wordmark**: The brand mark "ZALIZNA biblioteka", set in latin letters. In this feature it
  appears only as repeated decorative texture and never as interface copy.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The existing automated accessibility audit reports zero critical and zero serious
  violations on every page it already covers, unchanged from before the feature.
- **SC-002**: Body text measures at least 4.5:1 and large text at least 3:1 against the
  worst-case point of what is actually rendered behind it, across 100% of the text sampled on
  every page in scope.
- **SC-003**: A screen reader pass over each page in scope announces the same content in the
  same order as the equivalent pass before the feature: zero announcements added, zero lost.
- **SC-004**: A keyboard-only visitor can see which element holds focus at every step of the
  journey from the catalogue to a book, to a set of search results, through registering and
  signing in, and back, on 100% of the interactive elements they reach, form fields included.
- **SC-005**: With the platform set to request increased contrast, every page in scope renders
  on a plain solid ground with no grain and no watermark, and still satisfies SC-002 and
  SC-004.
- **SC-006**: Every interactive element presents a target of at least 44 by 44
  device-independent pixels, at 100% coverage across the pages in scope.
- **SC-007**: Changing one colour role or one spacing step requires an edit in exactly one
  location, and the change is visible on every page that uses it.
- **SC-008**: The existing test suite passes with no expected route, behaviour or piece of
  wording modified in any test.
- **SC-009**: Every page in scope reflows without horizontal scrolling of the page body at a
  320 device-independent pixel viewport and at 200% zoom.
- **SC-010**: The catalogue list still becomes readable within 3 seconds on a connection
  limited to 1.6 Mbps downlink with 150 ms of round-trip latency, so the decoration costs the
  catalogue none of its existing readability budget.

- **SC-011**: Every character the interface renders across the pages in scope is drawn from the
  chosen typeface, with zero substituted or missing glyphs, Ukrainian і, ї, є and ґ included.
- **SC-012**: No page in scope displays empty space in place of text at any point while the
  typeface is loading, on a connection limited to 1.6 Mbps downlink with 150 ms of round-trip
  latency.

## Assumptions

- The deep blue ground is the product's single theme. A light theme, and any control that lets
  a visitor switch themes, are out of scope.
- The wordmark stays in latin letters as "ZALIZNA biblioteka" even though the interface language
  is Ukrainian, because it is a brand mark rather than interface copy. It is decorative here and
  never announced, so it never reaches a Ukrainian screen reader voice.
- Every decorative element is generated. No commissioned artwork, illustration or photography
  is involved, and no downloaded image asset is introduced.
- No new pages, routes, capabilities or copy. The feature restyles what already exists and
  nothing else. FR-001 lists those pages, and the list is every page the site serves today.
- The identity is carried by colour, texture, spacing, scale and lettering. Fixel is a licensed
  open typeface rather than commissioned artwork, so it does not contradict the rule that every
  decorative element is generated. It is delivered from the site's own origin and reduced to the
  characters the interface actually uses, so that the catalogue keeps the readability budget of
  3 seconds on a 1.6 Mbps connection it committed to in `001-browse-catalogue`. SC-010 is what
  holds this to account, and a typeface that cannot be made to fit that budget is the wrong
  typeface.
- Light text on a dark ground appears to bloom, which is uncomfortable for readers generally and
  painful for readers with astigmatism. FR-024 exists because of that, and it is the reason body
  text is kept off pure white rather than pushed to the highest contrast available. Maximum
  contrast and maximum readability are not the same target.
- The account screens are designed to the same standard as the catalogue pages rather than
  merely made legible on the new ground. They are where a visitor is asked to trust the product
  with an address and a password, so a screen that still looks like a scaffold costs more there
  than anywhere else. This also settles a problem a narrower scope would have created: the
  accessibility checks already covering those screens measure contrast against whatever is
  actually behind the text, so a document wide dark ground under unchanged light theme text
  would have failed them.
- Nothing on these pages animates today and this feature adds no motion, so a reduced motion
  preference needs no separate treatment.
- Book cover art is content rather than decoration. It is unchanged, and the placeholder that
  stands in for a missing cover is restyled to sit on the new ground without changing how the
  entry is announced.
- Print styling is out of scope.
- The existing accessibility audit, endpoint tests and prerender tests are the regression net
  for "no behaviour changed". This feature adds checks for contrast measured over the decorated
  ground, for the increased contrast mode, and for target size, and modifies nothing the
  existing checks expect.
