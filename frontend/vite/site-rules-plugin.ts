import { existsSync, statSync } from 'node:fs'
import { join } from 'node:path'
import type { Plugin } from 'vite'
import { looksLikeAFile, redirectFor } from '../site-rules.js'

/**
 * Installs the two rules in `site-rules.ts` on the dev server.
 *
 * Ahead of React Router's own middleware, which is the whole point: that
 * middleware answers every address it does not recognise with the single-page
 * document and a 200, so anything running after it can only ever change a
 * response that has already said "this address is fine".
 *
 * The honest limit is worth stating here as well as in the plan: this serves
 * the dev server and the Playwright suite. Production hosting does not exist
 * yet, and when it does it is configured from the same table rather than from
 * this file.
 */
export function siteRules(): Plugin {
  return {
    name: 'vox-lib-site-rules',
    enforce: 'pre',

    configureServer(server) {
      const publicDir = typeof server.config.publicDir === 'string' ? server.config.publicDir : null

      // The returned function runs after Vite's own middlewares are installed,
      // which is how this gets in front of React Router's catch-all rather than
      // behind it.
      return () => {
        server.middlewares.use((request, response, next) => {
          const path = (request.url ?? '/').split('?')[0]

          const destination = redirectFor(path)

          if (destination !== null) {
            const query = (request.url ?? '').slice(path.length)

            response.statusCode = 301
            response.setHeader('Location', destination + query)
            response.end()
            return
          }

          // A file-shaped address that no file satisfies is a 404, not a page.
          if (looksLikeAFile(path) && !isServable(path, publicDir)) {
            response.statusCode = 404
            response.setHeader('Content-Type', 'text/plain; charset=utf-8')
            response.end('Not found')
            return
          }

          next()
        })
      }
    },
  }
}

/**
 * Whether something on disk or in the module graph can answer this path.
 *
 * Deliberately generous: anything under `/src`, `/node_modules`, `/@` or Vite's
 * own cache is the dev server's plumbing and is left alone. Being wrong in that
 * direction costs nothing — the request falls through to Vite, which is what
 * would have happened anyway — while being wrong the other way would 404 the
 * application's own modules.
 */
function isServable(path: string, publicDir: string | null): boolean {
  /*
    Route data, not a file. React Router fetches a route's loader data from
    `<path>.data` on every client-side navigation, and that address is
    file-shaped: it has a dot in its last segment. The build writes those files
    to disk, so in production the ordinary rule is right about them; the dev
    server generates them on the fly, so here they must fall through.

    Without this the static-file rule 404s every in-page navigation, which looks
    like a broken link and does not reproduce on a full page load -- the one
    case that does not fetch route data.
  */
  if (path.endsWith('.data')) {
    return true
  }

  if (
    path.startsWith('/src/') ||
    path.startsWith('/node_modules/') ||
    path.startsWith('/@') ||
    path.startsWith('/.vite/')
  ) {
    return true
  }

  if (publicDir === null) {
    return false
  }

  const candidate = join(publicDir, path)

  return existsSync(candidate) && statSync(candidate).isFile()
}
