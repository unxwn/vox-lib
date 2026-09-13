import AxeBuilder from '@axe-core/playwright'
import { expect, test, type Page } from '@playwright/test'
import { clearPagingCatalogue, seedPagingCatalogue } from './paging-fixture'
import { waitForHydration } from './support'

/**
 * The catalogue, checked the way it is actually used. Principle I is not
 * satisfied by markup that looks right: focus movement and live region
 * announcements only behave correctly in a real browser, so these run in one.
 *
 * The catalogue now lives at /books rather than at the root, which is the front
 * door. And it holds four books, so everything about paging is checked against
 * the fixture in paging-fixture.ts instead — four books against a page size of
 * twenty would leave those assertions passing by having no subject.
 */

/** The accessible name of the element the keyboard is currently on. */
async function focused(page: Page) {
  return page.evaluate(() => {
    const element = document.activeElement
    if (element === null || element === document.body) {
      return null
    }
    return {
      tag: element.tagName.toLowerCase(),
      text: (element.textContent ?? '').trim(),
      href: element.getAttribute('href'),
      outlineWidth: getComputedStyle(element).outlineWidth,
    }
  })
}

test.describe('the catalogue list', () => {
  test('has no critical or serious accessibility violations', async ({ page }) => {
    await page.goto('/books')
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    const { violations } = await new AxeBuilder({ page }).analyze()
    const blocking = violations.filter(
      (violation) => violation.impact === 'critical' || violation.impact === 'serious',
    )

    expect(
      blocking.map((violation) => `${violation.id}: ${violation.help}`),
      'SC-022 allows no critical or serious violations',
    ).toEqual([])
  })

  test('every book on the page is reachable by keyboard, with no trap', async ({ page }) => {
    await page.goto('/books')
    await waitForHydration(page)

    const bookLinks = page.locator('a.catalogue__link')
    const bookCount = await bookLinks.count()
    expect(bookCount, 'the catalogue lists its books').toBeGreaterThan(0)

    const expected = await bookLinks.evaluateAll((links) =>
      links.map((link) => link.getAttribute('href')),
    )

    // Tab from the top and record where focus lands. The bound is generous but
    // finite: a focus trap shows up as never reaching the last book rather than
    // as a hang.
    await page.evaluate(() => document.body.focus())
    const reached = new Set<string>()
    const missingOutline: string[] = []

    for (let step = 0; step < 200 && reached.size < bookCount; step++) {
      await page.keyboard.press('Tab')
      const current = await focused(page)

      if (current?.href != null && expected.includes(current.href)) {
        reached.add(current.href)

        // SC-024 asks for a visible focus indicator, not merely a focusable
        // element. A zero width outline is the default that browsers remove.
        if (current.outlineWidth === '0px') {
          missingOutline.push(current.href)
        }
      }
    }

    expect([...reached].sort(), 'every book is reachable by keyboard alone').toEqual(
      [...expected].filter((href): href is string => href !== null).sort(),
    )
    expect(missingOutline, 'every focused book link shows a focus indicator').toEqual([])
  })

  test('every credited name is a link to that person', async ({ page }) => {
    await page.goto('/books')

    // FR-033. Before this a credit was plain text, so the question a catalogue
    // invites — what else did they do? — had no answer anywhere on the site.
    const credits = page.locator('.catalogue__authors a')

    expect(await credits.count(), 'the catalogue credits somebody').toBeGreaterThan(0)

    const hrefs = await credits.evaluateAll((links) =>
      links.map((link) => link.getAttribute('href')),
    )

    expect(hrefs.every((href) => href?.startsWith('/authors/') === true)).toBe(true)
  })

  test('a compiler is labelled as one, and an author is not', async ({ page }) => {
    await page.goto('/books')

    // FR-066. A name beside a book is read as its author unless something says
    // otherwise, so labelling that would add noise to every book; leaving a
    // compiler unlabelled states something untrue about who wrote it.
    await expect(page.getByText('упорядник').first()).toBeVisible()
    await expect(page.getByText('(автор)')).toHaveCount(0)
  })

  test('the document declares Ukrainian', async ({ page }) => {
    await page.goto('/books')

    await expect(page.locator('html')).toHaveAttribute('lang', 'uk')
  })
})

/**
 * These share one seeded catalogue and run in order, because they all read the
 * same rows and the fixture writes to the database the dev server is serving.
 */
test.describe('paging the catalogue', () => {
  test.describe.configure({ mode: 'serial' })

  let totalBooks = 0

  test.beforeAll(async () => {
    totalBooks = await seedPagingCatalogue()
  })

  test.afterAll(async () => {
    await clearPagingCatalogue()
  })

  test('the first page says where the visitor is', async ({ page }) => {
    await page.goto('/books')
    await waitForHydration(page)

    const status = page.getByRole('status').filter({ hasText: 'Сторінка' })

    await expect(status).toHaveText(/Сторінка 1 з 2/)
    await expect(status).toHaveText(new RegExp(String(totalBooks)))
  })

  test('focus moves to the results heading when the page changes', async ({ page }) => {
    await page.goto('/books')
    await waitForHydration(page)

    await page.getByRole('link', { name: 'Наступна' }).click()
    await expect(page).toHaveURL(/\/books\/page\/2$/)

    // FR-019. Without this, following a pagination link leaves focus on a link
    // that no longer exists and a screen reader user restarts from the page
    // furniture, several stops from the books they asked for.
    await expect(page.getByRole('heading', { level: 1 })).toBeFocused()
  })

  test('the new position and result count are announced politely', async ({ page }) => {
    await page.goto('/books')
    await waitForHydration(page)

    const status = page.getByRole('status').filter({ hasText: 'Сторінка' })
    await expect(status).toHaveText(/Сторінка 1 з 2/)

    await page.getByRole('link', { name: 'Наступна' }).click()

    // A polite region, so it is read at the next pause rather than cutting in.
    await expect(status).toHaveText(/Сторінка 2 з 2/)
    await expect(status).toHaveText(new RegExp(String(totalBooks)))
  })

  test('the pagination is a named landmark whose links carry real addresses', async ({ page }) => {
    await page.goto('/books')
    await waitForHydration(page)

    const pagination = page.getByRole('navigation', { name: 'Сторінки каталогу' })
    await expect(pagination).toBeVisible()

    const hrefs = await pagination
      .locator('a')
      .evaluateAll((links) => links.map((link) => link.getAttribute('href')))

    expect(hrefs.length).toBeGreaterThan(0)
    expect(hrefs.every((href) => href !== null && href !== '#')).toBe(true)

    // FR-030: the first page has one address, and it is not /books/page/1.
    expect(hrefs).not.toContain('/books/page/1')
  })
})
