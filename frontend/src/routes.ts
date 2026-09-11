import { type RouteConfig, index, route } from '@react-router/dev/routes'

export default [
  index('routes/catalogue.tsx'),
  route('page/:page', 'routes/catalogue-page.tsx'),
  route('books/:slug', 'routes/book.tsx'),
  route('search', 'routes/search.tsx'),
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
