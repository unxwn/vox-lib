import { useId, type ReactNode } from 'react'

type FormFieldProps = {
  label: string
  type: 'email' | 'password' | 'text'
  name: string
  value: string
  onChange: (value: string) => void
  /**
   * The conventional token for this field, so a password manager fills it and
   * offers to save the result. FR-032.
   */
  autoComplete: string
  /** Why this field was rejected, in Ukrainian. Absent means it was not. */
  error?: string
  /** Read before the field is filled in, such as the password requirements. */
  hint?: ReactNode
  required?: boolean
  maxLength?: number
  autoFocus?: boolean
}

/**
 * One labelled control, wired so that a screen reader user learns which field to
 * correct without hunting.
 *
 * The three attributes that do that work are `aria-invalid` on the rejected
 * control, `aria-describedby` pointing at the message and the hint, and the
 * message living next to the field rather than in a summary somewhere else.
 * FR-031 is a property of this file, so every screen gets it by using this
 * rather than by remembering to.
 */
export function FormField({
  label,
  type,
  name,
  value,
  onChange,
  autoComplete,
  error,
  hint,
  required = true,
  maxLength,
  autoFocus = false,
}: FormFieldProps) {
  const id = useId()
  const errorId = `${id}-error`
  const hintId = `${id}-hint`

  // Order matters to a screen reader: it reads the description in the order
  // given, and the reason a field was rejected is the more urgent of the two.
  const describedBy =
    [error !== undefined ? errorId : null, hint !== undefined ? hintId : null]
      .filter((value) => value !== null)
      .join(' ') || undefined

  return (
    <div className="field">
      <label className="field__label" htmlFor={id}>
        {label}
      </label>

      {hint !== undefined && (
        <div className="field__hint" id={hintId}>
          {hint}
        </div>
      )}

      <input
        className="field__input"
        id={id}
        name={name}
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        autoComplete={autoComplete}
        aria-invalid={error !== undefined}
        aria-describedby={describedBy}
        required={required}
        maxLength={maxLength}
        autoFocus={autoFocus}
      />

      {error !== undefined && (
        <p className="field__error" id={errorId}>
          {error}
        </p>
      )}
    </div>
  )
}
