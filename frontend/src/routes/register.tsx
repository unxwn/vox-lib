import { useState } from 'react'
import { Link } from 'react-router'
import { FormField } from '../components/FormField'
import { StatusRegion } from '../components/StatusRegion'
import { sendAccountRequest, type AccountResult } from '../api/account'
import { describePolicy, usePasswordPolicy } from '../account/usePasswordPolicy'
import { useFocusOnChange } from '../account/useFocusOnChange'
import '../styles/account.css'

export function meta() {
  return [
    { title: 'Створити обліковий запис · vox-lib' },
    // FR-028. The page is also absent from the prerender list, so no document
    // for it is ever emitted; this covers a crawler that reaches it through the
    // single-page fallback.
    { name: 'robots', content: 'noindex, nofollow' },
  ]
}

type Phase = 'editing' | 'submitting' | 'sent'

export default function RegisterRoute() {
  const policy = usePasswordPolicy()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [phase, setPhase] = useState<Phase>('editing')
  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [failure, setFailure] = useState<string>()
  const [resent, setResent] = useState(false)

  const done = useFocusOnChange<HTMLHeadingElement>(phase === 'sent')

  async function submit(event: React.FormEvent) {
    event.preventDefault()

    setPhase('submitting')
    setErrors({})
    setFailure(undefined)

    const result: AccountResult<void> = await sendAccountRequest(
      '/api/account/registrations',
      'POST',
      { email, password },
    )

    if (result.ok) {
      setPhase('sent')
      return
    }

    setPhase('editing')

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

  // The success state replaces the form entirely. Leaving the form on the page
  // invites a second submission, and the person has nothing left to do here:
  // the next step is in their inbox.
  if (phase === 'sent') {
    return (
      <main className="account">
        <h1 className="account__heading" ref={done} tabIndex={-1}>
          Перевірте свою пошту
        </h1>
        <StatusRegion tone="status">
          <p>
            Ми надіслали лист на адресу {email}. Перейдіть за посиланням у ньому, щоб підтвердити
            адресу. Доки адресу не підтверджено, увійти не вдасться.
          </p>
        </StatusRegion>
        {/*
          Delivery is outside this system's control and confirmation is on the
          critical path, so every flow that waits on a message offers another
          one. Without this, a message lost to a spam filter or to an outage
          leaves a person with an account they can never use.
        */}
        {resent ? (
          <StatusRegion tone="status">
            <p>Ми надіслали лист ще раз. Перевірте також теку зі спамом.</p>
          </StatusRegion>
        ) : (
          <div className="account__actions">
            <button
              type="button"
              onClick={() => {
                void sendAccountRequest('/api/account/confirmation-requests', 'POST', {
                  email,
                }).then(() => setResent(true))
              }}
            >
              Лист не надійшов? Надіслати ще раз
            </button>
          </div>
        )}

        <div className="account__links">
          <Link to="/sign-in">Перейти до входу</Link>
          <Link to="/">Повернутися до каталогу</Link>
        </div>
      </main>
    )
  }

  return (
    <main className="account">
      <h1 className="account__heading">Створити обліковий запис</h1>

      <p className="account__intro">
        Обліковий запис потрібен, щоб слухати аудіокниги. Каталог можна переглядати й без нього.
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

        <FormField
          label="Пароль"
          type="password"
          name="password"
          value={password}
          onChange={setPassword}
          autoComplete="new-password"
          maxLength={256}
          error={errors.password?.[0]}
          // Stated before the form is submitted, not only after a failure.
          // FR-002.
          hint={describePolicy(policy)}
        />

        <div className="account__actions">
          <button type="submit" disabled={phase === 'submitting'}>
            Створити обліковий запис
          </button>
        </div>
      </form>

      <StatusRegion tone={failure === undefined ? 'status' : 'alert'}>
        {phase === 'submitting' && <p>Створюємо обліковий запис…</p>}
        {failure !== undefined && <p>{failure}</p>}
      </StatusRegion>

      <div className="account__links">
        <Link to="/sign-in">Уже маєте обліковий запис? Увійти</Link>
      </div>
    </main>
  )
}
