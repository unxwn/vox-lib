import { useEffect, useRef } from 'react'
import { Link, useNavigate, useNavigationType } from 'react-router'
import type { BookDetail } from '../api/catalogue'
import { loadBook, type BookState } from '../catalogue/book'
import { bookMeta } from '../catalogue/meta'
import { formatRunningTime } from '../catalogue/runningTime'
import { ChapterList } from '../components/ChapterList'
import { CoverArt } from '../components/CoverArt'
import { ErrorState } from '../components/ErrorState'
import { NotFound } from './not-found'
import '../styles/catalogue.css'

export async function loader({ params }: { params: { slug?: string } }) {
  return loadBook(params.slug)
}

/**
 * The same read, in the browser, for an address the build did not prerender.
 *
 * Only published books are prerendered, so only they have a .data file to load
 * from. A slug that is unknown, or that belongs to an unpublished book, has
 * none, and asking for one yields a bare 404 that the router cannot turn into
 * route data: the page then dies with "Application Error" instead of saying the
 * book was not found, which is precisely the case FR-011 exists for.
 *
 * So the prerendered data is used when it is there, and the API is asked
 * directly when it is not. The answer is the same either way, because the API
 * does not distinguish an unpublished book from an absent one.
 */
export async function clientLoader({
  params,
  serverLoader,
}: {
  params: { slug?: string }
  serverLoader: () => Promise<unknown>
}): Promise<BookState> {
  try {
    return (await serverLoader()) as BookState
  } catch {
    return loadBook(params.slug)
  }
}

export function meta({ data }: { data?: BookState }) {
  if (data?.status !== 'ok') {
    return [{ title: 'Книжку не знайдено · vox-lib' }]
  }

  return bookMeta(data.book.title, data.book.description)
}

export default function BookRoute({ loaderData }: { loaderData: BookState }) {
  const navigate = useNavigate()

  if (loaderData.status === 'not-found') {
    return <NotFound what="Книжку" />
  }

  if (loaderData.status === 'unavailable') {
    return (
      <main className="catalogue">
        <h1 className="catalogue__heading">Книжка</h1>
        <ErrorState onRetry={() => void navigate('.', { replace: true })} />
      </main>
    )
  }

  return <Book book={loaderData.book} />
}

function Book({ book }: { book: BookDetail }) {
  const navigationType = useNavigationType()
  const headingRef = useRef<HTMLHeadingElement>(null)

  // FR-019 applied to arriving at a book. Following a link from the catalogue
  // replaces the whole page without moving the keyboard, so focus would be left
  // on a link that no longer exists and a screen reader user would restart from
  // the page furniture rather than at the book they asked for.
  //
  // Only on PUSH, which is a link followed inside the application. A first load
  // and a back or forward step both arrive as POP, and on those nothing has
  // moved yet: taking focus there would be its own defect, and would fight the
  // browser over restoring the previous position.
  useEffect(() => {
    if (navigationType === 'PUSH') {
      headingRef.current?.focus()
    }
  }, [navigationType, book.slug])

  return (
    <main className="catalogue book">
      {/*
        The book's own language, declared on the content that is in it rather
        than on the document. The interface around it stays Ukrainian, so a
        screen reader reads the furniture in one voice and an English title in
        another, which is what FR-018 asks for.
      */}
      <h1 ref={headingRef} tabIndex={-1} className="catalogue__heading" lang={book.language}>
        {book.title}
      </h1>

      <p className="book__authors">
        <span className="book__label">Автор:</span>{' '}
        <span lang={book.language}>{book.authors.join(', ')}</span>
      </p>

      <div className="book__summary">
        <CoverArt url={book.coverArtUrl} />

        <dl className="book__facts">
          <div>
            <dt>Розділів</dt>
            <dd>{book.chapterCount}</dd>
          </div>
          <div>
            <dt>Загальна тривалість</dt>
            <dd>{formatRunningTime(book.totalRunningTimeSeconds)}</dd>
          </div>
        </dl>
      </div>

      <h2 className="book__section">Опис</h2>
      {book.description === null || book.description.trim() === '' ? (
        <p className="book__description book__description--absent">
          Опис цієї книжки ще не додано.
        </p>
      ) : (
        <p className="book__description" lang={book.language}>
          {book.description}
        </p>
      )}

      <h2 className="book__section">Розділи</h2>
      <ChapterList chapters={book.chapters} language={book.language} />

      {/*
        FR-009, and deliberately a sentence rather than a control. There is no
        play button here, disabled or otherwise: a disabled control still
        appears in a screen reader's list of controls and invites a listener to
        look for a way to enable it, when the honest answer is that playback
        does not exist yet.
      */}
      <p className="book__listening">Щоб слухати цю книжку, потрібен обліковий запис.</p>

      <p>
        <Link to="/">Повернутися до каталогу аудіокниг</Link>
      </p>
    </main>
  )
}
