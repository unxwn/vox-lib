import { Link } from 'react-router'

type EmptyStateProps = {
  /** What the visitor was looking at, so the message can say why it is empty. */
  reason: 'catalogue-is-empty' | 'no-such-page'
}

/**
 * Shown when there is nothing to list. FR-007 asks for more than a blank space:
 * it has to name why nothing is here and offer a way onward, on a page that is
 * still navigable, so nobody is left at a dead end wondering whether the page
 * finished loading.
 */
export function EmptyState({ reason }: EmptyStateProps) {
  return (
    <div className="state state--empty">
      {reason === 'catalogue-is-empty' ? (
        <p>У каталозі поки немає жодної книжки. Щойно з'являться перші записи, вони будуть тут.</p>
      ) : (
        <p>Такої сторінки каталогу немає. Можливо, ви перейшли за застарілим посиланням.</p>
      )}
      <p>
        <Link to="/">Повернутися на початок каталогу</Link>
      </p>
    </div>
  )
}
