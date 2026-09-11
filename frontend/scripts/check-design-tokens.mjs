#!/usr/bin/env node
/**
 * Enforces the token contract in `specs/003-visual-identity/contracts/design-tokens.md`.
 * Runs from `pnpm lint`, so every one of these is checked on every pull request.
 *
 * Six checks, and four of them exist to catch something that is invisible in
 * review:
 *
 *   1. a colour literal in any stylesheet other than tokens.css
 *   2. an alpha channel on a --surface* token
 *   3. a constrained colour pair below its contrast floor
 *   4. the grain band spanning further than it may
 *   5. a var(--name) that tokens.css does not define
 *   6. outline: none, anywhere, in any state
 *
 * The contrast matrix below is the enforced copy. data-model.md records the same
 * pairs in prose with their measured ratios; adding a constrained pair means
 * adding it in both places, and this is the one that fails the build. The
 * measured values are never read from anywhere: they are recomputed here from
 * tokens.css with the WCAG 2.x relative luminance formula, because a table
 * cannot notice that somebody edited a colour.
 */

import { readFileSync, readdirSync } from 'node:fs'
import { join, relative, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const stylesDir = join(dirname(fileURLToPath(import.meta.url)), '..', 'src', 'styles')
const tokensFile = 'tokens.css'

/**
 * Constrained pairs, as [foreground, background, floor, why].
 *
 * The floors come from the requirements, not from taste: 4.5:1 for body text,
 * 3:1 for large text, for a meaningful non-text boundary and for the focus
 * indicator. The last two rows are this design's own commitment to a region
 * reading as a distinct area rather than as a slightly different navy; they
 * have no standard behind them and are labelled as such.
 */
const CONTRAST_FLOORS = [
  ['--text', '--surface', 4.5, 'FR-006, body text on a region'],
  ['--text', '--surface-raised', 4.5, 'FR-006, body text on a nested region'],
  ['--text-muted', '--surface', 4.5, 'FR-006, secondary text on a region'],
  ['--text-muted', '--surface-raised', 4.5, 'FR-006, secondary text on a nested region'],
  ['--heading', '--surface', 3, 'FR-006, large text on a region'],
  ['--link', '--surface', 4.5, 'FR-006, a link on a region'],
  ['--link', '--surface-raised', 4.5, 'FR-006, a link on a nested region'],
  ['--danger', '--surface', 4.5, 'FR-006 and FR-021, a rejected field on a region'],
  ['--text', '--ground-grain-hi', 4.5, 'FR-006, body text on the ground at its lightest'],
  [
    '--text-muted',
    '--ground-grain-hi',
    4.5,
    'FR-006, secondary text on the ground at its lightest',
  ],
  ['--focus', '--surface', 3, 'FR-011, the focus indicator on a region'],
  ['--focus', '--surface-raised', 3, 'FR-011, the focus indicator on a nested region'],
  ['--focus', '--ground-grain-hi', 3, 'FR-011, the focus indicator on the ground at its lightest'],
  ['--border-control', '--surface', 3, 'FR-007, a control boundary on a region'],
  ['--border-control', '--surface-raised', 3, 'FR-007, a control boundary on a nested region'],
  ['--surface', '--ground', 1.3, 'FR-004, a region reading as delineated from the ground'],
  ['--surface-raised', '--surface', 1.15, 'FR-004, a nested region reading as distinct'],
]

/**
 * Ceilings rather than floors. Grain that spans too far stops reading as
 * texture and starts reading as banding.
 */
const CONTRAST_CEILINGS = [
  ['--ground-grain-hi', '--ground-grain-lo', 1.35, 'FR-002, the grain reading as texture'],
]

/**
 * Named CSS colours. Matched as whole words in declaration values, which is why
 * quoted strings are stripped first: `local('Tan')` is a typeface, not a colour.
 * `transparent` and `currentColor` are absent deliberately, and so is every
 * system colour keyword, because forced colours needs them by name.
 */
const NAMED_COLOURS = new Set(
  `aliceblue antiquewhite aqua aquamarine azure beige bisque black blanchedalmond blue
   blueviolet brown burlywood cadetblue chartreuse chocolate coral cornflowerblue cornsilk
   crimson cyan darkblue darkcyan darkgoldenrod darkgray darkgreen darkgrey darkkhaki
   darkmagenta darkolivegreen darkorange darkorchid darkred darksalmon darkseagreen
   darkslateblue darkslategray darkslategrey darkturquoise darkviolet deeppink deepskyblue
   dimgray dimgrey dodgerblue firebrick floralwhite forestgreen fuchsia gainsboro ghostwhite
   gold goldenrod gray green greenyellow grey honeydew hotpink indianred indigo ivory khaki
   lavender lavenderblush lawngreen lemonchiffon lightblue lightcoral lightcyan
   lightgoldenrodyellow lightgray lightgreen lightgrey lightpink lightsalmon lightseagreen
   lightskyblue lightslategray lightslategrey lightsteelblue lightyellow lime limegreen linen
   magenta maroon mediumaquamarine mediumblue mediumorchid mediumpurple mediumseagreen
   mediumslateblue mediumspringgreen mediumturquoise mediumvioletred midnightblue mintcream
   mistyrose moccasin navajowhite navy oldlace olive olivedrab orange orangered orchid
   palegoldenrod palegreen paleturquoise palevioletred papayawhip peachpuff peru pink plum
   powderblue purple rebeccapurple red rosybrown royalblue saddlebrown salmon sandybrown
   seagreen seashell sienna silver skyblue slateblue slategray slategrey snow springgreen
   steelblue tan teal thistle tomato turquoise violet wheat white whitesmoke yellow
   yellowgreen`
    .trim()
    .split(/\s+/),
)

const FUNCTIONAL_COLOUR = /\b(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch|color|color-mix)\s*\(/i
const HEX_COLOUR = /#[0-9a-f]{3,8}\b/i

const problems = []

function fail(file, message) {
  problems.push(`${file}: ${message}`)
}

/** Comments hold explanations that mention colours, so they are never scanned. */
function stripComments(css) {
  return css.replace(/\/\*[\s\S]*?\*\//g, '')
}

/** Quoted strings hold typeface names and URLs, neither of which is a colour. */
function stripStrings(css) {
  return css.replace(/'[^']*'|"[^"]*"/g, "''")
}

/** Every `property: value` in a flat stylesheet, with the line it sits on. */
function declarations(css) {
  const found = []
  const pattern = /([-\w]+)\s*:\s*([^;{}]+)(?=[;}])/g
  let match

  while ((match = pattern.exec(css)) !== null) {
    found.push({
      property: match[1],
      value: match[2].trim(),
      line: css.slice(0, match.index).split('\n').length,
    })
  }

  return found
}

// --- Colour arithmetic -----------------------------------------------------

/** WCAG 2.x relative luminance. Not an approximation of it. */
function luminance([r, g, b]) {
  const channel = (eight) => {
    const c = eight / 255
    return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4
  }

  return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b)
}

function contrast(a, b) {
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x)
  return (hi + 0.05) / (lo + 0.05)
}

