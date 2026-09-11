import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import { PAGES_IN_SCOPE, showWholePage, awaitSessionMenu } from './pages'
import { waitForHydration } from './support'
import { tabOrder } from './account-support'
import { decodePng, luminance, luminanceOf } from './colour'

/**
 * Contrast, over a ground that is no longer a flat colour.
 *
 * Two separate assertions, and the second is the one that matters.
 *
 * The first is the ordinary one: no critical or serious violations. The second
 * is that axe returned no *incomplete* results for the colour contrast rule,
 * and it exists because of how axe resolves what is behind a text node. It
 * walks the stack of elements covering the text from the front and stops at the
 * first opaque one, and it treats an element carrying a background-image as a
 * stop that marks the result incomplete rather than as a colour it can use.
 *
 * So the moment a decorated ground exists, any text not sitting on a fully
 * opaque fill has its contrast result silently downgraded from pass to
 * incomplete. Incomplete is neither critical nor serious, so the violation
 * count stays at zero and the suite stays green while proving nothing at all
 * about contrast. A region at 95% alpha looks identical to an opaque one and
 * has exactly that effect.
 *
 * If this spec ever reports incomplete results, a region has lost its opacity.
 * Find it. Relaxing the assertion removes the only thing standing between this
 * feature and a green suite that checks nothing.
 */

for (const { name, path, settled } of PAGES_IN_SCOPE) {
  test.describe(`contrast on ${name}`, () => {
    test('has no critical or serious violations, and none left unchecked', async ({ page }) => {
      await page.goto(path)
      await expect(page.locator(settled).first()).toBeVisible()
      await showWholePage(page)

      const { violations, incomplete } = await new AxeBuilder({ page }).analyze()

      const blocking = violations.filter(
        (violation) => violation.impact === 'critical' || violation.impact === 'serious',
      )

      expect(
        blocking.map((violation) => `${violation.id}: ${violation.help}`),
        `SC-002 allows no critical or serious violations on ${name}`,
      ).toEqual([])

      const unchecked = incomplete
        .filter((result) => result.id === 'color-contrast')
        .flatMap((result) => result.nodes.map((node) => node.html.slice(0, 120)))

      expect(
        unchecked,
        `axe could not compute contrast for ${unchecked.length} element(s) on ${name}. ` +
          'The usual cause is a region above the ground that is not fully opaque: axe ' +
          'then walks past it, reaches the decorated ground, and stops at a ' +
          'background-image it cannot read. Every contrast result on this page is now ' +
          'unchecked. Find the translucent region rather than relaxing this.',
      ).toEqual([])
    })
  })
}

/**
 * The ground's own obligation, measured rather than computed.
 *
 * The grain is a filter composited at low alpha over a fill and repeated, and
 * the watermark sits on top of it. What that adds up to on screen cannot be
 * derived from the tokens, so it is sampled off a real screenshot with the
 * content hidden. Both tokens are a declaration of a limit: the composite may
 * move between them and nowhere else. Below --ground-grain-lo the texture eats
 * the fill; above --ground-grain-hi the "on ground worst case" rows of the
 * contrast matrix stop being the worst case, and text over the ground can drop
 * under its floor without any token changing.
 */
test.describe('the decorated ground', () => {
  test('stays inside the luminance band its tokens declare', async ({ page }) => {
    await page.goto('/')
    await expect(page.locator('h1').first()).toBeVisible()

    const band = await page.evaluate(() => {
      const styles = getComputedStyle(document.documentElement)
      return {
        lo: styles.getPropertyValue('--ground-grain-lo').trim(),
        hi: styles.getPropertyValue('--ground-grain-hi').trim(),
      }
    })

    // Everything but the ground, out of the way. The ground is fixed to the
    // viewport, so removing the content from the flow does not move it.
    await page.addStyleTag({
      content: 'body > *:not(.ground) { display: none !important; }',
    })
    await page.waitForFunction(() => document.fonts.status === 'loaded')

    const pixels = decodePng(await page.screenshot({ type: 'png' }))

    const lo = luminanceOf(band.lo)
    const hi = luminanceOf(band.hi)

    // One 8-bit step of slack in each direction, for the rounding a screenshot
    // does. Anything wider would let a real excursion through.
    const step = luminance(3, 3, 3) - luminance(2, 2, 2)

    let darkest = Number.POSITIVE_INFINITY
    let lightest = Number.NEGATIVE_INFINITY
    let sampled = 0

    // A grid rather than a block: the grain is a repeated 160 pixel tile and
    // the watermark is on a -24 degree rotation, so a single region would miss
    // whichever of the two it did not land on.
    for (let y = 0; y < pixels.height; y += 7) {
      for (let x = 0; x < pixels.width; x += 11) {
        const at = (y * pixels.width + x) * 4
        const value = luminance(pixels.data[at], pixels.data[at + 1], pixels.data[at + 2])

        darkest = Math.min(darkest, value)
        lightest = Math.max(lightest, value)
        sampled++
      }
    }

    expect(sampled, 'the screenshot had pixels to sample').toBeGreaterThan(1000)

    expect(
      darkest,
      `the ground reaches a luminance of ${darkest.toFixed(5)}, darker than ` +
        `--ground-grain-lo (${band.lo}, ${lo.toFixed(5)}). The grain is eating the fill.`,
    ).toBeGreaterThanOrEqual(lo - step)

    expect(
      lightest,
      `the ground reaches a luminance of ${lightest.toFixed(5)}, lighter than ` +
        `--ground-grain-hi (${band.hi}, ${hi.toFixed(5)}). The "on ground, worst case" ` +
        'rows of the contrast matrix are computed against that value, so text over ' +
        'the ground may now be below its floor with no token having changed.',
    ).toBeLessThanOrEqual(hi + step)
  })
})

