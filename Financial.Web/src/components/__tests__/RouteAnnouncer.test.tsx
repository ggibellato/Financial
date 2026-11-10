import { Link, MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { fireEvent, render, screen, waitFor } from '../../test/renderWithFluent'
import RouteAnnouncer from '../RouteAnnouncer'

const renderAt = (initialEntry: string) =>
  render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <RouteAnnouncer />
      <Link to="/cashflow/monthly">Go to Monthly</Link>
      <Link to="/admin/investment/tax-rules">Go to Tax rules</Link>
      <Link to="/cashflow/accounts/acc-1">Go to an account</Link>
      <Link to="/not-a-page">Go nowhere</Link>
    </MemoryRouter>,
  )

describe('RouteAnnouncer', () => {
  it('titles the document with the current page', async () => {
    renderAt('/investments/dashboard')

    await waitFor(() => expect(document.title).toBe('Dashboard - Investments - Financial'))
  })

  it('announces the new page in a polite status region after navigation', async () => {
    renderAt('/investments/dashboard')

    fireEvent.click(screen.getByRole('link', { name: 'Go to Monthly' }))

    expect(await screen.findByRole('status')).toHaveTextContent('Monthly')
    await waitFor(() => expect(document.title).toBe('Monthly - CashFlow - Financial'))
  })

  it('covers admin pages nested under a group', async () => {
    renderAt('/investments/dashboard')

    fireEvent.click(screen.getByRole('link', { name: 'Go to Tax rules' }))

    expect(await screen.findByRole('status')).toHaveTextContent('Tax rules')
    await waitFor(() => expect(document.title).toBe('Tax rules - Investment - Admin - Financial'))
  })

  it('uses the detail label for a detail page', async () => {
    renderAt('/cashflow/monthly')

    fireEvent.click(screen.getByRole('link', { name: 'Go to an account' }))

    expect(await screen.findByRole('status')).toHaveTextContent('Account')
    await waitFor(() => expect(document.title).toBe('Account - CashFlow - Financial'))
  })

  it('falls back to the app name and an empty announcement for an unknown route', async () => {
    renderAt('/cashflow/monthly')

    fireEvent.click(screen.getByRole('link', { name: 'Go nowhere' }))

    await waitFor(() => expect(document.title).toBe('Financial'))
    expect(screen.getByRole('status')).toBeEmptyDOMElement()
  })
})
