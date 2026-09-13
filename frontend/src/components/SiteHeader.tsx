import { Link, NavLink } from 'react-router'
import { HeaderSearch } from './HeaderSearch'
import { SessionMenu } from './SessionMenu'

/**
 * The banner, on every page, in the order a keyboard walks it.
 *
 * Source order is tab order here, and the order is the contract
 * (contracts/site-addresses.md): skip link, wordmark, navigation, search,
 * account. The account slot is last for the reason recorded in SessionMenu —
 * it is the only part that depends on who is looking, and nothing a visitor has
 * already passed may move when the answer arrives.
 *
 * **The skip link is first, and exists because of everything after it.** A
 * banner carrying navigation, search and account controls in front of the
 * content on every page is precisely the condition that makes a skip link worth
 * having: without one, reaching the first word of a book's page costs the same
 * six or seven stops on every page a visitor opens (FR-007).
 *
 * There is no disclosure control and no burger. Three destinations wrap onto a
 * second line at 320 device-independent pixels perfectly well, and a menu that
 * has to be opened before it can be read is a worse answer than a bar that is
 * one line taller (FR-006).
 */
export function SiteHeader() {
  return (
    <header className="site-header">
      <a className="skip-link" href="#main">
        Перейти до основного вмісту
      </a>

      {/*
        Readable text, not an image and not a background: the name has to be
        selectable, searchable and announced once, exactly as written (FR-002).
        The decorated ground repeats a mark dozens of times and is aria-hidden,
        so this is the only place the site says its own name out loud.
      */}
      <Link className="site-header__wordmark" to="/">
        vox-lib
      </Link>

      <nav className="site-nav" aria-label="Основна навігація">
        <ul>
          <li>
            <NavLink to="/books">Каталог</NavLink>
          </li>
          <li>
            <NavLink to="/authors">Автори</NavLink>
          </li>
          <li>
            <NavLink to="/about">Про проєкт</NavLink>
          </li>
        </ul>
      </nav>

      <HeaderSearch />

      <SessionMenu />
    </header>
  )
}
