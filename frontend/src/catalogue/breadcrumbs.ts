/** One step in the trail. The last carries no href: it is the page itself. */
export type Crumb = {
  readonly label: string
  readonly href?: string
}

const HOME: Crumb = { label: 'Головна', href: '/' }

const CATALOGUE: Crumb = { label: 'Каталог', href: '/books' }

const AUTHORS: Crumb = { label: 'Автори', href: '/authors' }

/**
 * The trail above a page, derived from its address.
 *
 * Derived and never authored per page, which is what makes FR-031 — every trail
 * agrees with the address it sits above — hold by construction rather than by
 * review. A page cannot be given a trail that contradicts where it is, because
 * a page is not given a trail at all.
 *
 * The landing page and the not-found page have none. The first is the top level
 * and a trail of one entry pointing at itself is noise; the second has no place
 * in the hierarchy, which is why it offers search suggestions instead of a way
 * up from somewhere it never was.
 *
 * `title` is the name of the thing at the end of the trail, for the two
 * addresses whose last step is a record rather than a fixed word. The slug
 * stands in until the page knows its own name, so the trail never renders empty
 * while the data loads.
 */
export function breadcrumbsFor(pathname: string, title?: string): readonly Crumb[] {
  const segments = pathname.replace(/\/+$/, '').split('/').filter(Boolean)

  if (segments.length === 0) {
    return []
  }

  const [first, second, third] = segments

  if (first === 'books') {
    // /books
    if (second === undefined) {
      return [HOME, { label: CATALOGUE.label }]
    }

    // /books/page/:n
    if (second === 'page') {
      return [HOME, CATALOGUE, { label: `Сторінка ${third ?? ''}`.trim() }]
    }

    // /books/:slug
    return [HOME, CATALOGUE, { label: title ?? second }]
  }

  if (first === 'authors') {
    if (second === undefined) {
      return [HOME, { label: AUTHORS.label }]
    }

    return [HOME, AUTHORS, { label: title ?? second }]
  }

  const named = TOP_LEVEL[first]

  return named === undefined ? [] : [HOME, { label: named }]
}

/**
 * Every address one step below the landing page whose name is a fixed word.
 * An address absent from here has no trail, which is the not-found page's
 * answer as well as an unknown one's.
 *
 * **The account screens are deliberately absent.** A trail says where a page
 * sits in a hierarchy and offers the way back up it, and signing in is not a
 * place in the catalogue — it is something you are in the middle of doing.
 * "Головна › Вхід" above a form invites the reader to climb out of a task they
 * came to finish, and says nothing they did not already know. The screens carry
 * their own way back, which is the right shape for a task.
 */
const TOP_LEVEL: Record<string, string> = {
  search: 'Пошук',
  about: 'Про проєкт',
}
