import { act, fireEvent, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import Sidebar from '../Sidebar'
import { NAV_TREE, type NavCategory } from '../../navigation/navTree'
import { render } from '../../test/renderWithFluent'
import { mockMatchMedia } from '../../test/mockMatchMedia'

function LocationDisplay() {
  const location = useLocation()
  return <div data-testid="location">{location.pathname}</div>
}

const renderSidebar = (
  initialEntry = '/investments/active-investments',
  sidebarProps: { mobileOpen?: boolean; onMobileOpenChange?: (open: boolean) => void } = {},
) =>
  render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <Routes>
        <Route
          path="*"
          element={
            <>
              <Sidebar
                mobileOpen={sidebarProps.mobileOpen ?? false}
                onMobileOpenChange={sidebarProps.onMobileOpenChange ?? (() => {})}
              />
              <LocationDisplay />
              <button type="button">Outside</button>
            </>
          }
        />
      </Routes>
    </MemoryRouter>,
  )

const mainNav = () => screen.getByRole('navigation', { name: 'Main' })
const railButton = (label: string) => within(mainNav()).getByRole('button', { name: label })
const flyout = () => document.querySelector('.sidebar-flyout') as HTMLElement | null
const pagesOf = (category: NavCategory) => [...category.children, ...(category.groups ?? []).flatMap((group) => group.children)]
const adminCategory = NAV_TREE.find((category) => category.id === 'admin')!

