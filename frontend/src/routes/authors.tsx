import { Link } from 'react-router'
import { fetchAuthors, type AuthorSummary } from '../api/catalogue'
import { ErrorState } from '../components/ErrorState'
import '../styles/catalogue.css'

export type AuthorsState = { status: 'ok'; authors: AuthorSummary[] } | { status: 'unavailable' }

export async function loader(): Promise<AuthorsState> {
  const result = await fetchAuthors()

  return result.ok ? { status: 'ok', authors: result.value.items } : { status: 'unavailable' }
}

export function meta() {
  return [
    { title: 'Автори · vox-lib' },
    {
      name: 'description',
      content: 'Усі автори та упорядники, чиї книжки є в каталозі аудіокниг vox-lib.',
    },
  ]
}

/**
 * The second way into the catalogue.
 *
 * The order is the API's, not this page's: it is by sort name under the
 * Ukrainian collation, which is a database decision because it is the database
 * that knows how Ukrainian sorts (FR-039). Re-sorting here would quietly
 * disagree with it.
 */
export default function AuthorsRoute({ loaderData }: { loaderData: AuthorsState }) {
  if (loaderData.status === 'unavailable') {
    return (
      <div className="catalogue">
        <h1 className="catalogue__heading">Автори</h1>
        <ErrorState />
      </div>
    )
  }

  const { authors } = loaderData

  return (
    <div className="catalogue">
      <h1 className="catalogue__heading">Автори</h1>

      <p className="catalogue__position" role="status">
        {authors.length === 0 ? 'Наразі жодного автора' : `Авторів у каталозі: ${authors.length}`}
      </p>

      <ul className="authors__list">
        {authors.map((author) => (
          <li key={author.slug} className="authors__item">
            <Link to={`/authors/${author.slug}`} className="authors__link">
              <span className="authors__name">{author.name}</span>
              <span className="authors__facts">
                {describe(author)} · книжок: {author.bookCount}
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </div>
  )
}

/**
 * How this person is credited, across every published book of theirs.
 *
 * A set rather than one word, because somebody can write one book and compile
 * another and the index cannot claim a single role for them (SC-029).
 */
function describe(author: AuthorSummary): string {
  const names = author.roles.map((role) => (role === 'compiler' ? 'упорядник' : 'автор'))

  return names.join(', ')
}
