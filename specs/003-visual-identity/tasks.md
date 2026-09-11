---
description: "Task list for Visual Identity"
---

# Tasks: Visual Identity

**Input**: Design documents from `specs/003-visual-identity/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/design-tokens.md](./contracts/design-tokens.md)

**Tests**: Included. The spec's own Assumptions commit this feature to adding checks for contrast
over the decorated ground, for the increased contrast mode and for target size, and Principle I
is not satisfied by automation alone, so US2 also carries a manual screen reader pass. There is a
second reason particular to this feature: its whole risk is silent. A decorative layer that
reaches assistive technology, a region that loses its opacity, a font subset missing ґ, all of
them look correct on screen. The tests are the only thing that can see them.

**Organization**: Grouped by user story, implemented **sequentially** in priority order. Two
honest deviations from story independence are worth stating up front rather than discovering:

- The token file and the lint script that enforces it are US4's payload, but they sit in Phase 2
  because every other story is written against them. Building the restyle first and enforcing
  the rule afterwards would mean writing colour literals and then removing them. What is left in
  US4's own phase is its verifiable claim, SC-007, plus the sweep that proves nothing slipped.
- US2's `aria-hidden` treatment is built into `Ground.tsx` in US1, because a decorative layer
  cannot sensibly be created and then hidden as a second step. US2's phase is therefore the
  verification that nothing was lost, and it is where the cross-cutting accessibility
  obligations that no story owns outright, SC-004 and SC-006, are proved.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel, different files and no dependency on incomplete work
- **[Story]**: Which user story the task serves
- File paths are exact

## Path Conventions

Web application, frontend only. Everything is under `frontend/`, per the structure decision in
`plan.md`. `backend/` is not opened by this feature.

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 [P] Obtain Fixel Text and Fixel Display (variable) from `https://fixel.macpaw.com/` and subset both into `frontend/public/fonts/fixel-text.woff2` and `frontend/public/fonts/fixel-display.woff2` using the `pyftsubset` command in `quickstart.md`, with `--unicodes='U+0000-00FF,U+0100-017F,U+0400-045F,U+0490-0491,U+2000-206F,U+2116,U+20B4'`. `U+0490-0491` is ґ and Ґ and sits outside the main Cyrillic block, so a subset built from a "cyrillic" preset loses it silently. Commit the SIL Open Font License text alongside as `frontend/public/fonts/OFL.txt`. Confirm each file is under 50 KB, which is what R12 budgets against SC-010
- [X] T002 [P] Delete `frontend/src/index.css`. Confirm with `grep -rn "index.css" frontend/src` that no module imports it first: it is the Vite starter's own stylesheet, carrying a `#social` block, a `.counter` and a `#root` fixed at 1126 pixels, and it is the whole of what FR-018 asks to be removed. R10
- [X] T003 [P] Create `frontend/scripts/` and change the `lint` script in `frontend/package.json` to `oxlint && node scripts/check-design-tokens.mjs`. The frontend CI job already runs `pnpm lint`, so this puts the token rules in front of every pull request with no workflow change

---

