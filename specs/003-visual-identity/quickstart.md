# Quickstart: Verifying the Visual Identity

**Feature**: `specs/003-visual-identity/` | **Date**: 2026-09-10

How to run this feature and how each requirement is proved. Every success criterion in
[spec.md](./spec.md) maps to something runnable below, except the two that are manual by nature
and are marked as such.

## Prerequisites

Everything runs inside the dev container. The API must be up, because the catalogue reads it and
the prerender step asks it which books exist.

```bash
pnpm dev                     # API on :5080, web on :5173
```

Playwright reuses an already running dev server locally and starts one in CI.

## One-time setup: the typeface

Fixel ships as static OTF plus one full variable TTF, and must be subset before it is
committed. This runs once, and again only when the typeface is updated. It is deliberately
not part of the build; R4 in [research.md](./research.md) explains why.

```bash
# 1. Download the family from https://fixel.macpaw.com/ (the "Download" link is
#    https://fonts.macpaw.com/fonts/FixelAll.zip). Licence: SIL Open Font License.
#    Keep the licence file alongside the fonts.
unzip FixelAll.zip -x '__MACOSX/*' -d fixel

# 2. Fixel ships ONE variable font, FixelVariable/FixelVariable.ttf, not two.
#    Text and Display are not separate files: they are the two ends of its wdth
#    axis, Text at wdth=75 and Display at wdth=100, with wght 100-900 on both.
#    So each face is produced by pinning wdth first and subsetting the result.
for pair in "text:75" "display:100"; do
  name="${pair%%:*}"; wdth="${pair##*:}"

  pipx run --spec fonttools fonttools varLib.instancer \
    fixel/FixelVariable/FixelVariable.ttf "wdth=$wdth" \
    -o "Fixel-$name-var.ttf"

  pipx run --spec fonttools pyftsubset "Fixel-$name-var.ttf" \
    --output-file="frontend/public/fonts/fixel-$name.woff2" \
    --flavor=woff2 \
    --layout-features='kern,liga,tnum' \
    --unicodes='U+0000-00FF,U+0100-017F,U+0400-045F,U+0490-0491,U+2000-206F,U+2116,U+20B4' \
    --drop-tables+=DSIG
done

#    U+0490-0491 is ґ and Ґ; U+20B4 is the hryvnia sign.
#    Confirm the result is under ~50 KB each. It lands at roughly 48 KB.
ls -l frontend/public/fonts/
```

Pinning `wdth` rather than shipping the whole two-axis font is what keeps each file
under the budget: the unpinned variable font is 472 KB before subsetting, and a single
file serving both faces would have to carry both ends of the width axis to every page.
The `wght` axis survives, so `font-weight` still interpolates from 100 to 900 on both
faces and no second file is needed for bold.

The Ukrainian letters і, ї, є and ґ are why the ranges are spelled out rather than left
to a "cyrillic" preset. `U+0490-0491` sits outside the main Cyrillic block, and a subset built
without it loses ґ silently: the glyph is substituted from the fallback and nothing errors.
`typography.spec.ts` is the check that catches it.

## The full check, as CI runs it

```bash
dotnet format backend/VoxLib.slnx --verify-no-changes
dotnet test backend/VoxLib.slnx
pnpm format:check && pnpm lint && pnpm build
```

`pnpm lint` now runs oxlint and then `frontend/scripts/check-design-tokens.mjs`. The backend
commands are in the list because SC-008 is the requirement that nothing behind the presentation
moved, and the way to show that is that the untouched suite still passes.

## The accessibility suite

```bash
cd frontend
pnpm test:a11y                                  # everything
pnpm test:a11y tests/a11y/contrast.spec.ts      # one spec
pnpm exec playwright test --ui                  # watch it happen
```

The eight existing specs must pass unmodified. If a change to one of them looks necessary, that
is the feature changing behaviour and FR-015 says it must not.

## What proves what

