import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import InvestmentsLayout from '../InvestmentsLayout'

const InvestmentsLayoutWithRoutes = ({ initialEntry = '/investments/active-investments' }: { initialEntry?: string }) => (
  <MemoryRouter initialEntries={[initialEntry]}>
    <Routes>
      <Route path="/investments" element={<InvestmentsLayout />}>
        <Route path="active-investments" element={<p>Active Investments placeholder</p>} />
        <Route path="historic-investments" element={<h2>Historic Investments placeholder</h2>} />
        <Route path="dividend-check" element={<h2>Shares Dividend Check</h2>} />
        <Route path="current-values" element={<h2>Read Assets Current Values</h2>} />
      </Route>
    </Routes>
  </MemoryRouter>
)

describe('InvestmentsLayout', () => {
  it('renders the four existing investments tab links', () => {
    render(<InvestmentsLayoutWithRoutes />)

    expect(screen.getByRole('link', { name: 'Active Investments' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Historic Investments' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Shares Dividend Check' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Read Assets Current Values' })).toBeInTheDocument()
  })

  it('navigates to historic investments section', () => {
    render(<InvestmentsLayoutWithRoutes />)

    fireEvent.click(screen.getByRole('link', { name: 'Historic Investments' }))

    expect(screen.getByRole('heading', { name: 'Historic Investments placeholder' })).toBeInTheDocument()
    expect(screen.queryByText('Active Investments placeholder')).not.toBeInTheDocument()
  })

  it('active nav item receives active class', () => {
    render(<InvestmentsLayoutWithRoutes initialEntry="/investments/dividend-check" />)

    const activeLink = screen.getByRole('link', { name: 'Shares Dividend Check' })
    const activeInvestmentsLink = screen.getByRole('link', { name: 'Active Investments' })

    expect(activeLink).toHaveClass('active')
    expect(activeInvestmentsLink).not.toHaveClass('active')
  })
})
