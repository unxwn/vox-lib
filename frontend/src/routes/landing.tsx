import { Link } from 'react-router'
import { landingMeta } from '../catalogue/meta'
import '../styles/catalogue.css'

export function meta() {
  return landingMeta()
}

/**
 * The front door.
 *
 * Until now the root address was the catalogue, so the site opened on a list of
 * books and never said what it was or who it was for (FR-025).
 *
 * The claims below — who it is for, what it costs, why an account is needed —
 * were invented, so each is marked "До прикладу:" rather than presented as
 * fact. The marking shows the shape and length the section wants without
 * asserting anything; whoever knows the answers replaces the sentence and drops
 * the prefix.
 */
export default function LandingRoute() {
  return (
    <div className="catalogue landing">
      <h1 className="catalogue__heading">Аудіокниги українською</h1>

      <p className="landing__lead">
        vox-lib — бібліотека українських аудіокниг для тих, кому зручніше слухати, ніж читати.
      </p>

      <p className="about__example">
        <span className="about__example-mark">До прикладу:</span> для незрячих і слабозорих людей,
        для людей з дислексією, і для всіх, хто хоче слухати в дорозі. Користування бібліотекою
        безкоштовне. Обліковий запис потрібен лише щоб слухати: він запам'ятовує, де ви зупинилися,
        і переносить це між вашими пристроями.
      </p>

      <p className="landing__actions">
        <Link className="landing__cta" to="/books">
          Переглянути каталог
        </Link>
      </p>

      <p>
        <Link to="/about">Про проєкт</Link>
      </p>
    </div>
  )
}