/**
 * The decoration reaching nothing, which is the whole of FR-008.
 *
 * Three separate ways it could reach somebody, checked separately because they
 * fail separately: it could be announced, it could take a tab stop, or it could
 * change the order in which the real controls are reached.
 *
 * The wordmark repeats dozens of times on every page. Reachable at all, it
 * would be the loudest thing on this site to a screen reader, and it says
 * nothing that is not already in the heading.
 */
test.describe('the decoration', () => {
  for (const { name, path, settled } of PAGES_IN_SCOPE) {
    test(`is absent from the accessibility tree on ${name}`, async ({ page }) => {
      await page.goto(path)
      await expect(page.locator(settled).first()).toBeVisible()

      const ground = page.locator('.ground')
      await expect(ground, 'the ground is rendered at all').toHaveCount(1)

      // Every node inside the ground, as the accessibility tree sees it. An
      // aria-hidden subtree contributes nothing, so the whole thing has to be
      // missing rather than merely unlabelled.
      const exposed = await page.evaluate(() => {
        const ground = document.querySelector('.ground')

        if (ground === null) {
          return ['the ground is not in the document']
        }

        const reachable: string[] = []

        if (ground.getAttribute('aria-hidden') !== 'true') {
          reachable.push('the ground itself is not aria-hidden')
        }

        for (const element of ground.querySelectorAll('*')) {
          if (element.getAttribute('aria-hidden') === 'false') {
            reachable.push(`${element.tagName.toLowerCase()} re-exposes itself with aria-hidden`)
          }

          if (element.hasAttribute('role') && element.getAttribute('role') !== 'presentation') {
            reachable.push(
              `${element.tagName.toLowerCase()} carries role=${element.getAttribute('role')}`,
            )
          }
        }

        return reachable
      })

      expect(exposed, `FR-008. Nothing decorative may be announced on ${name}.`).toEqual([])

      // The aria snapshot is the browser's own tree rendered as text, so this
      // asks the question the way assistive technology would rather than by
      // reading attributes back off the markup.
      const snapshot = await page.locator('body').ariaSnapshot()

      expect(
        snapshot.includes('ZALIZNA'),
        `the wordmark appears in the accessibility tree on ${name}, which means a screen ` +
          'reader will read it, once per instance, on every page.',
      ).toBe(false)
    })

    test(`holds nothing focusable on ${name}`, async ({ page }) => {
      await page.goto(path)
      await expect(page.locator(settled).first()).toBeVisible()

      const focusable = await page.evaluate(() => {
        const ground = document.querySelector('.ground')

        if (ground === null) {
          return ['the ground is not in the document']
        }

        return [...ground.querySelectorAll('a[href], button, input, [tabindex], svg')]
          .filter((element) => {
            // An <svg> could take focus in older engines, which is what
            // focusable="false" is for.
            if (element.tagName.toLowerCase() === 'svg') {
              return element.getAttribute('focusable') !== 'false'
            }

            return element.getAttribute('tabindex') !== '-1'
          })
          .map((element) => element.tagName.toLowerCase())
      })

      expect(
        focusable,
        `FR-008. A tab stop inside the decoration on ${name} lands somewhere with nothing ` +
          'to say and no way to tell why the keyboard stopped there.',
      ).toEqual([])
    })
  }

  /**
   * The order the keyboard reaches things in, unchanged. The ground is a
   * sibling of the content and the first child of the body, so if it were ever
   * reachable this is where it would show up: before everything.
   */
  test('does not change what the keyboard reaches, or in what order', async ({ page }) => {
    await page.goto('/sign-in')
    await expect(page.locator('h1').first()).toBeVisible()
    await waitForHydration(page)
    await awaitSessionMenu(page)

    const withGround = await tabOrder(page)

    // A fresh load rather than tabbing on: tabOrder starts from wherever focus
    // already is, and the body is not focusable, so a second pass on the same
    // document continues from the last control and returns the same ring
    // rotated. Reloading puts the keyboard back at the start of the document.
    await page.goto('/sign-in')
    await expect(page.locator('h1').first()).toBeVisible()
    await waitForHydration(page)
    await awaitSessionMenu(page)

    await page.evaluate(() => document.querySelector('.ground')?.remove())

    const withoutGround = await tabOrder(page)

    expect(withGround.length, 'the page has controls to reach').toBeGreaterThan(0)
    expect(
      withGround,
      'removing the decoration changes nothing about what the keyboard reaches or in ' +
        'what order, because the decoration was never part of it.',
    ).toEqual(withoutGround)
  })
})
