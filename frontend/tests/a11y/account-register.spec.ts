import { expect, test } from '@playwright/test'
import {
  describedBy,
  expectNoBlockingViolations,
  freshEmail,
  goodPassword,
  tabOrder,
} from './account-support'
import { waitForHydration } from './support'

/**
 * User Story 1, checked the way it is used. These catch violations and
 * regressions; they do not replace the manual pass on VoiceOver and TalkBack
 * recorded in manual-verification.md, which is the only thing that proves the
 * screen is usable rather than merely conformant.
 */
test.describe('creating an account', () => {
  test('has no critical or serious accessibility violations', async ({ page }) => {
    await page.goto('/register')
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    await expectNoBlockingViolations(page, 'the registration screen')
  })

  test('the password requirements are readable before the form is submitted', async ({ page }) => {
    await page.goto('/register')
    await waitForHydration(page)

    // FR-002: available before submitting, not only after a failure, and
    // attached to the password field so a screen reader reads it there rather
    // than leaving it to be found somewhere on the page.
    //
    // Polled, because the numbers come from the API rather than being written
    // into the component twice. What is being asserted is that they arrive and
    // are attached to the field, not how fast a fetch on localhost is.
    await expect.poll(async () => describedBy(page, 'input[type="password"]')).toContain('12')

    expect(await describedBy(page, 'input[type="password"]')).toContain('символів')
  })

  test('every control is reachable by keyboard with a visible focus indicator', async ({
    page,
  }) => {
    await page.goto('/register')
    await waitForHydration(page)

    const landed = await tabOrder(page)

    expect(
      landed.some((element) => element.role === 'inputemail'),
      'the address field is reachable by keyboard alone',
    ).toBe(true)
    expect(
      landed.some((element) => element.role === 'inputpassword'),
      'the password field is reachable by keyboard alone',
    ).toBe(true)
    expect(
      landed.some((element) => element.role === 'buttonsubmit'),
      'the submit button is reachable by keyboard alone, so there is no focus trap',
    ).toBe(true)
    expect(
      landed.filter((element) => element.outline === '0px').map((element) => element.name),
      'SC-003 asks for a visible focus indicator, not merely a focusable element',
    ).toEqual([])
  })

  test('a rejected field is announced and tied to its own message', async ({ page }) => {
    await page.goto('/register')
    await waitForHydration(page)

    await page.getByLabel('Адреса електронної пошти').fill(freshEmail())
    await page.getByLabel('Пароль').fill('закоротко')
    await page.getByRole('button', { name: 'Створити обліковий запис' }).click()

    const password = page.locator('input[type="password"]')

    // FR-031: the rejected control says it is invalid, and points at the
    // message, so the reader learns which field to correct without hunting.
    await expect(password).toHaveAttribute('aria-invalid', 'true')

    const description = await describedBy(page, 'input[type="password"]')
    expect(description).toContain('12')

    // And the address, which was fine, is not marked.
    await expect(page.locator('input[type="email"]')).toHaveAttribute('aria-invalid', 'false')
  })

  test('the fields are identified so a password manager fills and saves them', async ({ page }) => {
    await page.goto('/register')

    // FR-032. These exact tokens are the convention browsers and password
    // managers key off; anything else silently stops autofill working.
    await expect(page.locator('input[type="email"]')).toHaveAttribute('autocomplete', 'username')
    await expect(page.locator('input[type="password"]')).toHaveAttribute(
      'autocomplete',
      'new-password',
    )
  })

  test('the outcome is announced and focus follows it', async ({ page }) => {
    await page.goto('/register')
    await waitForHydration(page)

    await page.getByLabel('Адреса електронної пошти').fill(freshEmail())
    await page.getByLabel('Пароль').fill(goodPassword)
    await page.getByRole('button', { name: 'Створити обліковий запис' }).click()

    // FR-033: a sighted person sees the page change, so a screen reader user is
    // told, and focus moves to the new heading rather than being left on a form
    // that is no longer there.
    const heading = page.getByRole('heading', { level: 1, name: 'Перевірте свою пошту' })
    await expect(heading).toBeVisible()
    await expect(heading).toBeFocused()

    await expect(page.locator('main').getByRole('status')).toContainText('надіслали лист')

    // Delivery is outside this system's control, so the person is offered
    // another message from here rather than being left with nothing to do if it
    // never arrives.
    await expect(
      page.getByRole('button', { name: 'Лист не надійшов? Надіслати ще раз' }),
    ).toBeVisible()
  })

  test('the screen is not offered to search engines', async ({ page }) => {
    await page.goto('/register')

    // FR-028. The page is also never prerendered, so this covers a crawler that
    // arrives through the single-page fallback.
    await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', /noindex/)
  })
})
