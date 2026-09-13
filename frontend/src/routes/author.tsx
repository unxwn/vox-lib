import { fetchAuthor, type AuthorDetail } from '../api/catalogue'
import { authorMeta } from '../catalogue/meta'
import { BookList } from '../components/BookList'
import { ErrorState } from '../components/ErrorState'
import { NotFound } from './not-found'
import '../styles/catalogue.css'

export type AuthorState =
  { status: 'ok'; author: AuthorDetail } | { status: 'not-found' } | { status: 'unavailable' }

export async function loader({ params }: { params: { slug?: string } }): Promise<AuthorState> {
  return load(params.slug)
}

/**
 * The same read in the browser, for an address the build did not prerender.
 * The reasoning is the book page's: a slug with no emitted document has no
 * .data file, and asking for one yields a bare 404 the router cannot turn into
 * route data, so the page would die with "Application Error" rather than saying
 * the author was not found.
 */
export async function clientLoader({
  params,
  serverLoader,
}: {
  params: { slug?: string }
  serverLoader: () => Promise<unknown>
}): Promise<AuthorState> {
  try {
    return (await serverLoader()) as AuthorState
  } catch {
    return load(params.slug)
  }
}

async function load(slug: string | undefined): Promise<AuthorState> {
  if (slug === undefined || slug === '') {
    return { status: 'not-found' }
  }

  const result = await fetchAuthor(slug)

  if (result.ok) {
    return { status: 'ok', author: result.value }
  }

  return { status: result.reason === 'notFound' ? 'not-found' : 'unavailable' }
}

/** What this page is called in the trail above it. */
export const handle = {
  crumb: (data: unknown) => {
    const state = data as AuthorState | undefined

    return state?.status === 'ok' ? state.author.name : undefined
  },
}

/** `loaderData`, not `data`: see the note on the same export in book.tsx. */
export function meta({ loaderData: data }: { loaderData?: AuthorState }) {
  if (data?.status !== 'ok') {
    return [{ title: 'Автора не знайдено · vox-lib' }]
  }

  return authorMeta(data.author.name, data.author.slug, data.author.bookCount)
}

export default function AuthorRoute({ loaderData }: { loaderData: AuthorState }) {
  if (loaderData.status === 'not-found') {
    return <NotFound what="Автора" />
  }

  if (loaderData.status === 'unavailable') {
    return (
      <div className="catalogue">
        <h1 className="catalogue__heading">Автор</h1>
        <ErrorState />
      </div>
    )
  }

  const { author } = loaderData

  return (
    <div className="catalogue">
      <h1 className="catalogue__heading">{author.name}</h1>

      <p className="catalogue__position" role="status">
        {`Книжок у каталозі: ${author.bookCount}`}
      </p>

      {/*
        Every published book they are credited on, and nothing they are not. The
        role held on each is shown beside their name on the book itself rather
        than repeated here, so the two cannot disagree.
      */}
      <BookList books={author.books} />
    </div>
  )
}
