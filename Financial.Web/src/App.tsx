import { NavLink, Outlet } from 'react-router-dom'
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
        </nav>
      </header>
      <main className="app__content">
        <Outlet />
      </main>
    </div>
  )
}

export default App
