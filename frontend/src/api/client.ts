/**
 * The one way the frontend talks to the API.
 *
 * Failures come back as values rather than exceptions, so every caller has to
 * decide what to show for each one. That is what FR-017 needs: a state a sighted
 * visitor would notice is a state a screen reader user has to be told about, and
 * a thrown error that nobody catches announces nothing.
 */
export type ApiResult<T> =
  | { ok: true; value: T }
  /** The resource does not exist: an unknown book, or a page past the end. */
  | { ok: false; reason: 'notFound' }
  /** The API could not be reached or did not answer properly. */
  | { ok: false; reason: 'unavailable' }

/**
 * In the browser the API is same origin, so paths stay relative. Hardcoding an
 * origin there would bypass the Vite proxy and bring CORS back.
 *
 * The prerender is the one case with no origin to be relative to, because it
 * runs in Node at build time, so the build supplies one.
 */
function apiOrigin(): string {
  if (typeof document !== 'undefined') {
    return ''
  }

  // Reached through globalThis rather than the process global directly, so the
  // browser bundle carries no reference a bundler might try to replace.
  const node = globalThis as { process?: { env?: Record<string, string | undefined> } }

  return node.process?.env?.VOXLIB_API_ORIGIN ?? 'http://localhost:5080'
}

export async function apiGet<T>(path: string, signal?: AbortSignal): Promise<ApiResult<T>> {
  if (!path.startsWith('/api/')) {
    throw new Error(`API paths must be relative and start with /api/, got "${path}".`)
  }

  let response: Response

  try {
    response = await fetch(`${apiOrigin()}${path}`, {
      headers: { Accept: 'application/json' },
      signal,
    })
  } catch {
    return { ok: false, reason: 'unavailable' }
  }

  if (response.status === 404) {
    return { ok: false, reason: 'notFound' }
  }

  if (!response.ok) {
    return { ok: false, reason: 'unavailable' }
  }

  try {
    return { ok: true, value: (await response.json()) as T }
  } catch {
    return { ok: false, reason: 'unavailable' }
  }
}