## Phase 2: Foundational (Blocking Prerequisites)

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 Create `frontend/src/styles/tokens.css` declaring every token from `data-model.md` on `:root` and nowhere else: the thirteen colour roles at their exact values, `--space-1` through `--space-12`, `--text-xs` through `--text-2xl`, `--radius-*`, `--shadow-*`, `--font-text`, `--font-display`, and the three thresholds `--target-min` (44px), `--focus-width` (3px) and `--focus-offset` (2px). No `--surface*` value may carry an alpha channel; contracts section 3 explains that opacity here is what keeps axe computing contrast at all
- [X] T005 Create `frontend/src/styles/base.css` holding everything that is true of every page: the two `@font-face` declarations with `font-display: swap` and a metric-overridden fallback family using `size-adjust`, `ascent-override` and `descent-override`; `color-scheme: dark` on `:root` so native fields, buttons and scrollbars stop rendering in the platform's light defaults (FR-013); `background-color: var(--ground)` on the document so the flat fill is what shows whenever the decorative layer is absent (FR-010); and body typography set for reversed text, meaning `--text` rather than white and line and letter spacing opened up, because light text on a dark ground blooms and that is painful rather than merely untidy for readers with astigmatism (FR-024, R6)
- [X] T006 Add the global focus rule to `frontend/src/styles/base.css`: an unscoped `:focus-visible` setting `outline: var(--focus-width) solid var(--focus)` with `outline-offset: var(--focus-offset)`. Unscoped is the point. The current rules are scoped to `.account`, `.session-menu` and a handful of catalogue selectors, so anything focusable outside those lists falls back to the user agent ring, which on navy is a dark blue that fails 3:1 outright. FR-011, R6
- [X] T007 Move the shared page furniture into `frontend/src/styles/base.css` and delete it from `frontend/src/styles/account.css` and `frontend/src/styles/catalogue.css`: the `.session-menu` block and the two `:focus-visible` rule sets. `SessionMenu` renders on every page from `root.tsx`, but `.session-menu` is declared in `account.css`, which only the five account routes import, so today the session menu has no layout at all on the catalogue, book, search and not-found pages and its links have no focus indicator. R11, and FR-019 leaves nowhere to hide it
- [X] T008 Import `./styles/tokens.css` and `./styles/base.css` from `frontend/src/root.tsx`, in that order, so that every route and every prerendered document receives them. `tokens.css` first because `base.css` reads from it
- [X] T009 Implement `frontend/scripts/check-design-tokens.mjs` with the six checks listed in `contracts/design-tokens.md` section 5: reject a colour literal in any stylesheet other than `tokens.css`; reject an alpha channel on any `--surface*` token; recompute every pair in the `data-model.md` contrast matrix from the values in `tokens.css` and fail any below its floor; fail a grain span above its ceiling; fail a `var(--name)` reference that `tokens.css` does not define; fail an `outline: none` anywhere. Use the WCAG 2.x relative luminance formula. The tightest pair is the control border against a raised surface at 3.33:1 against a 3:1 floor, so the arithmetic has to be right rather than approximately right

**Checkpoint**: Tokens exist, are enforced, and every page already renders on the flat navy ground with legible text and a visible focus ring. No decoration yet.

---

## Phase 3: User Story 1 - The site looks like a finished product (Priority: P1) 🎯 MVP

**Goal**: Every page the site serves moves onto the decorated navy ground, with content on opaque
regions and the interface set in Fixel, and nothing about what any page says or does changes.

**Independent Test**: Open each page in scope and confirm the ground, its grain and its watermark
are present, that content sits in delineated regions above them, and that every route, control
and word is identical to what it was before.

### Tests for User Story 1

> Write these first. `contrast.spec.ts` will fail until regions are opaque, which is the point.

- [X] T010 [P] [US1] Create `frontend/tests/a11y/contrast.spec.ts` asserting two separate things on every page in scope. First, that axe returns zero critical and serious violations **and zero `incomplete` results for the `color-contrast` rule**. The second half is the one that matters: axe stops resolving a background at the first opaque element and treats a `background-image` as a stop that marks the result incomplete, so a region that loses its opacity turns every contrast result from pass to incomplete while the suite stays green. Second, screenshot the ground with content hidden, sample its pixels, and assert the observed luminance stays inside the band declared by `--ground-grain-lo` and `--ground-grain-hi`. R1 and R9, and together they are SC-002
- [X] T011 [P] [US1] Create `frontend/tests/a11y/typography.spec.ts` asserting that every character the interface renders is drawn from Fixel rather than substituted, by comparing rendered glyph advance widths against the fallback family, with і, ї, є and ґ named explicitly (SC-011); and that every `@font-face` in force resolves `font-display` to `swap`, so no page shows empty space where words should be while a face is arriving (SC-012). A missing glyph does not error, it silently substitutes, which in a Ukrainian interface is easy to miss for months

### Implementation for User Story 1

