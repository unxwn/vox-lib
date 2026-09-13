import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import { showWholePage } from './pages'

/**
 * The front door (SC-014 in the spec's numbering of US2, FR-025).
 *
 * The root address used to be the catalogue, so the site opened on a list of
 * books and never said what it was. What this checks is that it now does, and
 * that the way into the catalogue is not buried underneath the explanation.
 *
 * The copy it checks for is deliberately shallow. Most of what a landing page
 * would claim — who runs the library, what it costs, why an account is needed —
 * is not yet known, and is on the page marked "До прикладу:" rather than as
 * fact. Asserting the marking rather than the sentences is what keeps this test
 * true when somebody writes the real ones.
 */
test.describe('the landing page', () => {
  test('has no critical or serious accessibility violations', async ({ page }) => {
    await page.goto('/')
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

  test('says what the site is', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { level: 1 })).toContainText('Аудіокниги')
    await expect(page.getByRole('main')).toContainText('бібліотека українських аудіокниг')
  })

  test('offers a way into the catalogue that actually resolves', async ({ page }) => {
    await page.goto('/')

    const wayIn = page
      .getByRole('main')
      .getByRole('link', { name: /каталог/i })
      .first()

    await expect(wayIn).toBeVisible()

    // FR-030: one address for the catalogue's first page, and this is it.
    await expect(wayIn).toHaveAttribute('href', '/books')

    await wayIn.click()
    await expect(page).toHaveURL(/\/books$/)
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Каталог')
  })

  test('marks the copy nobody has confirmed as an example rather than as fact', async ({
    page,
  }) => {
    await page.goto('/')

    // The claims about who this is for and what it costs are placeholders. The
    // marking is in the sentence rather than only in the colour, so it survives
    // being read aloud and being seen by somebody who cannot tell the two greys
    // apart.
    await expect(page.getByText('До прикладу:').first()).toBeVisible()
  })

  test('is the top of the hierarchy, so it carries no trail', async ({ page }) => {
    await page.goto('/')

    // A trail of one entry pointing at itself is noise.
    await expect(page.getByRole('navigation', { name: 'Навігаційний ланцюжок' })).toHaveCount(0)
  })

  test('is a real document, readable before any script runs', async ({ page }) => {
    // FR-032. Fetched rather than rendered: what a crawler and a device that has
    // not finished running JavaScript receive is the emitted HTML, not the
    // hydrated page.
    const response = await page.request.get('/')
    const html = await response.text()

    expect(response.status()).toBe(200)
    expect(html).toContain('бібліотека українських аудіокниг')
  })
})
