import { NavLink, Outlet } from 'react-router-dom'
import NavigationTreePanel from './components/NavigationTreePanel'
import './App.css'

function App() {
  return (
    <div className="app">
      <header className="app__header">
        <div>
          <p className="app__eyebrow">Financial</p>
          <h1>Portfolio Dashboard</h1>
          <p className="app__subtitle">Track brokers, portfolios, and assets in one place.</p>
        </div>
        <nav className="app__nav" aria-label="Primary">
          <NavLink to="/brokers">Brokers</NavLink>
          <NavLink to="/navigation">Navigation</NavLink>
        </nav>
      </header>
      <div className="app__shell">
        <aside className="app__sidebar" aria-label="Navigation tree">
          <NavigationTreePanel title="Navigation" />
        </aside>
        <main className="app__detail">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

export default App
