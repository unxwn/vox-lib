# Manual Screen Reader Verification: Browse the Book Catalogue

**Feature**: `specs/001-browse-catalogue/` | **Requirement**: SC-001, Constitution Principle I

Automated checks catch violations and regressions. They do not prove the product is usable by
someone who cannot see it. Principle I says a feature that is not operable with a screen
reader is not done, so each story ends with a pass on real assistive technology and the result
is recorded here before the story is called complete.

Record the assistive technology version, the operating system version and the browser, because
behaviour differs between them and a pass on one is not a pass on another.

## Story 1, the catalogue list (T037)

**Status: outstanding.** Everything else in Story 1 is complete and the automated
accessibility run in `frontend/tests/a11y/catalogue.spec.ts` passes: zero critical and
serious axe violations, every book on the page reachable by keyboard with a visible focus
indicator and no trap, focus landing on the results heading after a page change, and the
position and result count announced through a polite region. None of that is a substitute
for this table. Principle I is not satisfied until someone has worked through the catalogue
on real assistive technology, which needs an iOS device with VoiceOver and an Android device
with TalkBack.

| Field | Value |
| --- | --- |
| Date | |
| VoiceOver, iOS version, browser | |
| TalkBack, Android version, browser | |
| Every book announced by title and author | |
| Position and result count announced on a page change | |
| No unreachable content below the list | |
| Outcome | |
| Notes and defects raised | |

## Story 2, catalogue to chapter list (T051)

This is SC-001 in full: reaching a named book's chapter list from the catalogue without
sighted assistance.

**Status: outstanding.** Everything else in Story 2 is complete and the automated
accessibility run in `frontend/tests/a11y/book.spec.ts` passes: zero critical and serious axe
violations, one h1 naming the book with no heading level skipped beneath it, the chapters
announced as a list with a count that matches the number the page states, focus landing on
the book after it is reached from the catalogue, the book's own language declared on its
content while the document stays Ukrainian, and no playback control of any kind. None of that
is a substitute for this table. Principle I is not satisfied until someone has made the
journey on real assistive technology, which needs an iOS device with VoiceOver and an Android
device with TalkBack.

| Field | Value |
| --- | --- |
| Date | |
| VoiceOver, iOS version, browser | |
| TalkBack, Android version, browser | |
| Journey completed without sighted assistance | |
| Heading structure navigable | |
| Chapter list announced as a list with its count | |
| Page language announced correctly, including a book whose language differs | |
| Outcome | |
| Notes and defects raised | |

## Story 3, search (T062)

**Status: outstanding.** Everything else in Story 3 is complete and the automated
accessibility run in `frontend/tests/a11y/search.spec.ts` passes: zero critical and serious
axe violations, the field carrying a real label rather than a placeholder, the form operated
by keyboard alone from focus to submission, the result count announced through a polite
region when the displayed set changes, and focus deliberately left in the search field while
that happens. None of that is a substitute for this table: whether a polite announcement is
actually heard, and whether it arrives before the visitor has moved on, is a question only a
real screen reader answers.

| Field | Value |
| --- | --- |
| Date | |
| VoiceOver, iOS version, browser | |
| TalkBack, Android version, browser | |
| Result count announced without stealing focus | |
| Empty result state reachable and understandable | |
| Outcome | |
| Notes and defects raised | |
