/**
 * Shown while a page of the catalogue is on its way.
 *
 * role="status" is the point of this component rather than the spinner: a
 * sighted visitor sees that something is happening, and FR-017 says a screen
 * reader user has to be told the same thing. Politely, so it waits for a gap
 * rather than interrupting.
 */
export function LoadingState() {
  return (
    <p className="state state--loading" role="status">
      Завантажуємо каталог…
    </p>
  )
}
