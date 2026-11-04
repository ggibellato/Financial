import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { account, potsAccount } from '../../test/account'
import ReserveBucketsRedirect from '../ReserveBucketsRedirect'

const { getAccountsMock } = vi.hoisted(() => ({
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { getAccounts: getAccountsMock } as Partial<FinancialApiClient>,
}))

function Destination() {
  const location = useLocation()
  return <p>{`Now at ${location.pathname}${location.search}`}</p>
}

function renderRedirect() {
  render(
    <MemoryRouter initialEntries={['/admin/cashflow/reserve-buckets']}>
      <Routes>
        <Route path="/admin/cashflow/reserve-buckets" element={<ReserveBucketsRedirect />} />
        <Route path="*" element={<Destination />} />
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  getAccountsMock.mockReset()
})

describe('ReserveBucketsRedirect', () => {
  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-13] opens the Pots tab of the first Pots account', async () => {
    getAccountsMock.mockResolvedValue([
      account({ id: 'bank', name: 'Barclays' }),
      potsAccount({ id: 'first-pots', name: 'Reserva' }),
      potsAccount({ id: 'second-pots', name: 'Trips' }),
    ])
    renderRedirect()

    expect(await screen.findByText('Now at /cashflow/accounts/first-pots?tab=pots')).toBeInTheDocument()
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-13] opens the Accounts page when there is no Pots account', async () => {
    getAccountsMock.mockResolvedValue([account({ id: 'bank', name: 'Barclays' })])
    renderRedirect()

    expect(await screen.findByText('Now at /cashflow/accounts')).toBeInTheDocument()
  })

  it('shows the error with a retry when the accounts cannot be read', async () => {
    const user = userEvent.setup()
    getAccountsMock.mockRejectedValueOnce(new Error('offline')).mockResolvedValue([potsAccount({ id: 'first-pots', name: 'Reserva' })])
    renderRedirect()

    expect(await screen.findByText('Unable to load the accounts')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Try again' }))

    await waitFor(() => expect(screen.getByText('Now at /cashflow/accounts/first-pots?tab=pots')).toBeInTheDocument())
  })
})