- [X] T012 [US1] Create `frontend/src/components/Ground.tsx` rendering the decorative layer exactly as `contracts/design-tokens.md` section 2 specifies: a single `<div class="ground" aria-hidden="true">` containing a grain div and an inline `<svg aria-hidden="true" focusable="false">` whose `<defs><pattern patternUnits="userSpaceOnUse" patternTransform="rotate(-24)">` holds one `<text>` reading `ZALIZNA biblioteka`, filled across a `<rect width="100%" height="100%">`. The watermark **must** be inline SVG and not a `background-image`: an SVG referenced as an image renders in an isolated document that cannot reach the page's webfonts, so the wordmark would fall back to a generic family and FR-025 would fail with nothing visibly wrong. Carry that reason as a comment in the file; it is the single most breakable decision in the feature. R3
- [X] T013 [US1] Create `frontend/src/styles/ground.css`: position the ground layer `fixed`, `inset: 0`, `z-index: -1`, `pointer-events: none`; paint the grain from an `feTurbulence` `type="fractalNoise"` rendered into a 160 by 160 tile inlined as a `url("data:image/svg+xml,...")` background and repeated, composited at low alpha over `--ground`; and set the watermark `<text>` in `var(--font-display)` at a size small enough that no instance is the largest text on any page (FR-003). Tile and repeat rather than filtering the viewport: a viewport-sized `feTurbulence` is re-evaluated whenever its box changes, which on a mobile browser includes every address bar collapse. No animation, no transition, no `will-change` (FR-014). R2
- [X] T014 [US1] Mount `<Ground />` in `frontend/src/root.tsx` as the first child of `<body>`, a sibling of the content and never an ancestor of it, and import `./styles/ground.css`. R1 records that the sibling placement is not what protects the contrast check, the opaque regions are; it is kept because it keeps the filter's stacking context off the content subtree
- [X] T015 [P] [US1] Rewrite `frontend/src/styles/catalogue.css` against the tokens, covering the catalogue list, pagination, the book detail page, the search form and results, the not-found page and the three `.state` blocks. Every element that holds text takes a fully opaque `background-color: var(--surface)`, with `--surface-raised` where a region nests. Replace every remaining literal and ad hoc spacing value with a token, including the two `border: 1px solid var(--border)` declarations that currently resolve against an undefined custom property and fall back to `currentColor` by accident (R10). Preserve every existing comment's constraint: no fixed heights that clip a wrapped line, no `overflow: hidden` over live text, `overflow-wrap: anywhere` on long titles
- [X] T016 [P] [US1] Rewrite `frontend/src/styles/account.css` against the tokens for all five account screens. Field labels, typed text, placeholder and hint text, the password policy description and every status message take a token colour meeting FR-006 for their size (FR-021). Give controls `--border-control` rather than `--border-structural`, because on a text field the boundary is the affordance and nothing else says where the field is. Keep `--text-base` at 1rem on `.field__input`: below 16 pixels iOS zooms the page when a field takes focus and leaves a screen magnifier user somewhere they did not ask to be. Keep the `.field__error::before` word marker and the `aria-invalid` border weight, because colour must not become the only signal that a field was rejected (FR-022)
- [X] T017 [P] [US1] Redraw the `.cover-art--placeholder` rule used by `frontend/src/components/CoverArt.tsx` from the token palette: a `--surface-raised` fill with a `--border-structural` edge and diagonal hatching in the same family. It is currently `repeating-linear-gradient(45deg, rgb(0 0 0 / 6%) ...)` over `rgb(0 0 0 / 4%)`, black at very low alpha, which is a visible tint on white and invisible on navy, so on the new ground it would become an empty hole where a cover should be. FR-007 names the placeholder explicitly. Do not change how it is announced: it carries no accessible name because the entry around it is a single link naming the book and its author. R13
- [X] T018 [US1] Run `pnpm lint` and fix every literal the token script rejects across `catalogue.css`, `account.css`, `base.css` and `ground.css`. This is the first point where FR-017 is machine-checked rather than intended
- [X] T019 [US1] Run `pnpm test:a11y` and make T010 and T011 pass. If `contrast.spec.ts` reports `incomplete` results, a region is translucent: find it rather than relaxing the assertion, because the assertion is the only thing standing between this feature and a green suite that checks nothing

