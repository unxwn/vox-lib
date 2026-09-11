import { existsSync, readFileSync } from 'node:fs'
import { expect, test } from '@playwright/test'

/**
 * Assertions about what the build emitted, rather than about a running browser.
 *
 * FR-028 has two halves and this is the first: a page that was never generated
 * cannot be indexed. The account screens are personal to one person and are
 * deliberately absent from the prerender list, so the build must emit no
 * document for any of them, and must emit the single-page fallback that serves
 * them instead.
 *
 * The fallback assertion is the one that matters operationally. Nothing in this
 * repository serves the built frontend yet, and whatever eventually does has to
 * answer unknown paths with that file rather than a 404, or every account screen
 * will fail to load on a fresh visit in production while working perfectly in
 * development.
 */
const build = 'build/client'

const accountRoutes = ['register', 'confirm', 'sign-in', 'forgot-password', 'reset-password']

test.describe('what the build emits', () => {
  test.skip(
    !existsSync(`${build}/index.html`),
    'Run pnpm --filter frontend build first: these assertions are about build output.',
  )

  for (const route of accountRoutes) {
    test(`no document is generated for /${route}`, () => {
      expect(
        existsSync(`${build}/${route}/index.html`),
        `/${route} is personal to one person and must not be prerendered. FR-028.`,
      ).toBe(false)
    })
  }

  test('the single-page fallback exists, so the account screens can be served at all', () => {
    // Written to __spa-fallback.html rather than index.html because / is itself
    // prerendered as the catalogue.
    expect(existsSync(`${build}/__spa-fallback.html`)).toBe(true)
  })

  test('the catalogue is still prerendered with its content in the document', () => {
    const document = readFileSync(`${build}/index.html`, 'utf8')

    // The account work must not have disturbed the reason 001 prerenders at all.
    expect(document).toContain('<title>')
    expect(document.length).toBeGreaterThan(1000)
  })
})
