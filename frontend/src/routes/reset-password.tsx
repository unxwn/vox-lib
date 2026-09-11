import { useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { FormField } from '../components/FormField'
import { StatusRegion } from '../components/StatusRegion'
import { sendAccountRequest } from '../api/account'
import { describePolicy, usePasswordPolicy } from '../account/usePasswordPolicy'
import { useFocusOnChange } from '../account/useFocusOnChange'
import '../styles/account.css'

export function meta() {
  return [{ title: 'Новий пароль · vox-lib' }, { name: 'robots', content: 'noindex, nofollow' }]
}

type Phase = 'editing' | 'submitting' | 'done' | 'expired'

/**
 * The screen the recovery link points at.
 *
 * The requirements are stated here before the form is submitted, exactly as they
 * are when registering. This is the one place a person is choosing a password
 * under pressure, having already lost access once, so learning the rule only
 * after being refused is the worst possible moment for it.
 */
export default function ResetPasswordRoute() {
  const [parameters] = useSearchParams()
  const policy = usePasswordPolicy()

  const [password, setPassword] = useState('')
  const [phase, setPhase] = useState<Phase>('editing')
  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [failure, setFailure] = useState<string>()

  const accountId = parameters.get('account') ?? ''
  const token = parameters.get('token') ?? ''

  const heading = useFocusOnChange<HTMLHeadingElement>(phase === 'done' || phase === 'expired')

  async function submit(event: React.FormEvent) {
    event.preventDefault()

    setPhase('submitting')
    setErrors({})
    setFailure(undefined)

    const result = await sendAccountRequest('/api/account/recoveries', 'POST', {
      accountId,
      token,
      password,
    })

    if (result.ok) {
      setPhase('done')
      return
    }

    if (result.reason === 'linkExpired') {
      setPhase('expired')
      return
    }

    setPhase('editing')

    if (result.reason === 'invalid') {
      setErrors(result.errors)
      return
    }

    setFailure('Не вдалося зв’язатися із сервером. Перевірте з’єднання та спробуйте ще раз.')
  }

  if (phase === 'done') {
    return (
      <main className="account">
        <h1 className="account__heading" ref={heading} tabIndex={-1}>
          Пароль змінено
        </h1>
        <StatusRegion tone="status">
          <p>
            Тепер можна увійти з новим паролем. Усі сеанси, відкриті раніше на інших пристроях,
            завершено.
          </p>
        </StatusRegion>
        <div className="account__links">
          <Link to="/sign-in">Увійти</Link>
        </div>
      </main>
    )
  }

  if (phase === 'expired') {
    return (
      <main className="account">
        <h1 className="account__heading" ref={heading} tabIndex={-1}>
          Посилання більше не дійсне
        </h1>
        <StatusRegion tone="alert">
          <p>Термін дії посилання минув або його вже було використано. Попросіть нове посилання.</p>
        </StatusRegion>
        <div className="account__links">
          <Link to="/forgot-password">Надіслати нове посилання</Link>
        </div>
      </main>
    )
  }

  return (
    <main className="account">
      <h1 className="account__heading">Новий пароль</h1>

      <form className="account__form" onSubmit={submit} noValidate>
        <FormField
          label="Новий пароль"
          type="password"
          name="password"
          value={password}
          onChange={setPassword}
          autoComplete="new-password"
          maxLength={256}
          error={errors.password?.[0]}
          hint={describePolicy(policy)}
          autoFocus
        />

        <div className="account__actions">
          <button type="submit" disabled={phase === 'submitting'}>
            Встановити пароль
          </button>
        </div>
      </form>

      <StatusRegion tone={failure === undefined ? 'status' : 'alert'}>
        {phase === 'submitting' && <p>Зберігаємо новий пароль…</p>}
        {failure !== undefined && <p>{failure}</p>}
      </StatusRegion>
    </main>
  )
}
