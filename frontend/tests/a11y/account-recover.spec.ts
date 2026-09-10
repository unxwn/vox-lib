import { expect, test } from '@playwright/test'
import { describedBy, expectNoBlockingViolations, tabOrder } from './account-support'
import { waitForHydration } from './support'

/**
 * User Story 4. Both screens, because a person who has lost access is the person
 * least able to work around a screen that does not announce what happened.
 */
test.describe('asking for a way back in', () => {
  test('has no critical or serious accessibility violations', async ({ page }) => {
    await page.goto('/forgot-password')
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    await expectNoBlockingViolations(page, 'the recovery request screen')
  })

  test('is operable by keyboard with a visible focus indicator', async ({ page }) => {
    await page.goto('/forgot-password')
    await waitForHydration(page)

    const landed = await tabOrder(page)

    expect(landed.some((element) => element.role === 'inputemail')).toBe(true)
    expect(landed.some((element) => element.role === 'buttonsubmit')).toBe(true)
    expect(landed.filter((element) => element.outline === '0px')).toEqual([])
  })

  test('the answer says nothing about whether the address is known', async ({ page }) => {
    await page.goto('/forgot-password')
    await waitForHydration(page)

    await page.getByLabel('Адреса електронної пошти').fill('nobody-at-all@example.com')
    await page.getByRole('button', { name: 'Надіслати посилання' }).click()

    const heading = page.getByRole('heading', { level: 1, name: 'Перевірте свою пошту' })
    await expect(heading).toBeVisible()
    await expect(heading).toBeFocused()

    // "If there is an account" rather than "we have sent". FR-005 reaches the
    // wording, not only the status code.
    await expect(page.locator('main').getByRole('status')).toContainText('Якщо для адреси')
  })
})

test.describe('setting a new password', () => {
  const link = '/reset-password?account=00000000-0000-0000-0000-000000000001&token=not-real'

  test('has no critical or serious accessibility violations', async ({ page }) => {
    await page.goto(link)
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    await expectNoBlockingViolations(page, 'the new password screen')
  })

  test('the requirements are readable before the form is submitted', async ({ page }) => {
    await page.goto(link)
    await waitForHydration(page)

    // The one place a person chooses a password having already lost access
    // once, so learning the rule only after being refused is the worst possible
    // moment for it. FR-002.
    //
    // Polled, because the numbers come from the API rather than being written
    // into the component twice.
    await expect.poll(async () => describedBy(page, 'input[type="password"]')).toContain('12')
  })

  test('an expired link says so and offers a new one', async ({ page }) => {
    await page.goto(link)
    await waitForHydration(page)

    await page.getByLabel('Новий пароль').fill('достатньо-довгий-пароль-2026')
    await page.getByRole('button', { name: 'Встановити пароль' }).click()

    const heading = page.getByRole('heading', { level: 1, name: 'Посилання більше не дійсне' })
    await expect(heading).toBeVisible()
    await expect(heading).toBeFocused()

    await expect(page.getByRole('link', { name: 'Надіслати нове посилання' })).toBeVisible()
  })

  test('the screen is not offered to search engines', async ({ page }) => {
    await page.goto(link)

    await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', /noindex/)
  })
})
