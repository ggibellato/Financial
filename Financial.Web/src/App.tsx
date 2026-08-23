import { useEffect } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { FluentProvider } from '@fluentui/react-components'
import { setStoredDomain } from './utils/domainStorage'
import Sidebar from './components/Sidebar'
import Breadcrumb from './components/Breadcrumb'
import SyncStatusBanner from './components/SyncStatusBanner'
import { useSystemColorScheme } from './hooks/useSystemColorScheme'
import { financialDarkTheme, financialLightTheme } from './theme/fluentTheme'
import './App.css'

function App() {
  const location = useLocation()
  const colorScheme = useSystemColorScheme()

  useEffect(() => {
    if (location.pathname.startsWith('/investments')) {
      setStoredDomain('investments')
    } else if (location.pathname.startsWith('/cashflow')) {
      setStoredDomain('cashflow')
    }
  }, [location.pathname])

  return (
    <FluentProvider theme={colorScheme === 'dark' ? financialDarkTheme : financialLightTheme}>
      <div className="app">
        <Sidebar />
        <main className="app__content">
          <SyncStatusBanner />
          <Breadcrumb />
          <Outlet />
        </main>
      </div>
    </FluentProvider>
  )
}

export default App
