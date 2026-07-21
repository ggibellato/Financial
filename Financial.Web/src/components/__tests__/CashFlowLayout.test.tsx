import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import CashFlowLayout from '../CashFlowLayout'

const CashFlowLayoutWithRoutes = ({ initialEntry = '/cashflow/monthly' }: { initialEntry?: string }) => (
  <MemoryRouter initialEntries={[initialEntry]}>
    <Routes>
      <Route path="/cashflow" element={<CashFlowLayout />}>
        <Route path="monthly" element={<p>Monthly placeholder</p>} />
        <Route path="reserva" element={<p>Reserva placeholder</p>} />
        <Route path="mensais" element={<p>Mensais placeholder</p>} />
        <Route path="controle-mae" element={<p>Controle Mae placeholder</p>} />
        <Route path="investment-snapshots" element={<p>Investment Snapshots placeholder</p>} />
        <Route path="yearly-summary" element={<p>Yearly Summary placeholder</p>} />
      </Route>
    </Routes>
  </MemoryRouter>
)

describe('CashFlowLayout', () => {
  it('renders exactly the six cashflow tab links in order', () => {
    render(<CashFlowLayoutWithRoutes />)

    const links = screen.getAllByRole('link')
    expect(links).toHaveLength(6)
    expect(links.map((link) => link.textContent)).toEqual([
      'Monthly',
      'Reserva',
      'Mensais',
      'Controle Mae',
      'Investment Snapshots',
      'Yearly Summary',
    ])
  })

  it('navigates to the reserva section', () => {
    render(<CashFlowLayoutWithRoutes />)

    fireEvent.click(screen.getByRole('link', { name: 'Reserva' }))

    expect(screen.getByText('Reserva placeholder')).toBeInTheDocument()
    expect(screen.queryByText('Monthly placeholder')).not.toBeInTheDocument()
  })

  it('active nav item receives active class', () => {
    render(<CashFlowLayoutWithRoutes initialEntry="/cashflow/mensais" />)

    const activeLink = screen.getByRole('link', { name: 'Mensais' })
    const monthlyLink = screen.getByRole('link', { name: 'Monthly' })

    expect(activeLink).toHaveClass('active')
    expect(monthlyLink).not.toHaveClass('active')
  })
})
