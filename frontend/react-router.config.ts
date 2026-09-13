import type { Config } from '@react-router/dev/config'
// @ts-expect-error -- a plain .mjs module shared with the sitemap script, which
// runs under node outside the TypeScript build and so has no declarations.
import { cataloguePaths } from './scripts/catalogue-paths.mjs'

export default {
  // No Node process in production. The build emits static files that the API's
  // host serves, and the client hydrates them.
  ssr: false,

  // Keep the existing src/ layout rather than moving everything to app/, so
  // adopting the router stays a small diff.
  appDirectory: 'src',

  // Each of these becomes a real HTML document carrying the content it lists, so
  // a search engine and a device that has not finished running JavaScript both
  // receive it. That is FR-032, and it is why this feature prerenders at all.
  //
  // The list is discovered in scripts/catalogue-paths.mjs rather than here,
  // because the sitemap needs exactly the same list and two discoveries drift.
  prerender: cataloguePaths,
} satisfies Config
