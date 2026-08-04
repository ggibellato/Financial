import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import SidebarFlyout from '../SidebarFlyout'
import type { NavCategory } from '../../navigation/navTree'

const category: NavCategory = {
  id: 'cashflow',
  label: 'CashFlow',
  children: [
    { id: 'monthly', label: 'Monthly', route: '/cashflow/monthly' },
    { id: 'reserva', label: 'Reserva', route: '/cashflow/reserva' },
    { id: 'mensais', label: 'Mensais', route: '/cashflow/mensais' },
  ],
}

const anchorRect = { top: 10, right: 56, bottom: 40, left: 0, width: 56, height: 30 } as DOMRect

const renderFlyout = (onClose = vi.fn()) => {
  render(
    <MemoryRouter>
      <SidebarFlyout
        category={category}
        anchorRect={anchorRect}
        onClose={onClose}
        onMouseEnter={vi.fn()}
        onMouseLeave={vi.fn()}
        onBlur={vi.fn()}
      />
    </MemoryRouter>,
  )
  return onClose
}

describe('SidebarFlyout', () => {
  it('renders the category label as a non-clickable title', () => {
    renderFlyout()

    const title = screen.getByText('CashFlow')
    expect(title).toBeInTheDocument()
    expect(title.tagName).not.toBe('A')
    expect(title.tagName).not.toBe('BUTTON')
  })

  it('renders all children as links in category order', () => {
    renderFlyout()

    const links = screen.getAllByRole('link')
    expect(links.map((l) => l.textContent)).toEqual(['Monthly', 'Reserva', 'Mensais'])
  })

  it('clicking a child link calls onClose', () => {
    const onClose = renderFlyout()

    fireEvent.click(screen.getByRole('link', { name: 'Monthly' }))

    expect(onClose).toHaveBeenCalledWith()
  })

  it('pressing Escape calls onClose with refocus requested', () => {
    const onClose = renderFlyout()

    fireEvent.keyDown(screen.getByText('CashFlow').closest('.sidebar-flyout')!, { key: 'Escape' })

    expect(onClose).toHaveBeenCalledWith(true)
  })
})
