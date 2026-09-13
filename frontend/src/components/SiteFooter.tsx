import { Link } from 'react-router'

/**
 * The contentinfo landmark, on every page.
 *
 * It carries the one link nothing else on the site does: the licence the
 * typeface ships under. Fixel is distributed under the SIL Open Font License,
 * the licence file is served at /fonts/OFL.txt, and until now it was reachable
 * from nowhere at all — which is a licence obligation met by accident of the
 * file existing rather than on purpose (FR-003).
 */
export function SiteFooter() {
  return (
    <footer className="site-footer">
      {/*
        One sentence, and only what is true. What was here claimed who runs the
        library and what it costs, neither of which is mine to assert.
      */}
      <p className="site-footer__about">vox-lib — бібліотека українських аудіокниг.</p>
      <nav className="site-footer__links" aria-label="Додаткові посилання">
        <ul>
          <li>
            <Link to="/about">Про проєкт</Link>
          </li>
          <li>
            {/*
              A plain anchor, not a Link: the licence is a static file the build
              copies, not a route, and asking the router to navigate to it would
              land on the not-found page.
            */}
            <a href="/fonts/OFL.txt">Ліцензія шрифту Fixel (SIL OFL)</a>
          </li>
        </ul>
      </nav>
    </footer>
  )
}
