import { Link } from 'react-router'
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
    <main className="catalogue">
      <h1 className="catalogue__heading">{what} не знайдено</h1>
      <p>
        Можливо, ви перейшли за застарілим посиланням, або цієї сторінки більше немає в каталозі.
      </p>
      <p>
        <Link to="/">Повернутися до каталогу аудіокниг</Link>
      </p>
    </main>
  )
}

export default function NotFoundRoute() {
  return <NotFound />
}
