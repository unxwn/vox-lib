# Manual verification: Site Shell

**Feature**: `specs/004-site-shell/` | **Date**: 2026-09-12

What no suite decides. Everything else in this feature is checked by
`dotnet test`, `pnpm --filter frontend test:a11y` or
`frontend/scripts/check-design-tokens.mjs`; these four are here because a
machine can confirm the mechanism and not the outcome.

Run them on a freshly rebuilt container, after `pnpm dev`.

## 1. A real screen reader on the landmark list

**Why it is not automated.** The browser suite asserts that a banner, a main, a
contentinfo and four distinctly named navigation and search landmarks exist, and
that is a fact about the accessibility tree. Whether the resulting list is one a
person can navigate by is a judgement about wording and order that only a screen
reader user makes.

| Platform | How |
| --- | --- |
| iOS | VoiceOver, rotor set to Landmarks, on `/books/stratehiia-i-taktyka-liderstva` |
| Android | TalkBack, reading controls set to Headings then Landmarks, on `/search` |

What to check:

- the landmark list reads as six distinguishable places, not as "navigation,
  navigation, navigation";
- from the top of a book page, reaching the book's own title takes one gesture
  (the skip link), not a walk through the banner;
- the account control announces whether you are signed in without being hunted
  for;
- on `/search`, the suggestions announce as links and say where they go.

## 2. Whether six seconds reads as slow enough

**Why it is not automated.** `header-search.spec.ts` proves the example freezes
on focus, never starts under `prefers-reduced-motion`, and never becomes the
field's name. It cannot tell you whether the rotation is *comfortable* — six
seconds is an informed guess, and the failure mode is a placeholder that feels
like it is being watched rather than read.

Open `/books` and do not touch the field for a minute. The example should read
as something you notice when you look at it and forget when you do not. If it
pulls the eye, the interval is too short.

## 3. FR-054: the focus indicator stays distinct from the primary action

**This is the one requirement in the feature with no automated proof, and the
refusal is deliberate.**

`check-design-tokens.mjs` enforces a contrast matrix, and the obvious thing to
add to it is `--focus` against `--action`. That pair measures 2.06:1 with the
teal this started as and 1.60:1 with the violet it ended as, and adding either
would fail the build for a pair that never touches: the indicator is an
`outline` at `--focus-offset: 2px`, so what abuts the ring on both sides is the
region behind the button, and `--focus` against `--surface` is the pair that
decides whether the ring is visible — which *is* enforced.

FR-054 is about hue, not ratio. So it is decided by eye and recorded here:

1. open `/sign-in`;
2. Tab to the submit control;
3. the amber ring must read as a ring around a violet button, not as part of it.

Amber against violet plainly satisfies this. Re-check it whenever `--action` or
`--focus` changes, because that is exactly when it could stop being true and
nothing would fail.

## 4. The covers, at the proportions they were drawn at

**Why it is not automated.** `covers.spec.ts` asserts no broken image, the
rendered width at 1280, reflow at 320, and that each rendered box matches its
own `naturalWidth / naturalHeight` within a pixel. What it cannot tell you is
whether a cover *looks* right — whether the title on the artwork is legible at
128 pixels wide, which is the size a reader actually meets it at.

Open `/books` and read the four covers at a normal viewing distance. The four
run from 0.640 to 0.753 wide against tall; if one of them needs a larger box to
be legible, FR-049's 128 is the number to revisit, not the aspect ratio.

## What changed after this was first written

- The account screens lost their breadcrumb trails. Signing in is a task rather
  than a place in the catalogue, so there is nothing above it to climb back to;
  `contracts/site-addresses.md` records the change.
- The search field's label is visually hidden rather than shown. The field's
  accessible name still comes from a `<label>` and never from the placeholder,
  which is what FR-019 protects; what was dropped is a second visible word
  naming a control the submit button beside it already names.
- `--action` is violet rather than teal. The green sat outside a palette whose
  surfaces are indigo; the violet is the adjacent hue, and is the most saturated
  one that still clears 3:1 on both surfaces.