/** A fully opaque colour, or null if it is not one this script can read. */
function parseOpaqueColour(value) {
  const hex = /^#([0-9a-f]{3}|[0-9a-f]{6})$/i.exec(value.trim())

  if (hex !== null) {
    const digits =
      hex[1].length === 3
        ? hex[1]
            .split('')
            .map((digit) => digit + digit)
            .join('')
        : hex[1]

    return [0, 2, 4].map((at) => parseInt(digits.slice(at, at + 2), 16))
  }

  const rgb = /^rgba?\(\s*([\d.]+)[\s,]+([\d.]+)[\s,]+([\d.]+)\s*\)$/i.exec(value.trim())

  if (rgb !== null) {
    return [Number(rgb[1]), Number(rgb[2]), Number(rgb[3])]
  }

  return null
}

// --- Read the stylesheets --------------------------------------------------

const sheets = readdirSync(stylesDir)
  .filter((name) => name.endsWith('.css'))
  .map((name) => {
    const source = readFileSync(join(stylesDir, name), 'utf8')
    return { name, code: stripStrings(stripComments(source)) }
  })

const tokensSheet = sheets.find((sheet) => sheet.name === tokensFile)

if (tokensSheet === undefined) {
  console.error(`Missing ${relative(process.cwd(), join(stylesDir, tokensFile))}`)
  process.exit(1)
}

/** Every token tokens.css defines, by name. */
const tokens = new Map()

for (const declaration of declarations(tokensSheet.code)) {
  if (declaration.property.startsWith('--')) {
    tokens.set(declaration.property, declaration.value)
  }
}

// --- Check 1: no colour literal outside tokens.css -------------------------

