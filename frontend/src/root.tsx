import { Links, Meta, Outlet, Scripts, ScrollRestoration } from 'react-router'

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
        {children}
        <ScrollRestoration />
        <Scripts />
      </body>
    </html>
  )
}

export default function Root() {
  return <Outlet />
}
