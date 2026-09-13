import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import { AUTHOR, BOOK, showWholePage } from './pages'
import { signIn } from './session-fixture'
import { waitForHydration } from './support'

/**
 * The half of the site nothing has ever checked.
 *
 * Every browser assertion this suite makes has walked the anonymous variant:
 * axe, contrast, target size and focus have all only ever seen a page where the
 * account slot resolved to a single "Увійти" link. Signed in, that slot holds an
 * address and a form, and it appears on every page — so it is chrome the whole
 * site carries and none of it has been measured (SC-021).
 *
 * Serial, and one session for the file: registering, waiting for a message,
 * confirming and signing in is slow, and doing it per test would spend most of
 * the run in the post.
 */
/**
 * Where the session cookie is parked between establishing it once and reusing
 * it in every test below.
 */
const STATE = 'test-results/signed-in-state.json'

test.describe('while signed in', () => {
  test.describe.configure({ mode: 'serial' })

  let email = ''

  test.beforeAll(async ({ browser }) => {
    /*
      An explicitly empty context. `test.use` below points the page fixture at
      STATE, and that applies to anything this hook opens too — so the default
      context would try to read the file this hook exists to write.
    */
    const context = await browser.newContext({ storageState: undefined })
    const page = await context.newPage()

    email = await signIn(page)

    await context.storageState({ path: STATE })
    await context.close()
  })

  test.use({ storageState: STATE })

  /** The pages whose chrome depends on who is looking. */
  const PAGES = [
    { name: 'the landing page', path: '/' },
    { name: 'the catalogue', path: '/books' },
    { name: "one book's page", path: `/books/${BOOK}` },
    { name: "one author's page", path: `/authors/${AUTHOR}` },
  ] as const

  for (const { name, path } of PAGES) {
    test(`${name} has no critical or serious violations while signed in`, async ({ page }) => {
      await page.goto(path)
      await expect(page.locator('.session-menu__slot[data-session="signedIn"]')).toBeVisible()
      await showWholePage(page)

      const { violations } = await new AxeBuilder({ page }).analyze()
      const blocking = violations.filter(
        (violation) => violation.impact === 'critical' || violation.impact === 'serious',
      )

      expect(
        blocking.map((violation) => `${violation.id}: ${violation.help}`),
        `SC-022 allows no critical or serious violations on ${name}`,
      ).toEqual([])
    })

    test(`${name} leaves no contrast result unchecked while signed in`, async ({ page }) => {
      await page.goto(path)
      await expect(page.locator('.session-menu__slot[data-session="signedIn"]')).toBeVisible()
      await showWholePage(page)

      const { incomplete } = await new AxeBuilder({ page }).analyze()

      /*
        SC-023, and the failure mode that matters more than a violation. An
        incomplete means axe stopped before it could decide — usually because a
        region above the decorated ground is not fully opaque — and incomplete
        is neither critical nor serious, so the assertion above would stay green
        while checking nothing. The signed-in banner is new chrome and is
        exactly the sort of thing that arrives translucent.
      */
      const unchecked = incomplete
        .filter((result) => result.id === 'color-contrast')
        .flatMap((result) => result.nodes.map((node) => node.html.slice(0, 120)))

      expect(unchecked, `axe could not compute contrast on ${name} while signed in`).toEqual([])
    })

    test(`${name} keeps its targets at 44 by 44 while signed in`, async ({ page }) => {
      await page.goto(path)
      await expect(page.locator('.session-menu__slot[data-session="signedIn"]')).toBeVisible()
      await showWholePage(page)

      const small = await page.evaluate(() => {
        const described: string[] = []

        for (const element of document.querySelectorAll<HTMLElement>(
          'a[href], button, input, select, textarea, [tabindex]:not([tabindex="-1"])',
        )) {
          if (element.closest('[aria-hidden="true"]') !== null) {
            continue
          }

          const box = element.getBoundingClientRect()

          if (box.width === 0 && box.height === 0) {
            continue
          }

          if (getComputedStyle(element).clipPath.startsWith('inset(50%')) {
            continue
          }

          if (box.width < 44 || box.height < 44) {
            described.push(
              `${element.tagName.toLowerCase()} "${(element.textContent ?? '').trim().slice(0, 30)}"` +
                ` is ${Math.round(box.width)}x${Math.round(box.height)}`,
            )
          }
        }

        return described
      })

      expect(small, `${name} has ${small.length} targets below 44 by 44 while signed in`).toEqual(
        [],
      )
    })
  }

  test('the banner says who is signed in, and offers a way out', async ({ page }) => {
    await page.goto('/books')

    const slot = page.locator('.session-menu__slot[data-session="signedIn"]')
    await expect(slot).toBeVisible()

    // The address is clipped visually so it cannot resize the reserved box, but
    // it is whole in the accessibility tree, which is what a screen reader reads.
    await expect(slot.locator('.session-menu__who')).toHaveText(email)
    await expect(slot.getByRole('button', { name: 'Вийти' })).toBeVisible()

    // And no way in, because they are already in.
    await expect(slot.getByRole('link', { name: 'Увійти' })).toHaveCount(0)
  })

  test('the account controls are still the last stop in the banner', async ({ page }) => {
    await page.goto('/books')
    await expect(page.locator('.session-menu__slot[data-session="signedIn"]')).toBeVisible()
    await waitForHydration(page)

    // FR-008 in the state it was never checked in: signing out is reachable
    // before the content begins, so nobody has to traverse a page to leave.
    await page.evaluate(() => document.body.focus())

    const reached: string[] = []

    for (let step = 0; step < 30; step++) {
      await page.keyboard.press('Tab')

      const inMain = await page.evaluate(
        () => document.getElementById('main')?.contains(document.activeElement) ?? false,
      )

      if (inMain) {
        break
      }

      reached.push(await page.evaluate(() => document.activeElement?.textContent?.trim() ?? ''))
    }

    expect(reached, 'signing out is reached before the content').toContain('Вийти')
  })
})
