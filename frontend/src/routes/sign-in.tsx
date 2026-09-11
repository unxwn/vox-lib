import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { FormField } from '../components/FormField'
import { StatusRegion } from '../components/StatusRegion'
import { sendAccountRequest } from '../api/account'
import { readSession } from '../account/useSession'
import '../styles/account.css'

export function meta() {
  return [{ title: 'Вхід · vox-lib' }, { name: 'robots', content: 'noindex, nofollow' }]
}

type Failure =
  | { kind: 'message'; text: string }
  /** The password was right but the address is unconfirmed, so offer another message. */
  | { kind: 'notConfirmed'; text: string }
  /** The browser accepted the sign-in and then discarded the session. */
  | { kind: 'sessionNotKept' }

export default function SignInRoute() {
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [failure, setFailure] = useState<Failure>()
  const [resent, setResent] = useState(false)

  async function submit(event: React.FormEvent) {
    event.preventDefault()

    setBusy(true)
    setErrors({})
    setFailure(undefined)
    setResent(false)

    const result = await sendAccountRequest('/api/account/session', 'POST', { email, password })

    if (result.ok) {
      // FR-019. The API said yes and set the cookie; if reading the session back
      // shows nobody, this browser is discarding it. Private browsing and
      // blocked site data both look like this, and the alternative is returning
      // the person silently to a form they just filled in correctly, which is
      // indistinguishable from a wrong password.
      const session = await readSession()

      if (session.state !== 'signedIn') {
        setBusy(false)
        setFailure({ kind: 'sessionNotKept' })
        return
      }

      await navigate('/')
      return
    }

    setBusy(false)

    switch (result.reason) {
      case 'invalid':
        setErrors(result.errors)
        return
      case 'notConfirmed':
        setFailure({ kind: 'notConfirmed', text: result.detail })
        return
      case 'notRecognised':
      case 'tooManyAttempts':
        setFailure({ kind: 'message', text: result.detail })
        return
      default:
        setFailure({
          kind: 'message',
          text: 'Не вдалося зв’язатися із сервером. Перевірте з’єднання та спробуйте ще раз.',
        })
    }
  }

  async function resendConfirmation() {
    await sendAccountRequest('/api/account/confirmation-requests', 'POST', { email })
    setResent(true)
  }

  return (
    <main className="account">
      <h1 className="account__heading">Вхід</h1>

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
          // current-password, not new-password: this tells a password manager to
          // offer what it already has rather than to generate something.
          autoComplete="current-password"
          maxLength={256}
          error={errors.password?.[0]}
        />

        <div className="account__actions">
          <button type="submit" disabled={busy}>
            Увійти
          </button>
        </div>
      </form>

      {/* An alert rather than a status: the person asked to be let in and was
          not, so this interrupts rather than waiting for a pause. FR-033. */}
      <StatusRegion tone={failure === undefined ? 'status' : 'alert'}>
        {busy && <p>Входимо…</p>}
        {failure?.kind === 'message' && <p>{failure.text}</p>}
        {failure?.kind === 'notConfirmed' && <p>{failure.text}</p>}
        {failure?.kind === 'sessionNotKept' && (
          <p>
            Вхід виконано, але цей браузер не зберігає сеанс. Найчастіше так буває в приватному
            вікні або коли заблоковано дані сайтів. Дозвольте зберігати дані для цього сайту або
            скористайтеся звичайним вікном.
          </p>
        )}
        {resent && <p>Ми надіслали новий лист із посиланням для підтвердження.</p>}
      </StatusRegion>

      {failure?.kind === 'notConfirmed' && !resent && (
        <div className="account__actions">
          <button type="button" onClick={resendConfirmation}>
            Надіслати лист із підтвердженням ще раз
          </button>
        </div>
      )}

      <div className="account__links">
        <Link to="/forgot-password">Забули пароль?</Link>
        <Link to="/register">Створити обліковий запис</Link>
        <Link to="/">Повернутися до каталогу</Link>
      </div>
    </main>
  )
}
