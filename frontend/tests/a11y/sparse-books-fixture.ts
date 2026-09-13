import pg from 'pg'

/**
 * Books that are deliberately incomplete, and one that is not published.
 *
 * The catalogue the project actually holds is four books, and all four are
 * complete: every one has a description, a cover, chapters and Ukrainian
 * metadata, and none is a draft. That is good for readers and leaves four
 * requirements with nothing to assert against — a book with no chapters
 * (FR-011), a book with no description, a book whose metadata is in another
 * language (FR-018), and a book that is not published behaving exactly as if it
 * did not exist.
 *
 * So they are seeded here rather than shipped. Putting them in `books.json`
 * would mean showing a reader a book called "Книжка без опису" to keep a test
 * happy.
 */

const PREFIX = 'sparse-fixture-'

export const WITHOUT_CHAPTERS = `${PREFIX}no-chapters`
export const WITHOUT_DESCRIPTION = `${PREFIX}no-description`
export const IN_ANOTHER_LANGUAGE = `${PREFIX}english`
export const DRAFT = `${PREFIX}draft`

const AUTHOR_SLUG = `${PREFIX}author`

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

export async function seedSparseBooks(): Promise<void> {
  await withDatabase(async (client) => {
    const author = await client.query(
      `INSERT INTO authors (id, name, slug, sort_name)
       VALUES (gen_random_uuid(), 'Неповна Авторка', $1, 'Авторка, Неповна')
       ON CONFLICT (slug) DO UPDATE SET name = EXCLUDED.name
       RETURNING id`,
      [AUTHOR_SLUG],
    )

    const authorId = author.rows[0].id

    const books: ReadonlyArray<{
      slug: string
      title: string
      description: string | null
      language: string
      state: string
      chapters: number
    }> = [
      {
        slug: WITHOUT_CHAPTERS,
        title: 'Книжка без розділів',
        description: 'Розділи ще не додано.',
        language: 'uk',
        state: 'Published',
        chapters: 0,
      },
      {
        slug: WITHOUT_DESCRIPTION,
        title: 'Книжка без опису',
        description: null,
        language: 'uk',
        state: 'Published',
        chapters: 1,
      },
      {
        slug: IN_ANOTHER_LANGUAGE,
        title: 'A Book in English',
        description: 'Its metadata is written in English.',
        language: 'en',
        state: 'Published',
        chapters: 1,
      },
      {
        slug: DRAFT,
        title: 'Книжка, яка ще не опублікована',
        description: 'Її не має бачити ніхто.',
        language: 'uk',
        state: 'Draft',
        chapters: 1,
      },
    ]

    for (const book of books) {
      const inserted = await client.query(
        `INSERT INTO books
           (id, slug, title, description, language, publication_state, added_to_catalogue_at)
         VALUES (gen_random_uuid(), $1, $2, $3, $4, $5, now())
         ON CONFLICT (slug) DO UPDATE SET title = EXCLUDED.title
         RETURNING id`,
        [book.slug, book.title, book.description, book.language, book.state],
      )

      const bookId = inserted.rows[0].id

      await client.query(
        `INSERT INTO book_authors (book_id, author_id, role)
         VALUES ($1, $2, 'Author') ON CONFLICT DO NOTHING`,
        [bookId, authorId],
      )

      if (book.chapters > 0) {
        await client.query(
          `INSERT INTO chapters (id, book_id, title, position, running_time)
           VALUES (gen_random_uuid(), $1, 'Повний запис', 1, interval '1 hour')
           ON CONFLICT DO NOTHING`,
          [bookId],
        )
      }
    }
  })
}

export async function clearSparseBooks(): Promise<void> {
  await withDatabase(async (client) => {
    await client.query(`DELETE FROM books WHERE slug LIKE $1`, [`${PREFIX}%`])
    await client.query(`DELETE FROM authors WHERE slug = $1`, [AUTHOR_SLUG])
  })
}
