import { useContext } from 'react'
import { SessionContext, type SessionContextValue } from './SessionProvider'

export type { Session } from './SessionProvider'
export { readSession } from './SessionProvider'

/**
 * Who is signed in, as the provider in `root.tsx` read it.
 *
 * The contract the account screens already use is unchanged — the `Session`
 * union and `refresh` — but the state is no longer per call site. Every
 * consumer on a page now shares one read, which is FR-009; before this, each
 * one made its own request and a page with two consumers asked twice.
 */
export function useSession(): SessionContextValue {
  const value = useContext(SessionContext)

  if (value === null) {
    throw new Error('useSession was called outside SessionProvider.')
  }

  return value
}
