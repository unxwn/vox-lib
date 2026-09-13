import { apiGet, type ApiResult } from './client'
import type { components } from './schema'

/**
 * The catalogue's wire types come from the API's own OpenAPI document, so there
 * is one source of truth for them rather than a hand written copy on this side
 * that drifts quietly. Regenerate with `pnpm --filter frontend gen:api-types`.
 */
export type BookSummary = components['schemas']['BookSummary']
export type PagedBooks = components['schemas']['PagedBooks']
export type BookDetail = components['schemas']['BookDetail']
export type Chapter = components['schemas']['Chapter']
export type Cover = components['schemas']['Cover']
export type AuthorReference = components['schemas']['AuthorReference']
export type AuthorSummary = components['schemas']['AuthorSummary']
export type AuthorDetail = components['schemas']['AuthorDetail']

export function fetchCatalogue(page: number): Promise<ApiResult<PagedBooks>> {
  return apiGet<PagedBooks>(`/api/books?page=${page}`)
}

/**
 * One page of books matching a term. The term is encoded rather than
 * interpolated: a title with a & or a # in it is ordinary, and pasting one
 * straight into the query string would truncate the search silently.
 */
export function searchCatalogue(term: string, page: number): Promise<ApiResult<PagedBooks>> {
  return apiGet<PagedBooks>(`/api/books?q=${encodeURIComponent(term)}&page=${page}`)
}

export function fetchBook(slug: string): Promise<ApiResult<BookDetail>> {
  return apiGet<BookDetail>(`/api/books/${encodeURIComponent(slug)}`)
}

/** Every author with a published book. Unpaged: the index is the whole list. */
export function fetchAuthors(): Promise<ApiResult<{ items: AuthorSummary[] }>> {
  return apiGet<{ items: AuthorSummary[] }>('/api/authors')
}

export function fetchAuthor(slug: string): Promise<ApiResult<AuthorDetail>> {
  return apiGet<AuthorDetail>(`/api/authors/${encodeURIComponent(slug)}`)
}
