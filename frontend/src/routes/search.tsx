import { Form, Link, useSearchParams } from 'react-router'
import { BookList } from '../components/BookList'
import { ErrorState } from '../components/ErrorState'
import { Pagination } from '../components/Pagination'
import { loadSearch, searchHref, type SearchState } from '../catalogue/search'
import '../styles/catalogue.css'

/**
 * Search runs in the browser and nowhere else.
 *
 * `clientLoader` rather than `loader` is the whole of T060's prerender
 * exclusion: a result set depends on a query string, so there is no fixed
 * document to emit at build time, and listing one page per possible term is not
 * a thing that can be done. FR-012 asks for the catalogue and the book pages to
 * be real documents; it explicitly does not ask that of result pages.
 */
export async function clientLoader({ request }: { request: Request }) {
  return loadSearch(request.url)
}

export function meta() {
  return [
    { title: 'Пошук аудіокниг · vox-lib' },
    // Result pages are not for indexing: they are one visitor's query, they
    // duplicate content that already has a permanent address in the catalogue,
    // and an indexed one sends a reader to a page that may no longer match.
    { name: 'robots', content: 'noindex, follow' },
  ]
}

export default function SearchRoute({ loaderData }: { loaderData: SearchState }) {
  const [parameters] = useSearchParams()

  return (
    <main className="catalogue">
      <h1 className="catalogue__heading">Пошук аудіокниг</h1>

      <SearchForm term={parameters.get('q') ?? ''} />

      <SearchResults state={loaderData} />
    </main>
  )
}

/**
 * The search box.
 *
 * A real form submitting by GET, so pressing Enter in the field runs the search
 * with no pointing device involved and the result gets an address that can be
 * shared, bookmarked and returned to with the browser's back button. The submit
 * button is there for anyone who expects one and is reachable by keyboard like
 * any other control; it is not the only way to search.
 *
 * The label is a real label rather than a placeholder. A placeholder disappears
 * as soon as anything is typed, is not reliably announced, and fails contrast
 * requirements in most designs.
 */
function SearchForm({ term }: { term: string }) {
  return (
    <Form method="get" action="/search" role="search" className="search__form">
      <label className="search__label" htmlFor="search-term">
        Назва книжки або ім'я автора
      </label>
      <div className="search__controls">
        <input
          id="search-term"
          className="search__input"
          type="search"
          name="q"
          defaultValue={term}
          // The term is bounded on the server too; saying so here means a
          // visitor is stopped at the field rather than by an error page.
          maxLength={100}
          autoComplete="off"
        />
        <button type="submit">Шукати</button>
      </div>
    </Form>
  )
}

function SearchResults({ state }: { state: SearchState }) {
  if (state.status === 'idle') {
    return (
      <p className="search__prompt">
        Введіть назву книжки або ім'я автора, щоб знайти її в каталозі.
      </p>
    )
  }

  if (state.status === 'unavailable') {
    return <ErrorState />
  }

  if (state.status === 'no-such-page') {
    return (
      <>
        <p className="state state--empty">Такої сторінки результатів немає.</p>
        <p>
          <Link to={searchHref(state.term, 1)}>До першої сторінки результатів</Link>
        </p>
      </>
    )
  }

  const { term, page } = state
  const first = (page.page - 1) * page.pageSize + 1
  const last = first + page.items.length - 1

  return (
    <>
      {/*
        FR-014. Polite, and deliberately without moving focus: a visitor who has
        just typed is still in the search field, and pulling focus to the results
        would take the keyboard away mid-thought and make refining a term
        painful. The catalogue moves focus on a page change because there the
        visitor followed a link and left the field behind; here they did not.

        Visible as well as announced, so the count is one fact stated once rather
        than a hidden message duplicating a visible one.
      */}
      <p className="catalogue__position" role="status">
        {page.totalCount === 0
          ? `За запитом «${term}» нічого не знайдено`
          : `Знайдено книжок: ${page.totalCount} · сторінка ${page.page} з ${page.pageCount} · показано ${first}–${last}`}
      </p>

      {page.totalCount === 0 ? (
        <div className="state state--empty">
          <p>
            Спробуйте змінити запит: перевірте написання або введіть частину назви чи прізвища
            автора.
          </p>
          <p>
            <Link to="/">Переглянути весь каталог аудіокниг</Link>
          </p>
        </div>
      ) : (
        <BookList books={page.items} />
      )}

      <Pagination
        currentPage={page.page}
        pageCount={page.pageCount}
        hrefFor={(number) => searchHref(term, number)}
        label="Сторінки результатів пошуку"
      />
    </>
  )
}
