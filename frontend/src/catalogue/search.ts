import { searchCatalogue, type PagedBooks } from '../api/catalogue'

/**
 * What a search amounts to. "Idle" is its own state rather than a search for the
 * empty string: arriving at the page with nothing typed has found nothing, which
 * is not the same as having looked and found nothing, and telling a visitor
 * "0 results" before they have searched would be a lie.
 */
export type SearchState =
  | { status: 'idle' }
  | { status: 'ok'; term: string; page: PagedBooks }
  | { status: 'no-such-page'; term: string }
  | { status: 'unavailable'; term: string }

/** The address of a page of results for a term. */
export function searchHref(term: string, page: number): string {
  const query = new URLSearchParams({ q: term })

  if (page > 1) {
    query.set('page', String(page))
  }

  return `/search?${query.toString()}`
}

export async function loadSearch(url: string): Promise<SearchState> {
  const parameters = new URL(url).searchParams
  const term = (parameters.get('q') ?? '').trim()

  if (term === '') {
    return { status: 'idle' }
  }

  const page = Number(parameters.get('page') ?? '1')

  if (!Number.isInteger(page) || page < 1) {
    return { status: 'no-such-page', term }
  }

  const result = await searchCatalogue(term, page)

  if (result.ok) {
    return { status: 'ok', term, page: result.value }
  }

  return { status: result.reason === 'notFound' ? 'no-such-page' : 'unavailable', term }
}
