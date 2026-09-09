import { catalogueMeta } from '../catalogue/meta'
import { loadCataloguePage, type CatalogueState } from '../catalogue/page'
import { Catalogue } from './catalogue'

/**
 * Pages two and up. A separate route module because every catalogue page needs
 * its own address, per FR-004, and an address is what the prerender turns into a
 * real document and what a visitor can bookmark or share.
 */
export async function loader({ params }: { params: { page?: string } }) {
  return loadCataloguePage(params.page)
}

export function meta({ params }: { params: { page?: string } }) {
  return catalogueMeta(Number(params.page))
}

export default function CataloguePageRoute({ loaderData }: { loaderData: CatalogueState }) {
  return <Catalogue state={loaderData} />
}
