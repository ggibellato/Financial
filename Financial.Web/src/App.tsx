import { useEffect } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { setStoredDomain } from './utils/domainStorage'
import Sidebar from './components/Sidebar'
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
      <Sidebar />
      <main className="app__content">
        <Outlet />
      </main>
    </div>
  )
}

export default App
