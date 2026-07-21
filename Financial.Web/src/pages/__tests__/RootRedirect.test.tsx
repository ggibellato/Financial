import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it } from 'vitest'
import RootRedirect from '../RootRedirect'
import { setStoredDomain } from '../../utils/domainStorage'

const RootRedirectWithRoutes = () => (
  <MemoryRouter initialEntries={['/']}>
    <Routes>
      <Route path="/" element={<RootRedirect />} />
      <Route path="/investments/active-investments" element={<p>Investments landing</p>} />
      <Route path="/cashflow/monthly" element={<p>CashFlow landing</p>} />
    </Routes>
  </MemoryRouter>
)

describe('RootRedirect', () => {
  afterEach(() => {
    sessionStorage.clear()
  })

  it('redirects to investments when no domain is stored', () => {
    render(<RootRedirectWithRoutes />)

    expect(screen.getByText('Investments landing')).toBeInTheDocument()
  })

  it('redirects to cashflow when cashflow was last selected', () => {
    setStoredDomain('cashflow')

    render(<RootRedirectWithRoutes />)

    expect(screen.getByText('CashFlow landing')).toBeInTheDocument()
  })

  it('redirects to investments when investments was last selected', () => {
    setStoredDomain('investments')

    render(<RootRedirectWithRoutes />)

    expect(screen.getByText('Investments landing')).toBeInTheDocument()
  })
})