**Checkpoint**: Every page in scope carries the ground, content sits on opaque regions, the interface is in Fixel, and contrast is proved by measurement rather than by eye.

---

## Phase 4: User Story 2 - Nothing decorative reaches assistive technology (Priority: P2)

**Goal**: The site announces exactly what it announced before, the decoration is unreachable, and
the accessibility guarantees that the restyle could have quietly cost are proved intact.

**Independent Test**: Run the existing audit and a screen reader pass over each page in scope and
compare the announced output against the same pass before the feature. Any difference is a
failure.

### Tests for User Story 2

- [X] T020 [P] [US2] Create `frontend/tests/a11y/target-size.spec.ts` walking every interactive element on every page in scope and asserting a bounding box of at least 44 by 44 device-independent pixels (SC-006) and a non-zero `outlineWidth` under focus (SC-004). Assert across every interactive element rather than a named subset: `catalogue.spec.ts` today checks the outline only on `a.catalogue__link`, which is exactly why the session menu's missing indicator went unnoticed. FR-011, FR-012
- [X] T021 [P] [US2] Extend `frontend/tests/a11y/contrast.spec.ts` with an assertion that the ground element and everything inside it is absent from the accessibility tree, that no element within it is focusable, and that tabbing through a page reaches the same elements in the same order as before. FR-008, and US2 acceptance scenarios 1 and 2

### Implementation for User Story 2

- [X] T022 [US2] Give the pagination links a 44 by 44 target in `frontend/src/styles/catalogue.css`, using padding rather than a bare `min-height` so the hit area grows with the box. They are currently `padding: 0.4rem 0.7rem` on an inline-block, roughly 30 pixels tall, and they are the smallest targets on the site
- [X] T023 [US2] Bring every remaining control to `--target-min` across `frontend/src/styles/catalogue.css` and `frontend/src/styles/account.css`: the search field and its button, the retry button in the error state, the sign-out button, and the standalone links in `EmptyState`, `not-found`, `book`, `search` and the account screens' link lists. R7 records that this is achievable with no exception only because no link on this site sits mid-sentence; if one is ever added, FR-012 needs the WCAG 2.5.8 inline exception added deliberately rather than discovered in a failing test
- [X] T024 [US2] Run the full existing suite, `frontend/tests/a11y/catalogue.spec.ts`, `book.spec.ts`, `search.spec.ts` and the five `account-*.spec.ts` files, unmodified. They must pass as written. If any needs an edit, the feature has changed behaviour and FR-015 says it must not; fix the styling rather than the test
- [ ] T025 [US2] Manual screen reader pass on VoiceOver and TalkBack over every page in scope, following the procedure in `specs/001-browse-catalogue/manual-verification.md` and comparing against what it records. The specific thing being listened for is silence: the wordmark repeats dozens of times per page, and if it is reachable at all it will be unmistakable. Record the result in `specs/003-visual-identity/manual-verification.md`. SC-003, and Principle I is not satisfied without it

**Checkpoint**: The decoration is inaudible and unreachable, focus is visible everywhere, every target is hittable, and the eight existing specs pass untouched.

---

## Phase 5: User Story 3 - A visitor who needs more contrast gets a plain ground (Priority: P3)

**Goal**: A visitor who asks their system for increased contrast, or whose system supplies its
own colours, gets a plain solid ground with every other guarantee intact.

**Independent Test**: Set the platform to request increased contrast, open each page in scope,
and confirm no grain and no watermark render, that the ground is a plain solid fill, and that
every contrast obligation still holds.

### Tests for User Story 3

- [X] T026 [P] [US3] Create `frontend/tests/a11y/high-contrast.spec.ts` running every page in scope under `page.emulateMedia({ contrast: 'more' })` and again under `{ forcedColors: 'active' }`, asserting the ground element is not rendered, that axe still reports zero violations and zero `color-contrast` incompletes, and that the focus indicator is still visible. `playwright-core@1.63.0` carries both options, so SC-005 needs no manual pass. R5
- [X] T027 [P] [US3] Extend `frontend/tests/a11y/high-contrast.spec.ts` with SC-009: every page in scope reflows without horizontal scrolling of the page body at a 320 device-independent pixel viewport and at 200% zoom, in both the normal and the increased contrast modes

