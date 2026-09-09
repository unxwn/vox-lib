import { useEffect, useRef } from 'react'
import { Link, useNavigate, useNavigation } from 'react-router'
import type { PagedBooks } from '../api/catalogue'
import { catalogueMeta } from '../catalogue/meta'
import { loadCataloguePage, type CatalogueState } from '../catalogue/page'
import { BookList } from '../components/BookList'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { Pagination } from '../components/Pagination'
import '../styles/catalogue.css'

export async function loader() {
  return loadCataloguePage(undefined)
}

export function meta() {
  return catalogueMeta(1)
}

export default function CatalogueRoute({ loaderData }: { loaderData: CatalogueState }) {
  return <Catalogue state={loaderData} />
}

/**
 * Whether the catalogue has been on screen once already in this session.
 *
 * It lives outside the component on purpose. Page one and the later pages are
 * separate route modules, so moving between them unmounts one and mounts the
 * other, and anything held in a ref is born fresh each time. A per-component
 * "is this the first render" flag would therefore be true on every page change
 * and focus would never move.
 */
let catalogueHasBeenShown = false

export function Catalogue({ state }: { state: CatalogueState }) {
  const navigation = useNavigation()
  const navigate = useNavigate()
  const headingRef = useRef<HTMLHeadingElement>(null)
  const currentPage = state.status === 'ok' ? state.page.page : null

  // FR-019. Without this, following a pagination link leaves focus on the link
  // that no longer exists and a screen reader user restarts from the page
  // furniture, several tab stops away from the books they asked for. Arriving
  // fresh is excluded: nothing has moved yet, and taking focus on arrival would
  // be its own defect.
  useEffect(() => {
    if (!catalogueHasBeenShown) {
      catalogueHasBeenShown = true
      return
    }

    headingRef.current?.focus()
  }, [currentPage])

  const isLoading = navigation.state === 'loading'

  return (
    <main className="catalogue">
      <h1 ref={headingRef} tabIndex={-1} className="catalogue__heading">
        Каталог аудіокниг
      </h1>

      {/* Without this the search has no way in: it is a page of its own, and a
          visitor who knows the title they want should not have to page to it. */}
      <p className="catalogue__search-link">
        <Link to="/search">Шукати книжку за назвою або автором</Link>
      </p>

      {state.status === 'unavailable' && (
        <ErrorState onRetry={() => void navigate('.', { replace: true })} />
      )}

      {state.status === 'no-such-page' && <EmptyState reason="no-such-page" />}

      {state.status === 'ok' && <CatalogueResults page={state.page} isLoading={isLoading} />}
    </main>
  )
}

function CatalogueResults({ page, isLoading }: { page: PagedBooks; isLoading: boolean }) {
  const first = (page.page - 1) * page.pageSize + 1
  const last = first + page.items.length - 1

  return (
    <>
      {/*
        Both a visible summary and the polite announcement, deliberately the same
        element. FR-014 wants the count announced whenever the displayed set
        changes; making the announcement visible means everyone gets it, and
        keeping it to one element means it is not read twice.
      */}
      <p className="catalogue__position" role="status">
        {page.totalCount === 0
          ? 'Наразі жодної книжки'
          : `Сторінка ${page.page} з ${page.pageCount} · книжки ${first}–${last} з ${page.totalCount}`}
      </p>

      {isLoading && <LoadingState />}

      {page.items.length === 0 ? (
        <EmptyState reason="catalogue-is-empty" />
      ) : (
        <BookList books={page.items} />
      )}

      <Pagination currentPage={page.page} pageCount={page.pageCount} />
    </>
  )
}
