import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it } from 'vitest'
import DashboardPage from '../DashboardPage'
import Sidebar from '../../components/Sidebar'
import { NAV_TREE } from '../../navigation/navTree'

const renderDashboardRoute = () =>
  render(
    <MemoryRouter initialEntries={['/investments/dashboard']}>
      <Sidebar />
      <Routes>
        <Route path="/investments/dashboard" element={<DashboardPage />} />
      </Routes>
    </MemoryRouter>,
  )

describe('DashboardPage', () => {
  afterEach(() => {
    localStorage.clear()
  })

  it('renders_the_dashboard_route_with_its_four_panels', () => {
    renderDashboardRoute()

    expect(screen.getByRole('heading', { name: 'Dashboard', level: 2 })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Portfolio Summary' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Allocation Breakdown' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Data-Quality Warnings' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Upcoming Income' })).toBeInTheDocument()
  })

  it('is_reachable_from_the_sidebar_as_the_first_investments_entry', () => {
    renderDashboardRoute()

    const investments = NAV_TREE.find((category) => category.id === 'investments')!
    expect(investments.children[0]).toEqual({
      id: 'dashboard',
      label: 'Dashboard',
      route: '/investments/dashboard',
    })
    expect(screen.getByRole('link', { name: 'Dashboard' })).toHaveAttribute('href', '/investments/dashboard')
  })
})
