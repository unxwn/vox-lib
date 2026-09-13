import { expect, test } from '@playwright/test'
import { awaitSessionMenu, PAGES_IN_SCOPE, AUTHOR, BOOK, SEARCH_TERM } from './pages'
import { waitForHydration } from './support'

/**
 * The skeleton every page carries: the landmarks a screen reader moves between,
 * the name the site says once, the trail above the content, and the one Tab that
 * gets a keyboard past all of it.
 *
 * Before this feature a screen reader was offered two landmarks on every page —
 * a navigation called "Обліковий запис" and a main — and three routes had no way
 * back to the catalogue at all.
 */

/** The trail each address must carry, from contracts/site-addresses.md. */
const TRAILS: ReadonlyArray<{ path: string; trail: readonly string[] }> = [
  { path: '/', trail: [] },
  { path: '/books', trail: ['Головна', 'Каталог'] },
  { path: `/books/${BOOK}`, trail: ['Головна', 'Каталог', 'Стратегія і тактика лідерства'] },
  { path: '/authors', trail: ['Головна', 'Автори'] },
  { path: `/authors/${AUTHOR}`, trail: ['Головна', 'Автори', 'Джоко Віллінк'] },
  { path: '/about', trail: ['Головна', 'Про проєкт'] },
  { path: '/search', trail: ['Головна', 'Пошук'] },
  // The account screens carry none: signing in is a task rather than a place in
  // the catalogue, so there is nothing above it to climb back to.
  { path: '/sign-in', trail: [] },
  { path: '/register', trail: [] },
  { path: '/forgot-password', trail: [] },
  { path: '/no-such-page-exists', trail: [] },
]

test.describe('every page has a skeleton', () => {
  for (const { name, path, settled } of PAGES_IN_SCOPE) {
    test(`${name} presents the whole landmark set`, async ({ page }) => {
      await page.goto(path)
      await expect(page.locator(settled).first()).toBeVisible()
      await awaitSessionMenu(page)

      // SC-001. A banner and a contentinfo on every page, and exactly one main.
      await expect(page.getByRole('banner')).toHaveCount(1)
      await expect(page.getByRole('contentinfo')).toHaveCount(1)
      await expect(page.getByRole('main')).toHaveCount(1)

      // Every navigation and search landmark carries a name that tells it from
      // the others. Without this a screen reader's landmark list reads
      // "navigation, navigation, navigation" and is no use for moving about.
      const names = await page
        .locator('nav, [role="navigation"], [role="search"]')
        .evaluateAll((landmarks) =>
          landmarks.map(
            (landmark) =>
              landmark.getAttribute('aria-label') ?? landmark.getAttribute('aria-labelledby') ?? '',
          ),
        )

      expect(
        names.every((label) => label !== ''),
        `every landmark on ${name} is named`,
      ).toBe(true)
      expect(new Set(names).size, `every landmark name on ${name} is distinct`).toBe(names.length)
    })

    test(`${name} says the site's name exactly once`, async ({ page }) => {
      await page.goto(path)
      await expect(page.locator(settled).first()).toBeVisible()

      // SC-003. The decorated ground repeats a mark dozens of times and is
      // aria-hidden, so it must contribute nothing; the banner's wordmark is the
      // one place the site says its own name out loud.
      await expect(page.getByRole('link', { name: 'vox-lib', exact: true })).toHaveCount(1)

      const groundIsHidden = await page
        .locator('.ground')
        .evaluate((ground) => ground.getAttribute('aria-hidden') === 'true')

      expect(groundIsHidden, 'the decorated ground exposes nothing').toBe(true)
    })

    test(`${name} puts the skip link first, and it moves focus into main`, async ({ page }) => {
      await page.goto(path)
      await expect(page.locator(settled).first()).toBeVisible()
      await waitForHydration(page)

      /*
        FR-007, measured as "first in the tab order" rather than as "one Tab
        away", because the two come apart on the account screens: those move
        focus into their first field when they mount, and Chromium remembers
        where focus was even after it is blurred, so a Tab there continues from
        inside the form. That is correct behaviour for those pages and says
        nothing about the skip link.

        What the requirement needs is that nothing focusable precedes it, which
        is what makes it reachable from the top on every page — and that is a
        fact about the document rather than about where focus happens to be.
      */
      const firstFocusable = await page.evaluate(() => {
        const candidates = document.querySelectorAll<HTMLElement>(
          'a[href], button, input, select, textarea, [tabindex]:not([tabindex="-1"])',
        )

        for (const candidate of candidates) {
          if (candidate.offsetParent !== null || candidate.classList.contains('skip-link')) {
            return candidate.className
          }
        }

        return 'none'
      })

      expect(firstFocusable, 'nothing focusable precedes the skip link').toContain('skip-link')

      // And it does what it says. A fragment link moves focus only to a target
      // that can take it, which is why main carries tabindex="-1".
      await page.locator('.skip-link').focus()
      await expect(page.locator('.skip-link')).toBeFocused()
      await page.keyboard.press('Enter')

      const landedInMain = await page.evaluate(() => {
        const main = document.getElementById('main')
        const active = document.activeElement

        return main !== null && active !== null && (main === active || main.contains(active))
      })

      expect(landedInMain, 'activating the skip link moves focus into main').toBe(true)
    })

    test(`${name} reaches the catalogue, the search field and the about page`, async ({ page }) => {
      await page.goto(path)
      await expect(page.locator(settled).first()).toBeVisible()

      // SC-002, from the page itself rather than from somewhere else. Scoped to
      // the banner's navigation: the about page is deliberately linked from the
      // footer as well, and "reachable" is satisfied by either.
      const nav = page.getByRole('navigation', { name: 'Основна навігація' })

      await expect(nav.getByRole('link', { name: 'Каталог', exact: true })).toBeVisible()
      await expect(nav.getByRole('link', { name: 'Про проєкт', exact: true })).toBeVisible()
      await expect(
        page.getByRole('search', { name: 'Пошук по каталогу' }).getByRole('searchbox'),
      ).toBeVisible()
    })

    /**
     * SC-020 and FR-055. A disabled play control still appears in a screen
     * reader's list of controls and invites a listener to hunt for a way to
     * enable it, when the honest answer is that playback does not exist yet.
     */
    test(`${name} offers no play control, disabled or otherwise`, async ({ page }) => {
      await page.goto(path)
      await expect(page.locator(settled).first()).toBeVisible()

      const playVocabulary = /слухати зараз|відтвор|програти|play|пауза|pause/i

      const controls = await page
        .locator('button, [role="button"], audio, video')
        .evaluateAll((elements) =>
          elements.map((element) => (element.textContent ?? '').trim() + ' ' + element.tagName),
        )

      const offenders = controls.filter((control) => playVocabulary.test(control))

      expect(offenders, `${name} offers no way to press play`).toEqual([])
    })
  }
})

