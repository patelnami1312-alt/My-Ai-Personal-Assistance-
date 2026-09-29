import { NavLink, Outlet } from 'react-router-dom'

const links = [
  { to: '/', label: 'Dashboard', ico: '⌂', end: true },
  { to: '/chat', label: 'Chat', ico: '◎' },
  { to: '/transactions', label: 'Transactions', ico: '$' },
  { to: '/accounts', label: 'Accounts', ico: '🏦' },
  { to: '/budgets', label: 'Budgets', ico: '📊' },
  { to: '/bills', label: 'Bills', ico: '🧾' },
]

// Sibling agents are separate standalone apps (own backend/frontend/DB/port) — link out, don't route.
const siblingApps = [
  { url: 'http://localhost:5173', label: 'Personal Assistant', ico: '✨' },
  { url: 'http://localhost:5174', label: 'Coding Agent', ico: '⌥' },
  { url: 'http://localhost:5175', label: 'Fitness Agent', ico: '🏋' },
  { url: 'http://localhost:5177', label: 'Home Agent', ico: '🏠' },
  { url: 'http://localhost:5178', label: 'Shopping Agent', ico: '🛒' },
  { url: 'http://localhost:5179', label: 'Learning Agent', ico: '🎓' },
  { url: 'http://localhost:5180', label: 'Travel Agent', ico: '✈' },
]

export function Layout() {
  return (
    <div className="app">
      <aside className="sidebar">
        <div className="brand">
          <span className="dot">{'💰'}</span>
          <span>Finance Agent</span>
        </div>
        {links.map((l) => (
          <NavLink
            key={l.to}
            to={l.to}
            end={l.end}
            className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
          >
            <span className="ico">{l.ico}</span>
            {l.label}
          </NavLink>
        ))}
        {siblingApps.map((a) => (
          <a key={a.url} href={a.url} target="_blank" rel="noreferrer" className="nav-link">
            <span className="ico">{a.ico}</span>
            {a.label}
            <span className="faint" style={{ marginLeft: 'auto' }}>↗</span>
          </a>
        ))}
        <div className="spacer" />
        <div className="foot">standalone app &bull; :5176</div>
      </aside>
      <main className="main">
        <Outlet />
      </main>
    </div>
  )
}
