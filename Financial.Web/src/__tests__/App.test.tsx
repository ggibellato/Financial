import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Navigate, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it } from 'vitest'
import App from '../App'

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

    expect(screen.getByRole('navigation', { name: 'Main' })).toBeInTheDocument()
    expect(screen.getByText('Investments')).toBeInTheDocument()
    expect(screen.getAllByText('CashFlow').length).toBeGreaterThan(0)
    expect(screen.getByText('Admin')).toBeInTheDocument()
  })

  it('renders the breadcrumb above the routed content', () => {
    render(<AppWithRoutes />)

    expect(screen.getByText('Investments › Active Investments')).toBeInTheDocument()
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
})