describe('Sidebar rail', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
    localStorage.clear()
  })

  it('shows one rail button per category and no page links until a category is opened', () => {
    renderSidebar()

    expect(
      within(mainNav())
        .getAllByRole('button')
        .filter((button) => button.getAttribute('aria-expanded') !== null)
        .map((button) => button.textContent),
    ).toEqual(NAV_TREE.map((category) => category.label))
    expect(within(mainNav()).queryAllByRole('link')).toHaveLength(0)
    expect(mainNav()).not.toHaveClass('sidebar--collapsed')
  })

  it('hovering a category opens its flyout with that category\'s pages', () => {
    renderSidebar()

    fireEvent.mouseEnter(railButton('CashFlow'))

    const links = within(flyout()!).getAllByRole('link').map((link) => link.textContent)
    expect(links).toEqual(NAV_TREE[1].children.map((child) => child.label))
    expect(railButton('CashFlow')).toHaveAttribute('aria-expanded', 'true')
    expect(railButton('CashFlow')).toHaveAttribute('aria-controls', flyout()!.id)
  })

  it('shows the Admin pages under plain group headings, with nothing to expand', () => {
    renderSidebar()

    fireEvent.click(railButton('Admin'))

    const panel = flyout()!
    for (const group of adminCategory.groups!) {
      const list = within(panel).getByRole('list', { name: group.label })
      expect(within(list).getAllByRole('link').map((link) => link.textContent)).toEqual(group.children.map((child) => child.label))
    }
    expect(within(panel).queryAllByRole('button')).toHaveLength(0)
  })

  it('[AC P60-F13-consolidated-navigation-and-retirement-01] shows the F13 CashFlow and Admin CashFlow menus', () => {
    renderSidebar()

    fireEvent.click(railButton('CashFlow'))
    expect(within(flyout()!).getAllByRole('link').map((link) => link.textContent)).toEqual([
      'Accounts',
      'Annual summary',
      'Mensais',
      'Investment snapshots',
    ])

    fireEvent.click(railButton('Admin'))
    const adminCashFlow = within(flyout()!).getByRole('list', { name: 'CashFlow' })
    expect(within(adminCashFlow).getAllByRole('link').map((link) => link.textContent)).toEqual([
      'Accounts',
      'Categories',
      'Income sources',
      'Investment accounts',
      'Recurring bills',
    ])
  })

  it('[AC P61-F01-accounts-overview-tabs-and-monthly-fold-06] lists the CashFlow pages without Monthly', () => {
    renderSidebar()

    fireEvent.click(railButton('CashFlow'))

    expect(within(flyout()!).getByRole('link', { name: 'Accounts' })).toHaveAttribute('href', '/cashflow/accounts')
    expect(within(flyout()!).queryByRole('link', { name: 'Monthly' })).not.toBeInTheDocument()
    expect(document.querySelector('a[href^="/cashflow/monthly"]')).toBeNull()
  })

  it('every page in every category navigates to its route and closes the flyout', () => {
    renderSidebar()

    for (const category of NAV_TREE) {
      for (const page of pagesOf(category)) {
        fireEvent.click(railButton(category.label))
        const link = within(flyout()!)
          .getAllByRole('link')
          .find((candidate) => candidate.getAttribute('href') === page.route)!
        fireEvent.click(link)
        expect(screen.getByTestId('location')).toHaveTextContent(page.route)
        expect(flyout()).toBeNull()
      }
    }
  })

  it('rail buttons open their flyout but do not navigate', () => {
    renderSidebar()

    for (const category of NAV_TREE) {
      fireEvent.click(railButton(category.label))
    }

    expect(screen.getByTestId('location')).toHaveTextContent('/investments/active-investments')
  })

  it('marks the category of the current page, and the page link inside its flyout', () => {
    renderSidebar('/cashflow/mensais')

    expect(railButton('CashFlow')).toHaveClass('sidebar__category-header--active')
    expect(railButton('Investments')).not.toHaveClass('sidebar__category-header--active')

    fireEvent.click(railButton('CashFlow'))
    expect(within(flyout()!).getByRole('link', { name: 'Mensais' })).toHaveAttribute('aria-current', 'page')
    expect(within(flyout()!).getByRole('link', { name: 'Accounts' })).not.toHaveAttribute('aria-current')
  })

  it('marks the parent category on a detail route nested under one of its pages', () => {
    renderSidebar('/cashflow/accounts/8f3b1c1a-2e3a-4b1a-9a7f-100000000001')

    expect(railButton('CashFlow')).toHaveClass('sidebar__category-header--active')
  })

  it('moving the pointer off both the rail button and flyout closes it after ~250ms unless re-entered', () => {
    renderSidebar()

    const category = railButton('CashFlow').parentElement!
    fireEvent.mouseEnter(category)
    fireEvent.mouseLeave(category)
    act(() => {
      vi.advanceTimersByTime(100)
    })
    fireEvent.mouseEnter(category)
    act(() => {
      vi.advanceTimersByTime(250)
    })
    expect(flyout()).not.toBeNull()

    fireEvent.mouseLeave(category)
    act(() => {
      vi.advanceTimersByTime(250)
    })
    expect(flyout()).toBeNull()
  })

  it('focusing a rail button opens its flyout, and its links follow the button in tab order', () => {
    renderSidebar()

    fireEvent.focus(railButton('CashFlow'))

    const panel = flyout()!
    expect(railButton('CashFlow').compareDocumentPosition(panel) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(panel.compareDocumentPosition(railButton('Admin')) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('keeps the flyout open while focus moves from the rail button into it', () => {
    renderSidebar()

    fireEvent.focus(railButton('CashFlow'))
    const firstLink = within(flyout()!).getAllByRole('link')[0]
    fireEvent.blur(railButton('CashFlow'), { relatedTarget: firstLink })

    expect(flyout()).not.toBeNull()
  })

  it('closes the flyout when focus leaves both the rail button and the flyout', () => {
    renderSidebar()

    fireEvent.focus(railButton('CashFlow'))
    fireEvent.blur(railButton('CashFlow'), { relatedTarget: screen.getByRole('button', { name: 'Outside' }) })

    expect(flyout()).toBeNull()
  })

  it('pressing Escape inside the flyout closes it and returns focus to its rail button without reopening', () => {
    renderSidebar()

    const trigger = railButton('CashFlow')
    fireEvent.focus(trigger)
    const firstLink = within(flyout()!).getAllByRole('link')[0]
    firstLink.focus()
    fireEvent.keyDown(firstLink, { key: 'Escape' })

    expect(flyout()).toBeNull()
    expect(document.activeElement).toBe(trigger)
  })

  it('pressing Escape on the rail button closes its flyout and the next hover still opens it', () => {
    renderSidebar()

    const trigger = railButton('CashFlow')
    trigger.focus()
    fireEvent.keyDown(trigger, { key: 'Escape' })
    expect(flyout()).toBeNull()

    fireEvent.mouseEnter(trigger)
    expect(flyout()).not.toBeNull()
  })

  it('toggling minimises the rail to icons with accessible names, and expands it again', () => {
    renderSidebar()

    fireEvent.click(screen.getByRole('button', { name: 'Collapse sidebar' }))

    expect(mainNav()).toHaveClass('sidebar--collapsed')
    expect(railButton('CashFlow')).toHaveAttribute('aria-label', 'CashFlow')
    expect(within(mainNav()).queryByText('CashFlow')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Expand sidebar' }))

    expect(mainNav()).not.toHaveClass('sidebar--collapsed')
    expect(within(mainNav()).getByText('CashFlow')).toBeInTheDocument()
  })

  it('a minimised rail opens the same flyout on hover', () => {
    renderSidebar()
    fireEvent.click(screen.getByRole('button', { name: 'Collapse sidebar' }))

    fireEvent.mouseEnter(railButton('CashFlow'))

    expect(within(flyout()!).getAllByRole('link').map((link) => link.textContent)).toEqual(
      NAV_TREE[1].children.map((child) => child.label),
    )
  })

  it('persists the minimised state to localStorage on toggle', () => {
    renderSidebar()

    fireEvent.click(screen.getByRole('button', { name: 'Collapse sidebar' }))

    expect(localStorage.getItem('financial.sidebarCollapsed')).toBe('true')
  })

  it('renders already minimised on mount when localStorage has a stored true value', () => {
    localStorage.setItem('financial.sidebarCollapsed', 'true')

    renderSidebar()

    expect(mainNav()).toHaveClass('sidebar--collapsed')
  })

  it('the toggle button shows only its native tooltip, no flyout', () => {
    renderSidebar()

    const toggle = screen.getByRole('button', { name: 'Collapse sidebar' })
    fireEvent.mouseEnter(toggle)

    expect(toggle).toHaveAttribute('title', 'Collapse sidebar')
    expect(flyout()).toBeNull()
  })
})

describe('Sidebar on a phone', () => {
  const originalMatchMedia = window.matchMedia

  beforeEach(() => {
    mockMatchMedia(true)
  })

  afterEach(() => {
    window.matchMedia = originalMatchMedia
  })

  it('renders nothing when closed (OverlayDrawer unmounts its content)', () => {
    renderSidebar(undefined, { mobileOpen: false })

    expect(screen.queryByRole('link', { name: 'Active investments' })).not.toBeInTheDocument()
  })

  it('lists every page under its category heading, Admin groups included, with nothing to expand', () => {
    renderSidebar(undefined, { mobileOpen: true })

    const dialog = screen.getByRole('dialog')
    const links = within(dialog).getAllByRole('link').map((link) => link.getAttribute('href'))
    expect(links).toEqual(NAV_TREE.flatMap((category) => pagesOf(category).map((page) => page.route)))
    for (const category of NAV_TREE) {
      expect(within(dialog).getAllByText(category.label).length).toBeGreaterThan(0)
    }
    expect(within(dialog).getAllByRole('button').map((button) => button.getAttribute('aria-label'))).toEqual(['Close navigation'])
  })

  it('clicking the close button calls onMobileOpenChange(false)', () => {
    const onMobileOpenChange = vi.fn()
    renderSidebar(undefined, { mobileOpen: true, onMobileOpenChange })

    fireEvent.click(screen.getByRole('button', { name: 'Close navigation' }))

    expect(onMobileOpenChange).toHaveBeenCalledWith(false)
  })
})