### Implementation for User Story 3

- [X] T028 [US3] Add `@media (prefers-contrast: more)` and `@media (forced-colors: active)` blocks to `frontend/src/styles/ground.css`, both setting the ground element to `display: none`. Drop the whole container rather than the two layers individually: that makes FR-010's failure case identical to FR-009's deliberate one, since in both cases the flat `--ground` fill on the document is what remains. `forced-colors: active` also silently discards most `background-image` declarations, which is why the grain has to be dropped explicitly rather than left to be overridden. R5
- [X] T029 [US3] Add a `@media (forced-colors: active)` block to `frontend/src/styles/base.css` redeclaring the focus indicator in system colours, `Highlight` against `CanvasText`, so it survives the palette being replaced wholesale. An `outline` survives forced colours where a `box-shadow` ring would vanish entirely, which is why R6 chose the outline
- [X] T030 [US3] Verify FR-010's failure path by hand: comment out the `<Ground />` mount in `frontend/src/root.tsx`, load each page in scope, and confirm the flat `--ground` fill shows with every contrast obligation intact and nothing visibly broken. Restore the mount. This is the case no media query can emulate and no test can force

**Checkpoint**: All three ways the decoration can be absent, asked for, forced, and failed, produce the same legible plain ground.

---

## Phase 6: User Story 4 - One place to change a visual decision (Priority: P4)

**Goal**: A colour role or a spacing step is defined once and changed once, and the rule is
enforced rather than trusted.

**Independent Test**: Change one named colour and one named spacing step, then confirm exactly
one location was edited and that the change appears on every page that uses it.

### Implementation for User Story 4

- [X] T031 [US4] Verify SC-007 as its own exercise: change `--surface` in `frontend/src/styles/tokens.css` to a visibly different navy, run `pnpm build`, open every page in scope, and confirm all of them followed from that one edit. Repeat with `--space-4`. Revert both. If any page did not follow, it is holding a literal the script missed and the script needs the gap closed, not the page patching
- [X] T032 [US4] Audit `frontend/src/styles/catalogue.css`, `account.css`, `base.css` and `ground.css` by hand for values the script cannot catch: a spacing value expressed as a multiple that happens to match a step, a font size written in `px`, a `currentColor` standing in for a role that should be named. The script catches literals, not laziness
- [X] T033 [US4] Confirm `frontend/scripts/check-design-tokens.mjs` actually fails on each of its six conditions by introducing each violation in turn and watching `pnpm lint` reject it. A check nobody has seen fail is a check nobody knows works, and four of these six exist specifically to catch silent regressions

**Checkpoint**: Every visual decision is named, defined once, and mechanically prevented from being duplicated.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T034 [P] Run the full CI set locally exactly as `.github/workflows/ci.yml` does: `dotnet format backend/VoxLib.slnx --verify-no-changes`, `dotnet test backend/VoxLib.slnx`, then `pnpm format:check && pnpm lint && pnpm build`. The backend commands belong in this list because SC-008 is the claim that nothing behind the presentation moved, and an untouched suite still passing is how that is shown
- [X] T035 [P] Measure SC-010 by hand: throttle to 1.6 Mbps down with 150 ms round-trip latency, load `/` with an empty cache, and confirm book titles are readable within 3 seconds. `swap` means text appears in the fallback first, so "readable" is the bar rather than "in Fixel". Compare against the R12 figures: roughly 80 KB of fonts, about 1.4 KB of inline SVG, a grain tile under 400 bytes
- [X] T036 [P] Confirm the prerendered documents carry the ground: run `pnpm build`, open a built document from `frontend/build/client/` directly with JavaScript disabled, and check that the flat fill, the grain and the watermark all render. Both layers are markup and CSS rather than script precisely so this works, and `tests/prerender/prerender.spec.ts` is the existing guard on what those documents contain
- [X] T037 Review the whole diff against FR-015: no route added, changed or removed; no handler touched; no string altered; no DOM structure changed except the single `<Ground />` sibling. Any TSX diff outside `root.tsx`, `Ground.tsx` and `CoverArt.tsx` is a signal to look twice
- [X] T038 Update `CLAUDE.md` with the two gotchas this feature discovered, in the Gotchas section: that axe abandons the colour contrast check at the first `background-image` and marks the result incomplete rather than failed, so a translucent region over the decorated ground silently converts every contrast pass into an unchecked incomplete; and that an SVG referenced as an image cannot reach the page's webfonts, so a watermark drawn as a `background-image` renders in a fallback family with nothing visibly wrong. Both are the kind of thing that costs an afternoon on the second encounter
- [X] T039 Run the `quickstart.md` validation end to end, including the by-eye pass over all seven page types, forcing the empty and error states by stopping the API mid-session. Those are the shortest pages and the ones most likely to leave a single line stranded on the texture

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies. T001, T002 and T003 touch different files and can
  run together
