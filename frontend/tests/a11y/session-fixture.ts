import { expect, type Page } from '@playwright/test'
import { freshEmail, goodPassword } from './account-support'
import { waitForHydration } from './support'

/**
 * Reaches a signed-in session through the real screens, with no manual step.
 *
 * Every browser assertion this suite has ever made walked the anonymous variant
 * of the site: no axe run, no contrast measurement, no target-size check and no
 * focus assertion has seen a page while somebody was signed in. That is the gap
 * SC-021 names, and it exists because getting there needs a real address, a real
 * message and a real confirmation link.
 *
 * So this registers, reads the message out of Mailpit's HTTP API, follows the
 * confirmation link and signs in — every step through the interface a person
 * uses, rather than by writing a session row.
 *
 * **Idempotency comes from a fresh address, never from clearing anything.**
 * `freshEmail()` gives each run its own, so nothing collides with a previous
 * run and nothing has to be cleaned up. Emptying the mailbox or the accounts
 * table would work exactly once and would break every spec running beside this
 * one under `fullyParallel`.
 */

const MAILPIT = process.env.VOXLIB_MAILPIT_ORIGIN ?? 'http://localhost:8025'

type MailpitSummary = {
  ID: string
  To: { Address: string }[]
}

/**
 * The message this run's registration produced, waited for rather than assumed.
 *
 * Asked for by recipient rather than by reading the last N messages. The account
 * specs run beside this one and each sends its own mail, so a page of the most
 * recent messages can easily not contain this run's by the time it is read —
 * which fails as "no confirmation arrived" and looks like a broken flow.
 */
async function awaitMessageFor(page: Page, address: string): Promise<string> {
  const deadline = Date.now() + 30_000
  const query = encodeURIComponent(`to:${address}`)

  while (Date.now() < deadline) {
    const response = await page.request.get(`${MAILPIT}/api/v1/search?query=${query}`)

    if (response.ok()) {
      const body = (await response.json()) as { messages?: MailpitSummary[] }
      const mine = body.messages?.find((message) =>
        message.To.some((recipient) => recipient.Address === address),
      )

      if (mine !== undefined) {
        return mine.ID
      }
    }

    await page.waitForTimeout(250)
  }

  throw new Error(`No confirmation message arrived for ${address} within 30 seconds.`)
}

/** The confirmation address out of the message body. */
async function confirmationLink(page: Page, id: string): Promise<string> {
  const response = await page.request.get(`${MAILPIT}/api/v1/message/${id}`)

  expect(response.ok(), 'the message can be read back from Mailpit').toBe(true)

  const body = (await response.json()) as { Text?: string; HTML?: string }
  const source = `${body.Text ?? ''} ${body.HTML ?? ''}`

  const found = /https?:\/\/[^\s"'<>]*\/confirm\?[^\s"'<>]+/.exec(source)

  if (found === null) {
    throw new Error('The confirmation message carried no link to /confirm.')
  }

  // Followed as a path rather than as an absolute address, so the test drives
  // the site under test rather than whatever App:BaseAddress happens to say.
  return (
    new URL(found[0].replace(/&amp;/g, '&')).pathname +
    new URL(found[0].replace(/&amp;/g, '&')).search
  )
}

/**
 * Registers, confirms and signs in. Returns the address, so a caller can assert
 * the banner is showing the right one.
 */
export async function signIn(page: Page): Promise<string> {
  const email = freshEmail('signed-in')

  await page.goto('/register')
  await waitForHydration(page)
  await page.getByLabel('Адреса електронної пошти').fill(email)
  await page.getByLabel('Пароль').fill(goodPassword)
  await page.getByRole('button', { name: 'Створити обліковий запис' }).click()

  await expect(page.getByRole('heading', { level: 1, name: /Перевірте свою пошту/ })).toBeVisible()

  const id = await awaitMessageFor(page, email)
  await page.goto(await confirmationLink(page, id))
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 30_000 })

  await page.goto('/sign-in')
  await waitForHydration(page)
  await page.getByLabel('Адреса електронної пошти').fill(email)
  await page.getByLabel('Пароль').fill(goodPassword)
  await page.getByRole('button', { name: 'Увійти' }).click()

  // The banner is what proves it: the account slot resolves to the signed-in
  // shape, which is the chrome every assertion in signed-in.spec.ts is about.
  await expect(page.locator('.session-menu__slot[data-session="signedIn"]')).toBeVisible({
    timeout: 30_000,
  })

  return email
}