for (const sheet of sheets) {
  if (sheet.name === tokensFile) {
    continue
  }

  for (const { property, value, line } of declarations(sheet.code)) {
    if (property.startsWith('--')) {
      // The contract says a token is declared in exactly one file, so a
      // stylesheet declaring one is a second place to change it.
      fail(sheet.name, `line ${line}: declares ${property}. Tokens belong in ${tokensFile}`)
      continue
    }

    const hex = HEX_COLOUR.exec(value)
    const functional = FUNCTIONAL_COLOUR.exec(value)
    const named = value
      .toLowerCase()
      .split(/[^a-z]+/)
      .find((word) => NAMED_COLOURS.has(word))

    const literal = hex?.[0] ?? functional?.[0].replace(/\s*\($/, '()') ?? named

    if (literal !== undefined) {
      fail(
        sheet.name,
        `line ${line}: ${property} uses the colour literal ${literal}. ` +
          `Colours are named in ${tokensFile} and referred to by role (FR-017)`,
      )
    }
  }
}

// --- Check 2: no alpha channel on a --surface* token -----------------------

for (const [name, value] of tokens) {
  if (!name.startsWith('--surface')) {
    continue
  }

  if (parseOpaqueColour(value) === null) {
    fail(
      tokensFile,
      `${name} is ${value}, which is not a fully opaque colour this script can read. ` +
        'A region that is not opaque lets axe walk past it to the textured ground, ' +
        'which turns every contrast result on the page from pass to incomplete ' +
        'while the suite stays green (contracts/design-tokens.md, section 3)',
    )
  }
}

// --- Checks 3 and 4: the contrast matrix, recomputed -----------------------

function ratioBetween(a, b) {
  for (const name of [a, b]) {
    if (!tokens.has(name)) {
      fail(tokensFile, `the contrast matrix names ${name}, which is not defined`)
      return null
    }
  }

  const colours = [a, b].map((name) => parseOpaqueColour(tokens.get(name)))

  if (colours.some((colour) => colour === null)) {
    fail(tokensFile, `${a} or ${b} is not a colour this script can read`)
    return null
  }

  return contrast(colours[0], colours[1])
}

for (const [a, b, floor, why] of CONTRAST_FLOORS) {
  const measured = ratioBetween(a, b)

  if (measured !== null && measured < floor) {
    fail(
      tokensFile,
      `${a} on ${b} is ${measured.toFixed(2)}:1, below its floor of ${floor}:1 (${why})`,
    )
  }
}

for (const [a, b, ceiling, why] of CONTRAST_CEILINGS) {
  const measured = ratioBetween(a, b)

  if (measured !== null && measured > ceiling) {
    fail(
      tokensFile,
      `${a} against ${b} spans ${measured.toFixed(2)}:1, above its ceiling of ${ceiling}:1 (${why})`,
    )
  }
}

// --- Check 5: every var(--name) resolves -----------------------------------

for (const sheet of sheets) {
  const pattern = /var\(\s*(--[-\w]+)/g
  let match

  while ((match = pattern.exec(sheet.code)) !== null) {
    if (!tokens.has(match[1])) {
      const line = sheet.code.slice(0, match.index).split('\n').length

      fail(
        sheet.name,
        `line ${line}: var(${match[1]}) is not defined in ${tokensFile}. ` +
          'An undefined custom property does not error, it falls back silently',
      )
    }
  }
}

// --- Check 6: outline: none, anywhere --------------------------------------

for (const sheet of sheets) {
  for (const { property, value, line } of declarations(sheet.code)) {
    const removesOutline =
      (property === 'outline' || property === 'outline-style' || property === 'outline-width') &&
      /^(none|0|0px)$/i.test(value.trim())

    if (removesOutline) {
      fail(
        sheet.name,
        `line ${line}: ${property}: ${value} removes a focus indicator. ` +
          'FR-011 has no exception, and the failure is invisible in review',
      )
    }
  }
}

// --- Report ----------------------------------------------------------------

if (problems.length > 0) {
  console.error(`\ncheck-design-tokens: ${problems.length} problem(s)\n`)

  for (const problem of problems) {
    console.error(`  ${problem}`)
  }

  console.error('')
  process.exit(1)
}

console.log(
  `check-design-tokens: ${tokens.size} tokens, ` +
    `${CONTRAST_FLOORS.length + CONTRAST_CEILINGS.length} constrained pairs, ` +
    `${sheets.length} stylesheets. All within their limits.`,
)
