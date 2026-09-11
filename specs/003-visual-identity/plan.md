# Implementation Plan: Visual Identity

**Branch**: `feature/visual-design` | **Date**: 2026-09-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-visual-identity/spec.md`

## Summary

Give the site a face without touching what it does. Every page the product serves today moves
onto one deep navy ground carrying a generated grain and the repeated diagonal wordmark
"ZALIZNA biblioteka", with all content sitting on opaque regions above it, set in Fixel. No
route, handler, string or DOM structure that carries meaning changes; the diff is a token file,
two decorative layers, the stylesheets, and the tests that hold the new obligations.

The plan turns on one finding from Phase 0, read out of the axe-core source rather than its
documentation. To find what is behind a text node, axe walks the stack of elements covering it
from the front and stops at the first opaque one; an element carrying a `background-image`
counts as a stop and additionally marks the result incomplete. So the moment a decorated ground
exists, any text not sitting on an opaque fill has its contrast result downgraded from pass to
incomplete. Nothing fails. SC-001 keeps reporting zero violations while proving nothing about
contrast, which is the worst outcome available: a green check over a dropped guarantee.

What protects the check is therefore the opaque region, not where the decoration is mounted, and
FR-004 already requires exactly that. Every text-bearing region carries a fully opaque fill, the
walk breaks there, and contrast stays computed. The decoration is still mounted as a fixed
sibling of the content rather than on `body`, for the separate reasons in R1: it keeps the filter
and its stacking context off the content subtree, and it keeps the content free of an inherited
`background-image` for any tool less careful than axe.

Two layers, generated two different ways, because each has a constraint the other does not.
The grain is a small SVG `feTurbulence` tile referenced as a CSS background and repeated, which
keeps the filter off the compositor's full-viewport path. The watermark is an inline
`<svg><pattern>` holding one `<text>` node, because an SVG referenced as an image renders in an
isolated document that cannot reach the page's webfonts, and FR-025 requires the wordmark to be
set in Fixel Display. Both layers sit inside one `aria-hidden` element and are dropped wholesale
under `prefers-contrast: more` and `forced-colors: active`.

The palette is not chosen by eye. Thirteen colour roles are fixed, every pair that FR-006, FR-007
and FR-011 constrain is computed, and the numbers are in [data-model.md](./data-model.md). The
tightest is the control boundary against a raised surface at 3.33:1 against a 3:1 floor; body
text sits at 11.86:1. A script re-computes all of them in `pnpm lint`, so the floor cannot be
crossed by editing a token.

## Technical Context

**Language/Version**: TypeScript 7.0 on Node 24. No backend change: `backend/` is untouched by
this feature.

**Primary Dependencies**: React 19.2, React Router 8.3.1 and Vite 8.2, all unchanged. Fixel
(Text and Display, SIL Open Font License) is added as two subset `woff2` files under version
control. No new runtime dependency, and deliberately no CSS framework, no CSS-in-JS and no
stylelint; see R8 for why the token rule is a twenty line script instead.

**Storage**: None. This feature writes nothing and reads nothing.

**Testing**: Playwright 1.63 with `@axe-core/playwright` 4.13, the suite that already covers the
catalogue, the book page, search and all five account screens. Four new specs cover the
obligations this feature adds: contrast over the decorated ground, the increased contrast and
forced colours modes, target size, and glyph coverage. `frontend/scripts/check-design-tokens.mjs`
runs inside `pnpm lint` and enforces FR-006 and FR-017 statically. The existing backend tests are
the control: they must pass untouched, which is SC-008.

**Target Platform**: The same one origin as today, serving prerendered catalogue documents and a
single-page fallback for the account screens. The decoration has to render in a prerendered
document with no JavaScript, which is why both layers are markup and CSS rather than script.
VoiceOver on iOS and TalkBack on Android remain the platforms that decide whether this is done.

**Performance Goals**: No new goal, one budget defended. `001-browse-catalogue` committed the
catalogue to being readable within 3 seconds on 1.6 Mbps with 150 ms of latency, and SC-010
carries it forward. The decoration's whole cost is two subset `woff2` files at roughly 40 KB
each and about 1.4 KB of inline SVG, against a budget of roughly 600 KB. The grain tile is a
data URI, so it costs no request.

**Constraints**: Nothing decorative may be reachable, focusable or announced. Nothing may change
what a page says or how it is structured. Body text stays off pure white because reversed text
blooms. Every visual value is named once. The decoration must degrade to a flat fill on demand
and on failure.

**Scale/Scope**: Ten routes, one shared layout, three state components, two existing stylesheets
replaced by five, one new token file, two font files, four new test specs, one lint script. One
file is deleted.

No unresolved unknowns remain. Phase 0 is recorded in [research.md](./research.md).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluated against the six gates in `.specify/memory/constitution.md`.

**1. Accessibility.** PASS, and it is the whole substance of this plan rather than a box beside
it. The feature's risk runs entirely in this direction: a decorative layer is the classic way to
break a working screen reader experience, and this site works today. Four defences, each with a
test rather than an assurance. The decoration lives in one `aria-hidden="true"` element that
contains no focusable node, so US2 holds by construction. Contrast is computed from tokens
rather than judged, and the computation runs in `pnpm lint`. Focus indicators are specified
against the ground they now sit on, at 9.42:1, replacing a user agent default ring that would
be nearly invisible on navy. Target size is measured across every interactive element, which is
possible without exceptions only because no link on this site sits mid-sentence (R7).

Media session does not apply: this feature has no playback. Announcements are covered by the
stronger form of the rule, that they must not change at all, which SC-003 states and the
existing live region tests already assert.

Two accessibility defects that exist today are fixed because FR-019 leaves nowhere to hide them.
`.session-menu` is styled in `account.css`, which only the account routes import, so on every
catalogue page the session menu is unstyled and its focus indicator is the user agent default
(R11). And `.cover-art--placeholder` is drawn in black at 6% opacity, which disappears entirely
on a dark ground, so FR-007 forces it to be redrawn (R13).

**2. Dependency rule.** PASS, vacuously and worth stating. This feature adds no backend code,
touches no VoxLib project, and moves no logic. `VoxLib.Model` is not opened. The only rule the
frontend gains is that a stylesheet may not name a colour, which is enforced by a script rather
than by convention.

**3. Audio and access.** PASS by construction. No audio field, no storage interface, no signed
URL, no service worker. The feature cannot reach audio because it changes only presentation.

**4. HTTP.** PASS. No route is added, changed or removed; FR-015 forbids it and SC-008 measures
it. The two font files are static assets served from the site's own origin, which is FR-026 and
is also why no third party is contacted for them.

**5. Tests.** PASS. Every requirement names what proves it, and the mapping is in
[quickstart.md](./quickstart.md). The ones that carry the most weight: the token script fails the
build if any constrained pair drops below its floor; a Playwright spec samples the rendered
ground and asserts its luminance stays inside the declared band, which is what makes FR-006's
phrase "measured over the decorated ground" a measurement rather than a claim; a spec runs the
whole suite under `emulateMedia({ contrast: 'more' })` and again under
`forcedColors: 'active'`; and a spec walks every interactive element asserting a 44 by 44 box.

**6. Abstractions.** PASS. The plan adopts nothing from Principle VII's deferred list. It also
declines three things it could plausibly have taken: a CSS framework, a CSS-in-JS runtime, and
stylelint. Custom properties are the platform's own mechanism for FR-016 and need no library,
and the one rule worth enforcing is a grep (R8). The five stylesheets are a split of two existing
files along the lines the pages already follow, not a new architecture.

### Post-design re-check

Re-evaluated after the Phase 1 artifacts were written. All six gates still pass, and one tension
inside the spec surfaced during design and is resolved rather than waived.

FR-007 requires every meaningful non-text element to reach 3:1 against what is adjacent, and it
names "region edges" among them. Taken literally that puts every card outline at 3:1 against its
own fill, which on a dark ground reads as a heavy box drawn around everything and works against
FR-004's purpose. The resolution is to split the token rather than relax the number. A region is
delineated by its fill, which sits at 1.33:1 against the ground and is what the eye actually
reads as an edge, so its outline is structural and decorative at 1.28:1. A control boundary is
different in kind: on a text field the boundary is the affordance, and nothing else says where
the field is, so `--border-control` is held at 4.06:1 against a surface and 3.33:1 against a
raised surface. Both tokens exist, the script enforces the threshold on the second, and
[data-model.md](./data-model.md) records which elements may use which. No requirement is relaxed;
the reading of "carries meaning" is made explicit.

## Project Structure

### Documentation (this feature)

```text
specs/003-visual-identity/
├── plan.md                    # This file
├── spec.md                    # Feature specification
├── research.md                # Phase 0 output
├── data-model.md              # Phase 1 output, the token catalogue and contrast matrix
├── quickstart.md              # Phase 1 output, how each requirement is verified
├── contracts/
│   └── design-tokens.md       # Phase 1 output, the token and markup contract
├── checklists/
│   └── requirements.md        # Spec quality checklist
└── tasks.md                   # Phase 2 output, created by /speckit-tasks
```

### Source Code (repository root)

```text
frontend/
├── package.json                     # lint now also runs the token script
├── public/
│   └── fonts/
│       ├── fixel-text.woff2         # NEW, variable, subset to Latin + Ukrainian
│       └── fixel-display.woff2      # NEW, variable, subset to Latin + Ukrainian
├── scripts/
│   └── check-design-tokens.mjs      # NEW, enforces FR-006 and FR-017 in pnpm lint
├── src/
│   ├── index.css                    # DELETED, the Vite starter's own styling (FR-018)
│   ├── root.tsx                     # gains <Ground /> and the stylesheet imports
│   ├── components/
│   │   ├── Ground.tsx               # NEW, the aria-hidden decorative layer
│   │   ├── CoverArt.tsx             # placeholder redrawn for a dark ground
│   │   └── ...                      # unchanged otherwise
│   └── styles/
│       ├── tokens.css               # NEW, every visual decision, defined once
│       ├── base.css                 # NEW, document, typography, focus, targets
│       ├── ground.css               # NEW, the grain and watermark layers
│       ├── catalogue.css            # rewritten against the tokens
│       └── account.css              # rewritten against the tokens
└── tests/a11y/
    ├── contrast.spec.ts             # NEW, SC-002, samples the rendered ground
    ├── high-contrast.spec.ts        # NEW, SC-005, contrast: more and forced-colors
    ├── target-size.spec.ts          # NEW, SC-006
    ├── typography.spec.ts           # NEW, SC-011 and SC-012
    └── *.spec.ts                    # the eight existing specs, unchanged
