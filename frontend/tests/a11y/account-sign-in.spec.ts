import { expect, test } from '@playwright/test'
import { expectNoBlockingViolations, tabOrder } from './account-support'
import { waitForHydration } from './support'

/**
 * User Story 3. Signing in, and being told when it did not work.
 *
 * These use an address that has no account, deliberately: the refusal is the
 * part with the accessibility obligation, because a person who is refused has to
 * learn that they were, and why, without searching the page.
 */
test.describe('signing in', () => {
  test('has no critical or serious accessibility violations', async ({ page }) => {
    await page.goto('/sign-in')
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    await expectNoBlockingViolations(page, 'the sign-in screen')
  })

  test('every control is reachable by keyboard with a visible focus indicator', async ({
    page,
  }) => {
    await page.goto('/sign-in')
    await waitForHydration(page)

    const landed = await tabOrder(page)

    expect(landed.some((element) => element.role === 'inputemail')).toBe(true)
    expect(landed.some((element) => element.role === 'inputpassword')).toBe(true)
    expect(landed.some((element) => element.role === 'buttonsubmit')).toBe(true)
    expect(
      landed.filter((element) => element.outline === '0px').map((element) => element.name),
      'SC-003 asks for a visible focus indicator on every focusable control',
    ).toEqual([])
  })

  test('a failed sign-in is announced without the person having to look for it', async ({
    page,
  }) => {
    await page.goto('/sign-in')
    await waitForHydration(page)

    await page.getByLabel('Адреса електронної пошти').fill('nobody-at-all@example.com')
    await page.getByLabel('Пароль').fill('якийсь-довгий-пароль-2026')
    await page.getByRole('button', { name: 'Увійти' }).click()

    // An alert rather than a status: the person asked to be let in and was not.
    const alert = page.locator('main').getByRole('alert')
    await expect(alert).toBeVisible()
    await expect(alert).toContainText(/не знайдено|забагато/i)
  })

  test('the wording of a refusal says nothing about whether the address is known', async ({
    page,
  }) => {
    await page.goto('/sign-in')
    await waitForHydration(page)

    await page.getByLabel('Адреса електронної пошти').fill('nobody-at-all@example.com')
    await page.getByLabel('Пароль').fill('якийсь-довгий-пароль-2026')
    await page.getByRole('button', { name: 'Увійти' }).click()

    // FR-005 seen from the interface. The words a person reads must not do what
    // the status code carefully does not.
    const alert = await page.locator('main').getByRole('alert').textContent()

    expect(alert ?? '').not.toMatch(/не існує|немає такого користувача|не зареєстровано/i)
  })

  test('the fields are identified so a password manager fills them', async ({ page }) => {
    await page.goto('/sign-in')

    await expect(page.locator('input[type="email"]')).toHaveAttribute('autocomplete', 'username')
    // current-password, so the manager offers what it has rather than generating.
    await expect(page.locator('input[type="password"]')).toHaveAttribute(
      'autocomplete',
      'current-password',
    )
  })

  test('a way to recover a forgotten password is reachable from here', async ({ page }) => {
    await page.goto('/sign-in')

    await expect(page.getByRole('link', { name: 'Забули пароль?' })).toBeVisible()
  })

  test('the screen is not offered to search engines', async ({ page }) => {
    await page.goto('/sign-in')

    await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', /noindex/)
  })

  test('an anonymous visitor is offered a way in from every page', async ({ page }) => {
    await page.goto('/')
    await waitForHydration(page)

    const menu = page.getByRole('navigation', { name: 'Обліковий запис' })
    await expect(menu.getByRole('link', { name: 'Увійти' })).toBeVisible()
  })
})
