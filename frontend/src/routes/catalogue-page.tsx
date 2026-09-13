import { catalogueMeta } from '../catalogue/meta'
import { loadCataloguePage, type CatalogueState } from '../catalogue/page'
import { Catalogue } from './catalogue'

/**
 * Pages two and up. A separate route module because every catalogue page needs
 * its own address, per FR-029, and an address is what a visitor bookmarks.
 *
 * **It reads in the browser rather than at build time, and that is a
 * consequence of the catalogue's size rather than a preference.** With
 * `ssr:false`, React Router refuses a `loader` on a route that prerendering
 * emits no document for — correctly, because there would be no process to run
 * it. The real catalogue is four books against a fixed page size of twenty, so
 * no address matches this pattern and none is emitted.
 *
 * FR-032 is not lost by this today: it asks that catalogue pages be readable
 * without running scripts, and the one catalogue page that exists, `/books`, is
 * prerendered by `catalogue.tsx`. It would be lost the moment the catalogue
 * outgrows one page, because page two would then exist and be client-rendered.
 * **So: when the catalogue passes twenty books, this export becomes `loader`
 * again** — at which point `catalogue-paths.mjs` emits the page addresses and
 * React Router is satisfied. That obligation is recorded in CLAUDE.md as well as
 * here, because nothing fails until somebody adds the twenty-first book.
 */
export async function clientLoader({ params }: { params: { page?: string } }) {
  return loadCataloguePage(params.page)
}

export function meta({ params }: { params: { page?: string } }) {
  return catalogueMeta(Number(params.page))
}

export default function CataloguePageRoute({ loaderData }: { loaderData: CatalogueState }) {
  return <Catalogue state={loaderData} />
}
