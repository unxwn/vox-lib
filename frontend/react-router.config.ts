import type { Config } from '@react-router/dev/config'

const apiOrigin = process.env.VOXLIB_API_ORIGIN ?? 'http://localhost:5080'

/**
 * Every catalogue page, discovered from the API rather than hardcoded.
 *
 * Reading the API is the point. The obvious alternative, reading the committed
 * seed file, works only while books arrive by seeding; the moment an
 * administrative feature publishes one, the seed file stops being the truth and
 * this has to be rebuilt. Asking the API is the same path production uses.
 */
async function readPage(page: number): Promise<{ items: { slug: string }[]; pageCount: number }> {
  const response = await fetch(`${apiOrigin}/api/books?page=${page}`)

  if (!response.ok) {
    throw new Error(
      `The catalogue could not be read from ${apiOrigin} (HTTP ${response.status}). ` +
        'The API has to be running for the build to prerender the catalogue.',
    )
  }

  return (await response.json()) as { items: { slug: string }[]; pageCount: number }
}

async function cataloguePaths(): Promise<string[]> {
  const first = await readPage(1)

  // Page one is the catalogue's own address, so it is "/" rather than "/page/1".
  const pages = [
    '/',
    ...Array.from({ length: Math.max(first.pageCount - 1, 0) }, (_, i) => `/page/${i + 2}`),
  ]

  // Every published book gets its own document, which is what lets a search
  // engine and a device that has not finished running JavaScript both read the
  // book's title and description. Walking the pages is how the slugs are
  // discovered, for the same reason the page count is: the API is the truth
  // about what is published, and the seed file stops being so the moment a book
  // is published any other way.
  const books = [...first.items]

  for (let page = 2; page <= first.pageCount; page++) {
    books.push(...(await readPage(page)).items)
  }

  // /search is deliberately absent. Its results depend on a query string, so
  // there is no fixed document to emit, and FR-012 excludes result pages from
  // indexing rather than asking for one page per possible term.
  return [...pages, ...books.map((book) => `/books/${book.slug}`)]
}

export default {
  // No Node process in production. The build emits static files that the API's
  // host serves, and the client hydrates them.
  ssr: false,

  // Keep the existing src/ layout rather than moving everything to app/, so
  // adopting the router stays a small diff.
  appDirectory: 'src',

  // Each of these becomes a real HTML document carrying the books it lists, so a
  // search engine and a device that has not finished running JavaScript both
  // receive the content. That is FR-012, and it is why this feature prerenders
  // at all.
  prerender: cataloguePaths,
} satisfies Config
