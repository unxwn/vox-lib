import AxeBuilder from '@axe-core/playwright'
import { expect, type Page } from '@playwright/test'

/**
 * Shared checks for the account screens. They all carry the same obligations,
 * so the assertions are written once: SC-002 for violations, SC-003 for the
 * keyboard, FR-031 for a rejected field, and FR-032 for autofill.
 */

/** An address no other run has used, so a test never collides with an old account. */
export function freshEmail(prefix = 'a11y'): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.com`
}

export const goodPassword = 'дуже-довгий-пароль-2026'

export async function expectNoBlockingViolations(page: Page, what: string) {
  const { violations } = await new AxeBuilder({ page }).analyze()

  const blocking = violations.filter(
    (violation) => violation.impact === 'critical' || violation.impact === 'serious',
  )

  expect(
    blocking.map((violation) => `${violation.id}: ${violation.help}`),
    `SC-002 allows no critical or serious violations on ${what}`,
  ).toEqual([])
}

/**
 * Tabs from the top of the document and returns what focus landed on, in order.
 *
 * The bound is finite so that a focus trap shows up as a missing control rather
 * than as a hang, which is what SC-003 is actually asking about.
 */
export async function tabOrder(page: Page, steps = 25) {
  await page.evaluate(() => document.body.focus())

  const landed: { role: string; name: string; outline: string }[] = []

  for (let step = 0; step < steps; step++) {
    await page.keyboard.press('Tab')

    const current = await page.evaluate(() => {
      const element = document.activeElement

      if (element === null || element === document.body) {
        return null
      }

      const label =
        element.getAttribute('aria-label') ??
        (element.id !== ''
          ? (document.querySelector(`label[for="${element.id}"]`)?.textContent ?? '')
          : '') ??
        ''

      return {
        role: element.tagName.toLowerCase() + (element.getAttribute('type') ?? ''),
        name: (label || (element.textContent ?? '')).trim(),
        outline: getComputedStyle(element).outlineWidth,
      }
    })

    if (current !== null) {
      landed.push(current)
    }
  }

  return landed
}

/**
 * The accessible description a screen reader reads for a control, resolved
 * through aria-describedby exactly as assistive technology resolves it.
 */
export async function describedBy(page: Page, selector: string): Promise<string> {
  return page.evaluate((target) => {
    const element = document.querySelector(target)

    if (element === null) {
      return ''
    }

    return (element.getAttribute('aria-describedby') ?? '')
      .split(' ')
      .filter((id) => id !== '')
      .map((id) => document.getElementById(id)?.textContent ?? '')
      .join(' ')
      .trim()
  }, selector)
}
