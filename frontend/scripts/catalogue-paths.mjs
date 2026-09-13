/**
 * Every address the build emits a document for, discovered from the API.
 *
 * Reading the API is the point. The obvious alternative, reading the committed
 * seed file, works only while books arrive by seeding; the moment an
 * administrative feature publishes one, the seed file stops being the truth.
 * Asking the API is the same path production uses.
 *
 * It lives here, outside `react-router.config.ts`, because FR-032 and FR-058
 * are the same list of addresses seen from two sides — the prerender and the
 * sitemap — and discovering it twice is how the two drift apart.
 */

const apiOrigin = process.env.VOXLIB_API_ORIGIN ?? 'http://localhost:5080'

async function readJson(path) {
  const response = await fetch(`${apiOrigin}${path}`)

  if (!response.ok) {
    throw new Error(
      `${path} could not be read from ${apiOrigin} (HTTP ${response.status}). ` +
        'The API has to be running for the build to prerender the catalogue.',
    )
  }

  return await response.json()
}

/**
 * The landing page, the about page, every catalogue page, every book and every
 * author.
 *
 * `/search` is deliberately absent, and so are the account screens. Results
 * depend on a query string, so there is no fixed document to emit; the account
 * screens are personal to one person and a page that was never generated
 * cannot be indexed.
 */
export async function cataloguePaths() {
  const first = await readJson('/api/books?page=1')

  // Page one is the catalogue's own address, so it is "/books" rather than
  // "/books/page/1". That rule is catalogueHref's in the application, and the
  // two have to agree or the build emits a document nothing links to.
  const pages = [
    '/',
    '/about',
    '/books',
    ...Array.from({ length: Math.max(first.pageCount - 1, 0) }, (_, i) => `/books/page/${i + 2}`),
  ]

  const books = [...first.items]

  for (let page = 2; page <= first.pageCount; page++) {
    books.push(...(await readJson(`/api/books?page=${page}`)).items)
  }

  // The author index is unpaged, so one read discovers every author.
  const authors = await readAuthors()

  return [
    ...pages,
    ...books.map((book) => `/books/${book.slug}`),
    ...(authors.length > 0 ? ['/authors'] : []),
    ...authors.map((author) => `/authors/${author.slug}`),
  ]
}

/**
 * Every author with a published book. Tolerates the endpoint being absent, so
 * that a build against an API that predates the author resource still emits the
 * catalogue rather than failing outright.
 */
async function readAuthors() {
  try {
    const response = await fetch(`${apiOrigin}/api/authors`)

    if (!response.ok) {
      return []
    }

    const body = await response.json()

    return body.items ?? []
  } catch {
    return []
  }
}
