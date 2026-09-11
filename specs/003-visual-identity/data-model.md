# Data Model: Visual Identity

**Feature**: `specs/003-visual-identity/` | **Date**: 2026-09-10

This feature stores nothing. Its "data" is the set of named visual decisions FR-016 requires,
and the obligations FR-006, FR-007 and FR-011 place on the pairs they form. Both are recorded
here because both are checked: `frontend/scripts/check-design-tokens.mjs` reads the values out of
`tokens.css` and recomputes every ratio in the matrix below on each `pnpm lint`.

Every ratio in this document was computed with the WCAG 2.x relative luminance formula. None was
judged by eye.

## Entities

### Visual token

One named visual decision. Defined exactly once, in `frontend/src/styles/tokens.css`, as a CSS
custom property on `:root`, and referred to by name everywhere else. The unit SC-007 measures:
changing one means editing one line.

Five kinds, distinguished by prefix so the lint script can tell them apart:

| Kind | Prefix | Example | Constrained by |
| --- | --- | --- | --- |
| Colour role | none | `--surface` | The contrast matrix below |
| Spacing step | `--space-` | `--space-4` | The scale below |
| Type size | `--text-` | `--text-lg` | The scale below |
| Corner radius | `--radius-` | `--radius-md` | Nothing measurable |
| Elevation | `--shadow-` | `--shadow-raised` | Nothing measurable |

### Colour role

What a colour is *for*, never what it is. A role names the job so the value can change without
every call site changing meaning. Thirteen roles, listed with their values below.

Two roles exist that a smaller palette would merge, and the split is deliberate. `--ground` and
`--surface` are separated because a region is delineated by its fill rather than by its outline.
`--border-structural` and `--border-control` are separated because only the second one carries
meaning in the sense FR-007 uses: on a text field the boundary is the only thing saying where the
field is, whereas a card's outline is decoration over a fill that already delineates it. The
plan's post-design re-check records the reasoning.

### Ground

The composite backdrop, in three layers, from back to front:

1. The flat `--ground` fill, on the document.
2. The grain: an `feTurbulence` tile, repeated, compositing to a luminance excursion bounded by
   `--ground-grain-lo` and `--ground-grain-hi`.
3. The watermark: one `<pattern>` of `<text>` reading "ZALIZNA biblioteka", rotated -24 degrees.

Layers 2 and 3 live inside one `aria-hidden="true"` element and are removed together under
`prefers-contrast: more` and `forced-colors: active`, and absent by default if the element fails
to render. In all three of those cases layer 1 alone remains, which is FR-009 and FR-010.

**Validation**: the rendered luminance of the composite must stay within the band declared by
`--ground-grain-lo` and `--ground-grain-hi`. Asserted by `contrast.spec.ts`, not by arithmetic;
R9 explains why this one is measured rather than computed.

### Region

A delineated area holding content above the ground. Two variants, `--surface` and
`--surface-raised`, the second for a region nested inside another.

**Validation, and it is the load-bearing one**: a region's background colour must be fully
opaque. Not merely dark, not `rgb(... / 0.95)`. R1 establishes that a translucent region lets
axe's background walk reach the textured ground, at which point every contrast result on the page
degrades from pass to incomplete and SC-001 goes green while checking nothing. The lint script
rejects any `--surface*` value carrying an alpha channel.

### Wordmark

The brand mark "ZALIZNA biblioteka", in Latin letters, set in Fixel Display. In this feature it
appears only as repeated decorative texture. It is never interface copy, never announced, and
never the largest text on a page (FR-003).

## Colour roles and their values

```text
--ground             #070D22   the flat fill, and the whole ground under increased contrast
--ground-grain-lo    #04081A   darkest excursion the grain is permitted to reach
--ground-grain-hi    #0C1533   lightest excursion the grain is permitted to reach
--surface            #1A2654   a region holding content
--surface-raised     #25336C   a region nested inside another
--text               #E4E8F7   body text, off pure white by FR-024
--text-muted         #AFB8DC   secondary text: authors, durations, hints
--heading            #F0F3FC   headings
--link               #9CC7FF   links
--focus              #FFC857   the focus indicator, and nothing else on the page
--danger             #FF9AA8   the text of a rejected field or a failed action
--border-structural  #2B3768   decorative edges on regions that a fill already delineates
--border-control     #7684C6   the boundary of a control, where the boundary is the affordance
```

## Contrast matrix

Floors come from FR-006 (4.5:1 body, 3:1 large), FR-007 (3:1 meaningful non-text) and FR-011
(3:1 focus against both the element and its surroundings). "On ground worst" pairs are computed
against `--ground-grain-hi`, the lightest point the grain may reach, because that is the worst
case for light text.

