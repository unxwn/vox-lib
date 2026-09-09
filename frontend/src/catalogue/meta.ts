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

  return [
    { title },
    {
      name: 'description',
      content: 'Каталог українських аудіокниг: назви, автори та структура розділів.',
    },
  ]
}

/**
 * The document title and description for one book's page.
 *
 * The description is the book's own, trimmed to a length a search result will
 * actually show, so the page carries a real summary rather than the catalogue's
 * generic one.
 */
export function bookMeta(title: string, description: string | null) {
  return [
    { title: `${title} · vox-lib` },
    {
      name: 'description',
      content:
        description === null || description.trim() === ''
          ? `${title}: опис, автори та структура розділів аудіокниги.`
          : description.slice(0, 160),
    },
  ]
}

/** The title for a page that could not be found. */
export function notFoundMeta() {
  return [{ title: 'Сторінку не знайдено · vox-lib' }, { name: 'robots', content: 'noindex' }]
}
