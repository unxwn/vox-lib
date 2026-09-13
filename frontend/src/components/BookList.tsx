import { Link } from 'react-router'
import type { BookSummary } from '../api/catalogue'
import { bookHref } from '../catalogue/book'
import { AuthorLinks } from './AuthorLink'
import { CoverArt } from './CoverArt'

/**
 * A list of books, as the catalogue shows them and as a search shows them.
 *
 * One component rather than the same markup in three routes, because this is
 * exactly the part that must not drift: one link per book carrying the title and
 * the author together.
 *
 * The credits sit outside that link rather than inside it, which changed when
 * authors became addressable. A link inside a link is not markup a browser can
 * make sense of, and the author now has a destination of their own; so the
 * title is the link to the book and each name is a link to the person, which
 * costs one stop per name and is what FR-033 asks for.
 */
export function BookList({ books }: { books: readonly BookSummary[] }) {
  return (
    <ul className="catalogue__list">
      {books.map((book) => (
        <li key={book.slug} className="catalogue__item">
          <CoverArt cover={book.cover} />
          <div className="catalogue__entry">
            <Link to={bookHref(book.slug)} className="catalogue__link">
              <span className="catalogue__title">{book.title}</span>
            </Link>
            <p className="catalogue__authors">
              <AuthorLinks authors={book.authors} />
            </p>
          </div>
        </li>
      ))}
    </ul>
  )
}
