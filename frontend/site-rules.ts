/**
 * Two rules about addresses that whatever serves this site has to apply,
 * written once.
 *
 * They are here rather than inside the Vite plugin because they are not a
 * development concern: the same table has to be configured on the production
 * host, and a rule written twice is a rule that drifts. The plugin in
 * `vite/site-rules-plugin.ts` imports this; the host configuration, when there
 * is a host, is generated from or checked against it.
 *
 * Neither rule can be implemented inside the application. A single-page build
 * has no process of its own: by the time React Router sees an address, the
 * server has already answered 200 with a document, which is precisely what
 * teaches a crawler that the old address is still good.
 */

export type Redirect = {
  /** Matched against the path only, with no query string. */
  readonly from: RegExp
  /** May reference capture groups as $1, $2 and so on. */
  readonly to: string
}

/**
 * Where the catalogue used to live. `301` rather than `302`, because the move
 * is permanent and a temporary redirect asks every crawler and every browser to
 * keep checking the old address forever (FR-027).
 *
 * `/page/1` collapses onto `/books` rather than `/books/page/1`, so the
 * catalogue's first page has exactly one address (FR-030).
 */
export const REDIRECTS: readonly Redirect[] = [
  { from: /^\/page\/1\/?$/, to: '/books' },
  { from: /^\/page\/(\d+)\/?$/, to: '/books/page/$1' },
]

/** The new location for `path`, or null when no rule matches it. */
export function redirectFor(path: string): string | null {
  for (const rule of REDIRECTS) {
    const match = rule.from.exec(path)

    if (match !== null) {
      return rule.to.replace(/\$(\d+)/g, (_, group: string) => match[Number(group)] ?? '')
    }
  }

  return null
}

/**
 * Whether an address is asking for a file rather than for a page.
 *
 * The test is a dot in the last segment, which is what separates
 * `/covers/boiarynia.svg` from `/books/some-slug`. It matters because of what
 * happens when no such file exists: the single-page fallback answers every
 * unmatched address with the document, so a missing image comes back as
 * `200 text/html` and the browser fails to decode it. That is a broken image
 * icon no status check, monitor or test can see, and it is why two seeded books
 * showed one for months (FR-048).
 *
 * An address that matches no route and is *not* file-shaped still renders the
 * not-found page, which is a different thing and stays as it is.
 */
export function looksLikeAFile(path: string): boolean {
  const lastSegment = path.split('/').pop()

  return lastSegment !== undefined && lastSegment.includes('.')
}
