import { fetchCatalogue, type PagedBooks } from '../api/catalogue'

/**
 * What one page of the catalogue amounts to. Failures are values rather than
 * thrown errors, so each one can be rendered as a state the visitor is actually
 * told about instead of an empty screen.
 */
export type CatalogueState =
  { status: 'ok'; page: PagedBooks } | { status: 'no-such-page' } | { status: 'unavailable' }

/** The address of a catalogue page. Page one is the catalogue's own address. */
export function catalogueHref(page: number): string {
  return page <= 1 ? '/' : `/page/${page}`
}

/**
 * Reads one page of the catalogue. The route modules export this as `loader`
 * rather than `clientLoader`: with ssr:false a client loader runs only in the
 * browser, so the prerender would emit an empty shell and FR-012 would be lost.
 * `loader` is what runs at build time, and its result is baked into the document
 * and into the .data file the browser fetches on a later navigation.
 */
export async function loadCataloguePage(
  pageParameter: string | undefined,
): Promise<CatalogueState> {
  const page = pageParameter === undefined ? 1 : Number(pageParameter)

  if (!Number.isInteger(page) || page < 1) {
    return { status: 'no-such-page' }
  }

  const result = await fetchCatalogue(page)

  if (result.ok) {
    return { status: 'ok', page: result.value }
  }

  return { status: result.reason === 'notFound' ? 'no-such-page' : 'unavailable' }
}
