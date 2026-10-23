import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountDetailDto, AccountDto, AccountTransactionDto } from '../../api/types'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import { pinDate } from '../../test-utils/pinDate'
import AccountScreenPage from '../AccountScreenPage'
import { accountTransactionRow } from '../../test/accountTransactionRow'

const { getAccountsMock, getAccountMock, getAccountTransactionsMock } = vi.hoisted(() => ({
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  getAccountMock: vi.fn<FinancialApiClient['getAccount']>(),
  getAccountTransactionsMock: vi.fn<FinancialApiClient['getAccountTransactions']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAccounts: getAccountsMock,
    getAccount: getAccountMock,
    getAccountTransactions: getAccountTransactionsMock,
  } as Partial<FinancialApiClient>,
}))

const BARCLAYS_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000001'
const CHASE_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000002'

const ACCOUNTS: AccountDto[] = [
  { id: BARCLAYS_ID, name: 'Barclays', templateName: 'Bank', currency: 'GBP', isActive: true },
  { id: CHASE_ID, name: 'Chase', templateName: 'Bank', currency: 'GBP', isActive: true },
]

function detailFor(id: string, year: number, month: number): AccountDetailDto {
  const account = ACCOUNTS.find((candidate) => candidate.id === id)!
  return { ...account, year, month, balance: id === BARCLAYS_ID ? 2800.25 : 500, allowedActions: [] }
}

const TRANSACTIONS: AccountTransactionDto[] = [
  accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-14', description: 'Tesco', categoryName: 'Groceries', amount: -42.1, roundUp: -0.9, canEdit: true }),
  accountTransactionRow({ id: 't1', kind: 'TransferOut', date: '2026-10-05', counterpart: 'Chase', amount: -200, canEdit: true }),
]

function LocationText() {
  const location = useLocation()
  return <p>{`${location.pathname}${location.search}`}</p>
}

function renderScreen(entry: string) {
  return render(
    <MemoryRouter initialEntries={[entry]}>
      <Routes>
        <Route
          path="/cashflow/accounts/:accountId"
          element={
            <>
              <AccountScreenPage />
              <LocationText />
            </>
          }
        />
        <Route path="/cashflow/accounts" element={<p>Accounts page</p>} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('AccountScreenPage', () => {
  beforeEach(() => {
    pinDate('2026-10-15T12:00:00+01:00')
    getAccountsMock.mockReset().mockResolvedValue(ACCOUNTS)
    getAccountMock.mockReset().mockImplementation((id, year, month) => Promise.resolve(detailFor(id, year, month)))
    getAccountTransactionsMock.mockReset().mockResolvedValue(TRANSACTIONS)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('[AC P60-F05-bank-account-screen-02] switching account in the picker keeps the selected month', async () => {
    const user = userEvent.setup()
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-09`)

    await user.click(await screen.findByRole('combobox', { name: 'Account' }))
    await user.click(screen.getByRole('option', { name: 'Chase' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Chase' })).toBeInTheDocument()
    expect(screen.getByText(`/cashflow/accounts/${CHASE_ID}?month=2026-09`)).toBeInTheDocument()
    expect(getAccountMock).toHaveBeenLastCalledWith(CHASE_ID, 2026, 9)
  })

  it('[AC P60-F05-bank-account-screen-03] the header shows name, template, currency and the month-end balance from the detail', async () => {
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    expect(await screen.findByRole('heading', { level: 1, name: 'Barclays' })).toBeInTheDocument()
    expect(screen.getByText('Bank · GBP')).toBeInTheDocument()
    expect(screen.getByText('Balance at end of October 2026')).toBeInTheDocument()
    expect(screen.getByText('2,800.25 GBP')).toBeInTheDocument()
  })

  it('[AC P60-F05-bank-account-screen-04] the grid lists the month rows with signed amounts', async () => {
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    const table = await screen.findByRole('table', { name: 'Transactions' })
    await waitFor(() => expect(within(table).getByText('Tesco')).toBeInTheDocument())
    expect(within(table).getByText('-42.10')).toBeInTheDocument()
    expect(within(table).getByText('-0.90')).toBeInTheDocument()
    expect(within(table).getByText('-200.00')).toBeInTheDocument()
  })

  it('shows skeletons for the header and grid on first load', () => {
    getAccountMock.mockReturnValue(new Promise(() => {}))

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    expect(screen.getByLabelText('Loading account')).toBeInTheDocument()
    expect(screen.getAllByLabelText('Loading transactions')).toHaveLength(3)
  })

  it('shows the empty message naming the month when it has no transactions', async () => {
    getAccountTransactionsMock.mockResolvedValue([])

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    expect(await screen.findByText('No transactions in October 2026')).toBeInTheDocument()
  })

  it('reloads for the month chosen in the month input', async () => {
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
    await screen.findByRole('heading', { level: 1, name: 'Barclays' })

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })

    expect(await screen.findByText('Balance at end of August 2026')).toBeInTheDocument()
    expect(screen.getByText(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-08`)).toBeInTheDocument()
  })

  it('shows the not-found message with a link to the Accounts page for an unknown account', async () => {
    getAccountMock.mockRejectedValue(new ApiError('Account not found', 404))

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    expect(await screen.findByText('This account does not exist.')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('link', { name: 'Go to Accounts' }))
    expect(screen.getByText('Accounts page')).toBeInTheDocument()
  })

  it('shows the not-found message for a malformed id without loading anything', () => {
    renderScreen('/cashflow/accounts/not-a-guid?month=2026-10')

    expect(screen.getByText('This account does not exist.')).toBeInTheDocument()
    expect(getAccountMock).not.toHaveBeenCalled()
  })

  it('shows an error with Try again when the load fails, and Try again reloads', async () => {
    getAccountTransactionsMock.mockRejectedValueOnce(new ApiError('Server error', 500))

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    expect(await screen.findByText('Unable to load this account')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Barclays' })).toBeInTheDocument()
  })
})
