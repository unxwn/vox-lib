import { createContext, useCallback, useEffect, useState, type ReactNode } from 'react'

export type Session =
  | { state: 'loading' }
  | { state: 'anonymous' }
  | { state: 'signedIn'; email: string; isVerifiedBeneficiary: boolean }

export type SessionContextValue = {
  session: Session
  refresh: () => Promise<Session>
}

type SessionBody = {
  signedIn: boolean
  email: string | null
  isVerifiedBeneficiary: boolean
}

export const SessionContext = createContext<SessionContextValue | null>(null)

/**
 * The one in-flight read of the session, shared by everyone who asks.
 *
 * Module level rather than component state, and that is the whole of FR-009.
 * The banner's account slot, a book page's sign-in prompt and anything else
 * that wants to know are separate components mounting at slightly different
 * moments; holding the promise per consumer means one request each, all of them
 * asking the same question and getting the same answer. Held as the promise and
 * not the answer, so two consumers mounting before the first response lands
 * still share one request.
 */
let inFlight: Promise<Session> | null = null

/**
 * One read of the session, at most once per page view.
 *
 * An unreachable API is reported as anonymous rather than as an error: every
 * page uses this to decide whether to offer signing in or signing out, and a
 * page that cannot decide is worse than one that offers the sign-in link to
 * somebody who turns out to be signed in already.
 */
export function readSession(): Promise<Session> {
  inFlight ??= fetchSession()
  return inFlight
}

/**
 * For after something changed the session: signing out, or a sign-in that has
 * to be read back to find out whether the browser kept it. It discards the
 * shared answer, so the next read is a real one.
 */
export function refreshSession(): Promise<Session> {
  inFlight = fetchSession()
  return inFlight
}

async function fetchSession(): Promise<Session> {
  try {
    const response = await fetch('/api/account/session', {
      credentials: 'same-origin',
      headers: { Accept: 'application/json' },
    })

    if (!response.ok) {
      return { state: 'anonymous' }
    }

    const body = (await response.json()) as SessionBody

    return body.signedIn && body.email !== null
      ? {
          state: 'signedIn',
          email: body.email,
          isVerifiedBeneficiary: body.isVerifiedBeneficiary,
        }
      : { state: 'anonymous' }
  } catch {
    return { state: 'anonymous' }
  }
}

/**
 * Reads the session once for the whole page and hands the answer to everybody.
 *
 * It is read after the page has loaded rather than built into the document
 * because the catalogue pages are prerendered and who is signed in is not a
 * property of a document. That is also why the session cookie can be
 * SameSite=Strict at no cost: this request is made from a page already served
 * by this site, so it is same-site even when the person arrived from elsewhere.
 */
export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session>({ state: 'loading' })

  useEffect(() => {
    let cancelled = false

    void readSession().then((next) => {
      if (!cancelled) {
        setSession(next)
      }
    })

    return () => {
      cancelled = true
    }
  }, [])

  const refresh = useCallback(async () => {
    const next = await refreshSession()
    setSession(next)
    return next
  }, [])

  return <SessionContext value={{ session, refresh }}>{children}</SessionContext>
}
