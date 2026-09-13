import { expect, test } from '@playwright/test'

/**
 * What the site answers for an address, as opposed to what it renders at one.
 *
 * These are status codes rather than pages, and they are checked here because
 * nothing else can see them: a single-page build answers every unmatched
 * address with the document and a 200, so a moved page and a missing file both
 * looked like success until this feature. Two seeded books showed a broken image
 * icon for months and no test, status check or monitor could tell.
 */

test.describe('the catalogue has moved', () => {
  /** From contracts/site-addresses.md. */
  const MOVED: ReadonlyArray<{ from: string; to: string }> = [
    { from: '/page/1', to: '/books' },
    { from: '/page/2', to: '/books/page/2' },
    { from: '/page/7', to: '/books/page/7' },
  ]

  for (const { from, to } of MOVED) {
    test(`${from} is a permanent redirect to ${to}`, async ({ request }) => {
      const response = await request.get(from, { maxRedirects: 0 })

      // 301 rather than 302 or a client-side navigation. A permanent redirect is
      // what teaches a crawler and a browser the new address; anything that
      // answers 200 first teaches them the old one is still good, which is the
      // precise outcome FR-027 exists to prevent.
      expect(response.status(), `${from} answers 301`).toBe(301)
      expect(response.headers()['location'], `${from} points at ${to}`).toBe(to)
    })
  }

  test('the query string survives the redirect', async ({ request }) => {
    const response = await request.get('/page/2?q=test', { maxRedirects: 0 })

    expect(response.status()).toBe(301)
    expect(response.headers()['location']).toBe('/books/page/2?q=test')
  })

  test('the root is not a redirect: it serves the landing page', async ({ request }) => {
    const response = await request.get('/', { maxRedirects: 0 })

    expect(response.status()).toBe(200)
  })
})

test.describe("the catalogue's first page has one address", () => {
  test('every link to the first page leads to /books', async ({ page }) => {
    // FR-030. catalogueHref is the one place the rule is written, so this is
    // really a check that nothing has grown a second copy of it.
    await page.goto('/books')

    const firstPageLinks = await page
      .locator('a[href*="/books"]')
      .evaluateAll((links) => links.map((link) => link.getAttribute('href')))

    expect(
      firstPageLinks.filter((href) => href === '/books/page/1'),
      'nothing links to /books/page/1',
    ).toEqual([])
  })
})

test.describe('a file that is not there', () => {
  /**
   * FR-048, and the mechanism that makes SC-016 measurable. A path whose last
   * segment carries a dot is asking for a file; if no file answers it, the
   * honest reply is 404. Before this every one of these came back
   * `200 text/html`, and the only thing that failed was the browser trying to
   * decode a document as an image.
   */
  const MISSING_FILES = [
    '/covers/nothing.svg',
    '/fonts/nope.woff2',
    '/some.deep/path.png',
    '/favicon-that-does-not-exist.ico',
  ]

  for (const path of MISSING_FILES) {
    test(`${path} answers 404 rather than a page`, async ({ request }) => {
      const response = await request.get(path)

      expect(response.status(), `${path} is not found`).toBe(404)
      expect(
        response.headers()['content-type'] ?? '',
        `${path} is not answered with a document`,
      ).not.toContain('text/html')
    })
  }

  test('a file that is there is still served', async ({ request }) => {
    const response = await request.get('/fonts/OFL.txt')

    expect(response.status()).toBe(200)
  })
})

test.describe('an address that matches no route', () => {
  /**
   * The other half of the rule, and the reason it is a rule about file-shaped
   * paths rather than about unmatched ones: an address with no dot is still a
   * page, and it still renders the not-found page with the whole shell around
   * it and a way to search.
   */
  test('still renders the not-found page, with the shell and a way to search', async ({ page }) => {
    const response = await page.goto('/no-such-page-exists')

    expect(response?.status()).toBe(200)
    expect(response?.headers()['content-type'] ?? '').toContain('text/html')

    await expect(page.getByRole('banner')).toHaveCount(1)
    await expect(page.getByRole('contentinfo')).toHaveCount(1)
    await expect(page.getByRole('heading', { level: 1 })).toContainText('не знайдено')

    // SC-016: a way to look for what was wanted, not just a way back.
    await expect(page.getByRole('search', { name: 'Пошук по каталогу' })).toBeVisible()
  })

  test('an unknown book renders the not-found page', async ({ page }) => {
    await page.goto('/books/no-such-book-at-all')

    await expect(page.getByRole('heading', { level: 1 })).toContainText('не знайдено')
  })

  test('an unknown author renders the not-found page with a way to search', async ({ page }) => {
    await page.goto('/authors/no-such-person-at-all')

    await expect(page.getByRole('heading', { level: 1 })).toContainText('не знайдено')
    await expect(page.getByRole('search', { name: 'Пошук по каталогу' })).toBeVisible()
  })
})
