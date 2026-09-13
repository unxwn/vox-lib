import { useState } from 'react'
import { Link } from 'react-router'
import { StatusRegion } from './StatusRegion'
import { sendAccountRequest } from '../api/account'
import { useSession } from '../account/useSession'

/**
 * The banner's account slot: who is signed in, on every page.
 *
 * The specification raises a shared device as an edge case: a person has to be
 * able to tell whether they are still signed in, and a screen reader user has
 * to be able to tell without hunting. So this is a named landmark carrying the
 * address, not an avatar in a corner.
 *
 * Signed out it offers one way in rather than two. The sign-in page is where
 * the choice between signing in and registering is made, and it already carries
 * that link; offering both here asked the visitor to decide which of them they
 * were before the site had told them anything about itself.
 *
 * Two things about its shape are requirements rather than layout (FR-008).
 *
 * **It is last in the banner.** The session is read after the document loads,
 * so whatever this slot resolves into appears a moment after the page settles.
 * Last means nothing a visitor has already tabbed past can move underneath
 * them: every tab stop before this one is identical in both states.
 *
 * **It reserves its size while loading, and holds nothing focusable there.**
 * The box is the same height before and after, so no element on the page shifts
 * when the answer lands; and because the loading state contains no control, the
 * tab ring gains stops at the end rather than changing in the middle. Rendering
 * "sign in" and swapping it for "sign out" a moment later would be worse still:
 * a change of state a screen reader announces, about nothing.
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
        landmark means the landmark list changes under a screen reader user a
        moment after the page settles, which is exactly the sort of thing that
        makes a page feel unreliable to navigate.
      */}
      <div className="session-menu__slot" data-session={session.state}>
        {session.state === 'loading' ? null : session.state === 'signedIn' ? (
          <>
            {/*
              The address is bounded by the stylesheet so a long one cannot
              resize the reserved box. It stays whole in the accessibility tree,
              so what a screen reader reads is unchanged.
            */}
            <span className="session-menu__who" title={session.email}>
              {session.email}
            </span>
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
          <Link to="/sign-in">Увійти</Link>
        )}
      </div>

      {/* FR-033: signing out is a change of state a sighted person notices, so
          it is announced rather than only shown. */}
      <StatusRegion tone="status">
        {signedOut && session.state === 'anonymous' && <p>Ви вийшли з облікового запису.</p>}
      </StatusRegion>
    </nav>
  )
}
