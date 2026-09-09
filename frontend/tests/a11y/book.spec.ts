import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import { waitForHydration } from './support'

/**
 * One book's page. The journey this covers, catalogue to chapter list, is the
 * whole of SC-001, so what is checked here is the shape a screen reader
 * navigates by: the heading levels, the chapter list and its count, and where
 * focus lands on arrival.
 *
 * These catch violations and regressions. They do not replace the manual pass on
 * VoiceOver and TalkBack recorded in manual-verification.md.
 */

/** Seeded in Ukrainian, with chapters, a description and multiple authors. */
const BOOK = '/books/khiba-revut-voly'

test.describe('one book', () => {
  test('has no critical or serious accessibility violations', async ({ page }) => {
    await page.goto(BOOK)
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    const { violations } = await new AxeBuilder({ page }).analyze()
    const blocking = violations.filter(
      (violation) => violation.impact === 'critical' || violation.impact === 'serious',
    )

    expect(
      blocking.map((violation) => `${violation.id}: ${violation.help}`),
      'SC-002 allows no critical or serious violations',
    ).toEqual([])
  })

  test('is titled by the book and sectioned without skipping a heading level', async ({ page }) => {
    await page.goto(BOOK)

    await expect(page.getByRole('heading', { level: 1 })).toHaveText(
      'Хіба ревуть воли, як ясла повні?',
    )

    // A screen reader moves through a page by its headings, so the levels have
    // to describe the real structure: one h1 naming the book, and sections
    // under it at h2. A jump from h1 to h3 reads as a missing section.
    const levels = await page
      .locator('h1, h2, h3, h4, h5, h6')
      .evaluateAll((headings) => headings.map((heading) => Number(heading.tagName.slice(1))))

    expect(levels[0], 'the page starts at h1').toBe(1)
    expect(
      levels.filter((level) => level === 1),
      'exactly one h1',
    ).toHaveLength(1)
    levels.forEach((level, index) => {
      if (index > 0) {
        expect(level - levels[index - 1], 'no heading level is skipped').toBeLessThanOrEqual(1)
      }
    })
  })

  test('announces the chapters as a list with its count', async ({ page }) => {
    await page.goto(BOOK)

    // getByRole('list') is the assertion, not a locator convenience: it passes
    // only if the chapters really are a list to assistive technology, which is
    // what gives a screen reader the item count and lets its user jump between
    // items.
    const chapters = page.getByRole('list').filter({ has: page.locator('.chapters__item') })

    await expect(chapters).toBeVisible()
    await expect(chapters.getByRole('listitem')).toHaveCount(4)

    // The count the page states and the number of items it renders are the same
    // fact, so a reader is never told four and shown three.
    await expect(page.getByText('Розділів').locator('..')).toContainText('4')
  })

  test('a book with no chapters says so instead of leaving an empty list', async ({ page }) => {
    // Інтермеццо is seeded with no chapters on purpose.
    await page.goto('/books/intermezzo')

    await expect(page.locator('.chapters__empty')).toBeVisible()
    await expect(
      page.getByRole('list').filter({ has: page.locator('.chapters__item') }),
    ).toHaveCount(0)
  })

  test('a book with no description says so rather than showing a gap', async ({ page }) => {
    // Єретик is seeded without a description on purpose.
    await page.goto('/books/yeretyk')

    await expect(page.locator('.book__description--absent')).toBeVisible()
  })

  test('focus lands on the book when it is reached from the catalogue', async ({ page }) => {
    await page.goto('/')
    await waitForHydration(page)

    await page.getByRole('link', { name: /Ґудзик/ }).click()
    await expect(page).toHaveURL(/\/books\/gudzyk$/)

    // FR-019 applied to arriving at a book: without this the keyboard is left
    // on a link that no longer exists.
    await expect(page.getByRole('heading', { level: 1 })).toBeFocused()
  })

  test('states that listening needs an account, and offers no playback control', async ({
    page,
  }) => {
    await page.goto(BOOK)

    await expect(page.getByText('Щоб слухати цю книжку, потрібен обліковий запис.')).toBeVisible()

    // FR-010. Not a disabled play button either: a disabled control still
    // appears in a screen reader's list of controls and invites a listener to
    // hunt for a way to enable it.
    await expect(page.locator('audio, video')).toHaveCount(0)
    await expect(page.getByRole('button')).toHaveCount(0)
    await expect(page.getByRole('slider')).toHaveCount(0)
  })

  test('declares the book language on its own content, leaving the interface Ukrainian', async ({
    page,
  }) => {
    await page.goto('/books/kobzar-selected-poems')

    // FR-018: the document stays Ukrainian because the furniture around the
    // book is, while the book's own words are marked as English so a screen
    // reader switches voice for them rather than reading English with Ukrainian
    // pronunciation.
    await expect(page.locator('html')).toHaveAttribute('lang', 'uk')
    await expect(page.getByRole('heading', { level: 1 })).toHaveAttribute('lang', 'en')
  })

  test('an unknown book says what could not be found and offers a way back', async ({ page }) => {
    await page.goto('/books/no-such-book-at-all')

    await expect(page.getByRole('heading', { level: 1 })).toContainText('не знайдено')
    await expect(page.getByRole('link', { name: /Повернутися до каталогу/ })).toBeVisible()
  })

  test('an unpublished book is not reachable at its own address', async ({ page }) => {
    // Чорна рада is seeded as a draft, and must behave exactly as if absent.
    await page.goto('/books/chorna-rada')

    await expect(page.getByRole('heading', { level: 1 })).toContainText('не знайдено')
  })
})
