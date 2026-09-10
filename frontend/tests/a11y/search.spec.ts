import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import { waitForHydration } from './support'

/**
 * Search. What matters here is not that results appear but how a visitor who
 * cannot see them is told: the count has to be announced politely, and their
 * place in the form has to be left alone while it happens.
 *
 * These catch violations and regressions. They do not replace the manual pass on
 * VoiceOver and TalkBack recorded in manual-verification.md.
 */

// The status assertions below are scoped to main. The account menu carries a
// polite live region on every page, so a bare status lookup matches two
// elements: this page's result count and that one. Scoping says which is meant.
test.describe('search', () => {
  test('has no critical or serious accessibility violations', async ({ page }) => {
    await page.goto('/search?q=сон')
    await expect(page.locator('main').getByRole('status')).toContainText('Знайдено')

    const { violations } = await new AxeBuilder({ page }).analyze()
    const blocking = violations.filter(
      (violation) => violation.impact === 'critical' || violation.impact === 'serious',
    )

    expect(
      blocking.map((violation) => `${violation.id}: ${violation.help}`),
      'SC-002 allows no critical or serious violations',
    ).toEqual([])
  })

  test('the form is fully operable by keyboard, with no pointing device', async ({ page }) => {
    await page.goto('/search')
    await waitForHydration(page)

    // Tab to the field and type. Never a click: this is the whole point of the
    // test, and a click here would quietly stop testing it.
    const field = page.getByRole('searchbox', { name: /Назва книжки/ })
    await field.focus()
    await expect(field).toBeFocused()

    await page.keyboard.type('сон')
    await page.keyboard.press('Enter')

    await expect(page).toHaveURL(/\/search\?q=/)
    await expect(page.getByRole('link', { name: /Сон/ }).first()).toBeVisible()
  })

  test('the field has a real label, not a placeholder standing in for one', async ({ page }) => {
    await page.goto('/search')

    const field = page.getByRole('searchbox', { name: /Назва книжки/ })

    await expect(field).toBeVisible()
    // A placeholder vanishes as soon as anything is typed and is not reliably
    // announced, so it must not be the only naming.
    await expect(field).not.toHaveAttribute('placeholder', /.+/)
  })

  test('the number of results is announced politely', async ({ page }) => {
    await page.goto('/search?q=сон')

    const status = page.locator('main').getByRole('status')

    // role="status" is implicitly aria-live="polite", so it is read at the next
    // pause rather than interrupting what the visitor is doing.
    await expect(status).toContainText('Знайдено книжок: 2')
  })

  test('the count changes when the displayed set does, without moving focus', async ({ page }) => {
    await page.goto('/search?q=сон')
    await waitForHydration(page)

    const field = page.getByRole('searchbox', { name: /Назва книжки/ })
    await expect(page.locator('main').getByRole('status')).toContainText('Знайдено книжок: 2')

    await field.focus()
    await field.fill('шевченко')
    await page.keyboard.press('Enter')

    await expect(page.locator('main').getByRole('status')).toContainText('Знайдено книжок: 3')

    // FR-014 asks for the count to be announced; it does not ask for focus to
    // move, and taking it here would pull the keyboard out of the field a
    // visitor is still typing in. This is the difference between search and a
    // catalogue page change, and it is deliberate.
    await expect(field).toBeFocused()
  })

  test('a term matching nothing explains the outcome and offers the whole catalogue', async ({
    page,
  }) => {
    await page.goto('/search?q=щосьчогонемає')

    await expect(page.locator('main').getByRole('status')).toContainText('нічого не знайдено')
    await expect(page.getByRole('link', { name: /Переглянути весь каталог/ })).toBeVisible()
  })

  test('an author name finds every book they wrote', async ({ page }) => {
    await page.goto('/search?q=коцюбинський')

    await expect(page.locator('main').getByRole('status')).toContainText('Знайдено книжок: 3')
  })

  test('an unpublished book cannot be found by searching for it', async ({ page }) => {
    // Чорна рада is seeded as a draft.
    await page.goto('/search?q=чорна')

    await expect(page.locator('main').getByRole('status')).toContainText('нічого не знайдено')
  })

  test('results are not offered for indexing', async ({ page }) => {
    await page.goto('/search?q=сон')

    // FR-012 covers catalogue and book pages. A result page is one visitor's
    // query and duplicates content that already has a permanent address.
    await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', /noindex/)
  })

  test('results spanning more than one page are paged, carrying the term', async ({ page }) => {
    // "а" appears in the title or author of 23 published books.
    await page.goto('/search?q=а')
    await waitForHydration(page)

    const pagination = page.getByRole('navigation', { name: 'Сторінки результатів пошуку' })
    await expect(pagination).toBeVisible()

    await pagination.getByRole('link', { name: 'Наступна' }).click()

    // The term survives the page change: page two of a search is page two of
    // that search, not page two of the catalogue.
    await expect(page).toHaveURL(/q=%D0%B0/)
    await expect(page).toHaveURL(/page=2/)
    await expect(page.locator('main').getByRole('status')).toContainText('Знайдено книжок: 23')
  })

  test('the search is reachable from the catalogue', async ({ page }) => {
    await page.goto('/')
    await waitForHydration(page)

    await page.getByRole('link', { name: /Шукати книжку/ }).click()

    await expect(page).toHaveURL(/\/search$/)
    await expect(page.getByRole('searchbox', { name: /Назва книжки/ })).toBeVisible()
  })
})
