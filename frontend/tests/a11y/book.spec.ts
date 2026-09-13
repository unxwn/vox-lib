import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import { BOOK as REAL_BOOK, COMPILED_BOOK } from './pages'
import {
  clearSparseBooks,
  DRAFT,
  IN_ANOTHER_LANGUAGE,
  seedSparseBooks,
  WITHOUT_CHAPTERS,
  WITHOUT_DESCRIPTION,
} from './sparse-books-fixture'
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

/** A real book: Ukrainian, with a description, a cover and one credit. */
const BOOK = `/books/${REAL_BOOK}`

test.describe('one book', () => {
  /*
    Serial, because the sparse cases below share seeded rows in the database the
    dev server is reading. Under the default parallelism Playwright splits one
    file across workers, each runs beforeAll and afterAll for itself, and one
    worker's cleanup deletes the books another worker is still reading.
  */
  test.describe.configure({ mode: 'serial' })

  // The sparse cases below need books the real catalogue does not contain, and
  // they are seeded once for the file rather than per test.
  test.beforeAll(async () => {
    await seedSparseBooks()
  })

  test.afterAll(async () => {
    await clearSparseBooks()
  })

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
      'Стратегія і тактика лідерства',
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
    // One chapter today: FR-065 seeds each real book with the structure that is
    // known, which is a single chapter carrying its whole running time. The real
    // chapters arrive with the audio and replace it.
    await expect(chapters.getByRole('listitem')).toHaveCount(1)

    // The count the page states and the number of items it renders are the same
    // fact, so a reader is never told four and shown three.
    await expect(page.getByText('Розділів').locator('..')).toContainText('1')
  })

  test('a book with no chapters says so instead of leaving an empty list', async ({ page }) => {
    await page.goto(`/books/${WITHOUT_CHAPTERS}`)

    await expect(page.locator('.chapters__empty')).toBeVisible()
    await expect(
      page.getByRole('list').filter({ has: page.locator('.chapters__item') }),
    ).toHaveCount(0)
  })

  test('a book with no description says so rather than showing a gap', async ({ page }) => {
    await page.goto(`/books/${WITHOUT_DESCRIPTION}`)

    await expect(page.locator('.book__description--absent')).toBeVisible()
  })

  test('focus lands on the book when it is reached from the catalogue', async ({ page }) => {
    await page.goto('/books')
    await waitForHydration(page)

    await page.getByRole('link', { name: /Стратегія і тактика лідерства/ }).click()
    await expect(page).toHaveURL(new RegExp(`/books/${REAL_BOOK}$`))

    // FR-019 applied to arriving at a book: without this the keyboard is left
    // on a link that no longer exists.
    await expect(page.getByRole('heading', { level: 1 })).toBeFocused()
  })

  test('states that listening needs an account, and offers no playback control', async ({
    page,
  }) => {
    await page.goto(BOOK)

    const main = page.getByRole('main')

    await expect(main.getByText('Щоб слухати цю книжку, потрібен обліковий запис.')).toBeVisible()

    // FR-055: the message carries the way to get one. Naming what the visitor
    // lacks without offering it is a dead end, and registration is one step
    // away.
    await expect(main.getByRole('link', { name: 'Створити обліковий запис' })).toHaveAttribute(
      'href',
      '/register',
    )

    // FR-010. Not a disabled play button either: a disabled control still
    // appears in a screen reader's list of controls and invites a listener to
    // hunt for a way to enable it. Scoped to main, because the banner carries
    // the search form's submit button on every page.
    await expect(page.locator('audio, video')).toHaveCount(0)
    await expect(main.getByRole('button')).toHaveCount(0)
    await expect(main.getByRole('slider')).toHaveCount(0)
  })

  test('declares the book language on its own content, leaving the interface Ukrainian', async ({
    page,
  }) => {
    await page.goto(`/books/${IN_ANOTHER_LANGUAGE}`)

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
    await page.goto(`/books/${DRAFT}`)

    await expect(page.getByRole('heading', { level: 1 })).toContainText('не знайдено')
  })

  test('a compiler is credited as one beside their name', async ({ page }) => {
    await page.goto(`/books/${COMPILED_BOOK}`)

    // FR-066, as visible text rather than only in the response: an author needs
    // no label because it is what a reader assumes; a compiler does, because
    // assuming it there would be wrong.
    const credits = page.getByRole('main').locator('.book__authors')

    await expect(credits).toContainText('упорядник')
    await expect(credits.getByRole('link')).toHaveAttribute('href', /\/authors\//)
  })
})
