import { useEffect, useState } from 'react'
import './App.css'

type WeatherForecast = {
  date: string
  temperatureC: number
  temperatureF: number
  summary: string | null
}

type State =
  | { status: 'loading' }
  | { status: 'ok'; data: WeatherForecast[] }
  | { status: 'error'; message: string }

export default function App() {
  const [state, setState] = useState<State>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()

    fetch('/api/weatherforecast', { signal: controller.signal })
      .then(async (res) => {
        if (!res.ok) throw new Error(`API returned ${res.status} ${res.statusText}`)
        return (await res.json()) as WeatherForecast[]
      })
      .then((data) => setState({ status: 'ok', data }))
      .catch((err: unknown) => {
        if (err instanceof Error && err.name === 'AbortError') return
        setState({ status: 'error', message: err instanceof Error ? err.message : String(err) })
      })

    return () => controller.abort()
  }, [])

  return (
    <main>
      <h1>vox-lib</h1>
      <p className="subtitle">
        React {`•`} Vite {`•`} ASP.NET Core
      </p>

      {state.status === 'loading' && <p>Loading forecast…</p>}

      {state.status === 'error' && (
        <div className="error">
          <p>
            <strong>Could not reach the API.</strong> {state.message}
          </p>
          <p>
            Start the backend with <code>pnpm run api</code> from the repo root.
          </p>
        </div>
      )}

      {state.status === 'ok' && (
        <table>
          <thead>
            <tr>
              <th>Date</th>
              <th>°C</th>
              <th>°F</th>
              <th>Summary</th>
            </tr>
          </thead>
          <tbody>
            {state.data.map((f) => (
              <tr key={f.date}>
                <td>{f.date}</td>
                <td>{f.temperatureC}</td>
                <td>{f.temperatureF}</td>
                <td>{f.summary ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  )
}
