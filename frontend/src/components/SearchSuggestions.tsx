import { Link } from 'react-router'
import { useSuggestions } from '../search/useSuggestions'
import { searchHref } from '../catalogue/search'

/**
 * A few things worth searching for, as ordinary links.
 *
 * Links, not buttons and not a listbox, and that is the accessibility decision
 * the whole idea rests on (FR-020). A link can be reached by Tab, is announced
 * as a link, says where it goes, and can be opened in a new tab; a combobox
 * announcing changing results as somebody types is among the hardest patterns
 * in the specification to get right and is excluded by FR-024 anyway.
 *
 * It is the half of the suggestion idea a keyboard and a screen reader can
 * actually use — the rotating example inside the field is decoration, and this
 * is the substance.
 */
export function SearchSuggestions() {
  const suggestions = useSuggestions()

  if (suggestions.length === 0) {
    return null
  }

  return (
    <section className="suggestions" aria-labelledby="suggestions-heading">
      <h2 id="suggestions-heading" className="suggestions__heading">
        Спробуйте пошукати
      </h2>
      <ul className="suggestions__list">
        {suggestions.map((suggestion) => (
          <li key={`${suggestion.kind}-${suggestion.term}`}>
            <Link to={searchHref(suggestion.term, 1)}>{suggestion.term}</Link>
          </li>
        ))}
      </ul>
    </section>
  )
}
