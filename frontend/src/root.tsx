import { Links, Meta, Outlet, Scripts, ScrollRestoration } from 'react-router'
import { SessionMenu } from './components/SessionMenu'
import { Ground } from './components/Ground'

// tokens.css first: base.css reads every value it sets from it. Imported here
// rather than per route so that every page, and every prerendered document,
// receives both.
import './styles/tokens.css'
import './styles/base.css'
import './styles/ground.css'

/**
 * The document every page is served inside. The language is declared here, on
 * the html element, so a screen reader picks a Ukrainian voice for the whole
 * interface. A page whose own content is in another language overrides it on
 * the element that carries that content, which is what FR-018 asks for.
 */
export function Layout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="uk">
      <head>
        <meta charSet="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <Meta />
        <Links />
      </head>
      <body>
        {/*
          First child of the body, and a sibling of the content rather than an
          ancestor of it. What actually keeps axe computing contrast is the
          opaque region above it, not this placement; the placement is what
          keeps the filter's stacking context off the content subtree and stops
          any content element inheriting a decorative background.
        */}
        <Ground />
        {children}
        <ScrollRestoration />
        <Scripts />
      </body>
    </html>
  )
}

export default function Root() {
  return (
    <>
      {/*
        On every page, so a person on a shared device can tell at a glance, and
        with a screen reader, whether they are still signed in. It renders after
        the page loads rather than being built into the document, because these
        pages are prerendered and who is signed in is not a property of the
        document.
      */}
      <SessionMenu />
      <Outlet />
    </>
  )
}
