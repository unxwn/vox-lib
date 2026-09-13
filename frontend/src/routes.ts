import { type RouteConfig, index, route } from '@react-router/dev/routes'

export default [
  // The front door. The catalogue used to live here, which meant the site
  // opened on a list of books and never said what it was (FR-025).
  index('routes/landing.tsx'),

  // The catalogue, one level down, where its own trail can say so.
  //
  // The static segment is declared before the dynamic one deliberately. React
  // Router ranks a static segment above a dynamic one whatever the order here,
  // but writing them this way round means nobody has to know that to read it:
  // /books/page/2 is the second page, never a book whose slug is "page".
  route('books', 'routes/catalogue.tsx'),
  route('books/page/:page', 'routes/catalogue-page.tsx'),
  route('books/:slug', 'routes/book.tsx'),

  route('authors', 'routes/authors.tsx'),
  route('authors/:slug', 'routes/author.tsx'),

  route('search', 'routes/search.tsx'),
  route('about', 'routes/about.tsx'),

  // The account screens. They are personal to one person and are deliberately
  // absent from the prerender list in react-router.config.ts: there is no fixed
  // document to emit, and a page that was never generated cannot be indexed,
  // which is half of FR-028. They are served by the single-page fallback the
  // build already emits.
  route('register', 'routes/register.tsx'),
  route('confirm', 'routes/confirm.tsx'),
  route('sign-in', 'routes/sign-in.tsx'),
  route('forgot-password', 'routes/forgot-password.tsx'),
  route('reset-password', 'routes/reset-password.tsx'),
  // Anything else. A real route rather than an error boundary, so an address
  // that matches nothing still renders a page that says so and offers a way
  // back, per FR-011.
  route('*', 'routes/not-found.tsx'),
] satisfies RouteConfig