| Pair | Measured | Floor | Source |
| --- | --- | --- | --- |
| body text on surface | 11.86:1 | 4.5:1 | FR-006 |
| body text on raised surface | 9.72:1 | 4.5:1 | FR-006 |
| muted text on surface | 7.39:1 | 4.5:1 | FR-006 |
| muted text on raised surface | 6.06:1 | 4.5:1 | FR-006 |
| heading on surface | 13.06:1 | 3:1 | FR-006, large text |
| link on surface | 8.30:1 | 4.5:1 | FR-006 |
| link on raised surface | 6.80:1 | 4.5:1 | FR-006 |
| error text on surface | 7.20:1 | 4.5:1 | FR-006, FR-021 |
| body text on ground, worst case | 14.68:1 | 4.5:1 | FR-006 |
| muted text on ground, worst case | 9.15:1 | 4.5:1 | FR-006 |
| focus vs surface | 9.42:1 | 3:1 | FR-011 |
| focus vs raised surface | 7.72:1 | 3:1 | FR-011 |
| focus vs ground, worst case | 11.66:1 | 3:1 | FR-011 |
| control border vs surface | 4.06:1 | 3:1 | FR-007 |
| control border vs raised surface | 3.33:1 | 3:1 | FR-007 |
| surface vs ground | 1.33:1 | 1.3:1 | FR-004, delineation |
| raised surface vs surface | 1.22:1 | 1.15:1 | FR-004, delineation |
| grain span, hi vs lo | 1.11:1 | 1.35:1 max | FR-002, grain not banding |

The tightest pair is the control border against a raised surface at 3.33:1. It is the one to
watch when any of `--border-control`, `--surface-raised` or `--surface` is edited, and it is why
the script recomputes rather than trusting this table.

The last two rows are floors of a different kind: they are the thresholds at which a region reads
as a distinct area rather than as a slightly different navy. They are not WCAG requirements and
have no standard behind them; they are this design's own commitment to FR-004's "clearly
delineated" and FR-005's "visible around and between". The grain span row is a ceiling rather
than a floor, because grain that spans too far stops reading as texture and starts reading as
banding.

## Spacing and type scales

One scale each, so that "a spacing step" in SC-007 is a real unit.

```text
--space-1   0.25rem      --text-xs    0.8125rem
--space-2   0.5rem       --text-sm    0.9375rem
--space-3   0.75rem      --text-base  1rem
--space-4   1rem         --text-lg    1.25rem
--space-5   1.5rem       --text-xl    1.75rem
--space-6   2rem         --text-2xl   2.25rem
--space-8   3rem
--space-12  4rem
```

`--text-base` stays at `1rem`, which is 16 pixels, because `account.css` already records why:
below 16 pixels iOS zooms the page when a field takes focus and leaves a screen magnifier user
somewhere they did not ask to be. That constraint survives the restyle.

Two values are not on either scale and are named separately because they are thresholds rather
than steps:

```text
--target-min     44px      FR-012, the minimum interactive box
--focus-width    3px       FR-011, with --focus-offset 2px
--measure-prose  45rem     how wide a passage of running text may get
```

`--measure-prose` was added during the T032 audit rather than at design time. The three
blocks of prose on the book page each carried `max-width: 45rem` at their call site, which
is one decision written three times and exactly what FR-016 exists to prevent. It forms no
constrained pair, so the contrast matrix is unchanged.

## Typography

```text
--font-text     'Fixel Text', <metric-matched fallback stack>
--font-display  'Fixel Display', <metric-matched fallback stack>
```

Fixel Text sets running text, Fixel Display sets headings and the wordmark (FR-025). Both are
loaded with `font-display: swap` from the site's own origin (FR-026). The fallback stack carries
`size-adjust`, `ascent-override` and `descent-override` so that the swap changes letterforms
without moving lines; R4 explains why that matters more here than usual.

**Validation**: every character the interface renders must come from Fixel, Ukrainian і, ї, є and
ґ included. Asserted by `typography.spec.ts` (SC-011), which is the check that catches a subset
built from the wrong Unicode ranges. A missing glyph does not error; it silently substitutes from
the fallback, and in a Ukrainian interface that is easy to miss for a long time.

## Relationships

```text
Ground        contains      grain layer + watermark layer   both aria-hidden, both droppable
Ground        sits behind   Region                          sibling, never ancestor (R1)
Region        must be       opaque                          what keeps axe computing contrast
Region        contains      text in a Colour role
Colour role   constrained   the contrast matrix above
Visual token  defined once  tokens.css                      FR-016, enforced by the lint script
Wordmark      set in        --font-display                  why the watermark is inline SVG (R3)
```
