import { Link } from 'react-router'
import type { BookSummary } from '../api/catalogue'
import { bookHref } from '../catalogue/book'
import { CoverArt } from './CoverArt'

/**
 * A list of books, as the catalogue shows them and as a search shows them.
 *
 * One component rather than the same markup in two routes, because this is
 * exactly the part that must not drift: one link per book carrying the title and
 * the author together. Splitting them into two links would double the number of
 * stops to move through and make "by whom" a separate destination from "what",
 * and a copy of this markup elsewhere is how that regresses on one page only.
 */
export function BookList({ books }: { books: readonly BookSummary[] }) {
  return (
    <ul className="catalogue__list">
      {books.map((book) => (
        <li key={book.slug} className="catalogue__item">
          <CoverArt url={book.coverArtUrl} />
          <Link to={bookHref(book.slug)} className="catalogue__link">
            <span className="catalogue__title">{book.title}</span>
            <span className="catalogue__authors">{book.authors.join(', ')}</span>
          </Link>
        </li>
      ))}
    </ul>
  )
}
