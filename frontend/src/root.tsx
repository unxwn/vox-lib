import {
  Links,
  Meta,
  Outlet,
  Scripts,
  ScrollRestoration,
  useLocation,
  useMatches,
} from 'react-router'
import { SessionProvider } from './account/SessionProvider'
import { breadcrumbsFor } from './catalogue/breadcrumbs'
import { Breadcrumbs } from './components/Breadcrumbs'
import { Ground } from './components/Ground'
import { SiteFooter } from './components/SiteFooter'
import { SiteHeader } from './components/SiteHeader'

// tokens.css first: base.css reads every value it sets from it. Imported here
// rather than per route so that every page, and every prerendered document,
// receives both.
import './styles/tokens.css'
import './styles/base.css'
import './styles/ground.css'
import './styles/shell.css'

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

/**
 * The shell every page is drawn inside: a banner, the content, a footer.
 *
 * The provider wraps all three so the session is read once per page view rather
 * than once per component that wants it (FR-009). `Ground` stays where
 * 003-visual-identity left it, as the first child of the body and a sibling of
 * everything here.
 *
 * The trail sits inside `main` rather than above it. It is about the page, so
 * it belongs to the page; putting it in the banner would make it part of the
 * furniture a skip link exists to skip.
 */
export default function Root() {
  const { pathname } = useLocation()
  const matches = useMatches()

  /*
    The two addresses whose last step is a record rather than a fixed word — a
    book and an author — supply their own name through a route handle. The trail
    is still derived rather than authored: the route says what it is called, and
    breadcrumbs.ts decides where that name goes. Until the data lands the slug
    stands in, so the trail never renders empty.
  */
  const title = matches.reduce<string | undefined>((found, match) => {
    const handle = match.handle as { crumb?: (data: unknown) => string | undefined } | undefined

    return handle?.crumb?.(match.loaderData) ?? found
  }, undefined)

  return (
    <SessionProvider>
      <SiteHeader />

      {/*
        tabIndex={-1} is what makes the skip link work rather than merely exist.
        A fragment link moves focus only to a target that can take it; without
        this the browser scrolls to the content and leaves the keyboard back in
        the banner, which looks correct to a sighted visitor and does nothing at
        all for the person the link is for.
      */}
      <main id="main" tabIndex={-1}>
        <Breadcrumbs trail={breadcrumbsFor(pathname, title)} />
        <Outlet />
      </main>

      <SiteFooter />
    </SessionProvider>
  )
}
