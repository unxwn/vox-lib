import { type RouteConfig, index, route } from '@react-router/dev/routes'

export default [
  index('routes/catalogue.tsx'),
  route('page/:page', 'routes/catalogue-page.tsx'),
  route('books/:slug', 'routes/book.tsx'),
  route('search', 'routes/search.tsx'),
  // Anything else. A real route rather than an error boundary, so an address
  // that matches nothing still renders a page that says so and offers a way
  // back, per FR-011.
  route('*', 'routes/not-found.tsx'),
] satisfies RouteConfig
