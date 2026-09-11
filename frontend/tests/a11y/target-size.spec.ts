import { expect, test } from '@playwright/test'
import { PAGES_IN_SCOPE, showWholePage, awaitSessionMenu } from './pages'
import { waitForHydration } from './support'

/**
 * Two obligations that no single user story owns, checked across every
 * interactive element on every page rather than across a named list of them.
 *
 * The difference matters. The catalogue spec asserts a focus outline on
 * `a.catalogue__link` and nothing else, which is exactly why the session menu
 * could render on every page of this site with no focus indicator at all and no
 * test notice: it was never in anybody's list. A rule that applies to
 * "everything focusable" has to be checked against everything focusable, or the
 * next control added is outside it again.
 *
 * The 44 by 44 minimum is achievable here with no exception only because no
 * link on this site sits inside a sentence. WCAG 2.5.8 exempts one that does,
 * and the day the first inline link is added, that exemption has to be written
 * in deliberately rather than discovered as a failure here.
 */

const MINIMUM = 44

/** Everything a keyboard can land on, or a finger can hit. */
const INTERACTIVE = 'a[href], button, input, select, textarea, [tabindex]:not([tabindex="-1"])'

for (const { name, path, settled } of PAGES_IN_SCOPE) {
  test.describe(`${name}`, () => {
    test('gives every interactive element a target of at least 44 by 44', async ({ page }) => {
      await page.goto(path)
      await expect(page.locator(settled).first()).toBeVisible()
      await showWholePage(page)

      const small = await page.evaluate(
        ({ selector, minimum }) => {
          const described: string[] = []

          for (const element of document.querySelectorAll(selector)) {
            if (element.closest('[aria-hidden="true"]') !== null) {
              continue
            }

            const box = element.getBoundingClientRect()

            // Nothing rendered has nothing to hit. A control that is genuinely
            // not on the page is not this rule's business.
            if (box.width === 0 && box.height === 0) {
              continue
            }

            if (box.width < minimum || box.height < minimum) {
              const label =
                element.getAttribute('aria-label') ??
                (element.textContent ?? '').trim().slice(0, 30)

              described.push(
                `${element.tagName.toLowerCase()} "${label}" is ` +
                  `${Math.round(box.width)}x${Math.round(box.height)}`,
              )
            }
          }

          return described
        },
        { selector: INTERACTIVE, minimum: MINIMUM },
      )

      expect(
        small,
        `SC-006 asks for ${MINIMUM} by ${MINIMUM} on every interactive element, and ${name} ` +
          `has ${small.length} below it. Grow the hit area with padding rather than a bare ` +
          'min-height, so the box grows with its content instead of the text sitting in ' +
          'the middle of a target that stops where the text does.',
      ).toEqual([])
    })

    test('shows a focus indicator on every element the keyboard reaches', async ({ page }) => {
      await page.goto(path)
      await expect(page.locator(settled).first()).toBeVisible()
      // The session menu renders nothing until the session is known, so its
      // links are not in the tab ring until the router has hydrated.
      await waitForHydration(page)
      await awaitSessionMenu(page)
      await showWholePage(page)

      const reached: string[] = []
      const invisible: string[] = []

      // Tabbed rather than focused. :focus-visible follows the browser's own
      // heuristic about whether the person is using a keyboard, and a scripted
      // focus() on a link does not satisfy it: the outline is simply not drawn,
      // and the test would report a defect that a real keyboard user never
      // sees. Pressing Tab is the interaction the requirement is about.
      //
      // The bound is finite so a focus trap shows up as a short ring rather
      // than as a hang.
      for (let step = 0; step < 40; step++) {
        await page.keyboard.press('Tab')

        const stop = await page.evaluate(() => {
          const element = document.activeElement

          if (element === null || element === document.body) {
            return null
          }

          const styles = getComputedStyle(element)
          const label =
            element.getAttribute('aria-label') ?? (element.textContent ?? '').trim().slice(0, 30)

          return {
            what: `${element.tagName.toLowerCase()} "${label}"`,
            width: styles.outlineWidth,
            style: styles.outlineStyle,
            colour: styles.outlineColor,
          }
        })

        if (stop === null) {
          continue
        }

        reached.push(stop.what)

        if (stop.style === 'none' || parseFloat(stop.width) === 0) {
          invisible.push(`${stop.what}: outline ${stop.width} ${stop.style} ${stop.colour}`)
        }
      }

      expect(reached.length, `${name} has something the keyboard can reach`).toBeGreaterThan(0)

      expect(
        [...new Set(invisible)],
        `SC-004. ${invisible.length} stop(s) on ${name} take focus with no visible ` +
          'indicator. The rule in base.css is unscoped for exactly this reason, so an ' +
          'element reaching this state is one that has had its outline removed.',
      ).toEqual([])
    })
  })
}
