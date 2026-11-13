import { fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import SidebarFlyout from '../SidebarFlyout'
import type { NavCategory } from '../../navigation/navTree'

const category: NavCategory = {
  id: 'cashflow',
  label: 'CashFlow',
  children: [
    { id: 'accounts', label: 'Accounts', route: '/cashflow/accounts' },
    { id: 'mensais', label: 'Mensais', route: '/cashflow/mensais' },
  ],
}

const groupedCategory: NavCategory = {
  id: 'admin',
  label: 'Admin',
  children: [],
  groups: [
    {
      id: 'investment',
      label: 'Investment',
      children: [{ id: 'brokers', label: 'Brokers', route: '/admin/investment/brokers' }],
    },
    {
      id: 'cashflow',
      label: 'CashFlow',
      children: [{ id: 'categories', label: 'Categories', route: '/admin/cashflow/categories' }],
    },
  ],
}

const anchorRect = { top: 10, right: 56, bottom: 40, left: 0, width: 56, height: 30 } as DOMRect

const renderFlyout = (shown: NavCategory, onNavigate = vi.fn()) => {
  render(
    <MemoryRouter>
      <SidebarFlyout id="flyout" category={shown} anchorRect={anchorRect} onNavigate={onNavigate} />
    </MemoryRouter>,
  )
  return onNavigate
}

describe('SidebarFlyout', () => {
  it('sits beside its rail button', () => {
    renderFlyout(category)

    const panel = document.getElementById('flyout')!
    expect(panel).toHaveStyle({ top: '10px', left: '56px' })
  })

  it('renders the category label as a non-clickable title', () => {
    renderFlyout(category)

    const title = screen.getByText('CashFlow')
    expect(title.tagName).toBe('SPAN')
  })

  it('renders all pages as links in category order', () => {
    renderFlyout(category)

    expect(screen.getAllByRole('link').map((link) => link.textContent)).toEqual(['Accounts', 'Mensais'])
  })

  it('clicking a page calls onNavigate', () => {
    const onNavigate = renderFlyout(category)

    fireEvent.click(screen.getByRole('link', { name: 'Mensais' }))

    expect(onNavigate).toHaveBeenCalledTimes(1)
  })

  it('renders each group as a heading over its own list of pages', () => {
    const onNavigate = renderFlyout(groupedCategory)

    expect(within(screen.getByRole('list', { name: 'Investment' })).getByRole('link', { name: 'Brokers' })).toBeInTheDocument()
    expect(within(screen.getByRole('list', { name: 'CashFlow' })).getByRole('link', { name: 'Categories' })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('link', { name: 'Brokers' }))
    expect(onNavigate).toHaveBeenCalledTimes(1)
  })
})
