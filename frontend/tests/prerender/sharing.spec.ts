import { existsSync, readFileSync } from 'node:fs'
import { expect, test } from '@playwright/test'

/**
 * What the outside world is told (US8: SC-026, SC-027, FR-058, FR-059).
 *
 * All of it is about emitted files rather than about a running browser, because
 * that is what a crawler and a link preview actually read: neither runs the
 * page's scripts, so anything added after hydration is invisible to both.
 */
const build = 'build/client'

/** Every meta tag in a document, keyed by name or property. */
function metaOf(file: string): Map<string, string> {
  const html = readFileSync(file, 'utf8')
  const found = new Map<string, string>()

  for (const tag of html.matchAll(/<meta\b[^>]*>/g)) {
    const key = /(?:name|property)="([^"]+)"/.exec(tag[0])?.[1] ?? ''
    const content = /content="([^"]*)"/.exec(tag[0])?.[1] ?? ''

    if (key !== '') {
      found.set(key, content)
    }
  }

  return found
}

test.describe('sharing a link', () => {
  test.skip(
    !existsSync(`${build}/index.html`),
    'Run pnpm --filter frontend build first: these assertions are about build output.',
  )

  const apiOrigin = process.env.VOXLIB_API_ORIGIN ?? 'http://localhost:5080'

  test('a book carries the tags a preview card is built from', async ({ request }) => {
    const response = await request.get(`${apiOrigin}/api/books`)
    const page = (await response.json()) as {
      items: { slug: string; title: string; cover: { sources: { url: string }[] } | null }[]
    }

    const book = page.items.find((item) => item.cover !== null)

    expect(book, 'a book with a cover to share').toBeDefined()

    const meta = metaOf(`${build}/books/${book!.slug}/index.html`)

    // SC-026. The title is the book's own, which is the thing that was broken
    // before: `meta` reads `loaderData`, and a `data` destructure silently gave
    // every book the "not found" title in its emitted document.
    expect(meta.get('og:title')).toBe(book!.title)
    expect(meta.get('og:type')).toBe('book')
    expect(meta.get('og:description') ?? '').not.toBe('')

    // Absolute, because a relative URL in a preview card resolves against the
    // site showing the preview rather than against this one.
    expect(meta.get('og:url') ?? '').toMatch(/^https?:\/\/.+\/books\//)
    expect(meta.get('og:image') ?? '').toMatch(/^https?:\/\/.+\.webp$/)
    expect(meta.get('twitter:card')).toBe('summary_large_image')
  })

  test('an author and the landing page carry them too', async ({ request }) => {
    const response = await request.get(`${apiOrigin}/api/authors`)
    const index = (await response.json()) as { items: { slug: string; name: string }[] }
    const author = index.items[0]

    const authorMeta = metaOf(`${build}/authors/${author.slug}/index.html`)
    expect(authorMeta.get('og:title')).toBe(author.name)
    expect(authorMeta.get('og:url') ?? '').toMatch(/^https?:\/\/.+\/authors\//)

    const landing = metaOf(`${build}/index.html`)
    expect(landing.get('og:title') ?? '').toContain('vox-lib')
    expect(landing.get('og:type')).toBe('website')
  })

  /**
   * SC-027. The pages that stay out of an index are kept out by the robots meta
   * they carry, which is a directive; robots.txt is a request and is never the
   * protection. This asserts the directive is unchanged, because it is the half
   * that actually works.
   */
  test('the account screens and the search page are still kept out of an index', async ({
    page,
  }) => {
    for (const path of ['/register', '/sign-in', '/forgot-password', '/search?q=x']) {
      await page.goto(path)
      await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', /noindex/)
    }
  })

  test('robots.txt allows everything and names the sitemap', () => {
    const robots = readFileSync(`${build}/robots.txt`, 'utf8')

    expect(robots).toMatch(/^\s*User-agent:\s*\*/m)
    expect(robots).toMatch(/^\s*Allow:\s*\//m)
    expect(robots).toMatch(/^\s*Sitemap:\s*\/sitemap\.xml/m)

    // It must not list the pages that stay out. Naming them here would publish
    // the exact list of addresses worth looking at to anyone who reads the file.
    expect(robots).not.toContain('Disallow')
  })

  test('the sitemap lists exactly what the build emitted', async ({ request }) => {
    const sitemap = readFileSync(`${build}/sitemap.xml`, 'utf8')
    const listed = [...sitemap.matchAll(/<loc>([^<]+)<\/loc>/g)].map(
      (match) => new URL(match[1]).pathname,
    )

    const books = (await (await request.get(`${apiOrigin}/api/books`)).json()) as {
      items: { slug: string }[]
    }
    const authors = (await (await request.get(`${apiOrigin}/api/authors`)).json()) as {
      items: { slug: string }[]
    }

    const expected = [
      '/',
      '/about',
      '/books',
      ...books.items.map((book) => `/books/${book.slug}`),
      '/authors',
      ...authors.items.map((author) => `/authors/${author.slug}`),
    ]

    // FR-058. Both come from catalogue-paths.mjs, so this is really a check that
    // nothing has grown a second discovery: a sitemap advertising a page the
    // build never emitted sends a crawler to a 404.
    expect(listed.sort()).toEqual(expected.sort())

    // And nothing that is deliberately not indexed.
    expect(listed).not.toContain('/search')
    expect(listed).not.toContain('/sign-in')
  })
})
