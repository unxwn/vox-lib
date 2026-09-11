import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import { PAGES_IN_SCOPE, showWholePage, awaitSessionMenu } from './pages'
import { waitForHydration } from './support'

/**
 * The three ways the decoration can be absent, and the one shape they all have
 * to produce.
 *
 * A visitor can ask their system for increased contrast, or their system can
 * replace the palette wholesale, or the decorative layer can simply fail to
 * render. All three leave the flat --ground fill on the document and nothing
 * else, which is why the whole container is dropped rather than its two layers
 * individually: it makes the deliberate case and the failure case identical,
 * and there is then only one outcome to have got right.
 *
 * Playwright can emulate the first two, so neither needs a manual pass. The
 * third cannot be forced by any media query and is verified by hand; the
 * procedure is in quickstart.md.
 */

/**
 * `contrastIsMeasurable` is false for forced colours, and the reason is worth
 * writing down because the alternative reading is that the page is broken.
 *
 * Under forced colours Chromium replaces `color` with a system colour, and the
 * page really does render black on white: `getComputedStyle(h1).color` comes
 * back as `rgb(0, 0, 0)`. But it does *not* replace `-webkit-text-fill-color`,
 * which keeps the author's value, and axe-core reads the foreground from that
 * property in preference to `color`. Asked about the same heading in the same
 * rendering state, the browser says black and axe says #f0f3fc.
 *
 * So axe's contrast rule under forced colours measures a colour that is never
 * painted, and reports a violation for every piece of text on any dark-themed
 * site. Running it there proves nothing and would have to be worked around by
 * lightening a palette that is not what anybody sees. Every other rule still
 * runs, and the contrast obligation itself is proved in the mode above, where
 * axe and the browser agree.
 */
const MODES = [
  { name: 'increased contrast', media: { contrast: 'more' as const }, contrastIsMeasurable: true },
  {
    name: 'forced colours',
    media: { forcedColors: 'active' as const },
    contrastIsMeasurable: false,
  },
]

for (const mode of MODES) {
  for (const { name, path, settled } of PAGES_IN_SCOPE) {
    test.describe(`${name} under ${mode.name}`, () => {
      test('drops the decoration and keeps every other guarantee', async ({ page }) => {
        await page.emulateMedia(mode.media)
        await page.goto(path)
        await expect(page.locator(settled).first()).toBeVisible()
        await showWholePage(page)

        // Not merely invisible: not rendered. The element still exists in the
        // document, because dropping it is a stylesheet's decision rather than
        // a component's, so what is asserted is that it generates no box.
        const rendered = await page.evaluate(() => {
          const ground = document.querySelector('.ground')

          if (ground === null) {
            return 'the ground is not in the document at all'
          }

          const styles = getComputedStyle(ground)
          return styles.display === 'none' ? null : `display: ${styles.display}`
        })

        expect(
          rendered,
          `SC-005. The decoration is still rendered on ${name} under ${mode.name}. ` +
            'A visitor who asked for more contrast, or whose system supplies its own ' +
            'colours, gets the plain ground and nothing over it.',
        ).toBeNull()

        const builder = new AxeBuilder({ page })

        if (!mode.contrastIsMeasurable) {
          builder.disableRules(['color-contrast'])
        }

        const { violations, incomplete } = await builder.analyze()

        const blocking = violations.filter(
          (violation) => violation.impact === 'critical' || violation.impact === 'serious',
        )

        expect(
          blocking.map((violation) => `${violation.id}: ${violation.help}`),
          `no critical or serious violations on ${name} under ${mode.name}`,
        ).toEqual([])

        if (mode.contrastIsMeasurable) {
          const unchecked = incomplete
            .filter((result) => result.id === 'color-contrast')
            .flatMap((result) => result.nodes.map((node) => node.html.slice(0, 120)))

          expect(
            unchecked,
            `axe could not compute contrast for ${unchecked.length} element(s) on ${name} ` +
              `under ${mode.name}.`,
          ).toEqual([])
        }
      })

      test('still draws a focus indicator', async ({ page }) => {
        await page.emulateMedia(mode.media)
        await page.goto(path)
        await expect(page.locator(settled).first()).toBeVisible()
        await waitForHydration(page)
        await awaitSessionMenu(page)

        // Tabbed rather than focused: :focus-visible follows the browser's own
        // heuristic about whether a keyboard is in use, and a scripted focus on
        // a link does not satisfy it.
        await page.keyboard.press('Tab')

        const indicator = await page.evaluate(() => {
          const element = document.activeElement

          if (element === null || element === document.body) {
            return null
          }

          const styles = getComputedStyle(element)
          return { width: styles.outlineWidth, style: styles.outlineStyle }
        })

        expect(indicator, `${name} has something the keyboard can reach`).not.toBeNull()

        expect(
          indicator?.style !== 'none' && parseFloat(indicator?.width ?? '0') > 0,
          `SC-004 under ${mode.name} on ${name}. Forced colours replaces the palette ` +
            'wholesale, which is why the indicator is an outline redeclared in system ' +
            'colours: a box-shadow ring would vanish entirely and nothing would say ' +
            'where the keyboard is.',
        ).toBe(true)
      })
    })
  }
}

/**
 * SC-009, reflow. A page has to be readable without scrolling it sideways, both
 * on a narrow screen and when it is zoomed, and in both contrast modes, because
 * dropping the decoration changes what is painted and could in principle change
 * what is laid out.
 *
 * 200% zoom is emulated by halving the viewport rather than by scaling the
 * page, because that is exactly what zooming does to the layout: it halves the
 * number of CSS pixels available. 640 by 512 is a 1280 by 1024 window at 200%.
 */
const VIEWPORTS = [
  { name: '320 pixels wide', size: { width: 320, height: 720 } },
  { name: '200% zoom', size: { width: 640, height: 512 } },
]

for (const mode of [{ name: 'normal contrast', media: {}, contrastIsMeasurable: true }, ...MODES]) {
  for (const viewport of VIEWPORTS) {
    for (const { name, path, settled } of PAGES_IN_SCOPE) {
      test(`${name} reflows at ${viewport.name} under ${mode.name}`, async ({ page }) => {
        await page.emulateMedia(mode.media)
        await page.setViewportSize(viewport.size)
        await page.goto(path)
        await expect(page.locator(settled).first()).toBeVisible()

        const overflow = await page.evaluate(() => {
          const root = document.documentElement

          // A single pixel of slack for subpixel rounding. Anything a person
          // could actually scroll to is far wider than that.
          if (root.scrollWidth <= root.clientWidth + 1) {
            return null
          }

          const widest = [...document.querySelectorAll('body *')]
            .filter((element) => element.getBoundingClientRect().right > root.clientWidth + 1)
            .map((element) => `${element.tagName.toLowerCase()}.${element.className}`)
            .slice(0, 5)

          return { scrollWidth: root.scrollWidth, clientWidth: root.clientWidth, widest }
        })

        expect(
          overflow,
          `SC-009. ${name} scrolls sideways at ${viewport.name} under ${mode.name}. ` +
            'Two-directional scrolling is the thing reflow exists to prevent: a reader ' +
            'at high zoom loses the start of every line.',
        ).toBeNull()
      })
    }
  }
}
