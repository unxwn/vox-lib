import { useCallback, useEffect, useState } from 'react'

export type Session =
  | { state: 'loading' }
  | { state: 'anonymous' }
  | { state: 'signedIn'; email: string; isVerifiedBeneficiary: boolean }

type SessionBody = {
  signedIn: boolean
  email: string | null
  isVerifiedBeneficiary: boolean
}

/**
 * Reads the session after the page has loaded.
 *
 * It is read rather than built into the document because the account pages are
 * personal to one person and are deliberately not generated ahead of time, as
 * the catalogue pages are. That is also why the session cookie can be
 * SameSite=Strict at no cost: this request is made from a page already served by
 * this site, so it is same-site even when the person arrived from elsewhere.
 */
export function useSession(): { session: Session; refresh: () => Promise<Session> } {
  const [session, setSession] = useState<Session>({ state: 'loading' })

  // The first read, cancelled if the component goes away before it lands. A
  // late setState on an unmounted component is harmless in React 19 but the
  // flag also stops a slow first read overwriting a fresher one from refresh.
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

  // For after something changed the session: signing out, or a sign-in that has
  // to be read back to find out whether the browser kept it.
  const refresh = useCallback(async () => {
    const next = await readSession()
    setSession(next)
    return next
  }, [])

  return { session, refresh }
}

/**
 * One read of the session.
 *
 * An unreachable API is reported as anonymous rather than as an error: every
 * page uses this to decide whether to offer signing in or signing out, and a
 * page that cannot decide is worse than one that offers the sign-in link to
 * somebody who turns out to be signed in already.
 */
export async function readSession(): Promise<Session> {
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
