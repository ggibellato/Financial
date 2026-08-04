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

        expect(screen.getByText(`${category.label} › ${child.label}`)).toBeInTheDocument()

        unmount()
      }
    }
  })

  it('renders an em dash for an unmatched route', () => {
    renderBreadcrumb('/unknown')

    expect(screen.getByText('—')).toBeInTheDocument()
  })

  it('breadcrumb text is not a link and has no interactive role', () => {
    renderBreadcrumb('/cashflow/monthly')

    expect(screen.queryByRole('link')).not.toBeInTheDocument()
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('renders regardless of sidebar collapsed state', () => {
    localStorage.setItem('financial.sidebarCollapsed', 'true')

    renderBreadcrumb('/cashflow/monthly')

    expect(screen.getByText('CashFlow › Monthly')).toBeInTheDocument()
  })
})
