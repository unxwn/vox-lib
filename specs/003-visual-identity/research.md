# Phase 0 Research: Visual Identity

**Feature**: `specs/003-visual-identity/` | **Date**: 2026-09-10

Every unknown in the plan's Technical Context is resolved below. Two findings were taken from
the installed sources rather than from documentation, because the documentation is either silent
or imprecise on the point that matters: axe-core 4.13.0's contrast walk (R1) and Playwright
1.63's media emulation surface (R5). Colour values were computed, not judged; the arithmetic is
in [data-model.md](./data-model.md).

## R1. Where the decorative layers live, and what actually protects the contrast check

**Decision**: One element, `<div class="ground" aria-hidden="true">`, rendered by `Ground.tsx` as
the first child of `<body>` and a sibling of the content, `position: fixed`, `inset: 0`,
`z-index: -1`, `pointer-events: none`. Separately and independently, every region that carries
text is given a fully opaque `background-color`.

**Rationale**: These are two decisions that are easy to confuse, and only the second one protects
the accessibility check.

Reading `axe.js` at 4.13.0: `isOpaque(node)` returns
`elementHasImage(node, style) || getOwnBackgroundColor(style).alpha === 1`, and the accumulation
loop in `getBackgroundColor` breaks as soon as it sees `bgColor.alpha === 1`. `elementHasImage`
sets `incompleteData.set('bgColor', 'bgImage')` when it finds a non-gradient background image,
and the message table renders that as "Element's background color could not be determined due to
a background image". Axe walks the rect stack, which is composed of every element whose box
covers the text, so a fixed sibling painted behind the content is in that stack exactly as an
ancestor would be. Mounting the decoration on a sibling therefore does not, by itself, keep axe
working.

What keeps it working is the opaque region. If the region's own fill is `alpha === 1`, the loop
breaks there and never reaches the ground, so no image is seen and the ratio is computed
normally. If the region is translucent, the walk continues to the ground, meets the grain, and
the result becomes incomplete rather than failed. That is the dangerous outcome, because
`SC-001` counts critical and serious violations and an incomplete result is neither: the suite
stays green while the guarantee it existed to provide is gone. FR-004 already requires content to
sit on regions that subdue the texture, so the requirement and the mechanism coincide; this
research only establishes that the fill has to be genuinely opaque rather than merely dark.

The sibling placement is kept for two smaller reasons that stand on their own. It keeps the
turbulence filter and the stacking context it creates off the content subtree, so a repaint of
the ground cannot invalidate the content's layer. And it leaves the content free of an inherited
`background-image` for any future tool less careful than axe about where the image sits.

**Alternatives considered**:

- `background-image` on `body`. The conventional placement, and the one most examples use. It
  makes `body` an ancestor of everything, so any text not on an opaque region resolves through
  it, and it puts a full-viewport paint under the whole document.
- A wrapper `<div>` around the content. Same problem as `body` with an extra element.
- Painting the grain into a `<canvas>`. Requires JavaScript, so it would be absent from the
  prerendered documents until hydration, which is a visible flash on exactly the pages that
  matter most for SC-010.

## R2. How the grain is generated

**Decision**: An SVG `feTurbulence` with `type="fractalNoise"`, rendered into a 160 by 160 tile,
inlined as a `url("data:image/svg+xml,...")` CSS background on the ground element and repeated.
Base frequency around 0.8, one octave, the result desaturated and composited at low alpha over
the flat `--ground` fill.

**Rationale**: Turbulence is the only way to get an even, non-repeating-looking grain without
shipping an image, and FR-002 and the spec's "every decorative element is generated" rule out an
image. The important choice is tile-and-repeat rather than one viewport-sized filter. An
`feTurbulence` covering the whole viewport is re-evaluated by the filter pipeline whenever its
box changes, which on a mobile browser includes every address bar collapse; a 160 pixel tile is
rasterised once and repeated by the compositor. The tile size is chosen to be large enough that
the eye does not read the repeat and small enough that the data URI stays under about 400 bytes.

A background image is the right mechanism here specifically because turbulence needs no font, so
none of R3's isolation problem applies.

**Alternatives considered**:

