import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { StatusRegion } from '../components/StatusRegion'
import { sendAccountRequest } from '../api/account'
import { useFocusOnChange } from '../account/useFocusOnChange'
import '../styles/account.css'

export function meta() {
  return [
    { title: 'Підтвердження адреси · vox-lib' },
    { name: 'robots', content: 'noindex, nofollow' },
  ]
}

type Phase = 'working' | 'confirmed' | 'alreadyConfirmed' | 'expired' | 'unavailable'

const HEADINGS: Record<Phase, string> = {
  working: 'Підтверджуємо адресу…',
  confirmed: 'Адресу підтверджено',
  alreadyConfirmed: 'Адресу вже підтверджено',
  expired: 'Посилання більше не дійсне',
  unavailable: 'Не вдалося підтвердити адресу',
}

/**
 * The screen the link in the confirmation message points at.
 *
 * It confirms on load rather than asking the person to press a button. They
 * already acted, by following a link they were sent; asking again would be a
 * second step for no gain, and for someone navigating by screen reader every
 * extra step is a real cost.
 */
export default function ConfirmRoute() {
  const [parameters] = useSearchParams()
  const [phase, setPhase] = useState<Phase>('working')
  const [resent, setResent] = useState(false)

  const accountId = parameters.get('account') ?? ''
  const token = parameters.get('token') ?? ''

  // Focus follows the outcome, so a screen reader user is moved to the answer
  // rather than being left on a heading that says the work is still going on.
  const heading = useFocusOnChange<HTMLHeadingElement>(phase)

  useEffect(() => {
    let cancelled = false

    void sendAccountRequest<{ outcome: string }>('/api/account/confirmations', 'POST', {
      accountId,
      token,
    }).then((result) => {
      if (cancelled) {
        return
      }

      if (result.ok) {
        setPhase(result.value.outcome === 'confirmed' ? 'confirmed' : 'alreadyConfirmed')
        return
      }

      setPhase(result.reason === 'linkExpired' ? 'expired' : 'unavailable')
    })

    return () => {
      cancelled = true
    }
  }, [accountId, token])

  return (
    <main className="account">
      <h1 className="account__heading" ref={heading} tabIndex={-1}>
        {HEADINGS[phase]}
      </h1>

      <StatusRegion tone={phase === 'expired' || phase === 'unavailable' ? 'alert' : 'status'}>
        {phase === 'working' && <p>Зачекайте, будь ласка.</p>}
        {phase === 'confirmed' && <p>Тепер ви можете увійти до свого облікового запису.</p>}
        {phase === 'alreadyConfirmed' && (
          <p>Цю адресу вже було підтверджено раніше. Можна одразу входити.</p>
        )}
        {phase === 'expired' && (
          <p>Термін дії посилання минув або його вже було використано. Надішліть новий лист.</p>
        )}
        {phase === 'unavailable' && (
          <p>Не вдалося зв’язатися із сервером. Перевірте з’єднання та спробуйте ще раз.</p>
        )}
      </StatusRegion>

      {/* The way onward is where focus lands, per acceptance scenario 5, rather
          than somewhere the person has to go looking for. */}
      {(phase === 'confirmed' || phase === 'alreadyConfirmed') && (
        <div className="account__links">
          <Link to="/sign-in">Увійти</Link>
          <Link to="/">Повернутися до каталогу</Link>
        </div>
      )}

      {phase === 'expired' && <ResendForm resent={resent} onResent={() => setResent(true)} />}
    </main>
  )
}

/**
 * Every flow that waits on a message offers another one, because delivery is
 * outside this system's control and confirmation is on the critical path:
 * without a new link, an expired one is a dead end.
 */
function ResendForm({ resent, onResent }: { resent: boolean; onResent: () => void }) {
  const [email, setEmail] = useState('')
  const [sending, setSending] = useState(false)

  async function submit(event: React.FormEvent) {
    event.preventDefault()
    setSending(true)

    await sendAccountRequest('/api/account/confirmation-requests', 'POST', { email })

    setSending(false)
    onResent()
  }

  if (resent) {
    return (
      <StatusRegion tone="status">
        <p>
          Якщо для цієї адреси є непідтверджений обліковий запис, ми надіслали новий лист із
          посиланням.
        </p>
      </StatusRegion>
    )
  }

  return (
    <form className="account__form" onSubmit={submit} noValidate>
      <label className="field__label" htmlFor="resend-email">
        Адреса електронної пошти
      </label>
      <input
        className="field__input"
        id="resend-email"
        name="email"
        type="email"
        value={email}
        onChange={(event) => setEmail(event.target.value)}
        autoComplete="username"
        maxLength={254}
        required
      />
      <div className="account__actions">
        <button type="submit" disabled={sending}>
          Надіслати новий лист
        </button>
      </div>
    </form>
  )
}
