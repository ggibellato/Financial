import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { afterEach, describe, expect, it } from 'vitest'
import Sidebar from '../Sidebar'
import { NAV_TREE } from '../../navigation/navTree'

function LocationDisplay() {
  const location = useLocation()
  return <div data-testid="location">{location.pathname}</div>
}

const renderSidebar = (initialEntry = '/investments/active-investments') =>
  render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <Routes>
        <Route
          path="*"
          element={
            <>
              <Sidebar />
              <LocationDisplay />
            </>
          }
        />
      </Routes>
    </MemoryRouter>,
  )

describe('Sidebar', () => {
  afterEach(() => {
    localStorage.clear()
  })

  it('renders Expanded by default with no stored preference', () => {
    renderSidebar()

    expect(screen.getByText('Investments')).toBeInTheDocument()
    expect(screen.getByText('CashFlow')).toBeInTheDocument()
    for (const category of NAV_TREE) {
      for (const child of category.children) {
        expect(screen.getByRole('link', { name: child.label })).toBeInTheDocument()
      }
    }
    expect(screen.getByRole('navigation', { name: 'Main' })).not.toHaveClass('sidebar--collapsed')
  })

  it('toggling collapses and expands the sidebar', () => {
    renderSidebar()

    fireEvent.click(screen.getByRole('button', { name: 'Collapse sidebar' }))

    expect(screen.getByRole('navigation', { name: 'Main' })).toHaveClass('sidebar--collapsed')
    expect(screen.queryByRole('link', { name: 'Active Investments' })).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Expand sidebar' }))

    expect(screen.getByRole('navigation', { name: 'Main' })).not.toHaveClass('sidebar--collapsed')
    expect(screen.getByRole('link', { name: 'Active Investments' })).toBeInTheDocument()
  })

  it('persists collapsed state to localStorage on toggle', () => {
    renderSidebar()

    fireEvent.click(screen.getByRole('button', { name: 'Collapse sidebar' }))

    expect(localStorage.getItem('financial.sidebarCollapsed')).toBe('true')
  })

  it('renders already Collapsed on mount when localStorage has a stored true value', () => {
    localStorage.setItem('financial.sidebarCollapsed', 'true')

    renderSidebar()

    expect(screen.getByRole('navigation', { name: 'Main' })).toHaveClass('sidebar--collapsed')
    expect(screen.queryByRole('link', { name: 'Active Investments' })).not.toBeInTheDocument()
  })

  it('highlights only the nav item matching the current route', () => {
    renderSidebar('/cashflow/monthly')

    expect(screen.getByRole('link', { name: 'Monthly' })).toHaveClass('active')
    expect(screen.getByRole('link', { name: 'Active Investments' })).not.toHaveClass('active')
    expect(screen.getByRole('link', { name: 'Reserva' })).not.toHaveClass('active')
  })

  it('category headers do not navigate', () => {
    renderSidebar()

    fireEvent.click(screen.getByText('Investments'))
    fireEvent.click(screen.getByText('CashFlow'))

    expect(screen.getByTestId('location')).toHaveTextContent('/investments/active-investments')
  })

  it('all ten children navigate to their routes', () => {
    renderSidebar()

    for (const category of NAV_TREE) {
      for (const child of category.children) {
        fireEvent.click(screen.getByRole('link', { name: child.label }))
        expect(screen.getByTestId('location')).toHaveTextContent(child.route)
      }
    }
  })
})
