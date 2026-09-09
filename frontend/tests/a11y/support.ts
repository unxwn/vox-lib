import type { Page } from '@playwright/test'

/**
 * Waits until the router has taken over the document.
 *
 * Until it has, a link is an ordinary link and following it reloads the page,
 * which is the correct fallback but is not the behaviour the focus requirements
 * describe. Clicking too early therefore tests the wrong thing, and does so
 * intermittently: it only happens when the machine is busy enough for the click
 * to beat hydration. The router publishes its instance on the window when it
 * hydrates, so that is the signal to wait for.
 */
export async function waitForHydration(page: Page) {
  await page.waitForFunction(() => {
    const router = (
      window as unknown as {
        __reactRouterDataRouter?: { state?: { initialized?: boolean } }
      }
    ).__reactRouterDataRouter

    return router?.state?.initialized === true
  })
}
