import AxeBuilder from '@axe-core/playwright'
import { expect, test, type Page } from '@playwright/test'

/**
 * The catalogue, checked the way it is actually used. Principle I is not
 * satisfied by markup that looks right: focus movement and live region
 * announcements only behave correctly in a real browser, so these run in one.
 *
 * These checks catch violations and regressions. They do not replace the manual
 * pass on VoiceOver and TalkBack recorded in manual-verification.md, which is
 * the only thing that proves the catalogue is usable rather than merely
 * conformant.
 */

/**
 * Waits until the router has taken over the document.
 *
 * Until it has, a pagination link is an ordinary link and following it reloads
 * the page, which is the correct fallback but is not the behaviour FR-019
 * describes. Clicking too early therefore tests the wrong thing, and does so
 * intermittently: it only happens when the machine is busy enough for the click
 * to beat hydration. The router publishes its instance on the window when it
 * hydrates, so that is the signal to wait for.
 */
async function waitForHydration(page: Page) {
  await page.waitForFunction(() => {
    const router = (
      window as unknown as {
        __reactRouterDataRouter?: { state?: { initialized?: boolean } }
      }
    ).__reactRouterDataRouter

    return router?.state?.initialized === true
  })
}

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
    await page.goto('/')
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    const { violations } = await new AxeBuilder({ page }).analyze()
    const blocking = violations.filter(
      (violation) => violation.impact === 'critical' || violation.impact === 'serious',
    )

    expect(
      blocking.map((violation) => `${violation.id}: ${violation.help}`),
      'SC-002 allows no critical or serious violations',
    ).toEqual([])
  })

  test('every book on the page is reachable by keyboard, with no trap', async ({ page }) => {
    await page.goto('/')
    await waitForHydration(page)

    const bookLinks = page.locator('a.catalogue__link')
    const bookCount = await bookLinks.count()
    expect(bookCount, 'the first page lists a full page of books').toBe(20)

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

      if (current?.href !== null && current !== null && expected.includes(current.href)) {
        reached.add(current.href)

        // SC-004 asks for a visible focus indicator, not merely a focusable
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

  test('focus moves to the results heading when the page changes', async ({ page }) => {
    await page.goto('/')
    await waitForHydration(page)

    await page.getByRole('link', { name: 'Наступна' }).click()
    await expect(page).toHaveURL(/\/page\/2$/)

    const heading = page.getByRole('heading', { level: 1 })
    await expect(heading).toBeFocused()
  })

  test('the new position and result count are announced politely', async ({ page }) => {
    await page.goto('/')
    await waitForHydration(page)

    // Scoped to the position region: the loading indicator is a polite region
    // too, and during a navigation both are on the page.
    const status = page.getByRole('status').filter({ hasText: 'Сторінка' })
    await expect(status).toHaveText(/Сторінка 1 з 2/)

    await page.getByRole('link', { name: 'Наступна' }).click()

    // A polite region, so it is read at the next pause rather than cutting in.
    await expect(status).toHaveText(/Сторінка 2 з 2/)
    await expect(status).toHaveText(/26/)
  })

  test('the pagination is a named landmark whose links carry real addresses', async ({ page }) => {
    await page.goto('/')

    const pagination = page.getByRole('navigation', { name: 'Сторінки каталогу' })
    await expect(pagination).toBeVisible()

    const hrefs = await pagination
      .locator('a')
      .evaluateAll((links) => links.map((link) => link.getAttribute('href')))

    expect(hrefs.length).toBeGreaterThan(0)
    expect(hrefs.every((href) => href !== null && href !== '#')).toBe(true)
  })

  test('a book with no cover art is still announced by title and author', async ({ page }) => {
    await page.goto('/')

    // Ґудзик is seeded without cover art on purpose.
    const link = page.getByRole('link', { name: /Ґудзик/ })

    await expect(link).toBeVisible()
    await expect(link).toContainText('Ірен Роздобудько')
  })

  test('the document declares Ukrainian', async ({ page }) => {
    await page.goto('/')

    await expect(page.locator('html')).toHaveAttribute('lang', 'uk')
  })
})
