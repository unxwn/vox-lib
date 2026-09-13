import { Link } from 'react-router'
import { aboutMeta } from '../catalogue/meta'
import '../styles/catalogue.css'

export function meta() {
  return aboutMeta()
}

/**
 * What the project is, at more length than the front door gives it. Linked from
 * the banner and from the footer, so it is reachable from every page.
 *
 * **Every paragraph here is marked as an example, and that marking is the
 * point.** The copy was invented — who runs the library, that it is staffed by
 * volunteers, what it will cost — and none of that is mine to assert. Prefixed
 * with "До прикладу:" it shows the shape and length each section wants without
 * claiming to be true, so whoever knows the answers replaces the sentence and
 * deletes the prefix.
 */
export default function AboutRoute() {
  return (
    <div className="catalogue about">
      <h1 className="catalogue__heading">Про проєкт</h1>

      <section className="about__section">
        <h2>Що це</h2>
        <p className="about__example">
          <span className="about__example-mark">До прикладу:</span> vox-lib — бібліотека українських
          аудіокниг. Ми збираємо записи, які можна слухати вільно, і робимо їх доступними з
          будь-якого пристрою.
        </p>
      </section>

      <section className="about__section">
        <h2>Для кого це</h2>
        <p className="about__example">
          <span className="about__example-mark">До прикладу:</span> насамперед для людей, яким
          друкований текст недоступний або незручний: незрячих і слабозорих, людей з дислексією,
          людей, яким важко тримати книжку. Сайт зроблено так, щоб ним було зручно користуватися з
          клавіатури та з програмою екранного доступу, і це вимога до кожної сторінки, а не окремий
          режим.
        </p>
      </section>

      <section className="about__section">
        <h2>Хто це веде</h2>
        <p className="about__example">
          <span className="about__example-mark">До прикладу:</span> спільнота волонтерів. Бібліотека
          безкоштовна, реклами немає, і плати за доступ не буде.
        </p>
      </section>

      <section className="about__section">
        <h2>Що далі</h2>
        <p className="about__example">
          <span className="about__example-mark">До прикладу:</span> зараз доступний каталог: назви,
          автори, описи й тривалість записів. Прослуховування, синхронізація місця зупинки між
          пристроями та пошук за жанрами з'являться згодом.
        </p>
      </section>

      <p>
        <Link to="/books">Переглянути каталог аудіокниг</Link>
      </p>
    </div>
  )
}
