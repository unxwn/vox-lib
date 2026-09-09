import { Link } from 'react-router'
import { catalogueHref } from '../catalogue/page'

type PaginationProps = {
  currentPage: number
  pageCount: number
  /**
   * The address of a numbered page. Defaults to the catalogue's, and is given
   * explicitly by a search, whose pages carry the term as well as the number.
   */
  hrefFor?: (page: number) => string
  /** Names this navigation, so it can be told apart from any other on the page. */
  label?: string
}

/**
 * How many pages sit either side of the current one before the list is cut
 * short. The catalogue is expected to reach the low thousands of books, which is
 * over a hundred pages, and a hundred links is its own accessibility problem:
 * a screen reader user would have to move past all of them to reach anything
 * after.
 */
const WINDOW = 2

function pagesToShow(currentPage: number, pageCount: number): (number | 'gap')[] {
  const wanted = new Set<number>([1, pageCount])

  for (let page = currentPage - WINDOW; page <= currentPage + WINDOW; page++) {
    if (page >= 1 && page <= pageCount) {
      wanted.add(page)
    }
  }

  const ordered = [...wanted].sort((a, b) => a - b)
  const withGaps: (number | 'gap')[] = []

  ordered.forEach((page, index) => {
    if (index > 0 && page - ordered[index - 1] > 1) {
      withGaps.push('gap')
    }
    withGaps.push(page)
  })

  return withGaps
}

/**
 * Numbered pagination, as a landmark of its own so it can be jumped to and
 * jumped over.
 *
 * Every page is a real link with a real address, not a button that mutates state
 * in place. That is what lets a page be opened in a new tab, shared, bookmarked
 * and prerendered, and it is why FR-004 asks for an address per page.
 */
export function Pagination({
  currentPage,
  pageCount,
  hrefFor = catalogueHref,
  label = 'Сторінки каталогу',
}: PaginationProps) {
  if (pageCount <= 1) {
    return null
  }

  return (
    <nav className="pagination" aria-label={label}>
      <ul className="pagination__list">
        {currentPage > 1 && (
          <li>
            <Link to={hrefFor(currentPage - 1)} rel="prev">
              Попередня
            </Link>
          </li>
        )}

        {pagesToShow(currentPage, pageCount).map((page, index) =>
          page === 'gap' ? (
            <li key={`gap-${index}`} aria-hidden="true" className="pagination__gap">
              …
            </li>
          ) : (
            <li key={page}>
              <Link
                to={hrefFor(page)}
                aria-current={page === currentPage ? 'page' : undefined}
                // The visible text is a bare number, which reads as "5" and
                // means nothing on its own out of context.
                aria-label={
                  page === currentPage
                    ? `Сторінка ${page}, поточна`
                    : `Сторінка ${page} з ${pageCount}`
                }
              >
                {page}
              </Link>
            </li>
          ),
        )}

        {currentPage < pageCount && (
          <li>
            <Link to={hrefFor(currentPage + 1)} rel="next">
              Наступна
            </Link>
          </li>
        )}
      </ul>
    </nav>
  )
}