- `repeating-linear-gradient` noise. Cheap, but gradients read as stripes or moiré at small
  sizes rather than as grain, and axe classifies gradients separately (`bgGradient`), so the
  distinction is not worth relying on.
- Multiple layered `radial-gradient` dots. Produces visible regularity at the sizes involved.
- A base64 PNG of noise. Smaller to decode, but it is an image asset, which the spec excludes,
  and it cannot adapt if `--ground` changes.

## R3. How the watermark is generated, and why it is not a background image

**Decision**: An inline `<svg aria-hidden="true" focusable="false">` inside the ground element,
containing a single `<pattern>` with `patternTransform="rotate(-24)"` and one `<text>` node
reading "ZALIZNA biblioteka", filling a `<rect width="100%" height="100%">`.

**Rationale**: This is the one place where the obvious mechanism is wrong. An SVG referenced as
an image, whether through `background-image`, `<img>` or `content`, is rendered as an isolated
document: it cannot reach the referencing page's stylesheets, and in particular it cannot use a
webfont the page has loaded. A watermark drawn that way would fall back to a generic family, and
FR-025 requires the wordmark to be set in Fixel Display. Inline SVG is part of the document tree,
so CSS applies to it normally and `font-family: var(--font-display)` resolves.

A `<pattern>` gives the even repeat FR-002 asks for from one text node rather than from dozens of
positioned elements, and `patternTransform` gives the diagonal without rotating any content.
`aria-hidden="true"` covers FR-008 and `focusable="false"` covers the older behaviour where an
SVG could take tab focus.

Two costs are accepted. Find-in-page will match the wordmark's letters in browsers that search
SVG text, which is cosmetic and affects no requirement. And the wordmark's glyphs are drawn from
the Display subset, so R4's subset must include the Latin letters the wordmark uses even though
the interface itself is Ukrainian.

**Alternatives considered**:

- An SVG data URI background. Rejected for the font isolation above. Worth recording because it
  is what most watermark examples do and it fails silently: the page looks fine, in the wrong
  typeface.
- The wordmark converted to outlines and embedded as paths. Works, and keeps the shapes, but it
  freezes the letterforms at build time, adds a tool, and makes changing the mark a binary edit.
  Reconsider only if the pattern approach shows a rendering cost.
- Repeated `aria-hidden` DOM elements. Roughly eighty positioned spans per viewport, all of them
  in every prerendered document, for the same result.

## R4. Delivering Fixel

**Decision**: Two variable `woff2` files, Fixel Text and Fixel Display, subset to the Latin and
Ukrainian Cyrillic ranges plus punctuation and figures, served from `frontend/public/fonts/` and
declared with `font-display: swap`. The fallback family carries `size-adjust`, `ascent-override`
and `descent-override` tuned to Fixel's metrics. Subsetting is a documented manual step, recorded
in [quickstart.md](./quickstart.md), not a build dependency.

**Rationale**: FR-026 has two halves. "No third party" rules out the Google Fonts CDN, which also
costs a DNS lookup and a TLS handshake to a second origin before the first byte of a font. "No
empty space where words should be" is exactly what `font-display: swap` provides and what the
default `auto` behaviour, a block period of up to three seconds, does not; on the 1.6 Mbps
connection SC-012 names, `auto` is precisely the case that fails.

`swap` trades invisible text for a reflow when the face arrives, and that reflow is a real cost
on a page of book titles. The metric override properties on the fallback `@font-face` are what
keep it small: matching the fallback's effective x-height and line box to Fixel's means the swap
changes glyph shapes without moving lines.

Subsetting is not optional arithmetic. Fixel carries over 11,000 glyphs; the interface uses a few
hundred. The budget SC-010 defends is roughly 600 KB in total, and two unsubset variable faces
would consume most of it on their own. Doing it manually rather than in the build keeps a Python
font toolchain out of the CI image for an input that changes only when the typeface is updated,
which the plan records as a cost in its own right.

**Alternatives considered**:

- Static weights instead of variable. Fewer bytes per file, but the design uses at least regular,
  medium and bold in Text, so three files against one, and the variable file wins once three
  weights are in play.
- `font-display: optional`. Avoids the reflow entirely by not swapping on a slow connection, but
  then the identity is absent for first-time visitors on exactly the connections this product
  cares about.