```

**Structure Decision**: The existing frontend, unchanged in shape. The one structural addition is
`src/styles/tokens.css` as the single place FR-016 requires, imported once from `root.tsx` so
that every route and the prerendered documents all receive it. `Ground.tsx` is a component rather
than markup pasted into `root.tsx` only because it carries a comment explaining why it is a
sibling and not an ancestor, which is the single most breakable decision in the feature.

`index.css` is deleted rather than rewritten. It is the Vite starter's own stylesheet, still
carrying rules for a demo counter and a social bar this product never had, and no module imports
it (R10). Its deletion is FR-018, and it is not an unrelated cleanup: two live rules currently
reference `var(--border)`, which that orphan file was the only definition of, so the site is
today relying on an undefined custom property falling back to `currentColor`.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified

No violations. The plan adopts no deferred pattern and adds no project.

Four costs are recorded because they are the largest in the plan and the obvious places to push
back:

| Addition | Why needed | What it costs |
| --- | --- | --- |
| Two decorative layers generated two different ways, rather than one mechanism for both | An SVG referenced as an image renders in an isolated document that cannot reach the page's webfonts, so a watermark drawn that way would not be in Fixel and FR-025 would fail. Turbulence has no such problem and is much cheaper as a repeated tile than as a full-viewport filter | Two mechanisms to understand instead of one, and a comment in `ground.css` explaining why they differ |
| Every text-bearing region carries a fully opaque fill | Axe stops resolving the background at the first opaque element covering the text, and treats a `background-image` as a stop that marks the result incomplete. A translucent region lets the walk reach the ground, so every contrast result becomes "incomplete" and SC-001 reports zero violations while checking nothing | Regions cannot be tinted glass over the texture, which rules out the most obvious way to let the identity show through the content |
| Two subset font files under version control, plus a manual subsetting step | Fixel ships as full OTF. Unsubset, the pair would cost several hundred KB against a 600 KB budget, and SC-010 would fail. Subsetting at build time would put a Python font tool in the CI image for an input that changes about never | Roughly 80 KB in the repository, and a documented manual step in `quickstart.md` that runs again only when the typeface is updated |
| A bespoke twenty line lint script instead of stylelint | The only rule worth enforcing is that colours and spacing come from tokens, plus recomputing the contrast matrix. Stylelint brings a dependency, a config, a plugin ecosystem and a second lint pass in CI to express one grep and some arithmetic | A script nobody else maintains, which has to be kept in step with the token file it reads |
