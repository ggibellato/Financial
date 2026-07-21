import { useEffect } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { setStoredDomain } from './utils/domainStorage'
import './App.css'

function App() {
  const location = useLocation()

  useEffect(() => {
    if (location.pathname.startsWith('/investments')) {
      setStoredDomain('investments')
    } else if (location.pathname.startsWith('/cashflow')) {
      setStoredDomain('cashflow')
    }
  }, [location.pathname])

  return (
    <div className="app">
      <nav className="app__domain-switcher" aria-label="Domain">
        <NavLink to="/investments">Investments</NavLink>
        <NavLink to="/cashflow">CashFlow</NavLink>
      </nav>
      <main className="app__content">
        <Outlet />
      </main>
    </div>
  )
}

export default App