- Self-hosting the full unsubset files. Simplest, and it fails SC-010.

## R5. Increased contrast and forced colours

**Decision**: `@media (prefers-contrast: more)` and `@media (forced-colors: active)` both set the
ground element to `display: none` and the ground token to a flat fill. Under forced colours the
palette additionally yields to the system colours, and the focus indicator is redeclared using
`Highlight`/`CanvasText` so it survives. Verified with Playwright's
`page.emulateMedia({ contrast: 'more' })` and `page.emulateMedia({ forcedColors: 'active' })`.

**Rationale**: FR-009 names two distinct situations, "asks for increased contrast" and "supplies
its own colours", and they are two different media features. `prefers-contrast: more` is the
person turning up contrast; `forced-colors: active` is Windows High Contrast and friends
replacing the palette wholesale, which also silently discards most `background-image`
declarations, so the grain must be dropped explicitly rather than left to be overridden.

Dropping the whole layer with `display: none` rather than hiding the layers individually is what
makes FR-010's failure case free: if the ground element is absent for any reason, the flat
`--ground` fill on the document is what shows, which is the same result.

`playwright-core@1.63.0`'s type definitions carry `contrast?: null|"no-preference"|"more"` and
`forcedColors?: null|"active"|"none"` on `emulateMedia`, so SC-005 is automatable in the existing
harness with no new dependency and no manual pass.

**Alternatives considered**:

- Only `prefers-contrast: more`. Leaves forced-colours users with a partially overridden
  decoration, which is the worst of both.
- `prefers-contrast: custom`. Signals that a person has set their own palette, but it is not the
  signal FR-009 describes and support is uneven.
- A visible control to turn the texture off. A new capability, which FR-015 forbids.

## R6. The focus indicator on a dark ground

**Decision**: A two part indicator, `outline: 3px solid var(--focus)` with
`outline-offset: 2px`, where `--focus` is a warm amber that measures 9.42:1 against a surface,
7.72:1 against a raised surface and 11.66:1 against the lightest excursion of the grain. Declared
once in `base.css` on `:focus-visible` for every focusable element, replacing the per-selector
rules in the two current stylesheets.

**Rationale**: The existing rules use `outline: 3px solid currentColor`, which inherits the text
colour. On the light scaffold that produced a near-black ring on white. On navy it produces a
near-white ring, which does clear 3:1, but it is the same colour as the text it surrounds and so
reads as a thickening rather than as a marker. A dedicated hue is unambiguous and is the only
element on the page in that colour, which is worth more on a dark interface than the extra
contrast is.

The offset matters more here than on a light ground: with no gap the ring merges into the light
text at small sizes. Three pixels at two pixels of offset satisfies the perimeter that WCAG 2.4.13
describes without depending on it, since the spec's own FR-011 sets the bar at 3:1 against both
the element and its surroundings, and both are computed in [data-model.md](./data-model.md).

The current rules are also incomplete in a way FR-011 does not permit: they are scoped to
`.account`, `.session-menu` and a handful of catalogue selectors, so anything focusable outside
those lists falls back to the user agent ring. R11 records where that already bites.

**Alternatives considered**:

- `outline: 3px solid currentColor` carried over. Cheapest, and it makes the indicator the same
  colour as everything else on a dark page.
- A `box-shadow` ring. Better looking at rounded corners, and it disappears entirely under
  forced colours, where `outline` survives.
- Relying on the user agent's own indicator. It is the thing the spec's "MUST NOT remove or
  weaken" clause protects, but on a navy ground the default ring in several browsers is a dark
  blue that fails 3:1 outright.

## R7. Target size, and whether the 44 pixel rule has an exception

**Decision**: 44 by 44 device-independent pixels for every interactive element, enforced with no
exception, achieved by padding rather than by minimum height alone. Verified by a Playwright spec
that walks every element matching an interactive selector and asserts its bounding box.

