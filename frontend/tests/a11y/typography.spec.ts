import { expect, test } from '@playwright/test'
import { PAGES_IN_SCOPE } from './pages'

/**
 * That the interface is actually set in Fixel, and that no page ever shows
 * empty space where its words should be.
 *
 * A missing glyph does not error. The browser substitutes it from the next
 * family in the stack and the page looks fine, which in a Ukrainian interface
 * is how ґ can be quietly absent for months: it sits at U+0490, outside the
 * main Cyrillic block, so a subset built from a "cyrillic" preset drops it and
 * nothing anywhere says so.
 *
 * The check is advance widths, measured three times. The face is put in front
 * of three unrelated fallbacks, and the character is measured in each. If the
 * face has the glyph it wins in all three and the three widths are identical;
 * if it does not, three different fonts draw the character and they will not
 * agree.
 *
 * Comparing against one fallback instead is not enough, and this was found the
 * hard way: Fixel Text draws the digit 2 at 547 units where the metric-matched
 * fallback draws it at 547.4, which the canvas rounds to the same number. One
 * comparison called a glyph that is present substituted. Three fonts do not
 * collide like that.
 */

/** Only the face. The fallbacks are appended by the measurement below. */
const FIXEL_TEXT = "'Fixel Text'"
const FIXEL_DISPLAY = "'Fixel Display'"

/**
 * Named one at a time because they are the four the subsetting step is most
 * likely to lose, and a failure naming the character is worth more than a
 * failure naming a page.
 */
const UKRAINIAN = ['і', 'ї', 'є', 'ґ', 'І', 'Ї', 'Є', 'Ґ']

/**
 * Outside every range the subset was built with, so Fixel cannot have it. It is
 * the control: if this one measures as *different*, the method is not detecting
 * substitution at all and every pass below is meaningless.
 */
const NOT_IN_THE_SUBSET = 'Ω'

/**
 * Whether each character is drawn from the named face or substituted.
 *
 * Measured at a size where a real difference cannot hide in a rounding, behind
 * three fallbacks chosen to be as unlike each other as the platform offers.
 */
async function drawnFromFace(
  page: import('@playwright/test').Page,
  characters: string[],
  face: string,
) {
  return page.evaluate(
    ({ characters, face }) => {
      const canvas = document.createElement('canvas')
      const context = canvas.getContext('2d')

      if (context === null) {
        throw new Error('no 2d context')
      }

      const width = (character: string, stack: string) => {
        context.font = `1000px ${stack}`
        return context.measureText(character).width
      }

      return characters.map((character) => {
        const widths = [`${face}, monospace`, `${face}, serif`, `${face}, 'Fixel Fallback'`].map(
          (stack) => width(character, stack),
        )

        return {
          character,
          widths,
          fromFace: widths.every((measured) => Math.abs(measured - widths[0]) < 0.01),
        }
      })
    },
    { characters, face },
  )
}

test.describe('the typeface', () => {
  test('carries the Ukrainian letters, in both faces', async ({ page }) => {
    await page.goto('/')
    await page.waitForFunction(() => document.fonts.status === 'loaded')

    for (const [face, stack] of [
      ['Fixel Text', FIXEL_TEXT],
      ['Fixel Display', FIXEL_DISPLAY],
    ]) {
      const measured = await drawnFromFace(page, [...UKRAINIAN, NOT_IN_THE_SUBSET], stack)

      for (const { character, widths, fromFace } of measured) {
        if (character === NOT_IN_THE_SUBSET) {
          expect(
            fromFace,
            `${character} is outside every range the subset was built with, so ${face} ` +
              `cannot have it and three different fonts should have drawn it. They all ` +
              `agreed at ${widths.join(', ')}, which means this test cannot tell ` +
              'substitution from coverage and none of its other results mean anything.',
          ).toBe(false)
          continue
        }

        expect(
          fromFace,
          `${character} is not drawn from ${face}: behind three different fallbacks it ` +
            `measured ${widths.join(', ')}, so the face has no glyph for it and each ` +
            'fallback drew its own. U+0490 and U+0491 sit outside the main Cyrillic ' +
            'block and are the usual casualty of a subset built from a preset.',
        ).toBe(true)
      }
    }
  })

  for (const { name, path, settled } of PAGES_IN_SCOPE) {
    test(`draws every character ${name} renders from Fixel`, async ({ page }) => {
      await page.goto(path)
      await expect(page.locator(settled).first()).toBeVisible()
      await page.waitForFunction(() => document.fonts.status === 'loaded')

      const characters = await page.evaluate(() => {
        const text = document.body.innerText
        return [...new Set(text)].filter((character) => /\S/u.test(character))
      })

      expect(characters.length, `${name} renders some text`).toBeGreaterThan(10)

      const measured = await drawnFromFace(page, characters, FIXEL_TEXT)
      const substituted = measured
        .filter(({ fromFace }) => !fromFace)
        .map(({ character }) => `${character} (U+${character.codePointAt(0)?.toString(16)})`)

      expect(
        substituted,
        `${name} renders ${substituted.length} character(s) that Fixel does not have, ` +
          'so they are silently substituted from the fallback family. Either the ' +
          'subset needs the range that holds them, or the copy uses a character the ' +
          'interface was never meant to.',
      ).toEqual([])
    })
  }

  /**
   * A face that is still arriving must never leave a page with empty space
   * where its words should be. `swap` shows the fallback immediately and
   * exchanges it when the real face lands, which is why the fallback carries
   * metric overrides: the exchange changes letterforms without moving lines.
   */
  test('loads every face with font-display: swap', async ({ page }) => {
    await page.goto('/')
    await page.waitForFunction(() => document.fonts.status === 'loaded')

    const faces = await page.evaluate(() =>
      [...document.fonts].map((face) => ({ family: face.family, display: face.display })),
    )

    expect(faces.length, 'the document declares some faces').toBeGreaterThan(0)

    const blocking = faces.filter((face) => face.display !== 'swap')

    expect(
      blocking.map((face) => `${face.family}: font-display: ${face.display}`),
      'SC-012. Any value but swap can leave a page rendering invisible text while ' +
        'a face is on its way, which on a slow connection is a blank page that ' +
        'looks like a failure.',
    ).toEqual([])
  })
})
