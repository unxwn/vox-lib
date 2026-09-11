import { useState } from 'react'
import { Link } from 'react-router'
import { FormField } from '../components/FormField'
import { StatusRegion } from '../components/StatusRegion'
import { sendAccountRequest } from '../api/account'
import { useFocusOnChange } from '../account/useFocusOnChange'
import '../styles/account.css'

export function meta() {
  return [
    { title: 'Відновлення доступу · vox-lib' },
    { name: 'robots', content: 'noindex, nofollow' },
  ]
}

/**
 * Asking for a way back in.
 *
 * The answer is the same whether or not the address has an account, and the
 * wording says so plainly rather than implying that a message is definitely on
 * its way. Anything more specific here would tell whoever typed the address
 * whether it is registered.
 */
export default function ForgotPasswordRoute() {
  const [email, setEmail] = useState('')
  const [busy, setBusy] = useState(false)
  const [sent, setSent] = useState(false)
  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [failure, setFailure] = useState<string>()

  const done = useFocusOnChange<HTMLHeadingElement>(sent)

  async function submit(event: React.FormEvent) {
    event.preventDefault()

    setBusy(true)
    setErrors({})
    setFailure(undefined)

    const result = await sendAccountRequest('/api/account/recovery-requests', 'POST', { email })

    setBusy(false)

    if (result.ok) {
      setSent(true)
      return
    }

    if (result.reason === 'invalid') {
      setErrors(result.errors)
      return
    }

    setFailure(
      result.reason === 'tooManyAttempts'
        ? result.detail
        : 'Не вдалося зв’язатися із сервером. Перевірте з’єднання та спробуйте ще раз.',
    )
  }

  if (sent) {
    return (
      <main className="account">
        <h1 className="account__heading" ref={done} tabIndex={-1}>
          Перевірте свою пошту
        </h1>
        <StatusRegion tone="status">
          <p>
            Якщо для адреси {email} є обліковий запис, ми надіслали лист із посиланням для
            встановлення нового пароля. Посилання дійсне 24 години й спрацює один раз.
          </p>
        </StatusRegion>
        <div className="account__links">
          <Link to="/sign-in">Повернутися до входу</Link>
        </div>
      </main>
    )
  }

  return (
    <main className="account">
      <h1 className="account__heading">Відновлення доступу</h1>

      <p className="account__intro">
        Вкажіть адресу електронної пошти вашого облікового запису. Ми надішлемо посилання, за яким
        можна встановити новий пароль.
      </p>

      <form className="account__form" onSubmit={submit} noValidate>
        <FormField
          label="Адреса електронної пошти"
          type="email"
          name="email"
          value={email}
          onChange={setEmail}
          autoComplete="username"
          maxLength={254}
          error={errors.email?.[0]}
          autoFocus
        />

        <div className="account__actions">
          <button type="submit" disabled={busy}>
            Надіслати посилання
          </button>
        </div>
      </form>

      <StatusRegion tone={failure === undefined ? 'status' : 'alert'}>
        {busy && <p>Надсилаємо…</p>}
        {failure !== undefined && <p>{failure}</p>}
      </StatusRegion>

      <div className="account__links">
        <Link to="/sign-in">Згадали пароль? Увійти</Link>
      </div>
    </main>
  )
}
