import { expect, test } from '@playwright/test'
import { expectNoBlockingViolations } from './account-support'
import { waitForHydration } from './support'

/**
 * User Story 5. The control that makes a borrowed device safe to hand back.
 *
 * These run against an anonymous visitor, which is the state the account menu is
 * in for anybody who has not signed in. The signed-out announcement itself is
 * covered end to end by the endpoint tests and by the manual pass, because
 * getting a real session here would mean confirming an address from a mailbox
 * inside a browser test.
 */
test.describe('the account menu', () => {
  test('has no critical or serious accessibility violations', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('navigation', { name: 'Обліковий запис' })).toBeVisible()

    await expectNoBlockingViolations(page, 'the account menu')
  })

  test('is a named landmark, so it can be jumped to rather than hunted for', async ({ page }) => {
    await page.goto('/')
    await waitForHydration(page)

    // The specification raises a shared device as an edge case: a person has to
    // be able to tell whether they are still signed in, and with a screen reader
    // that means a landmark with a name rather than something in a corner.
    const menu = page.getByRole('navigation', { name: 'Обліковий запис' })

    await expect(menu).toBeVisible()
  })

  test('carries a live region so a change of state is announced, not only shown', async ({
    page,
  }) => {
    await page.goto('/')
    await waitForHydration(page)

    const menu = page.getByRole('navigation', { name: 'Обліковий запис' })

    // FR-033. The region is present before it has anything to say: a live region
    // added at the moment it speaks is frequently missed, because the reader has
    // nothing to compare it against.
    await expect(menu.locator('[aria-live="polite"]')).toHaveCount(1)
  })

  test('is reachable by keyboard without hunting through the page', async ({ page }) => {
    await page.goto('/')
    await waitForHydration(page)

    // Wait for the session to be known. Who is signed in is established after
    // the page loads, by design, so tabbing before it lands measures the empty
    // landmark rather than the menu.
    await expect(page.getByRole('link', { name: 'Увійти' })).toBeVisible()

    await page.evaluate(() => document.body.focus())

    // Early rather than first. The requirement is that a person can get to it
    // without traversing the catalogue, not that it wins a race with whatever
    // the browser puts before it.
    const reached: string[] = []

    for (let step = 0; step < 5; step++) {
      await page.keyboard.press('Tab')
      reached.push(await page.evaluate(() => document.activeElement?.textContent?.trim() ?? ''))
    }

    expect(reached).toContain('Увійти')
  })
})
