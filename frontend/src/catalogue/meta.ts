/**
 * The origin absolute addresses are built against.
 *
 * Open Graph requires absolute URLs: a relative one is resolved against
 * whatever site is doing the resolving, which for a link preview is the site
 * showing the preview rather than this one. It is read from the environment
 * because the build is the only place that knows where the site is served from,
 * and it falls back to the dev server so a local build produces something
 * checkable rather than something empty.
 */
const SITE_ORIGIN = (
  (globalThis as { process?: { env?: Record<string, string | undefined> } }).process?.env
    ?.VOXLIB_SITE_ORIGIN ?? 'http://localhost:5173'
).replace(/\/+$/, '')

/**
 * The tags that decide what a shared link looks like (FR-059).
 *
 * `og:image` is the cover at its largest prepared width where there is one, and
 * absent where there is not — an og:image pointing at nothing is worse than
 * none, because a preview card then renders a broken image rather than falling
 * back to text.
 */
function sharing({
  title,
  description,
  path,
  image,
  type = 'website',
}: {
  title: string
  description: string
  path: string
  image?: string
  type?: string
}) {
  return [
    { property: 'og:title', content: title },
    { property: 'og:description', content: description },
    { property: 'og:url', content: `${SITE_ORIGIN}${path}` },
    { property: 'og:type', content: type },
    { property: 'og:site_name', content: 'vox-lib' },
    { name: 'twitter:card', content: image === undefined ? 'summary' : 'summary_large_image' },
    ...(image === undefined ? [] : [{ property: 'og:image', content: image }]),
  ]
}

/**
 * The document title and description for a catalogue page.
 *
 * A page without a title is a critical accessibility failure, not a nicety: it
 * is what a screen reader reads on arrival and what a tab list, a bookmark and a
 * search result all show, so with none the page announces itself as its own
 * address. The page number is in the title because every page has its own
 * address and they would otherwise be indistinguishable.
 */
export function catalogueMeta(page: number) {
  const title =
    page <= 1 ? 'Каталог аудіокниг · vox-lib' : `Каталог аудіокниг, сторінка ${page} · vox-lib`
  const description = 'Каталог українських аудіокниг: назви, автори та структура розділів.'

  return [{ title }, { name: 'description', content: description }]
}

/**
 * The document title and description for one book's page.
 *
 * The description is the book's own, trimmed to a length a search result will
 * actually show, so the page carries a real summary rather than the catalogue's
 * generic one.
 */
export function bookMeta(
  title: string,
  description: string | null,
  slug?: string,
  coverUrl?: string,
) {
  const summary =
    description === null || description.trim() === ''
      ? `${title}: опис, автори та структура розділів аудіокниги.`
      : description.slice(0, 160)

  return [
    { title: `${title} · vox-lib` },
    { name: 'description', content: summary },
    ...(slug === undefined
      ? []
      : sharing({
          title,
          description: summary,
          path: `/books/${slug}`,
          image: coverUrl,
          // A book is a written work rather than a generic page, which is what
          // lets a preview card show it as one.
          type: 'book',
        })),
  ]
}

/** One author's page. */
export function authorMeta(name: string, slug: string, bookCount: number) {
  const description = `Аудіокниги, у яких бере участь ${name}, у каталозі vox-lib. Книжок: ${bookCount}.`

  return [
    { title: `${name} · vox-lib` },
    { name: 'description', content: description },
    ...sharing({ title: name, description, path: `/authors/${slug}`, type: 'profile' }),
  ]
}

/** The front door's title and description. */
export function landingMeta() {
  const title = 'vox-lib — українські аудіокниги'
  const description = 'Бібліотека українських аудіокниг для тих, кому зручніше слухати.'

  return [
    { title },
    { name: 'description', content: description },
    ...sharing({ title, description, path: '/' }),
  ]
}

/** The about page's title and description. */
export function aboutMeta() {
  return [
    { title: 'Про проєкт · vox-lib' },
    {
      name: 'description',
      content: 'Що таке vox-lib і для кого ця бібліотека.',
    },
  ]
}

/** The title for a page that could not be found. */
export function notFoundMeta() {
  return [{ title: 'Сторінку не знайдено · vox-lib' }, { name: 'robots', content: 'noindex' }]
}
