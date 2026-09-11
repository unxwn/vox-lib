import { expect, test } from '@playwright/test'
import { expectNoBlockingViolations } from './account-support'

/**
 * User Story 2. The screen a person reaches from their inbox, which is the one
 * they arrive at with the least context, so what it says and where focus lands
 * matter more here than anywhere else in the feature.
 */
test.describe('confirming an address', () => {
  // A link that was never valid, so the screen shows the expired branch without
  // needing a real account. The valid branch is covered end to end by the
  // endpoint tests and by the manual pass.
  const deadLink = '/confirm?account=00000000-0000-0000-0000-000000000001&token=not-a-real-token'

  test('has no critical or serious accessibility violations', async ({ page }) => {
    await page.goto(deadLink)
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    await expectNoBlockingViolations(page, 'the confirmation screen')
  })

  test('the outcome is announced rather than only shown', async ({ page }) => {
    await page.goto(deadLink)

    const heading = page.getByRole('heading', { level: 1, name: 'Посилання більше не дійсне' })
    await expect(heading).toBeVisible()

    // FR-033: a sighted person sees the heading change, so a screen reader user
    // is told. This one is an alert rather than a status, because the person
    // asked for something and did not get it.
    await expect(page.locator('main').getByRole('alert')).toContainText(
      'Термін дії посилання минув',
    )
  })

  test('focus follows the outcome', async ({ page }) => {
    await page.goto(deadLink)

    // Otherwise the reader is left on a heading that still says the work is
    // going on, with the answer somewhere below.
    await expect(page.getByRole('heading', { level: 1 })).toBeFocused()
  })

  test('an expired link offers another message from the same screen', async ({ page }) => {
    await page.goto(deadLink)

    const field = page.getByLabel('Адреса електронної пошти')
    await expect(field).toBeVisible()
    await expect(field).toHaveAttribute('autocomplete', 'username')

    await field.fill('someone@example.com')
    await page.getByRole('button', { name: 'Надіслати новий лист' }).click()

    await expect(page.locator('main').getByRole('status')).toContainText('надіслали новий лист')
  })

  test('the screen is not offered to search engines', async ({ page }) => {
    await page.goto(deadLink)

    await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', /noindex/)
  })
})
