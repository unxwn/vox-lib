type CoverArtProps = {
  url: string | null
}

/**
 * A book's cover, or a placeholder standing in its place.
 *
 * It carries no accessible name in either case. The entry around it is a single
 * link that already names the book and its author, so a description here would
 * be read out a second time and say nothing new. FR-006 asks that a missing
 * cover change how the entry looks and not how it is announced, which is exactly
 * this: the placeholder is decorative too.
 */
export function CoverArt({ url }: CoverArtProps) {
  if (url === null) {
    return <span className="cover-art cover-art--placeholder" aria-hidden="true" />
  }

  return <img className="cover-art" src={url} alt="" loading="lazy" />
}
