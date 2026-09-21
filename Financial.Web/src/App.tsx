import { Suspense, useEffect, useRef, useState } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { Button, FluentProvider, makeStyles } from '@fluentui/react-components'
import { setStoredDomain } from './utils/domainStorage'
import Sidebar from './components/Sidebar'
import Breadcrumb from './components/Breadcrumb'
import LoadingState from './components/LoadingState'
import SyncStatusBanner from './components/SyncStatusBanner'
import PaymentDueBanner from './components/PaymentDueBanner'
import ColourModeToggleButton from './components/ColourModeToggleButton'
import { ColourModeProvider, useColourMode } from './context/ColourModeContext'
import { financialDarkTheme, financialLightTheme } from './theme/fluentTheme'
import './App.css'

function HamburgerIcon() {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <line x1="3" y1="6" x2="21" y2="6" />
      <line x1="3" y1="12" x2="21" y2="12" />
      <line x1="3" y1="18" x2="21" y2="18" />
    </svg>
  )
}

const useAppShellStyles = makeStyles({
  mobileNavToggle: {
    display: 'none',
    '@media (max-width: 599px)': {
      display: 'inline-flex',
    },
  },
})

function AppShell() {
  const styles = useAppShellStyles()
  const location = useLocation()
  const { colourMode } = useColourMode()
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const mobileNavToggleRef = useRef<HTMLButtonElement>(null)
  const wasMobileNavOpenRef = useRef(false)

  useEffect(() => {
    if (location.pathname.startsWith('/investments')) {
      setStoredDomain('investments')
    } else if (location.pathname.startsWith('/cashflow')) {
      setStoredDomain('cashflow')
    }
  }, [location.pathname])

  // https://react.dev/learn/you-might-not-need-an-effect#adjusting-some-state-when-a-prop-changes
  const [previousPathname, setPreviousPathname] = useState(location.pathname)
  if (previousPathname !== location.pathname) {
    setPreviousPathname(location.pathname)
    if (mobileNavOpen) setMobileNavOpen(false)
  }

  // Navigating unmounts the just-clicked NavLink in the same pass, dropping focus to <body>.
  useEffect(() => {
    if (wasMobileNavOpenRef.current && !mobileNavOpen) {
      mobileNavToggleRef.current?.focus()
    }
    wasMobileNavOpenRef.current = mobileNavOpen
  }, [mobileNavOpen])

  return (
    <FluentProvider theme={colourMode === 'dark' ? financialDarkTheme : financialLightTheme}>
      <PaymentDueBanner />
      <div className="app">
        <Sidebar mobileOpen={mobileNavOpen} onMobileOpenChange={setMobileNavOpen} />
        <main className="app__content">
          <SyncStatusBanner />
          <div className="app__topbar">
            <Button
              ref={mobileNavToggleRef}
              className={styles.mobileNavToggle}
              appearance="subtle"
              icon={<HamburgerIcon />}
              aria-label="Open navigation"
              onClick={() => setMobileNavOpen(true)}
            />
            <Breadcrumb />
            <ColourModeToggleButton />
          </div>
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
