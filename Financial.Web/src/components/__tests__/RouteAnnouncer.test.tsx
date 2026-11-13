import { Link, MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { fireEvent, render, screen, waitFor } from '../../test/renderWithFluent'
import RouteAnnouncer from '../RouteAnnouncer'

const renderAt = (initialEntry: string) =>
  render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <RouteAnnouncer />
      <Link to="/cashflow/mensais">Go to Mensais</Link>
      <Link to="/admin/investment/brokers">Go to Brokers</Link>
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

    fireEvent.click(screen.getByRole('link', { name: 'Go to Mensais' }))

    expect(await screen.findByRole('status')).toHaveTextContent('Mensais')
    await waitFor(() => expect(document.title).toBe('Mensais - CashFlow - Financial'))
  })

  it('covers admin pages nested under a group', async () => {
    renderAt('/investments/dashboard')

    fireEvent.click(screen.getByRole('link', { name: 'Go to Brokers' }))

    expect(await screen.findByRole('status')).toHaveTextContent('Brokers')
    await waitFor(() => expect(document.title).toBe('Brokers - Investment - Admin - Financial'))
  })

  it('uses the detail label for a detail page', async () => {
    renderAt('/cashflow/mensais')

    fireEvent.click(screen.getByRole('link', { name: 'Go to an account' }))

    expect(await screen.findByRole('status')).toHaveTextContent('Account')
    await waitFor(() => expect(document.title).toBe('Account - CashFlow - Financial'))
  })

  it('falls back to the app name and an empty announcement for an unknown route', async () => {
    renderAt('/cashflow/mensais')

    fireEvent.click(screen.getByRole('link', { name: 'Go nowhere' }))

    await waitFor(() => expect(document.title).toBe('Financial'))
    expect(screen.getByRole('status')).toBeEmptyDOMElement()
  })
})
