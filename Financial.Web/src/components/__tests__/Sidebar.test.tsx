import { act, fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
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

  describe('collapsed-mode flyouts', () => {
    const collapse = () => fireEvent.click(screen.getByRole('button', { name: 'Collapse sidebar' }))

    beforeEach(() => {
      vi.useFakeTimers()
    })

    afterEach(() => {
      vi.useRealTimers()
    })

    it('collapsed sidebar opens a flyout listing exactly that category\'s children on hover', () => {
      renderSidebar()
      collapse()

      fireEvent.mouseEnter(screen.getByRole('button', { name: 'CashFlow' }))

      const flyoutList = screen.getByRole('list', { name: 'CashFlow' })
      const links = Array.from(flyoutList.querySelectorAll('a')).map((a) => a.textContent)
      expect(links).toEqual(NAV_TREE[1].children.map((c) => c.label))
    })

    it('clicking a flyout child navigates and closes the flyout', () => {
      renderSidebar()
      collapse()

      fireEvent.mouseEnter(screen.getByRole('button', { name: 'CashFlow' }))
      const flyoutList = screen.getByRole('list', { name: 'CashFlow' })
      fireEvent.click(within(flyoutList).getByRole('link', { name: 'Monthly' }))

      expect(screen.getByTestId('location')).toHaveTextContent('/cashflow/monthly')
      expect(screen.queryByRole('list', { name: 'CashFlow' })).not.toBeInTheDocument()
    })

    it('moving the pointer off both the icon and flyout closes it after ~250ms unless re-entered', () => {
      renderSidebar()
      collapse()

      const trigger = screen.getByRole('button', { name: 'CashFlow' })
      fireEvent.mouseEnter(trigger)
      fireEvent.mouseLeave(trigger)

      act(() => {
        vi.advanceTimersByTime(100)
      })
      const flyoutList = screen.getByRole('list', { name: 'CashFlow' })
      fireEvent.mouseEnter(flyoutList.closest('.sidebar-flyout')!)

      act(() => {
        vi.advanceTimersByTime(250)
      })
      expect(screen.getByRole('list', { name: 'CashFlow' })).toBeInTheDocument()

      fireEvent.mouseLeave(flyoutList.closest('.sidebar-flyout')!)
      act(() => {
        vi.advanceTimersByTime(250)
      })
      expect(screen.queryByRole('list', { name: 'CashFlow' })).not.toBeInTheDocument()
    })

    it('tab-focusing a category icon opens the identical flyout as hovering does', () => {
      renderSidebar()
      collapse()

      fireEvent.focus(screen.getByRole('button', { name: 'CashFlow' }))

      const flyoutList = screen.getByRole('list', { name: 'CashFlow' })
      const links = Array.from(flyoutList.querySelectorAll('a')).map((a) => a.textContent)
      expect(links).toEqual(NAV_TREE[1].children.map((c) => c.label))
    })

    it('pressing Escape while a flyout is open closes it and returns focus to the triggering icon', () => {
      renderSidebar()
      collapse()

      const trigger = screen.getByRole('button', { name: 'CashFlow' })
      fireEvent.focus(trigger)
      const flyoutRoot = screen.getByRole('list', { name: 'CashFlow' }).closest('.sidebar-flyout')!
      fireEvent.keyDown(flyoutRoot, { key: 'Escape' })

      expect(screen.queryByRole('list', { name: 'CashFlow' })).not.toBeInTheDocument()
      expect(document.activeElement).toBe(trigger)
    })

    it('blurring to an element outside the trigger and flyout closes immediately', () => {
      renderSidebar()
      collapse()

      const trigger = screen.getByRole('button', { name: 'CashFlow' })
      fireEvent.focus(trigger)
      expect(screen.getByRole('list', { name: 'CashFlow' })).toBeInTheDocument()

      fireEvent.blur(trigger, { relatedTarget: document.body })

      expect(screen.queryByRole('list', { name: 'CashFlow' })).not.toBeInTheDocument()
    })

    it('expanded sidebar shows no flyout on hover or focus', () => {
      renderSidebar()

      fireEvent.mouseEnter(screen.getByText('CashFlow'))

      expect(document.querySelector('.sidebar-flyout')).not.toBeInTheDocument()
    })

    it('toggle button still shows only its native tooltip, no flyout', () => {
      renderSidebar()
      collapse()

      const toggle = screen.getByRole('button', { name: 'Expand sidebar' })
      fireEvent.mouseEnter(toggle)

      expect(toggle).toHaveAttribute('title', 'Expand sidebar')
      expect(document.querySelector('.sidebar-flyout')).not.toBeInTheDocument()
    })
  })
})
