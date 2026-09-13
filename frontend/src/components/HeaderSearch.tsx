import { useEffect, useRef, useState } from 'react'
import { Form, useSearchParams } from 'react-router'
import { useSuggestions } from '../search/useSuggestions'

/** How long one example stays before the next replaces it. */
const ROTATION_MS = 6000

/** What the field says when the catalogue offers nothing to quote. */
const NEUTRAL_PLACEHOLDER = "назва книжки або ім'я автора"

/**
 * The site's one search field, in the banner on every page.
 *
 * A form submitting by GET, so pressing Enter runs the search with no pointing
 * device involved and the result gets an address that can be shared, bookmarked
 * and returned to with the back button (FR-014, FR-015). The submit button is
 * there for anyone who expects one; it is never the only way to search. There is
 * exactly one of these per page (FR-011).
 *
 * The router's `Form` rather than a bare `<form>`, and the difference matters
 * for one reason: a bare form reloads the whole document on every search, which
 * throws away the visitor's place in it — including the keyboard, which lands
 * back at the top rather than staying in the field they just typed in. It still
 * renders `<form method="get" action="/search">`, so with no JavaScript at all
 * it submits exactly as a plain form does.
 *
 * **The placeholder rotates, and four rules make that safe rather than hostile.**
 * A placeholder that changes under a reader is ordinarily a defect, so each of
 * these is a requirement rather than a refinement:
 *
 * - it is supplementary in every state, because a real visible `<label>` says
 *   what the field is for and never changes (FR-019). The accessible name comes
 *   from the label, so it is identical whatever the example currently reads;
 * - it freezes the moment the field takes focus, and stays frozen for the rest
 *   of the visit — nothing moves while somebody is typing or reading it;
 * - it never starts at all under `prefers-reduced-motion: reduce`;
 * - it falls back to a fixed neutral phrase when the catalogue offers nothing,
 *   so the field is never bare.
 */
export function HeaderSearch() {
  const [parameters] = useSearchParams()
  const suggestions = useSuggestions()
  const [index, setIndex] = useState(0)
  const frozen = useRef(false)

  /*
    The example as it read at the moment the field took focus, kept verbatim.

    Freezing the interval is not enough on its own: the suggestions arrive after
    the page does, so a visitor who reaches the field before they land would see
    the neutral placeholder swap for an example underneath them — a change after
    focus, which is the one thing the freeze exists to prevent. Holding the
    string rather than the timer is what makes "does not change again for the
    rest of the visit" true rather than nearly true.
  */
  const [frozenExample, setFrozenExample] = useState<string | null>(null)
  const isFrozen = frozenExample !== null
  const field = useRef<HTMLInputElement>(null)
  const submitted = useRef(false)

  /*
    Put the visitor back in the field they just typed in.

    Submitting the form blurs it, so focus lands on the document body, and the
    next Tab then starts at the top of the banner — past the skip link, the
    wordmark and three navigation links — to get back to where they already
    were. FR-014 asks that the result count be announced without focus being
    dragged to the results, and this is the other half of that: it should not be
    dropped on the floor either.

    Only after a submission from this field. Arriving at a result address from a
    link or a bookmark takes no focus at all, which is right: nothing has moved.
  */
  const term = parameters.get('q') ?? ''

  useEffect(() => {
    if (!submitted.current) {
      return
    }

    submitted.current = false

    // Now and again on the next frame. The router settles the navigation around
    // this effect, and which side of that the focus lands on varies with how
    // busy the machine is; doing it twice costs nothing and stops the behaviour
    // depending on load.
    field.current?.focus()

    const frame = requestAnimationFrame(() => field.current?.focus())

    return () => cancelAnimationFrame(frame)
  }, [term])

  useEffect(() => {
    if (isFrozen || suggestions.length === 0) {
      return
    }

    // Asked here rather than once at module load, because a visitor can change
    // the setting without reloading and the next interval must respect it.
    if (window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) {
      return
    }

    const timer = window.setInterval(() => {
      setIndex((current) => (current + 1) % suggestions.length)
    }, ROTATION_MS)

    return () => window.clearInterval(timer)
  }, [isFrozen, suggestions.length])

  const example = suggestions[index]
  const placeholder =
    frozenExample ?? (example === undefined ? NEUTRAL_PLACEHOLDER : `напр. ${example.term}`)

  return (
    <Form
      className="site-search"
      role="search"
      aria-label="Пошук по каталогу"
      method="get"
      action="/search"
      onSubmit={() => {
        submitted.current = true
      }}
    >
      {/*
        A real <label>, and the field's accessible name comes from it rather
        than from the placeholder — which is the substance of FR-019 and the
        reason the example inside the field is allowed to rotate at all.
        It is visually hidden rather than shown: the submit button beside it
        already says "Шукати" in plain sight, so a second visible word naming
        the same control was repeating itself and taking the field's room.
      */}
      <label className="visually-hidden" htmlFor="site-search-term">
        Пошук у каталозі
      </label>
      <div className="site-search__controls">
        <input
          ref={field}
          id="site-search-term"
          className="site-search__input"
          type="search"
          name="q"
          // The field carries the current term, so arriving at a result page
          // from its own address shows what was searched for rather than an
          // empty box (FR-016).
          defaultValue={parameters.get('q') ?? ''}
          placeholder={placeholder}
          // Bounded on the server too; saying so here means a visitor is
          // stopped at the field rather than by an error page.
          maxLength={100}
          autoComplete="off"
          onFocus={() => {
            // Permanently, and via a ref as well as state so that a second
            // focus cannot restart it through a stale closure.
            if (!frozen.current) {
              frozen.current = true
              setFrozenExample(placeholder)
            }
          }}
        />
        <button type="submit">Шукати</button>
      </div>
    </Form>
  )
}
