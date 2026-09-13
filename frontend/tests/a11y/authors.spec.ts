import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import { AUTHOR, COMPILED_BOOK, COMPILER, showWholePage } from './pages'
import { waitForHydration } from './support'

/**
 * Authors as a resource (SC-013, SC-029, FR-037).
 *
 * The catalogue could always be browsed by book. The question a catalogue
 * invites — what else did this person do? — had no answer anywhere on the site
 * until now, because a credit was plain text.
 */
test.describe('the author index', () => {
  test('has no critical or serious accessibility violations', async ({ page }) => {
    await page.goto('/authors')
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
    await showWholePage(page)

    const { violations } = await new AxeBuilder({ page }).analyze()
    const blocking = violations.filter(
      (violation) => violation.impact === 'critical' || violation.impact === 'serious',
    )

    expect(
      blocking.map((violation) => `${violation.id}: ${violation.help}`),
      'SC-022 allows no critical or serious violations',
    ).toEqual([])
  })

  test('lists everyone credited on a published book, and says how', async ({ page }) => {
    await page.goto('/authors')

    const entries = page.locator('.authors__item')

    expect(await entries.count(), 'the index lists people').toBeGreaterThan(0)

    // SC-029 and FR-035: whatever their role. A compiler is credited on a book,
    // so they belong in the index, and the index has to say that is what they
    // are — leaving it unlabelled would state something untrue about who wrote
    // the book.
    const compiler = entries.filter({ hasText: 'Дмитро Савченко' })

    await expect(compiler).toHaveCount(1)
    await expect(compiler).toContainText('упорядник')
  })

  test('orders by surname rather than by the order the rows were written', async ({ page }) => {
    await page.goto('/authors')

    const names = await page
      .locator('.authors__name')
      .evaluateAll((items) => items.map((item) => (item.textContent ?? '').trim()))

    // The seed inserts Віллінк, Козловський, Савченко, Бобрович in that order.
    // Бобрович comes first here because the index sorts by sort name under the
    // Ukrainian collation, which is a database decision and not this page's.
    expect(names[0]).toContain('Бобрович')
  })

  test('every entry leads to a page that exists', async ({ page }) => {
    await page.goto('/authors')

    const hrefs = await page
      .locator('.authors__link')
      .evaluateAll((links) => links.map((link) => link.getAttribute('href')))

    expect(hrefs.length).toBeGreaterThan(0)

    for (const href of hrefs) {
      expect(href).not.toBeNull()

      const response = await page.request.get(href as string)
      expect(response.status(), `${href} resolves`).toBe(200)
    }
  })
})

test.describe("one author's page", () => {
  test('is reached from a book in one step', async ({ page }) => {
    // SC-013. The whole point of making a credit addressable.
    await page.goto(`/books/${COMPILED_BOOK}`)
    await waitForHydration(page)

    await page
      .getByRole('main')
      .getByRole('link', { name: /Дмитро Савченко/ })
      .click()

    await expect(page).toHaveURL(new RegExp(`/authors/${COMPILER}$`))
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Дмитро Савченко')
  })

  test('lists the books they are credited on, and nothing else', async ({ page }) => {
    await page.goto(`/authors/${AUTHOR}`)

    const listed = await page
      .locator('.catalogue__title')
      .evaluateAll((items) => items.map((item) => (item.textContent ?? '').trim()))

    expect(listed.length).toBeGreaterThan(0)

    // Checked against the API rather than against a list written here, so the
    // page and the resource cannot drift apart without this noticing.
    const response = await page.request.get(`/api/authors/${AUTHOR}`)
    const author = (await response.json()) as { books: { title: string }[] }

    expect(listed.sort()).toEqual(author.books.map((book) => book.title).sort())
  })

  test('has no critical or serious accessibility violations', async ({ page }) => {
    await page.goto(`/authors/${AUTHOR}`)
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
    await showWholePage(page)

    const { violations } = await new AxeBuilder({ page }).analyze()
    const blocking = violations.filter(
      (violation) => violation.impact === 'critical' || violation.impact === 'serious',
    )

    expect(blocking.map((violation) => violation.id)).toEqual([])
  })

  test('an unknown author says so and offers a way to search', async ({ page }) => {
    // FR-037: an author with nothing a visitor may see is not found, exactly as
    // an absent one is.
    await page.goto('/authors/no-such-person-at-all')

    await expect(page.getByRole('heading', { level: 1 })).toContainText('не знайдено')
    await expect(page.getByRole('search', { name: 'Пошук по каталогу' })).toBeVisible()
  })
})
