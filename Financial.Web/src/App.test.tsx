import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Navigate, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import App from './App'

vi.mock('./api/financialApiClient', () => ({
  createFinancialApiClient: () => ({}),
}))

const AppWithRoutes = ({ initialEntry = '/' }: { initialEntry?: string }) => (
  <MemoryRouter initialEntries={[initialEntry]}>
    <Routes>
      <Route path="/" element={<App />}>
        <Route index element={<Navigate to="/active-investments" replace />} />
        <Route path="active-investments" element={<p>Active Investments placeholder</p>} />
        <Route path="historic-investments" element={<h2>Historic Investments placeholder</h2>} />
        <Route path="dividend-check" element={<h2>Shares Dividend Check</h2>} />
        <Route path="current-values" element={<h2>Read Assets Current Values</h2>} />
        <Route path="*" element={<p>Page not found.</p>} />
      </Route>
    </Routes>
  </MemoryRouter>
)

describe('App', () => {
  it('renders four nav items', () => {
    render(<AppWithRoutes />)

    expect(screen.getByRole('link', { name: 'Active Investments' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Historic Investments' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Shares Dividend Check' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Read Assets Current Values' })).toBeInTheDocument()
  })

  it('navigates to historic investments section', () => {
    render(<AppWithRoutes />)

    fireEvent.click(screen.getByRole('link', { name: 'Historic Investments' }))

    expect(screen.getByRole('heading', { name: 'Historic Investments placeholder' })).toBeInTheDocument()
    expect(screen.queryByText('Active Investments placeholder')).not.toBeInTheDocument()
  })

  it('default route redirects to active investments', () => {
    render(<AppWithRoutes initialEntry="/" />)

    expect(screen.getByText('Active Investments placeholder')).toBeInTheDocument()
  })

  it('navigates to dividend check section', async () => {
    render(<AppWithRoutes />)

    fireEvent.click(screen.getByRole('link', { name: 'Shares Dividend Check' }))

    expect(screen.getByRole('heading', { name: 'Shares Dividend Check' })).toBeInTheDocument()
    expect(screen.queryByText('Active Investments placeholder')).not.toBeInTheDocument()
  })

  it('navigates to current values section', () => {
    render(<AppWithRoutes />)

    fireEvent.click(screen.getByRole('link', { name: 'Read Assets Current Values' }))

    expect(screen.getByRole('heading', { name: 'Read Assets Current Values' })).toBeInTheDocument()
  })

  it('active nav item receives active class', () => {
    render(<AppWithRoutes initialEntry="/dividend-check" />)

    const activeLink = screen.getByRole('link', { name: 'Shares Dividend Check' })
    const activeInvestmentsLink = screen.getByRole('link', { name: 'Active Investments' })
    const currentValuesLink = screen.getByRole('link', { name: 'Read Assets Current Values' })

    expect(activeLink).toHaveClass('active')
    expect(activeInvestmentsLink).not.toHaveClass('active')
    expect(currentValuesLink).not.toHaveClass('active')
  })

  it('legacy broker route returns 404', () => {
    render(<AppWithRoutes initialEntry="/brokers" />)

    expect(screen.getByText('Page not found.')).toBeInTheDocument()
  })

  it('legacy navigation route returns 404', () => {
    render(<AppWithRoutes initialEntry="/navigation" />)

    expect(screen.getByText('Page not found.')).toBeInTheDocument()
  })
})
