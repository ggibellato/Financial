import { Suspense, useEffect } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { FluentProvider } from '@fluentui/react-components'
import { setStoredDomain } from './utils/domainStorage'
import Sidebar from './components/Sidebar'
import Breadcrumb from './components/Breadcrumb'
import LoadingState from './components/LoadingState'
import SyncStatusBanner from './components/SyncStatusBanner'
import PaymentDueBanner from './components/PaymentDueBanner'
import { ColourModeProvider, useColourMode } from './context/ColourModeContext'
import { financialDarkTheme, financialLightTheme } from './theme/fluentTheme'
import './App.css'

function AppShell() {
  const location = useLocation()
  const { colourMode } = useColourMode()

  useEffect(() => {
    if (location.pathname.startsWith('/investments')) {
      setStoredDomain('investments')
    } else if (location.pathname.startsWith('/cashflow')) {
      setStoredDomain('cashflow')
    }
  }, [location.pathname])

  return (
    <FluentProvider theme={colourMode === 'dark' ? financialDarkTheme : financialLightTheme}>
      <div className="app">
        <Sidebar />
        <main className="app__content">
          <SyncStatusBanner />
          <PaymentDueBanner />
          <Breadcrumb />
          <Suspense fallback={<LoadingState />}>
            <Outlet />
          </Suspense>
        </main>
      </div>
    </FluentProvider>
  )
}

function App() {
  return (
    <ColourModeProvider>
      <AppShell />
    </ColourModeProvider>
  )
}

export default App