- **Foundational (Phase 2)**: Depends on Setup. **Blocks every user story.** T004 blocks
  T005 through T009, because everything reads from the token file
- **US1 (Phase 3)**: Depends on Phase 2. This is the MVP
- **US2 (Phase 4)**: Depends on US1. The decoration has to exist before it can be proved
  inaudible, and the regions have to exist before targets can be sized against them
- **US3 (Phase 5)**: Depends on US1. Cannot drop a decoration that has not been built
- **US4 (Phase 6)**: Depends on US1 through US3, because it audits what they produced. Its
  enforcement mechanism was delivered in T009
- **Polish (Phase 7)**: Depends on all four stories

### Within Each User Story

- Tests are written before the implementation they check. `contrast.spec.ts` failing on
  `incomplete` results is the expected starting state
- `Ground.tsx` (T012) before `ground.css` (T013) before mounting it (T014)
- The stylesheet rewrites (T015, T016, T017) are independent of each other and of the
  ground layer
- The lint sweep (T018) and the test run (T019) come last in US1, because both are checks
  on everything before them

### Parallel Opportunities

- T001, T002, T003 across three separate files
- T015, T016, T017 are three different stylesheets, and T010, T011 are two different specs
- T020 and T021 are different files
- T026 and T027 are the same file and are therefore **not** parallel, despite both being tests
- T034, T035, T036 are three independent verification passes

---

## Parallel Example: User Story 1

```bash
# The two new specs, written before the implementation they check:
Task: "Create frontend/tests/a11y/contrast.spec.ts"
Task: "Create frontend/tests/a11y/typography.spec.ts"

# The three stylesheet rewrites, once the ground layer exists:
Task: "Rewrite frontend/src/styles/catalogue.css against the tokens"
Task: "Rewrite frontend/src/styles/account.css against the tokens"
Task: "Redraw the cover art placeholder from the token palette"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1: Setup, three tasks, all parallel
2. Phase 2: Foundational, the token file and everything that reads from it. **Blocking**
3. Phase 3: User Story 1
4. **STOP and VALIDATE**: every page on the decorated ground, contrast measured, nothing
   said differently
5. This is demonstrable on its own. It is also the point of maximum risk, because
   everything US2 protects is already in place and not yet proved

### Incremental Delivery

1. Setup and Foundational: every page is already on the flat navy ground with legible text and a
   visible focus ring, before a single decorative pixel exists. Worth pausing at: if anything is
   wrong here, it is wrong in one file
2. US1: the identity arrives. Demo
3. US2: the accessibility guarantees are proved rather than assumed. This is the phase that
   decides whether the feature ships
4. US3: the plain ground for whoever asks for it, plus the two failure paths
5. US4: the rule is enforced and audited

### Notes

- `[P]` means different files and no dependency on incomplete work
- Commit after each task or logical group
- The riskiest single line in this feature is a `--surface` token with an alpha channel. It looks
  identical, and it turns SC-001 into a check of nothing. T009 rejects it, T010 catches it, and
  `contracts/design-tokens.md` section 3 explains it. That redundancy is deliberate