| Criterion | Proved by | How it fails |
| --- | --- | --- |
| SC-001 zero violations | the eight existing specs, unmodified | a new violation, or an existing spec needing an edit |
| SC-002 contrast over the ground | `check-design-tokens.mjs` for text on regions; `contrast.spec.ts` for the ground's own luminance band | a token pair below its floor, or grain outside the declared band |
| SC-002 (silent regression) | `contrast.spec.ts` asserts zero *incomplete* `color-contrast` results | a region loses its opacity and axe stops computing contrast at all |
| SC-003 announcements unchanged | the existing live region assertions in `catalogue.spec.ts` and the account specs | any change to what is announced |
| SC-004 focus visible everywhere | `target-size.spec.ts` walks every interactive element and asserts a non-zero outline | a focusable element with no `:focus-visible` outline |
| SC-005 increased contrast | `high-contrast.spec.ts` under `emulateMedia({ contrast: 'more' })` and `{ forcedColors: 'active' }` | the ground element rendered when it should be gone |
| SC-006 target size | `target-size.spec.ts` asserts a 44 by 44 box | a control smaller than the minimum |
| SC-007 one place to change | `check-design-tokens.mjs` rejects colour literals outside `tokens.css` | a hard-coded colour anywhere |
| SC-008 nothing behind it moved | `dotnet test` and the eight existing frontend specs | any of them needing modification |
| SC-009 reflow | `high-contrast.spec.ts` at a 320 pixel viewport and at 200% zoom | horizontal scrolling of the page body |
| SC-010 readability budget | manual, throttled; see below | the catalogue not readable within 3 seconds |
| SC-011 glyph coverage | `typography.spec.ts` measures rendered glyph widths against the fallback | a character substituted from the fallback family |
| SC-012 no invisible text | `typography.spec.ts` asserts `font-display: swap` is in force on every face | a face loading with `auto` or `block` |

## Manual checks

Two things cannot honestly be automated here.

**SC-010, the readability budget.** Throttle to 1.6 Mbps down with 150 ms of round-trip latency
in the browser's network panel, load `/` with an empty cache, and confirm the book titles are
readable within 3 seconds. `swap` means text appears in the fallback first, so "readable" is the
bar rather than "in Fixel".

It is worth doing this against the production build rather than the dev server, and with the
cache genuinely empty:

```bash
pnpm build
python3 -m http.server 5199 --directory frontend/build/client
# then throttle and load http://127.0.0.1:5199/
```

Measured on 2026-09-10, against the build, cache disabled, at those figures:

| Measurement | Result |
| --- | --- |
| Book titles on screen (raw document, user agent styling) | 192 ms |
| Book titles readable as designed (region painted, palette applied) | 657 ms |
| `load` event | 2286 ms |
| Total transferred | 263 KB |
| Requests | 1 document, 2 stylesheets, 13 scripts, 2 fonts, 2 images |

Against the R12 estimates, two of the three came in smaller than budgeted and one larger:

| Asset | R12 estimate | Measured |
| --- | --- | --- |
| Fonts | ~80 KB | 94 KB (47 KB each, both under the 50 KB ceiling T001 set) |
| Inline watermark SVG | ~1.4 KB | 418 bytes raw, 254 bytes gzipped |
| Grain tile data URI | <400 B | 377 bytes |

The fonts are the only overrun, by 14 KB against a budget of roughly 600 KB, and they are
non-blocking under `swap`: the 192 ms figure is reached without them.

**The screen reader pass.** SC-003 says the announced content is identical to before the feature,
and the only way to know is to listen. Follow the procedure in
`specs/001-browse-catalogue/manual-verification.md` on VoiceOver and TalkBack, over the same
journeys, and compare against what it records. The specific thing being listened for is silence:
the wordmark repeats dozens of times per page, and if it is reachable at all it will be
unmistakable.

## Checking the design by eye

The automated checks prove the obligations, not that it looks right. Worth opening after any
change to `tokens.css`:

```bash
# every page in scope, in one pass
open http://localhost:5173/            # catalogue list
open http://localhost:5173/page/2      # pagination, the smallest targets
open http://localhost:5173/books/<slug># book detail, the largest region
open http://localhost:5173/search      # the search field on a dark ground
open http://localhost:5173/nonsense    # not-found, the shortest page
open http://localhost:5173/register    # a form with hints and a password policy
open http://localhost:5173/sign-in     # a form that can be rejected
```

Look for the things the checks do not cover: whether the ground still reads as texture rather
than as noise at the sizes involved, whether the watermark is quiet enough to ignore, and whether
the regions sit on the ground rather than floating above it. The empty and error states are worth
forcing by stopping the API mid-session, since they are the shortest pages and the ones most
likely to leave a single line stranded on the texture.
