/**
 * The decorated ground: a grain layer and the repeated wordmark, behind
 * everything, reaching nothing.
 *
 * Three decisions here are load-bearing, and two of them fail silently.
 *
 * **The watermark is inline SVG, and must stay inline SVG.** An SVG referenced
 * as an image, whether through `background-image`, an `<img>` or `content`,
 * renders in an isolated document that cannot reach the page's webfonts. The
 * wordmark would fall back to a generic family, FR-025 would be broken, and
 * nothing on screen would look wrong enough to notice. This is the single most
 * breakable line in the feature.
 *
 * **The container is `aria-hidden` and holds nothing focusable.** The wordmark
 * repeats dozens of times per page. Reachable at all, it would be the loudest
 * thing on the site to a screen reader, and it says nothing.
 *
 * **It is a sibling of the content, never an ancestor.** What actually keeps
 * axe computing contrast is the opaque region above, not this; but as a sibling
 * the filter's stacking context stays off the content subtree, and no content
 * element inherits a `background-image` for any tool less careful than axe.
 */
export function Ground() {
  return (
    <div className="ground" aria-hidden="true">
      <div className="ground__grain" />

      {/*
        focusable="false" as well as aria-hidden: an <svg> could take focus in
        older engines, and a tab stop on a decoration is a tab stop that lands
        somewhere with nothing to say.
      */}
      <svg className="ground__mark" aria-hidden="true" focusable="false">
        <defs>
          <pattern
            id="ground-wordmark"
            patternUnits="userSpaceOnUse"
            width="420"
            height="200"
            patternTransform="rotate(-24)"
          >
            <text className="ground__wordmark" x="0" y="60">
              ZALIZNA biblioteka
            </text>
            {/*
              Offset on the second row so the mark reads as a scattered texture
              rather than as a grid of columns, and drawn a second time one whole
              tile to the left.

              A pattern tile clips at its own edge; it does not spill into the
              next repeat. The wordmark renders 287 units wide, so this row
              starts at 210 and wants to run to 497, and everything past 420 is
              simply cut: every second line came out as "ZALIZNA bibli", an "o"
              sliced down the middle, and nothing after it. The copy at -210
              covers the same string shifted one tile back, so the part this tile
              loses off its right edge is the part the next tile draws at its
              left, and the two halves meet exactly on the boundary.

              The first row needs no such twin: it starts at 0 and ends at 287,
              inside the tile. Any change to the wordmark's rendered width, the
              tile, or the stagger has to keep both of those true.
            */}
            <text className="ground__wordmark" x="210" y="160">
              ZALIZNA biblioteka
            </text>
            <text className="ground__wordmark" x="-210" y="160">
              ZALIZNA biblioteka
            </text>
          </pattern>
        </defs>
        <rect width="100%" height="100%" fill="url(#ground-wordmark)" />
      </svg>
    </div>
  )
}
