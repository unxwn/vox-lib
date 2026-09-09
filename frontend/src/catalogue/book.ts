import { fetchBook, type BookDetail } from '../api/catalogue'

/**
 * What one book's page amounts to. As with the catalogue, a failure is a value
 * rather than a thrown error, so each one is rendered as something the visitor
 * is actually told about.
 */
export type BookState =
  { status: 'ok'; book: BookDetail } | { status: 'not-found' } | { status: 'unavailable' }

/** The address of a book's page. */
export function bookHref(slug: string): string {
  return `/books/${slug}`
}

/**
 * Reads one book. Exported as `loader` by the route rather than `clientLoader`
 * for the same reason the catalogue is: with ssr:false only `loader` runs during
 * the prerender, and it is the prerender that puts the book's title and
 * description into the served document, which is FR-012.
 */
export async function loadBook(slug: string | undefined): Promise<BookState> {
  if (slug === undefined || slug === '') {
    return { status: 'not-found' }
  }

  const result = await fetchBook(slug)

  if (result.ok) {
    return { status: 'ok', book: result.value }
  }

  // An unpublished book and one that never existed both arrive here as a
  // not-found, which is deliberate: the API does not distinguish them and
  // neither does the page.
  return { status: result.reason === 'notFound' ? 'not-found' : 'unavailable' }
}
