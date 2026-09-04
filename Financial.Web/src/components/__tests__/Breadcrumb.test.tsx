import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it } from 'vitest'
import Breadcrumb from '../Breadcrumb'
import { NAV_TREE } from '../../navigation/navTree'

const renderBreadcrumb = (initialEntry: string) =>
  render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <Breadcrumb />
    </MemoryRouter>,
  )

describe('Breadcrumb', () => {
  afterEach(() => {
    localStorage.clear()
  })

  it('renders "Category › Child" for every one of the ten leaf routes', () => {
    for (const category of NAV_TREE) {
      for (const child of category.children) {
        const { unmount } = renderBreadcrumb(child.route)

        expect(screen.getByRole('navigation', { name: 'Breadcrumb' })).toHaveTextContent(
          `${category.label}${child.label}`,
        )
        expect(screen.getByText(child.label)).toHaveAttribute('aria-current', 'page')

        unmount()
      }
    }
  })

  it('renders an em dash for an unmatched route', () => {
    renderBreadcrumb('/unknown')

    expect(screen.getByText('—')).toBeInTheDocument()
    expect(screen.getByText('—')).toHaveAttribute('aria-current', 'page')
  })

  it('breadcrumb text is not a link and has no interactive role', () => {
    renderBreadcrumb('/cashflow/monthly')

    expect(screen.queryByRole('link')).not.toBeInTheDocument()
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('renders regardless of sidebar collapsed state', () => {
    localStorage.setItem('financial.sidebarCollapsed', 'true')

    renderBreadcrumb('/cashflow/monthly')

    expect(screen.getByRole('navigation', { name: 'Breadcrumb' })).toHaveTextContent('CashFlowMonthly')
  })
})
