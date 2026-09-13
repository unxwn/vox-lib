import { useEffect, useState } from 'react'

export type SearchSuggestion = {
  term: string
  kind: 'title' | 'author'
}

/**
 * The one in-flight read of the suggestions, shared by everyone who asks.
 *
 * Module level for the same reason the session is: the header field and the
 * list beneath it are separate components, and holding the promise per consumer
 * would ask the server the same question twice and get two different answers —
 * so the example rotating inside the field would name books the links below it
 * do not.
 */
let inFlight: Promise<SearchSuggestion[]> | null = null

async function fetchSuggestions(): Promise<SearchSuggestion[]> {
  try {
    const response = await fetch('/api/search-suggestions', {
      headers: { Accept: 'application/json' },
    })

    if (!response.ok) {
      return []
    }

    const body = (await response.json()) as { items?: SearchSuggestion[] }

    return body.items ?? []
  } catch {
    // An empty list, never a thrown error. Search is never blocked by the
    // thing that decorates it: the field keeps its label, its neutral
    // placeholder and its submit button, and works exactly as before.
    return []
  }
}

/**
 * A handful of things worth searching for, read once per page view.
 *
 * The read starts in an effect rather than during render, so the prerendered
 * document carries no suggestions and the page is identical for everyone until
 * the browser has run — which is what keeps a sampled, uncacheable response out
 * of a cacheable document.
 */
export function useSuggestions(): SearchSuggestion[] {
  const [suggestions, setSuggestions] = useState<SearchSuggestion[]>([])

  useEffect(() => {
    let cancelled = false

    inFlight ??= fetchSuggestions()

    void inFlight.then((next) => {
      if (!cancelled) {
        setSuggestions(next)
      }
    })

    return () => {
      cancelled = true
    }
  }, [])

  return suggestions
}
