import { expect, test } from '@playwright/test'
import { PAGES_IN_SCOPE, SEARCH_TERM } from './pages'
import { waitForHydration } from './support'

/** The result count, told apart from the loading region that shares its role. */
const results = (page: import('@playwright/test').Page) =>
  page.locator('main').getByRole('status').filter({ hasText: 'Знайдено' })

/**
 * Waits until the suggestions have landed, so the example under test is the
 * rotating one rather than the neutral placeholder that stands in before they
 * arrive.
 */
async function awaitExample(field: import('@playwright/test').Locator) {
  await expect(field).toHaveAttribute('placeholder', /^напр\. /, { timeout: 10_000 })
}

/**
 * The search field in the banner, and the example that rotates inside it.
 *
 * A placeholder that changes under a reader is ordinarily a defect, so each of
 * the four rules below is a requirement rather than a refinement, and each is
 * checked here: it freezes on focus and stays frozen, it never starts under
 * reduced motion, it never becomes the field's name, and the field works from
 * everywhere without it.
 */

/** Long enough to cross two of the six-second intervals. */
const PAST_TWO_INTERVALS = 13_000

test.describe('searching from anywhere', () => {
  for (const { name, path, settled } of PAGES_IN_SCOPE) {
    test(`${name} can run a search by keyboard alone`, async ({ page }) => {
      await page.goto(path)
      await expect(page.locator(settled).first()).toBeVisible()
      await waitForHydration(page)

      // SC-007. Never a click: the point is that a search runs with no pointing
      // device involved, and a click here would quietly stop testing it.
      const field = page.getByRole('searchbox', { name: 'Пошук у каталозі' })
      await field.focus()
      await expect(field).toBeFocused()

      await page.keyboard.type(SEARCH_TERM)
      await page.keyboard.press('Enter')

      await expect(page).toHaveURL(/\/search\?q=/)
      await expect(results(page)).toBeVisible()
    })
  }

  test('the result has an address that can be returned to in a fresh session', async ({
    browser,
  }) => {
    const first = await browser.newPage()
    await first.goto('/books')
    await waitForHydration(first)

    const field = first.getByRole('searchbox', { name: 'Пошук у каталозі' })
    await field.focus()
    await first.keyboard.type(SEARCH_TERM)
    await first.keyboard.press('Enter')
    await expect(results(first)).toBeVisible()

    const address = first.url()
    await first.close()

    // SC-007's other half: a brand new context, so nothing is carried over.
    const context = await browser.newContext()
    const second = await context.newPage()
    await second.goto(address)

    await expect(results(second)).toBeVisible()
    await expect(second.getByRole('searchbox', { name: 'Пошук у каталозі' })).toHaveValue(
      SEARCH_TERM,
    )

    await context.close()
  })

  test('exactly one search landmark and one field per page', async ({ page }) => {
    for (const { path } of PAGES_IN_SCOPE) {
      await page.goto(path)
      await expect(page.getByRole('heading', { level: 1 }).first()).toBeVisible()

      // FR-011. The results page used to carry a second form in its body.
      await expect(page.getByRole('search')).toHaveCount(1)
      await expect(page.locator('input[name="q"]')).toHaveCount(1)
    }
  })
})

test.describe('the rotating example', () => {
  test('freezes when the field takes focus, and stays frozen', async ({ page }) => {
    await page.goto('/books')
    await waitForHydration(page)

    const field = page.getByRole('searchbox', { name: 'Пошук у каталозі' })
    await awaitExample(field)

    // SC-008. Focus, then wait past two intervals: nothing may change while
    // somebody is reading or typing in the field.
    await field.focus()
    const frozen = await field.getAttribute('placeholder')

    await page.waitForTimeout(PAST_TWO_INTERVALS)

    expect(await field.getAttribute('placeholder'), 'the example froze on focus').toBe(frozen)
  })

  test('never starts at all under reduced motion', async ({ page }) => {
    await page.emulateMedia({ reducedMotion: 'reduce' })
    await page.goto('/books')
    await waitForHydration(page)

    const field = page.getByRole('searchbox', { name: 'Пошук у каталозі' })
    await awaitExample(field)

    // Untouched this time: the field is never focused, so nothing has frozen
    // it. Under reduced motion the interval must not have started at all, so
    // the example it settled on is the one it still shows two intervals later.
    const first = await field.getAttribute('placeholder')
    await page.waitForTimeout(PAST_TWO_INTERVALS)

    expect(await field.getAttribute('placeholder'), 'zero changes under reduced motion').toBe(first)
  })

  test('is never what names the field', async ({ page }) => {
    await page.goto('/books')
    await waitForHydration(page)

    const field = page.getByRole('searchbox', { name: 'Пошук у каталозі' })
    await awaitExample(field)

    // FR-019, and the reason the example is allowed to move at all: the name
    // comes from a label, so it is the same whatever the example reads.
    const before = await field.evaluate((element) => {
      const input = element as HTMLInputElement
      return { name: input.labels?.[0]?.textContent?.trim() ?? '', example: input.placeholder }
    })

    await page.waitForTimeout(PAST_TWO_INTERVALS)

    const after = await field.evaluate((element) => {
      const input = element as HTMLInputElement
      return { name: input.labels?.[0]?.textContent?.trim() ?? '', example: input.placeholder }
    })

    expect(after.name, 'the name did not move').toBe(before.name)
    expect(after.name).not.toBe(after.example)
  })
})

test.describe('the suggestions beneath the field', () => {
  for (const path of ['/search', '/no-such-page-exists']) {
    test(`${path} offers them as keyboard-reachable links`, async ({ page }) => {
      await page.goto(path)
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

      const suggestions = page.locator('.suggestions__list a')

      // SC-009. Links, not buttons and not a listbox: a link can be reached,
      // announced and activated by a keyboard and a screen reader, which the
      // example rotating inside the field cannot. This is the half of the idea
      // that is actually usable.
      await expect(suggestions.first()).toBeVisible()

      const first = suggestions.first()
      await first.focus()
      await expect(first).toBeFocused()

      const term = (await first.textContent())?.trim() ?? ''
      await page.keyboard.press('Enter')

      await expect(page).toHaveURL(/\/search\?q=/)

      // FR-023: activating a suggestion always finds something. It holds only
      // because the term is a published row's own text, copied verbatim.
      await expect(results(page)).toContainText('Знайдено книжок:')
      await expect(page.getByRole('searchbox', { name: 'Пошук у каталозі' })).toHaveValue(term)
    })
  }
})
