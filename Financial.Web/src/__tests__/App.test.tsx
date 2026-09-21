import { fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Navigate, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import App from '../App'
import { financialLightTheme } from '../theme/fluentTheme'
import { mockMatchMedia } from '../test/mockMatchMedia'

const AppWithRoutes = ({ initialEntry = '/investments/active-investments' }: { initialEntry?: string }) => (
  <MemoryRouter initialEntries={[initialEntry]}>
    <Routes>
      <Route path="/" element={<App />}>
        <Route path="investments/active-investments" element={<p>Investments domain content</p>} />
        <Route path="cashflow/monthly" element={<p>CashFlow domain content</p>} />
        <Route path="*" element={<Navigate to="/investments/active-investments" replace />} />
      </Route>
    </Routes>
  </MemoryRouter>
)

describe('App', () => {
  afterEach(() => {
    sessionStorage.clear()
    localStorage.clear()
  })

  it('renders the sidebar with all three categories', () => {
    render(<AppWithRoutes />)

    const sidebar = screen.getByRole('navigation', { name: 'Main' })
    expect(sidebar).toBeInTheDocument()
    expect(within(sidebar).getByText('Investments')).toBeInTheDocument()
    expect(within(sidebar).getAllByText('CashFlow').length).toBeGreaterThan(0)
    expect(within(sidebar).getByText('Admin')).toBeInTheDocument()
  })

  it('renders the breadcrumb above the routed content', () => {
    render(<AppWithRoutes />)

    expect(screen.getByRole('navigation', { name: 'Breadcrumb' })).toHaveTextContent('InvestmentsActive Investments')
  })

  it('switches to the cashflow domain content', () => {
    render(<AppWithRoutes />)

    fireEvent.click(screen.getByRole('link', { name: 'Monthly' }))

    expect(screen.getByText('CashFlow domain content')).toBeInTheDocument()
    expect(screen.queryByText('Investments domain content')).not.toBeInTheDocument()
  })

  it('switches back to the investments domain content', () => {
    render(<AppWithRoutes initialEntry="/cashflow/monthly" />)

    fireEvent.click(screen.getByRole('link', { name: 'Active Investments' }))

    expect(screen.getByText('Investments domain content')).toBeInTheDocument()
    expect(screen.queryByText('CashFlow domain content')).not.toBeInTheDocument()
  })

  it('active nav link receives active class', () => {
    render(<AppWithRoutes initialEntry="/cashflow/monthly" />)

    expect(screen.getByRole('link', { name: 'Monthly' })).toHaveClass('active')
    expect(screen.getByRole('link', { name: 'Active Investments' })).not.toHaveClass('active')
  })

  it('persists the active domain to sessionStorage on navigation', () => {
    render(<AppWithRoutes />)

    fireEvent.click(screen.getByRole('link', { name: 'Monthly' }))

    expect(sessionStorage.getItem('financial.selectedDomain')).toBe('cashflow')
  })

  it('renders_in_light_mode_by_default_regardless_of_os_preference', () => {
    render(<AppWithRoutes />)

    const provider = document.querySelector('.fui-FluentProvider') as HTMLElement
    const instanceClass = Array.from(provider.classList).find((c) => /^fui-FluentProvider_r_\d+_$/.test(c))
    const themeRule = Array.from(document.styleSheets)
      .flatMap((sheet) => Array.from(sheet.cssRules))
      .find((rule) => 'selectorText' in rule && (rule as CSSStyleRule).selectorText === `.${instanceClass}`) as
      | CSSStyleRule
      | undefined

    expect(themeRule).toBeDefined()
    expect(themeRule!.cssText).toContain(`--colorNeutralBackground1: ${financialLightTheme.colorNeutralBackground1};`)
  })

  it('renders_the_colour_mode_toggle_button_in_the_topbar', () => {
    render(<AppWithRoutes />)

    expect(screen.getByRole('button', { name: /switch to dark mode/i })).toBeInTheDocument()
  })

  describe('mobile navigation (below the phone breakpoint)', () => {
    const originalMatchMedia = window.matchMedia

    beforeEach(() => {
      mockMatchMedia(true)
    })

    afterEach(() => {
      window.matchMedia = originalMatchMedia
    })

    it('nav links are not in the DOM until the hamburger toggle opens the drawer', () => {
      render(<AppWithRoutes />)

      expect(screen.queryByRole('link', { name: 'Monthly' })).not.toBeInTheDocument()

      fireEvent.click(screen.getByRole('button', { name: 'Open navigation' }))

      expect(screen.getByRole('link', { name: 'Monthly' })).toBeInTheDocument()
    })

    it('clicking a nav link navigates and closes the drawer, returning focus to the toggle', () => {
      render(<AppWithRoutes />)

      const toggle = screen.getByRole('button', { name: 'Open navigation' })
      fireEvent.click(toggle)
      fireEvent.click(screen.getByRole('link', { name: 'Monthly' }))

      expect(screen.getByText('CashFlow domain content')).toBeInTheDocument()
      expect(screen.queryByRole('link', { name: 'Monthly' })).not.toBeInTheDocument()
      expect(document.activeElement).toBe(toggle)
    })
  })
})
