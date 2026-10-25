import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountDto, AccountGroupSummaryDto } from '../../api/types'
import { fireEvent, render, screen, within } from '../../test/renderWithFluent'
import { pinDate } from '../../test-utils/pinDate'
import AccountsPage from '../AccountsPage'
import { account } from '../../test/account'

const { getDefaultGroupSummaryMock, getAccountsMock } = vi.hoisted(() => ({
  getDefaultGroupSummaryMock: vi.fn<FinancialApiClient['getDefaultGroupSummary']>(),
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getDefaultGroupSummary: getDefaultGroupSummaryMock,
    getAccounts: getAccountsMock,
  } as Partial<FinancialApiClient>,
}))

const SUMMARY: AccountGroupSummaryDto = {
  year: 2026,
  month: 10,
  members: [
    { accountId: 'barclays', name: 'Barclays', templateName: 'Bank', currency: 'GBP', balance: 2800.25, roundUpTotal: 1.2, statement: null, balanceIsOwed: false },
    { accountId: 'chase', name: 'Chase', templateName: 'Bank', currency: 'GBP', balance: 500, roundUpTotal: null, statement: null, balanceIsOwed: false },
    { accountId: 'old', name: 'Old Savings', templateName: 'Bank', currency: 'GBP', balance: 10, roundUpTotal: null, statement: null, balanceIsOwed: false },
    { accountId: 'itau', name: 'Itau', templateName: 'Bank', currency: 'BRL', balance: 1000, roundUpTotal: null, statement: null, balanceIsOwed: false },
  ],
  totals: [
    { currency: 'BRL', balance: 1000, roundUpTotal: 0, cardOutstandingTotal: 0 },
    { currency: 'GBP', balance: 3310.25, roundUpTotal: 1.2, cardOutstandingTotal: 0 },
  ],
  categoryTotalsSum: 0,
  incomeTotals: [],
  totalIncoming: 0,
}

const ACCOUNTS: AccountDto[] = [
  account({ id: 'barclays', name: 'Barclays' }),
  account({ id: 'chase', name: 'Chase' }),
  account({ id: 'itau', name: 'Itau', currency: 'BRL' }),
  account({ id: 'old', name: 'Old Savings', isActive: false }),
]

function renderPage() {
  return render(
    <MemoryRouter>
      <AccountsPage />
    </MemoryRouter>,
  )
}

describe('AccountsPage', () => {
  beforeEach(() => {
    pinDate('2026-10-15T12:00:00+01:00')
    getDefaultGroupSummaryMock.mockReset().mockResolvedValue(SUMMARY)
    getAccountsMock.mockReset().mockResolvedValue(ACCOUNTS)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('[AC P60-F05-bank-account-screen-01] lists the default group members with balances and per-currency totals, each opening its account screen', async () => {
    renderPage()

    const table = await screen.findByRole('table', { name: 'Accounts' })
    expect(getDefaultGroupSummaryMock).toHaveBeenCalledWith(2026, 10)
    const [, ...rows] = within(table).getAllByRole('row')
    expect(rows.map((row) => within(row).getByRole('link').textContent)).toEqual(['Barclays', 'Chase', 'Old Savings', 'Itau'])
    expect(within(rows[0]).getByText('2,800.25')).toBeInTheDocument()
    expect(within(rows[0]).getByRole('link', { name: 'Barclays' })).toHaveAttribute(
      'href',
      '/cashflow/accounts/barclays?month=2026-10',
    )
    expect(screen.getByText('Bank total BRL:', { exact: false })).toHaveTextContent('Bank total BRL: 1,000.00 · Bank total GBP: 3,310.25')
  })

  it('labels the footer as the bank total, which leaves out card members', async () => {
    getDefaultGroupSummaryMock.mockResolvedValue({
      ...SUMMARY,
      members: [
        ...SUMMARY.members,
        { accountId: 'amex', name: 'BA Amex', templateName: 'CreditCard', currency: 'GBP', balance: -812.33, roundUpTotal: null, statement: { outstandingTotal: 64.1, isPaid: false }, balanceIsOwed: true },
      ],
      totals: [
        { currency: 'BRL', balance: 1000, roundUpTotal: 0, cardOutstandingTotal: 0 },
        { currency: 'GBP', balance: 3310.25, roundUpTotal: 1.2, cardOutstandingTotal: 64.1 },
      ],
    })
    renderPage()

    expect(await screen.findByRole('link', { name: 'BA Amex' })).toBeInTheDocument()
    expect(screen.getByText('Bank total BRL:', { exact: false })).toHaveTextContent('Bank total BRL: 1,000.00 · Bank total GBP: 3,310.25')
  })

  it('shows the month the balances belong to', async () => {
    renderPage()

    expect(await screen.findByText('Default group · balances at end of October 2026')).toBeInTheDocument()
  })

  it('marks an inactive member with an Inactive badge', async () => {
    renderPage()

    const table = await screen.findByRole('table', { name: 'Accounts' })
    const oldRow = within(table).getByRole('link', { name: 'Old Savings' }).closest('tr') as HTMLElement
    expect(within(oldRow).getByText('Inactive')).toBeInTheDocument()
    const barclaysRow = within(table).getByRole('link', { name: 'Barclays' }).closest('tr') as HTMLElement
    expect(within(barclaysRow).queryByText('Inactive')).not.toBeInTheDocument()
  })

  it('shows loading while the summary is pending', () => {
    getDefaultGroupSummaryMock.mockReturnValue(new Promise(() => {}))

    renderPage()

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error with Try again, and Try again reloads', async () => {
    getDefaultGroupSummaryMock.mockRejectedValueOnce(new Error('boom'))

    renderPage()

    expect(await screen.findByText('Unable to load the accounts')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('table', { name: 'Accounts' })).toBeInTheDocument()
    expect(getDefaultGroupSummaryMock).toHaveBeenCalledTimes(2)
  })

  it('shows the empty message when the default group has no members', async () => {
    getDefaultGroupSummaryMock.mockResolvedValue({ ...SUMMARY, members: [], totals: [] })

    renderPage()

    expect(await screen.findByText('No accounts in the default group for this month')).toBeInTheDocument()
  })

  it('opens the account screen when a row link is followed', async () => {
    render(
      <MemoryRouter initialEntries={['/cashflow/accounts']}>
        <Routes>
          <Route path="/cashflow/accounts" element={<AccountsPage />} />
          <Route path="/cashflow/accounts/:accountId" element={<p>Account screen</p>} />
        </Routes>
      </MemoryRouter>,
    )

    fireEvent.click(await screen.findByRole('link', { name: 'Chase' }))

    expect(screen.getByText('Account screen')).toBeInTheDocument()
  })
})
