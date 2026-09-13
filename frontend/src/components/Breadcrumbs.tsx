import { Link } from 'react-router'
import type { Crumb } from '../catalogue/breadcrumbs'

/**
 * Where this page sits, and the way back up.
 *
 * An ordered list inside a named navigation landmark, because the order is the
 * meaning: these are steps up a hierarchy, not a set of related links. The last
 * item is the current page, so it is not a link — a link to where you already
 * are is a control that does nothing — and it carries `aria-current="page"` so
 * a screen reader says which of the steps is the one you are on (FR-004).
 */
export function Breadcrumbs({ trail }: { trail: readonly Crumb[] }) {
  if (trail.length === 0) {
    return null
  }

  return (
    <nav className="breadcrumbs" aria-label="Навігаційний ланцюжок">
      <ol className="breadcrumbs__list">
        {trail.map((crumb, index) => {
          const isCurrent = index === trail.length - 1

          return (
            <li key={`${crumb.label}-${index}`} className="breadcrumbs__item">
              {isCurrent || crumb.href === undefined ? (
                <span aria-current="page">{crumb.label}</span>
              ) : (
                <Link to={crumb.href}>{crumb.label}</Link>
              )}
            </li>
          )
        })}
      </ol>
    </nav>
  )
}
