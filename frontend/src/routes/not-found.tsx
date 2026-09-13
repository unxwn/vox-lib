import { Link } from 'react-router'
import { SearchSuggestions } from '../components/SearchSuggestions'
import { notFoundMeta } from '../catalogue/meta'
import '../styles/catalogue.css'

export function meta() {
  return notFoundMeta()
}

/**
 * What is shown when an address matches nothing.
 *
 * FR-011 asks for more than the status code: it has to name what could not be
 * found and offer a way onward, so a visitor who followed a stale link is not
 * left at a dead end. Exported separately from the route because a book's own
 * page renders it too, for a slug that is unknown or unpublished.
 */
export function NotFound({ what = 'Сторінку' }: { what?: string }) {
  return (
    <div className="catalogue">
      <h1 className="catalogue__heading">{what} не знайдено</h1>
      <p>
        Можливо, ви перейшли за застарілим посиланням, або цієї сторінки більше немає в каталозі.
      </p>
      <p>
        <Link to="/books">Повернутися до каталогу аудіокниг</Link>
      </p>

      {/*
        A way to look for what was wanted is worth more here than a way back to
        somewhere the visitor was never trying to go (FR-021).
      */}
      <SearchSuggestions />
    </div>
  )
}

export default function NotFoundRoute() {
  return <NotFound />
}
