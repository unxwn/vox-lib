import { existsSync, readFileSync } from 'node:fs'
import { expect, test } from '@playwright/test'

/**
 * Assertions about what the build emitted, rather than about a running browser.
 *
 * FR-028 has two halves and this is the first: a page that was never generated
 * cannot be indexed. The account screens are personal to one person and are
 * deliberately absent from the prerender list, so the build must emit no
 * document for any of them, and must emit the single-page fallback that serves
 * them instead.
 *
 * The fallback assertion is the one that matters operationally. Nothing in this
 * repository serves the built frontend yet, and whatever eventually does has to
 * answer unknown paths with that file rather than a 404, or every account screen
 * will fail to load on a fresh visit in production while working perfectly in
 * development.
 */
const build = 'build/client'

const accountRoutes = ['register', 'confirm', 'sign-in', 'forgot-password', 'reset-password']

test.describe('what the build emits', () => {
  test.skip(
    !existsSync(`${build}/index.html`),
    'Run pnpm --filter frontend build first: these assertions are about build output.',
  )

  for (const route of accountRoutes) {
    test(`no document is generated for /${route}`, () => {
      expect(
        existsSync(`${build}/${route}/index.html`),
        `/${route} is personal to one person and must not be prerendered. FR-028.`,
      ).toBe(false)
    })
  }

  test('the single-page fallback exists, so the account screens can be served at all', () => {
    // Written to __spa-fallback.html rather than index.html because / is itself
    // prerendered as the catalogue.
    expect(existsSync(`${build}/__spa-fallback.html`)).toBe(true)
  })

  test('the catalogue is still prerendered with its content in the document', () => {
    const document = readFileSync(`${build}/index.html`, 'utf8')

    // The account work must not have disturbed the reason 001 prerenders at all.
    expect(document).toContain('<title>')
    expect(document.length).toBeGreaterThan(1000)
  })
})

/**
 * FR-032 and SC-012: the pages that are meant to be readable without running a
 * script actually were emitted, and carry their content rather than a shell.
 *
 * Checked against what the API says exists rather than against a list written
 * here, so the build and the catalogue cannot drift apart without this noticing.
 */
test.describe('the documents the build emits for readers', () => {
  test.skip(
    !existsSync(`${build}/index.html`),
    'Run pnpm --filter frontend build first: these assertions are about build output.',
  )

  const apiOrigin = process.env.VOXLIB_API_ORIGIN ?? 'http://localhost:5080'

  test('the landing page, the catalogue and the about page are real documents', () => {
    for (const [name, path] of [
      ['the landing page', 'index.html'],
      ['the catalogue', 'books/index.html'],
      ['the about page', 'about/index.html'],
    ]) {
      expect(existsSync(`${build}/${path}`), `${name} is emitted as a document`).toBe(true)
    }

    // Content, not a shell: a document that exists but carries nothing is
    // exactly as useless to a crawler as no document at all.
    expect(readFileSync(`${build}/books/index.html`, 'utf8')).toContain('Каталог аудіокниг')
    expect(readFileSync(`${build}/index.html`, 'utf8')).toContain('бібліотека українських')
  })

  test('every published book has its own document, carrying its own title', async ({ request }) => {
    const response = await request.get(`${apiOrigin}/api/books`)
    const page = (await response.json()) as { items: { slug: string; title: string }[] }

    expect(page.items.length, 'the catalogue holds books to check').toBeGreaterThan(0)

    for (const book of page.items) {
      const file = `${build}/books/${book.slug}/index.html`

      expect(existsSync(file), `${book.slug} is emitted as a document`).toBe(true)
      expect(readFileSync(file, 'utf8'), `${book.slug} carries its own title`).toContain(book.title)
    }
  })

  test('every author has their own document, carrying their own name', async ({ request }) => {
    const response = await request.get(`${apiOrigin}/api/authors`)
    const index = (await response.json()) as { items: { slug: string; name: string }[] }

    expect(index.items.length, 'the catalogue credits somebody').toBeGreaterThan(0)
    expect(existsSync(`${build}/authors/index.html`), 'the author index is emitted').toBe(true)

    for (const author of index.items) {
      const file = `${build}/authors/${author.slug}/index.html`

      expect(existsSync(file), `${author.slug} is emitted as a document`).toBe(true)
      expect(readFileSync(file, 'utf8'), `${author.slug} carries their name`).toContain(author.name)
    }
  })

  test('the search results are not emitted, because they depend on a query', () => {
    expect(existsSync(`${build}/search/index.html`)).toBe(false)
  })
})
