import { Link } from 'react-router'
import type { AuthorReference } from '../api/catalogue'

/**
 * A credited name, as a link to the person.
 *
 * The one place a name becomes a link (FR-033). Before this a book's authors
 * were plain text, so the obvious question a catalogue invites — what else did
 * they do? — had no answer anywhere on the site.
 *
 * An author carries no label and a compiler does, which is a deliberate
 * asymmetry rather than an oversight. A name beside a book is read as its
 * author unless something says otherwise, so labelling that adds noise to every
 * book on the site; leaving a compiler unlabelled, on the other hand, states
 * something untrue about who wrote it (FR-066).
 */
export function AuthorLink({ author }: { author: AuthorReference }) {
  return (
    <span className="author-link">
      <Link to={`/authors/${author.slug}`}>{author.name}</Link>
      {author.role === 'compiler' && <span className="author-link__role"> (упорядник)</span>}
    </span>
  )
}

/** Several credits, separated for reading rather than by a bare comma in text. */
export function AuthorLinks({ authors }: { authors: readonly AuthorReference[] }) {
  return (
    <>
      {authors.map((author, index) => (
        <span key={author.slug}>
          {index > 0 && ', '}
          <AuthorLink author={author} />
        </span>
      ))}
    </>
  )
}
