import type { Cover } from '../api/catalogue'

type CoverArtProps = {
  cover: Cover | null | undefined
  /**
   * How wide the image is laid out, in CSS pixels, so the browser can pick a
   * source before it knows anything about the layout. Defaults to the catalogue
   * listing's width; the book page passes its own.
   */
  width?: number
}

/**
 * A book's cover, or a placeholder standing in its place.
 *
 * It carries no accessible name in either case. The entry around it already
 * names the book and its author, so a description here would be read a second
 * time and say nothing new. FR-046 asks that a missing cover change how the
 * entry looks and not how it is announced, which is exactly this: the
 * placeholder is decorative too.
 *
 * Three things here are requirements rather than polish.
 *
 * **`srcset` over the prepared widths, never a resize.** The browser knows the
 * viewport, the pixel density and the layout; it picks, and object storage
 * serves a file that already exists. Nothing is resized on request (FR-045).
 *
 * **`width` and `height` are set from the source.** They reserve the box before
 * the image arrives, so the text below it does not jump when it does — and the
 * ratio is the image's own, because the CSS fixes the width and lets the height
 * follow (FR-067).
 *
 * **`onError` falls back to the placeholder.** A failed `<img>` is the broken
 * image icon SC-015 forbids, and storage being unreachable must degrade to the
 * same designed state a book with no cover already has.
 */
export function CoverArt({ cover, width = 128 }: CoverArtProps) {
  const sources = cover?.sources ?? []

  if (sources.length === 0) {
    return <Placeholder />
  }

  // The largest prepared width is the src, so a browser that ignores srcset
  // entirely still gets something that is not too small for the box.
  const largest = sources.reduce((a, b) => (a.width >= b.width ? a : b))

  return (
    <img
      className="cover-art"
      src={largest.url}
      srcSet={sources.map((source) => `${source.url} ${source.width}w`).join(', ')}
      sizes={`${width}px`}
      width={width}
      alt=""
      loading="lazy"
      onError={(event) => {
        // Replaced rather than hidden: hiding it would collapse the box and
        // shift everything below, which is the shift the width above exists to
        // prevent.
        event.currentTarget.replaceWith(placeholderElement())
      }}
    />
  )
}

function Placeholder() {
  return <span className="cover-art cover-art--placeholder" aria-hidden="true" />
}

/**
 * The placeholder as a bare element, for the error path. React cannot re-render
 * its way out of an image that has already failed to decode, because the state
 * change would have to come from the element it is replacing.
 */
function placeholderElement(): HTMLElement {
  const span = document.createElement('span')

  span.className = 'cover-art cover-art--placeholder'
  span.setAttribute('aria-hidden', 'true')

  return span
}
