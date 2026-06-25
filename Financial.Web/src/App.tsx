import { NavLink, Outlet } from 'react-router-dom'
import './App.css'

function App() {
  return (
    <div className="app">
      <nav className="app__nav" aria-label="Primary">
        <NavLink to="/portfolio-navigator">Portfolio Navigator</NavLink>
        <NavLink to="/dividend-check">Shares Dividend Check</NavLink>
        <NavLink to="/current-values">Read Assets Current Values</NavLink>
      </nav>
      <main className="app__content">
        <Outlet />
      </main>
    </div>
  )
}

export default App
