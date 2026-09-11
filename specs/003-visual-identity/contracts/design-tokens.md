# Contract: Design Tokens and Decorative Layers

**Feature**: `specs/003-visual-identity/` | **Date**: 2026-09-10

This feature exposes no HTTP surface. Its interface is internal and has two halves: the token
names any stylesheet may use, and the markup contract the decorative layer and the regions must
honour. Both are stable names other code depends on, so both are written down here rather than
being whatever `tokens.css` happens to contain.

Values live in [data-model.md](../data-model.md). This document is the interface: what exists,
what may use it, and what breaks if it is used wrongly.

## 1. Token interface

Declared once on `:root` in `frontend/src/styles/tokens.css`. Any stylesheet may read any of
these. No stylesheet may declare one, and no stylesheet may use a colour or a spacing value that
is not one of these.

### Colour roles

| Token | May be used for | Must not be used for |
| --- | --- | --- |
| `--ground` | the document fill; the flat ground under increased contrast | any region that holds text |
| `--ground-grain-lo` / `--ground-grain-hi` | declaring the grain's permitted luminance band; the contrast matrix computes "on ground" pairs against `-hi` | painting anything directly |
| `--surface` | a region holding content | text, borders |
| `--surface-raised` | a region nested in another region | text, borders |
| `--text` | body text | large decorative text, headings |
| `--text-muted` | authors, durations, hints, the password policy | anything that is the only statement of a fact |
| `--heading` | `h1` to `h6` | body text |
| `--link` | links | plain text, which would make text look interactive |
| `--focus` | the focus indicator, and nothing else | any other element, ever: its value is that nothing else on the page is this colour |
| `--danger` | the text of a rejected field or a failed action | the *only* signal that something failed (FR-022) |
| `--border-structural` | decorative edges on regions | the boundary of any control |
| `--border-control` | the boundary of an input, button or other control | large fills, where it is too light |

### Scales

`--space-1` `-2` `-3` `-4` `-5` `-6` `-8` `-12` for every margin, padding and gap.
`--text-xs` `-sm` `-base` `-lg` `-xl` `-2xl` for every font size.
`--radius-sm` `-md` `-lg` for every corner.
`--shadow-region`, `--shadow-raised` for elevation.
`--font-text`, `--font-display` for every family.
`--measure-prose` (45rem) for the width of a passage of running text. Not a spacing step:
it is a decision about how far the eye travels before it has to find the start of the next
line, and it is named because three separate blocks of prose hold the same one.
`--target-min` (44px), `--focus-width` (3px), `--focus-offset` (2px) as thresholds, not steps.

### Rules a consumer must follow

1. No colour literal outside `tokens.css`. Not hex, not `rgb(`, not `hsl(`, not a named colour.
   Enforced by `check-design-tokens.mjs`; this is FR-017.
2. No spacing or size value invented at a call site. Use a scale step. `100%`, `0`, `1px` hairlines
   and intrinsic keywords such as `min-content` are not spacing values and are permitted.
3. A `--surface*` value must be fully opaque. See section 3.
4. `--focus` appears exactly once in the codebase, in the `:focus-visible` rule in `base.css`.
5. Adding a token means adding a row to the matrix in **both** `data-model.md` and
   `check-design-tokens.mjs` if it forms a constrained pair. The script holds the enforced copy
   rather than parsing the document: the matrix's "Pair" column is prose ("body text on
   surface"), so a script reading it would still need a prose-to-token mapping of its own, and
   would gain a fragile markdown parser and a dependency on `specs/` for nothing. `data-model.md`
   is the prose record and the script is what fails the build; the script recomputes every
   measured ratio from `tokens.css` rather than trusting either.

## 2. Ground markup contract

Rendered by `frontend/src/components/Ground.tsx`, mounted once in `root.tsx` as the first child
of `<body>`.

```text
<div class="ground" aria-hidden="true">        fixed, inset 0, z-index -1, pointer-events none
  <div class="ground__grain"></div>            background-image: the feTurbulence data URI tile
  <svg class="ground__mark" aria-hidden="true" focusable="false">
    <defs>
      <pattern id="…" patternUnits="userSpaceOnUse"
               patternTransform="rotate(-24)">
        <text>ZALIZNA biblioteka</text>          font-family: var(--font-display)
      </pattern>
    </defs>
    <rect width="100%" height="100%" fill="url(#…)"/>
  </svg>
</div>
```

Obligations:

- **A sibling of the content, never an ancestor.** R1 explains that this is not what protects the
  contrast check, but it is what keeps the filter's stacking context off the content subtree.
- **`aria-hidden="true"` on the container**, and nothing focusable inside it. This is FR-008 in
  full: no announcement, no reading order, no tab stop.
- **`focusable="false"` on the `<svg>`**, for the older behaviour where an SVG could take focus.
- **`pointer-events: none`**, so the layer cannot intercept a click meant for content.
- **The watermark must be inline SVG.** Not a `background-image`, not an `<img>`, not `content`.
  An SVG referenced as an image renders in an isolated document and cannot reach the page's
  webfonts, so the wordmark would silently render in a fallback family and FR-025 would fail with
  nothing visibly wrong. R3.
- **The whole element is `display: none`** under `prefers-contrast: more` and
  `forced-colors: active`. Dropping the container rather than the two layers individually is what
  makes FR-010's failure case identical to FR-009's deliberate one.
- **No animation, no transition, no `will-change`.** FR-014.

## 3. Region contract

Any element that holds text and sits above the ground.

```text
.region { background-color: var(--surface); }        fully opaque, no alpha
```

The opacity is not a style preference. Axe resolves what is behind a text node by walking the
stack of elements covering it and stopping at the first opaque one, treating any
`background-image` it meets as a stop that marks the result incomplete. An opaque region makes
the walk break at the region, so the ratio is computed normally. A region at
`rgb(26 38 84 / 0.95)` looks identical and lets the walk continue to the textured ground, at
which point every `color-contrast` result on the page becomes incomplete rather than pass. Since
SC-001 counts critical and serious violations, and incomplete is neither, the suite stays green
while the guarantee is gone.

This is the single easiest thing in the feature to break by accident and the hardest to notice,
which is why the lint script rejects an alpha channel on any `--surface*` token and why
`contrast.spec.ts` asserts that axe returned zero *incomplete* `color-contrast` results as well
as zero violations.

## 4. Focus contract

One rule, in `base.css`, applying to every focusable element:

```text
:focus-visible {
  outline: var(--focus-width) solid var(--focus);
  outline-offset: var(--focus-offset);
}
```

- No selector list. The current stylesheets scope focus rules to `.account`, `.session-menu` and
  a handful of catalogue selectors, which is why the session menu on catalogue pages has no
  indicator today (R11). FR-011 says every interactive element, so the rule is unscoped.
- Nothing may set `outline: none` anywhere, for any element, in any state.
- Under `forced-colors: active` the outline is redeclared in system colours so it survives the
  palette being replaced.

## 5. What the lint script checks

`frontend/scripts/check-design-tokens.mjs`, run by `pnpm lint`. It fails the build on any of:

| Check | Requirement |
| --- | --- |
| A colour literal in any stylesheet other than `tokens.css` | FR-017 |
| A `--surface*` token carrying an alpha channel | FR-004, and section 3 above |
| Any pair in the contrast matrix below its floor | FR-006, FR-007, FR-011 |
| The grain span above its ceiling | FR-002 |
| A token referenced by name that `tokens.css` does not define | FR-016, and the `var(--border)` defect R10 records |
| `outline: none` anywhere | FR-011 |

The last check exists because the failure it catches is invisible in review and total in effect.
