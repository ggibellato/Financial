import type { ComponentProps } from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { account, potsAccount, settlementAccount } from '../../test/account'
import MovedPageRedirect from '../MovedPageRedirect'

const { getAccountsMock } = vi.hoisted(() => ({
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { getAccounts: getAccountsMock } as Partial<FinancialApiClient>,
}))

function Destination() {
  const location = useLocation()
  return <p>{`Now at ${location.pathname}${location.search}${location.state?.movedNotice ? ' with notice' : ''}`}</p>
}

function renderRedirect(props: ComponentProps<typeof MovedPageRedirect> = { page: 'reserva', tab: 'pots' }) {
  render(
    <MemoryRouter initialEntries={['/old-address']}>
      <Routes>
        <Route path="/old-address" element={<MovedPageRedirect {...props} />} />
        <Route path="*" element={<Destination />} />
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  getAccountsMock.mockReset()
})

describe('MovedPageRedirect', () => {
  it('shows the loading state while the accounts are read', () => {
    getAccountsMock.mockReturnValue(new Promise(() => {}))
    renderRedirect()

    expect(screen.getByText('Loading...')).toBeInTheDocument()
    expect(screen.queryByText(/Now at/)).not.toBeInTheDocument()
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-13] [AC P60-F13-consolidated-navigation-and-retirement-04] opens the given tab of the account that replaces the page, not the first by name', async () => {
    getAccountsMock.mockResolvedValue([
      account({ id: 'bank', name: 'Barclays' }),
      potsAccount({ id: 'alpha-pots', name: 'Alpha', replacesPage: null }),
      potsAccount({ id: 'reserva', name: 'Reserva', replacesPage: 'reserva' }),
    ])
    renderRedirect()

    expect(await screen.findByText('Now at /cashflow/accounts/reserva?tab=pots')).toBeInTheDocument()
  })

  it('[AC P60-F13-consolidated-navigation-and-retirement-04] follows the marked account even when it is inactive', async () => {
    getAccountsMock.mockResolvedValue([
      potsAccount({ id: 'old-pots', name: 'Old pots', isActive: false, replacesPage: 'reserva' }),
      potsAccount({ id: 'trips', name: 'Trips', replacesPage: null }),
    ])
    renderRedirect()

    expect(await screen.findByText('Now at /cashflow/accounts/old-pots?tab=pots')).toBeInTheDocument()
  })

  it('[AC P60-F13-consolidated-navigation-and-retirement-04] opens the account screen with no tab when none is given', async () => {
    getAccountsMock.mockResolvedValue([settlementAccount({ id: 'mae', name: 'Mãe', replacesPage: 'controle-mae' })])
    renderRedirect({ page: 'controle-mae' })

    expect(await screen.findByText('Now at /cashflow/accounts/mae')).toBeInTheDocument()
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-13] [AC P60-F13-consolidated-navigation-and-retirement-04] opens the Accounts page with the moved notice when no account replaces the page', async () => {
    getAccountsMock.mockResolvedValue([account({ id: 'bank', name: 'Barclays' })])
    renderRedirect()

    expect(await screen.findByText('Now at /cashflow/accounts with notice')).toBeInTheDocument()
  })

  it('shows the error with a retry when the accounts cannot be read', async () => {
    const user = userEvent.setup()
    getAccountsMock.mockRejectedValueOnce(new Error('offline')).mockResolvedValue([potsAccount({ id: 'reserva', name: 'Reserva', replacesPage: 'reserva' })])
    renderRedirect()

    expect(await screen.findByText('Unable to load the accounts')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Try again' }))

    await waitFor(() => expect(screen.getByText('Now at /cashflow/accounts/reserva?tab=pots')).toBeInTheDocument())
  })
})