**Rationale**: FR-012 states the rule without an exception, and WCAG 2.5.8 has one, for links
inside a block of text, because a link in mid-sentence cannot be given a 44 pixel box without
wrecking the line. So the question is whether this site has any such link, and the answer is
that it does not. Every one of the 24 `<Link>` elements is either a block-level card link
(`BookList`), a navigation link (`Pagination`, `SessionMenu`), or alone inside its own `<p>`
(`EmptyState`, `not-found`, `book`, `search`, and the account screens' link lists). Every
`<button>` is a standalone control. None sits between words in a sentence.

That makes the requirement satisfiable exactly as written, and it is worth recording why, because
the moment somebody writes a sentence with a link in the middle of it, FR-012 becomes
unsatisfiable and needs the WCAG exception added deliberately rather than discovered in a failing
test.

The pagination links are the ones that actually change: they are currently
`padding: 0.4rem 0.7rem` on an inline-block, which is roughly 30 pixels tall.

**Alternatives considered**:

- Adopting WCAG 2.5.8's inline exception up front. Defensible, and it weakens a requirement the
  site currently has no need to weaken.
- The 24 pixel AA minimum instead of 44. The spec chose 44, and the product's audience is the
  reason.

## R8. Enforcing "defined once, referred to by name"

**Decision**: `frontend/scripts/check-design-tokens.mjs`, run from `pnpm lint` after oxlint. It
does two things: it fails if any stylesheet other than `tokens.css` contains a colour literal
(hex, `rgb(`, `hsl(`, or a named colour), and it recomputes every contrast pair the design
depends on from the values in `tokens.css`, failing if any drops below its floor.

**Rationale**: FR-016 and FR-017 are the requirements most likely to decay, because nothing about
a hard-coded colour hurts until the sixth one. A check is worth having. The question is what it
costs. Stylelint would bring a dependency, a configuration file, a plugin choice and a second
pass in the CI job to express a rule that is one regular expression, which is exactly the trade
Principle VII tells us to refuse.

Folding the contrast matrix into the same script is what makes FR-006 a build-time guarantee for
all text on opaque regions, rather than something the Playwright suite samples at runtime. The
two are complementary: the script proves the tokens are sound, and `contrast.spec.ts` (R9) proves
the rendered page actually uses them.

The frontend CI job runs `format:check`, `lint` and `build`, so hanging this off `lint` puts it
in front of every pull request with no workflow change.

**Alternatives considered**:

- Stylelint with `declaration-property-value-disallowed-list`. The standard answer, and heavier
  than the problem.
- A code review convention. This is the thing that decays.
- Type-safe tokens in TypeScript with generated CSS. Real benefits at a much larger scale, and it
  would put a build step between editing a colour and seeing it, which is the opposite of SC-007.

## R9. Measuring contrast "over the decorated ground"

**Decision**: Two checks, because the phrase covers two different situations. For text on opaque
regions, which after R1 is all of it, the ratio is computed from tokens by the script in R8. For
the ground itself, `contrast.spec.ts` screenshots the ground with the content hidden, samples the
pixels, and asserts that the observed luminance stays inside the band the tokens declare between
`--ground-grain-lo` and `--ground-grain-hi`.

**Rationale**: FR-006 asks for the worst case behind the text rather than an average, and the
honest reading of that is a pixel measurement. But R1 forces every text-bearing region to be
opaque, which means the true worst case behind any text is that region's flat fill, and a pixel
sample would only ever confirm the arithmetic. Sampling all text would be slow, flaky at
subpixel boundaries, and would prove less than the arithmetic already does.

What the arithmetic cannot prove is that the grain stayed within the range the tokens claim.
Turbulence output depends on the filter parameters, the compositing mode and the alpha, and any
of those can be edited without touching a colour token. So the pixel sample is pointed at the
ground rather than at the text: it establishes that the band is real, and the band is what every
"on ground" figure in the contrast matrix is computed against. Together the two checks cover
FR-006 completely, with the expensive one doing only the part the cheap one cannot.

**Alternatives considered**:

- Sampling every text node's backdrop. The literal reading, and slower and flakier for no gain
  once regions are opaque.
- Trusting axe alone. R1 is why not: over a decorated ground axe reports incomplete, not pass.
- Computing the composite mathematically from the turbulence parameters. Turbulence is a
  pseudo-random function; predicting its extrema analytically is far harder than reading them off
  a rendered frame.

## R10. Removing the starter styling

**Decision**: Delete `frontend/src/index.css`. Move nothing out of it.

**Rationale**: FR-018 asks for the starter's styling to go, and this file is all of it: a purple
accent, a `#social` block, a `.counter`, a `#root` fixed at 1126 pixels, and a
`prefers-color-scheme` dark block for a light theme this product does not have. No module imports
it, which is verifiable with a single grep, so deleting it changes nothing at runtime.

It does document one live defect. `catalogue.css` and `account.css` reference `var(--border)`
twice, and `index.css` was the only file defining it. Because nothing imports that file, those
declarations resolve against an undefined custom property, become invalid at computed-value time,
and fall back to `currentColor`. The borders on the search field and the listening notice are
therefore currently the text colour by accident rather than by choice. `tokens.css` gives them a
real definition, so this is fixed as a consequence of the feature rather than as a separate
change.

**Alternatives considered**:

- Rewriting `index.css` into the token file. Same outcome with a confusing history: the file's
  name and its contents would have nothing to do with each other.
- Leaving it as dead code and noting it. What the house rule normally requires, and FR-018
  explicitly asks for its removal, so it is in scope.

## R11. The session menu is unstyled on catalogue pages

**Decision**: Move the shared furniture, `.session-menu` and the global `:focus-visible` rule,
out of `account.css` into `base.css`, which `root.tsx` imports once for every route.

**Rationale**: This is a defect that already exists and that FR-019 leaves nowhere to hide.
`SessionMenu` renders on every page from `root.tsx`, but `.session-menu` is declared in
`account.css`, and only the five account routes import that file. On the catalogue, the book
page, search and the not-found page the session menu therefore has no layout at all, and its
links fall back to the user agent focus ring because the `:focus-visible` rule sits in the same
unimported file.

Nothing catches it today: `catalogue.spec.ts` asserts a focus indicator only on
`a.catalogue__link`. On the current white ground the consequence is cosmetic. On navy the missing
focus ring is an accessibility failure, so the new `target-size.spec.ts` and the focus assertions
cover every interactive element on the page rather than a named subset.

**Alternatives considered**:

- Importing `account.css` from the catalogue routes. Spreads the problem instead of fixing it.
- Leaving the split and duplicating the rules. Directly contrary to FR-016.

## R12. What the decoration costs, against the budget it must not spend

**Decision**: Accept two subset `woff2` files at roughly 40 KB each, about 1.4 KB of inline SVG
per document, and a grain tile of under 400 bytes as a data URI. Hold the whole feature to no
more than a 15% share of the 3 second budget SC-010 defends.

**Rationale**: SC-010 exists because a generated grain and a repeated watermark are exactly the
kind of decoration that quietly costs a second. At 1.6 Mbps, roughly 200 KB per second, 80 KB of
fonts is about 0.4 seconds of transfer, and because `font-display: swap` is in force none of it
blocks text from appearing, so it costs nothing against "readable". The inline SVG is in the
prerendered document and compresses well. The grain tile costs no request at all.

The one real risk is paint rather than transfer, which is why R2 tiles the turbulence instead of
filtering the viewport.

**Alternatives considered**:

- Deferring the fonts behind a media query or a script. Adds a second render pass and a flash for
  a saving `swap` already provides.
- Inlining the fonts as data URIs in the CSS. Blocks the stylesheet on the full font payload,
  which is strictly worse.

## R13. The cover art placeholder on a dark ground

**Decision**: Redraw `.cover-art--placeholder` from the token palette, as a `--surface-raised`
fill with a `--border-structural` edge and diagonal hatching in the same family, replacing the
current black-on-transparent striping.

**Rationale**: The placeholder is currently
`repeating-linear-gradient(45deg, rgb(0 0 0 / 6%) ...)` over `rgb(0 0 0 / 4%)`. Both are black at
very low alpha, which is a visible tint on white and invisible on navy, so on the new ground the
placeholder would become an empty hole where a cover should be. FR-007 names the placeholder
explicitly among the non-text elements that must reach 3:1 against what is adjacent.

Nothing about how it is announced changes: `CoverArt` already renders it with no accessible name
because the entry around it is a single link naming the book and its author, and that is
unchanged.

**Alternatives considered**:

- Inverting the existing gradient to white at low alpha. Works, and reintroduces two colour
  literals that FR-017 forbids.
- A generated cover per book, from the title. A new capability, which FR-015 forbids.
