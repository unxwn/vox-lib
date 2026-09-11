import { expect, type Page } from '@playwright/test'

/**
 * Every page this site serves, which is the scope the visual identity applies
 * to. The four specs the identity added all walk this list, so "every page in
 * scope" means the same thing in each of them and adding a route means adding
 * it in one place.
 *
 * The addresses match the ones the existing specs already use, so a page is
 * checked here in the same state it is checked in elsewhere.
 */
export type PageInScope = {
  /** What to call it in a failure message. */
  readonly name: string
  readonly path: string
  /** Something that is only on the page once it has actually rendered. */
  readonly settled: string
}

/** Seeded in Ukrainian, with chapters, a description and multiple authors. */
const BOOK = 'khiba-revut-voly'

/**
 * A link that was never valid, so the account screens reached from an inbox
 * show their expired branch without needing a real account. The same pair the
 * account specs use.
 */
const DEAD_LINK = 'account=00000000-0000-0000-0000-000000000001&token=not-a-real-token'

export const PAGES_IN_SCOPE: readonly PageInScope[] = [
  { name: 'the catalogue', path: '/', settled: 'h1' },
  { name: 'the second page of the catalogue', path: '/page/2', settled: 'h1' },
  { name: "one book's page", path: `/books/${BOOK}`, settled: 'h1' },
  { name: 'search, with results', path: '/search?q=сон', settled: 'main [role="status"]' },
  { name: 'not found', path: '/no-such-page-exists', settled: 'h1' },
  { name: 'register', path: '/register', settled: 'h1' },
  { name: 'confirming an address', path: `/confirm?${DEAD_LINK}`, settled: 'h1' },
  { name: 'sign in', path: '/sign-in', settled: 'h1' },
  { name: 'forgot password', path: '/forgot-password', settled: 'h1' },
  { name: 'a new password', path: `/reset-password?${DEAD_LINK}`, settled: 'h1' },
]

/**
 * Grows the viewport until the whole document fits inside it.
 *
 * Axe resolves what is behind a text node by sampling `elementsFromPoint` at
 * several points across the node's box. A point below the fold is outside the
 * viewport, where that call returns nothing at all, so the samples disagree and
 * axe reports the node as "partially obscured by other elements" and marks its
 * contrast result *incomplete*. Nothing is wrong with the page: the only thing
 * that decides whether it happens is whether a line of text happens to straddle
 * the bottom edge, which moves with every padding change.
 *
 * Left alone that would make the zero-incompletes assertion flap, and the
 * temptation would be to filter the message out, which would also throw away
 * the genuine overlaps it reports. Showing the whole page instead removes the
 * artefact and leaves the assertion total: with nothing below the fold, an
 * "obscured" result afterwards is a real one.
 */
export async function showWholePage(page: Page, width = 1280) {
  // Twice, because resizing reflows the document and can change its height.
  for (let attempt = 0; attempt < 2; attempt++) {
    const height = await page.evaluate(() => document.documentElement.scrollHeight)
    const wanted = Math.min(Math.max(height + 64, 720), 8000)

    if (page.viewportSize()?.height === wanted) {
      return
    }

    await page.setViewportSize({ width, height: wanted })
  }
}

/**
 * Waits for the session menu to know whether anybody is signed in.
 *
 * It renders nothing at all until its request comes back, so its links join the
 * tab ring a moment after the router hydrates. Anything that presses Tab, or
 * compares one tab ring against another, has to wait for it or it is timing the
 * network rather than testing the page.
 */
export async function awaitSessionMenu(page: Page) {
  await expect(page.locator('.session-menu a, .session-menu button').first()).toBeVisible()
}
