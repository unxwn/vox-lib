import type { ApiResult } from './client'

/**
 * The account calls.
 *
 * They do not go through `apiGet` in `client.ts`, because everything here
 * changes state and therefore has to carry two things that a catalogue read
 * does not: the session cookie, which means `credentials: 'same-origin'`, and
 * the antiforgery token, which proves the request came from this site.
 */

export type AccountFailure =
  /** The submission was rejected. `errors` names the fields. */
  | { ok: false; reason: 'invalid'; errors: Record<string, string[]> }
  /** The address and password were not recognised. Says nothing about which. */
  | { ok: false; reason: 'notRecognised'; detail: string }
  /** The password was right but the address is not confirmed yet. */
  | { ok: false; reason: 'notConfirmed'; detail: string }
  /** Too many attempts, or a lockout. The two are deliberately alike. */
  | { ok: false; reason: 'tooManyAttempts'; detail: string }
  /** The link has been used or has expired. */
  | { ok: false; reason: 'linkExpired'; detail: string }
  /** The API could not be reached or did not answer properly. */
  | { ok: false; reason: 'unavailable' }

export type AccountResult<T> = { ok: true; value: T } | AccountFailure

type ProblemBody = {
  detail?: string
  title?: string
  errors?: Record<string, string[]>
}

const TOKEN_COOKIE = 'XSRF-TOKEN'
const TOKEN_HEADER = 'X-XSRF-TOKEN'

function readTokenCookie(): string | undefined {
  if (typeof document === 'undefined') {
    return undefined
  }

  const match = document.cookie.split('; ').find((entry) => entry.startsWith(`${TOKEN_COOKIE}=`))

  return match === undefined ? undefined : decodeURIComponent(match.slice(TOKEN_COOKIE.length + 1))
}

/**
 * Fetches the token if this browser does not have one yet. Called before every
 * state-changing request rather than once at startup, because the cookie can be
 * absent for reasons the page cannot see: a private window, cleared site data,
 * or simply a first visit that began on a prerendered catalogue page.
 */
async function antiforgeryToken(): Promise<string | undefined> {
  const existing = readTokenCookie()

  if (existing !== undefined) {
    return existing
  }

  await fetch('/api/account/antiforgery-token', { credentials: 'same-origin' })

  return readTokenCookie()
}

async function send<T>(
  path: string,
  method: 'POST' | 'DELETE',
  body?: unknown,
): Promise<AccountResult<T>> {
  const token = await antiforgeryToken()

  let response: Response

  try {
    response = await fetch(path, {
      method,
      credentials: 'same-origin',
      headers: {
        Accept: 'application/json',
        ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
        ...(token === undefined ? {} : { [TOKEN_HEADER]: token }),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    return { ok: false, reason: 'unavailable' }
  }

  return interpret<T>(response)
}

async function interpret<T>(response: Response): Promise<AccountResult<T>> {
  if (response.ok) {
    if (response.status === 204) {
      return { ok: true, value: undefined as T }
    }

    try {
      return { ok: true, value: (await response.json()) as T }
    } catch {
      // A 202 carries no body by design, and neither does a 204 that arrived
      // with a content type. Neither is a failure.
      return { ok: true, value: undefined as T }
    }
  }

  let problem: ProblemBody = {}

  try {
    problem = (await response.json()) as ProblemBody
  } catch {
    problem = {}
  }

  const detail = problem.detail ?? problem.title ?? ''

  switch (response.status) {
    case 400:
      return { ok: false, reason: 'invalid', errors: problem.errors ?? {} }
    case 401:
      return { ok: false, reason: 'notRecognised', detail }
    case 403:
      return { ok: false, reason: 'notConfirmed', detail }
    case 410:
      return { ok: false, reason: 'linkExpired', detail }
    case 429:
      return { ok: false, reason: 'tooManyAttempts', detail }
    default:
      return { ok: false, reason: 'unavailable' }
  }
}

export type PasswordPolicy = {
  minimumLength: number
  requiresDigit: boolean
  requiresUppercase: boolean
  requiresLowercase: boolean
  requiresNonAlphanumeric: boolean
}

export async function fetchPasswordPolicy(): Promise<ApiResult<PasswordPolicy>> {
  try {
    const response = await fetch('/api/account/password-policy', {
      headers: { Accept: 'application/json' },
    })

    return response.ok
      ? { ok: true, value: (await response.json()) as PasswordPolicy }
      : { ok: false, reason: 'unavailable' }
  } catch {
    return { ok: false, reason: 'unavailable' }
  }
}

export { send as sendAccountRequest }
