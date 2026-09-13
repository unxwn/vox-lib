import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import { SEARCH_TERM } from './pages'
import { clearPagingCatalogue, seedPagingCatalogue } from './paging-fixture'
import { waitForHydration } from './support'

/**
 * Search. What matters here is not that results appear but how a visitor who
 * cannot see them is told: the count has to be announced politely, and their
 * place in the form has to be left alone while it happens.
 *
 * These catch violations and regressions. They do not replace the manual pass on
 * VoiceOver and TalkBack recorded in manual-verification.md.
 */

// The status assertions below are scoped to main. The account slot carries a
// polite live region on every page, so a bare status lookup matches two
// elements: this page's result count and that one. Scoping says which is meant.
//
// The field itself moved into the banner in this feature, so these operate the
// header field: there is exactly one search input per page now, and the results
// page no longer carries a second one in its body (FR-011).
test.describe('search', () => {
  test('has no critical or serious accessibility violations', async ({ page }) => {
    await page.goto(`/search?q=${encodeURIComponent(SEARCH_TERM)}`)
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
    const field = page.getByRole('searchbox', { name: 'Пошук у каталозі' })
    await field.focus()
    await expect(field).toBeFocused()

    await page.keyboard.type(SEARCH_TERM)
    await page.keyboard.press('Enter')

    await expect(page).toHaveURL(/\/search\?q=/)
    await expect(page.locator('main').getByRole('status')).toContainText('Знайдено')
  })

  /**
   * The field now carries a placeholder by design: US3 turns it into a rotating
   * example drawn from the catalogue. So the assertion is no longer "there is no
   * placeholder" — that is false on purpose — but the thing FR-019 actually
   * protects, which is that the *label* says what the field is for and the
   * example never becomes the naming.
   */
  /**
   * The field carries a placeholder by design: US3 turns it into a rotating
   * example drawn from the catalogue. So the assertion is not "there is no
   * placeholder" — that is false on purpose — but the thing FR-019 protects,
   * which is that the field's accessible name comes from a real label and never
   * from the example, so the naming does not move while the example does.
   *
   * The label is visually hidden rather than shown. What that costs is nothing
   * a screen reader can detect; what it buys is the field's width back, on a
   * bar where the submit button beside it already says "Шукати" in plain sight.
   */
  test('the field is named by a real label, never by the example in it', async ({ page }) => {
    await page.goto('/search')

    const field = page.getByRole('searchbox', { name: 'Пошук у каталозі' })
    await expect(field).toBeAttached()

    // A real label element, associated with the field.
    const labelled = await field.evaluate((element) => {
      const input = element as HTMLInputElement
      const label = input.labels?.[0]

      return {
        hasLabel: label !== undefined,
        labelText: label?.textContent?.trim() ?? '',
        placeholder: input.placeholder,
      }
    })

    expect(labelled.hasLabel, 'the field has a real label element').toBe(true)
    expect(labelled.labelText).toBe('Пошук у каталозі')

    // The accessible name comes from the label and not from the example, so it
    // is the same whatever the example currently reads.
    expect(labelled.labelText).not.toBe(labelled.placeholder)
  })

  test('the number of results is announced politely', async ({ page }) => {
    await page.goto(`/search?q=${encodeURIComponent(SEARCH_TERM)}`)

    const status = page.locator('main').getByRole('status')

    // role="status" is implicitly aria-live="polite", so it is read at the next
    // pause rather than interrupting what the visitor is doing.
    await expect(status).toContainText('Знайдено книжок: 1')
  })

  test('the count changes when the displayed set does, without moving focus', async ({ page }) => {
    await page.goto(`/search?q=${encodeURIComponent(SEARCH_TERM)}`)
    await waitForHydration(page)

    const status = page.locator('main').getByRole('status')
    const field = page.getByRole('searchbox', { name: 'Пошук у каталозі' })

    await expect(status).toContainText('Знайдено книжок: 1')
    const before = await status.textContent()

    /*
      A broader term, and the assertion is that the announcement changes rather
      than what it changes to. An exact total would be a claim about how many
      books the catalogue holds, which another spec running beside this one is
      entitled to change while it seeds its own — and the subject here is the
      announcement, not the arithmetic.
    */
    await field.focus()
    await field.fill('о')
    await page.keyboard.press('Enter')

    await expect(status).not.toHaveText(before ?? '')
    await expect(status).toContainText('Знайдено книжок:')

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

  test('an author name finds every book they are credited on', async ({ page }) => {
    await page.goto('/search?q=' + encodeURIComponent('Козловський'))

    await expect(page.locator('main').getByRole('status')).toContainText('Знайдено книжок: 1')
    await expect(page.getByRole('link', { name: /Людина на перехресті/ }).first()).toBeVisible()
  })

  test('an unpublished book cannot be found by searching for it', async ({ page }) => {
    // Nothing published carries this word.
    await page.goto('/search?q=' + encodeURIComponent('неопублікована'))

    await expect(page.locator('main').getByRole('status')).toContainText('нічого не знайдено')
  })

  test('results are not offered for indexing', async ({ page }) => {
    await page.goto(`/search?q=${encodeURIComponent(SEARCH_TERM)}`)

    // FR-012 covers catalogue and book pages. A result page is one visitor's
    // query and duplicates content that already has a permanent address.
    await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', /noindex/)
  })

  /**
   * The real catalogue is smaller than one page, so this runs against the seeded
   * one. Without it the assertion would pass by having nothing to page (FR-068).
   */
  test.describe('results that run past one page', () => {
    test.describe.configure({ mode: 'serial' })

    test.beforeAll(async () => {
      await seedPagingCatalogue()
    })

    test.afterAll(async () => {
      await clearPagingCatalogue()
    })

    test('are paged, carrying the term', async ({ page }) => {
      await page.goto('/search?q=' + encodeURIComponent('Ярмарок'))
      await waitForHydration(page)

      const pagination = page.getByRole('navigation', { name: 'Сторінки результатів пошуку' })
      await expect(pagination).toBeVisible()

      await pagination.getByRole('link', { name: 'Наступна' }).click()

      // The term survives the page change: page two of a search is page two of
      // that search, not page two of the catalogue.
      await expect(page).toHaveURL(/q=/)
      await expect(page).toHaveURL(/page=2/)
      await expect(page.locator('main').getByRole('status')).toContainText('Знайдено книжок:')
    })
  })

  /**
   * The search field is in the banner on every page now, so it is not "reachable
   * from the catalogue" so much as always present. That is the stronger property
   * and the one FR-001 asks for.
   */
  test('the search field is present on the catalogue itself', async ({ page }) => {
    await page.goto('/books')
    await waitForHydration(page)

    const field = page.getByRole('searchbox', { name: 'Пошук у каталозі' })

    await expect(field).toBeVisible()

    await field.focus()
    await page.keyboard.type(SEARCH_TERM)
    await page.keyboard.press('Enter')

    await expect(page).toHaveURL(/\/search\?q=/)
  })
})