test.describe('the trail above each page', () => {
  for (const { path, trail } of TRAILS) {
    test(`${path} carries the trail its address implies`, async ({ page }) => {
      await page.goto(path)
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

      const crumbs = page.getByRole('navigation', { name: 'Навігаційний ланцюжок' })

      if (trail.length === 0) {
        // SC-004's other half: the landing page is the top level, and the
        // not-found page has no place in the hierarchy at all.
        await expect(crumbs).toHaveCount(0)
        return
      }

      await expect(crumbs).toHaveCount(1)
      await expect(crumbs.locator('li')).toHaveText([...trail])

      // The last item is the current page: not a link, and marked as current so
      // a screen reader says which of the steps you are standing on.
      const last = crumbs.locator('li').last()
      await expect(last.locator('a')).toHaveCount(0)
      await expect(last.locator('[aria-current="page"]')).toHaveCount(1)

      // Every earlier item is a link that actually resolves.
      const hrefs = await crumbs
        .locator('li a')
        .evaluateAll((links) => links.map((link) => link.getAttribute('href')))

      expect(hrefs.length).toBe(trail.length - 1)

      for (const href of hrefs) {
        expect(href).not.toBeNull()
        const response = await page.request.get(href as string)
        expect(response.status(), `${href} resolves`).toBeLessThan(400)
      }
    })
  }
})

test.describe('the footer', () => {
  test('says what the site is and links the font licence', async ({ page }) => {
    await page.goto('/')

    const footer = page.getByRole('contentinfo')

    await expect(footer).toContainText('vox-lib')
    await expect(footer.getByRole('link', { name: 'Про проєкт' })).toBeVisible()

    // FR-003. The licence ships with the site and was linked from nowhere at
    // all until now, which is an obligation met by accident of the file
    // existing rather than on purpose.
    const licence = footer.getByRole('link', { name: /OFL|Ліцензія/ })
    await expect(licence).toHaveAttribute('href', '/fonts/OFL.txt')

    const response = await page.request.get('/fonts/OFL.txt')
    expect(response.status()).toBe(200)
  })
})

test.describe('the search landmark', () => {
  test('there is exactly one search field per page', async ({ page }) => {
    for (const { path } of PAGES_IN_SCOPE) {
      await page.goto(path)
      await expect(page.getByRole('heading', { level: 1 }).first()).toBeVisible()

      // FR-011. The results page used to carry its own form in the body, which
      // gave that page two search landmarks and two inputs named the same thing.
      await expect(page.getByRole('search')).toHaveCount(1)
      await expect(page.locator('input[name="q"]')).toHaveCount(1)
    }
  })

  test('the search page carries the current term in the header field', async ({ page }) => {
    await page.goto(`/search?q=${encodeURIComponent(SEARCH_TERM)}`)

    await expect(page.locator('input[name="q"]')).toHaveValue(SEARCH_TERM)
  })
})
