import type { Chapter } from '../api/catalogue'
import { formatRunningTime } from '../catalogue/runningTime'

type ChapterListProps = {
  chapters: readonly Chapter[]
  /** The book's own language, so chapter titles are read in the right voice. */
  language: string
}

/**
 * A book's chapters, as a real list.
 *
 * A list is what makes this navigable rather than merely readable: a screen
 * reader announces "list, 12 items" on entry and lets its user jump from one
 * item to the next, so the shape of the book is available without reading all
 * of it. Rendering the same rows as a stack of divs would lose the count and
 * the jumping both, which is most of what SC-001 is asking for.
 *
 * When a book has no chapters yet, this says so in a sentence rather than
 * leaving an empty list behind: an empty region announces itself as "list, 0
 * items", which sounds like a page that failed to load.
 */
export function ChapterList({ chapters, language }: ChapterListProps) {
  if (chapters.length === 0) {
    return (
      <p className="chapters__empty">
        Розділи цієї книжки ще не додані. Щойно вони з'являться, ви побачите їх тут.
      </p>
    )
  }

  return (
    <ol className="chapters__list">
      {chapters.map((chapter) => (
        <li key={chapter.position} className="chapters__item">
          <span className="chapters__title" lang={language}>
            {chapter.title}
          </span>
          <span className="chapters__duration">
            {formatRunningTime(chapter.runningTimeSeconds)}
          </span>
        </li>
      ))}
    </ol>
  )
}
