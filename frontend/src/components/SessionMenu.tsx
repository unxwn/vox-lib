import { useState } from 'react'
import { Link } from 'react-router'
import { StatusRegion } from './StatusRegion'
import { sendAccountRequest } from '../api/account'
import { useSession } from '../account/useSession'

/**
 * Who is signed in, on every page.
 *
 * The specification raises a shared device as an edge case: a person has to be
 * able to tell whether they are still signed in, and a screen reader user has to
 * be able to tell without hunting. So this is a named landmark carrying the
 * address, not an avatar in a corner.
 *
 * While the session is still being read it renders nothing rather than guessing.
 * Showing "sign in" and then swapping it for "sign out" a moment later is a
 * change of state a screen reader would announce, about nothing.
 */
export function SessionMenu() {
  const { session, refresh } = useSession()
  const [signedOut, setSignedOut] = useState(false)

  async function signOut() {
    await sendAccountRequest('/api/account/session', 'DELETE')
    await refresh()
    setSignedOut(true)
  }

  return (
    <nav className="session-menu" aria-label="Обліковий запис">
      {/*
        The landmark is present from the first render, before the session is
        known. Rendering nothing until it arrives and then adding a navigation
        landmark means the landmark list and the tab order change under a screen
        reader user a moment after the page settles, which is exactly the sort of
        thing that makes a page feel unreliable to navigate.
      */}
      {session.state === 'loading' ? null : session.state === 'signedIn' ? (
        <>
          <span>Ви увійшли як {session.email}</span>
          {/*
            A button in a form, never a link. No request that changes state may
            use a method a browser will follow from a link: a prefetching client
            or an accelerator would sign the person out without being asked.
          */}
          <form
            onSubmit={(event) => {
              event.preventDefault()
              void signOut()
            }}
          >
            <button type="submit">Вийти</button>
          </form>
        </>
      ) : (
        <>
          <Link to="/sign-in">Увійти</Link>
          <Link to="/register">Створити обліковий запис</Link>
        </>
      )}

      {/* FR-033: signing out is a change of state a sighted person notices, so
          it is announced rather than only shown. */}
      <StatusRegion tone="status">
        {signedOut && session.state === 'anonymous' && <p>Ви вийшли з облікового запису.</p>}
      </StatusRegion>
    </nav>
  )
}
