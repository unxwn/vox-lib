import { useEffect, useRef } from 'react'

/**
 * Moves focus to an element when a value changes.
 *
 * Used when a screen replaces what it was showing: after registering, after
 * confirming, after a failed sign-in. A sighted person sees the page change; a
 * screen reader user is still where they were, reading a form that no longer
 * matters, unless focus follows. This is the mechanical half of FR-033.
 */
export function useFocusOnChange<T extends HTMLElement>(trigger: unknown) {
  const target = useRef<T>(null)

  useEffect(() => {
    target.current?.focus()
  }, [trigger])

  return target
}
