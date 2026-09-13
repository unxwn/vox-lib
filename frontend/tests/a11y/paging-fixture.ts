import pg from 'pg'

/**
 * A catalogue big enough to page.
 *
 * The real catalogue is four books against a fixed page size of twenty, so
 * nothing paginates and every paging assertion the site already held would pass
 * by having no subject — the same failure mode as the contrast incompletes in
 * CLAUDE.md, where a suite stays green while checking nothing (FR-068).
 *
 * It writes to the database rather than intercepting the network, and it has to:
 * `/books` is a prerendered route, so its data comes from the loader rather than
 * from a request the browser makes, and a route intercept never fires.
 *
 * The rows are removed again afterwards. Every book it adds carries the slug
 * prefix below, so the cleanup can be exact rather than a truncation that would
 * take the real catalogue with it.
 *
 * **Their titles all begin with Я, the last letter of the Ukrainian alphabet,
 * and that is not decoration.** Specs run in parallel, so while these rows exist
 * another file may be reading the catalogue; titles that sorted anywhere else
 * would push the four real books off the first page and break assertions that
 * have nothing to do with paging.
 */

export const PAGE_SIZE = 20

/** Enough for a second page with a visible remainder on it. */
export const EXTRA_BOOKS = 22

export const SLUG_PREFIX = 'paging-fixture-'

const AUTHOR_SLUG = 'paging-fixture-author'

const connectionString =
  process.env.VOXLIB_DB_CONNECTION_URL ?? 'postgresql://voxlib:voxlib@localhost:5432/voxlib'

async function withDatabase<T>(work: (client: pg.Client) => Promise<T>): Promise<T> {
  const client = new pg.Client({ connectionString })
  await client.connect()

  try {
    return await work(client)
  } finally {
    await client.end()
  }
}

/**
 * Adds enough books for the catalogue to run to a second page, and returns how
 * many books the catalogue then holds.
 */
export async function seedPagingCatalogue(): Promise<number> {
  return withDatabase(async (client) => {
    await client.query('BEGIN')

    const author = await client.query(
      `INSERT INTO authors (id, name, slug, sort_name)
       VALUES (gen_random_uuid(), 'Авторка Тестова', $1, 'Тестова, Авторка')
       ON CONFLICT (slug) DO UPDATE SET name = EXCLUDED.name
       RETURNING id`,
      [AUTHOR_SLUG],
    )

    const authorId = author.rows[0].id

    for (let index = 1; index <= EXTRA_BOOKS; index++) {
      const slug = `${SLUG_PREFIX}${String(index).padStart(2, '0')}`

      const book = await client.query(
        `INSERT INTO books
           (id, slug, title, description, language, publication_state, added_to_catalogue_at)
         VALUES (gen_random_uuid(), $1, $2, null, 'uk', 'Published', now())
         ON CONFLICT (slug) DO UPDATE SET title = EXCLUDED.title
         RETURNING id`,
        [slug, `Ярмарок гортання ${String(index).padStart(2, '0')}`],
      )

      const bookId = book.rows[0].id

      await client.query(
        `INSERT INTO book_authors (book_id, author_id, role)
         VALUES ($1, $2, 'Author')
         ON CONFLICT DO NOTHING`,
        [bookId, authorId],
      )

      await client.query(
        `INSERT INTO chapters (id, book_id, title, position, running_time)
         VALUES (gen_random_uuid(), $1, 'Повний запис', 1, interval '1 hour')
         ON CONFLICT DO NOTHING`,
        [bookId],
      )
    }

    await client.query('COMMIT')

    const total = await client.query(
      `SELECT count(*)::int AS count FROM books WHERE publication_state = 'Published'`,
    )

    return total.rows[0].count as number
  })
}

/** Removes exactly what {@link seedPagingCatalogue} added. */
export async function clearPagingCatalogue(): Promise<void> {
  await withDatabase(async (client) => {
    await client.query(`DELETE FROM books WHERE slug LIKE $1`, [`${SLUG_PREFIX}%`])
    await client.query(`DELETE FROM authors WHERE slug = $1`, [AUTHOR_SLUG])
  })
}
