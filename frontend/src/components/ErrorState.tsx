type ErrorStateProps = {
  onRetry?: () => void
}

/**
 * Shown when the catalogue could not be reached, which is the dropped connection
 * the specification raises as an edge case.
 *
 * role="alert" rather than "status": the visitor asked for something and did not
 * get it, so this interrupts. It says what failed and what to do about it, and
 * the retry is a real button so it can be reached by keyboard like anything else.
 */
export function ErrorState({ onRetry }: ErrorStateProps) {
  return (
    <div className="state state--error" role="alert">
      <p>Не вдалося завантажити каталог.</p>
      <p>Схоже, немає зв'язку із сервером. Перевірте з'єднання та спробуйте ще раз.</p>
      {onRetry !== undefined && (
        <button type="button" onClick={onRetry}>
          Спробувати ще раз
        </button>
      )}
    </div>
  )
}
