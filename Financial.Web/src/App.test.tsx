import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Navigate, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it } from 'vitest'
import App from './App'

const AppWithRoutes = ({ initialEntry = '/investments' }: { initialEntry?: string }) => (
  <MemoryRouter initialEntries={[initialEntry]}>
    <Routes>
      <Route path="/" element={<App />}>
        <Route path="investments" element={<p>Investments domain content</p>} />
        <Route path="cashflow" element={<p>CashFlow domain content</p>} />
        <Route path="*" element={<Navigate to="/investments" replace />} />
      </Route>
    </Routes>
  </MemoryRouter>
)

describe('App', () => {
  afterEach(() => {
    sessionStorage.clear()
  })

  it('renders exactly two domain switcher options', () => {
    render(<AppWithRoutes />)

    const links = screen.getAllByRole('link')
    expect(links).toHaveLength(2)
    expect(screen.getByRole('link', { name: 'Investments' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'CashFlow' })).toBeInTheDocument()
  })

  it('switches to the cashflow domain content', () => {
    render(<AppWithRoutes />)

    fireEvent.click(screen.getByRole('link', { name: 'CashFlow' }))

    expect(screen.getByText('CashFlow domain content')).toBeInTheDocument()
    expect(screen.queryByText('Investments domain content')).not.toBeInTheDocument()
  })

  it('switches back to the investments domain content', () => {
    render(<AppWithRoutes initialEntry="/cashflow" />)

    fireEvent.click(screen.getByRole('link', { name: 'Investments' }))

    expect(screen.getByText('Investments domain content')).toBeInTheDocument()
    expect(screen.queryByText('CashFlow domain content')).not.toBeInTheDocument()
  })

  it('active domain link receives active class', () => {
    render(<AppWithRoutes initialEntry="/cashflow" />)

    expect(screen.getByRole('link', { name: 'CashFlow' })).toHaveClass('active')
    expect(screen.getByRole('link', { name: 'Investments' })).not.toHaveClass('active')
  })

  it('persists the active domain to sessionStorage on navigation', () => {
    render(<AppWithRoutes />)

    fireEvent.click(screen.getByRole('link', { name: 'CashFlow' }))

    expect(sessionStorage.getItem('financial.selectedDomain')).toBe('cashflow')
  })
})
