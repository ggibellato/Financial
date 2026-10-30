import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountDto, AccountGroupMemberDto, AccountGroupSummaryDto } from '../../api/types'
import { fireEvent, render, screen, within } from '../../test/renderWithFluent'
import { pinDate } from '../../test-utils/pinDate'
import AccountsPage from '../AccountsPage'
import { account, potsAccount } from '../../test/account'

const { getDefaultGroupSummaryMock, getOtherAccountsMock, getAccountsMock } = vi.hoisted(() => ({
  getDefaultGroupSummaryMock: vi.fn<FinancialApiClient['getDefaultGroupSummary']>(),
  getOtherAccountsMock: vi.fn<FinancialApiClient['getOtherAccounts']>(),
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getDefaultGroupSummary: getDefaultGroupSummaryMock,
    getOtherAccounts: getOtherAccountsMock,
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

function cardMember(accountId: string, name: string, balance: number, outstandingTotal: number): AccountGroupMemberDto {
  return { accountId, name, templateName: 'CreditCard', currency: 'GBP', balance, roundUpTotal: null, statement: { outstandingTotal, isPaid: false }, balanceIsOwed: true }
}

const OTHER_ACCOUNTS: AccountGroupMemberDto[] = [
  { accountId: 'old-pots', name: 'Old pots', templateName: 'Pots', currency: 'GBP', balance: 12.5, roundUpTotal: null, statement: null, balanceIsOwed: false },
  { accountId: 'reserva', name: 'Reserva', templateName: 'Pots', currency: 'GBP', balance: 4210.55, roundUpTotal: null, statement: null, balanceIsOwed: false },
]

const ACCOUNTS: AccountDto[] = [
  potsAccount({ id: 'reserva', name: 'Reserva' }),
  potsAccount({ id: 'old-pots', name: 'Old pots', isActive: false }),
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
    getOtherAccountsMock.mockReset().mockResolvedValue(OTHER_ACCOUNTS)
    getAccountsMock.mockReset().mockResolvedValue(ACCOUNTS)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('[AC P60-F05-bank-account-screen-01] lists the default group members with balances and per-currency totals, each opening its account screen', async () => {
    renderPage()

    const table = await screen.findByRole('table', { name: 'Default group' })
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
        cardMember('amex', 'BA Amex', -812.33, 64.1),
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

  it('[AC P60-F07-credit-card-template-and-card-migration-08] shows a card\'s balance as owed and names its template "Credit card"', async () => {
    getDefaultGroupSummaryMock.mockResolvedValue({
      ...SUMMARY,
      members: [
        ...SUMMARY.members,
        cardMember('amex', 'BA Amex', -812.33, 64.1),
        cardMember('visa', 'Visa', 0, 0),
      ],
    })
    renderPage()

    const amexRow = (await screen.findByRole('link', { name: 'BA Amex' })).closest('tr')!
    expect(within(amexRow).getByText('Credit card')).toBeInTheDocument()
    expect(within(amexRow).getByText('Owed 812.33')).toBeInTheDocument()
    expect(within(screen.getByRole('link', { name: 'Visa' }).closest('tr')!).getByText('Owed 0.00')).toBeInTheDocument()
    expect(within(screen.getByRole('link', { name: 'Barclays' }).closest('tr')!).getByText('Bank')).toBeInTheDocument()
    expect(within(screen.getByRole('link', { name: 'Barclays' }).closest('tr')!).getByText('2,800.25')).toBeInTheDocument()
  })

  it('sorts the Balance column by net position, so the most owed card comes first', async () => {
    getDefaultGroupSummaryMock.mockResolvedValue({
      ...SUMMARY,
      members: [
        ...SUMMARY.members,
        cardMember('amex', 'BA Amex', -812.33, 64.1),
        cardMember('visa', 'Visa', -15, 15),
      ],
    })
    renderPage()

    const accountsTable = await screen.findByRole('table', { name: 'Default group' })
    fireEvent.click(within(accountsTable).getByRole('button', { name: 'Balance' }))

    const [, ...rows] = within(screen.getByRole('table', { name: 'Default group' })).getAllByRole('row')
    expect(rows.map((row) => within(row).getByRole('link').textContent)).toEqual(['BA Amex', 'Visa', 'Old Savings', 'Chase', 'Itau', 'Barclays'])
    expect(within(rows[0]).getByText('Owed 812.33')).toBeInTheDocument()
  })

  it('shows the month the balances belong to', async () => {
    renderPage()

    expect(await screen.findByText('Balances at end of October 2026')).toBeInTheDocument()
    expect(screen.getAllByRole('heading', { level: 2 }).map((heading) => heading.textContent)).toEqual(['Default group', 'Other accounts'])
  })

  it('marks an inactive member with an Inactive badge', async () => {
    renderPage()

    const table = await screen.findByRole('table', { name: 'Default group' })
    const oldRow = within(table).getByRole('link', { name: 'Old Savings' }).closest('tr') as HTMLElement
    expect(within(oldRow).getByText('Inactive')).toBeInTheDocument()
    const barclaysRow = within(table).getByRole('link', { name: 'Barclays' }).closest('tr') as HTMLElement
    expect(within(barclaysRow).queryByText('Inactive')).not.toBeInTheDocument()
  })

  it('[AC P60-F09-pots-template-and-reserva-migration-02] lists the other accounts by name with their balance, as plain text with no footer', async () => {
    renderPage()

    const table = await screen.findByRole('table', { name: 'Other accounts' })
    expect(getOtherAccountsMock).toHaveBeenCalledWith(2026, 10)
    expect(screen.getByRole('heading', { name: 'Other accounts' })).toBeInTheDocument()
    const [header, ...rows] = within(table).getAllByRole('row')
    expect(within(header).getAllByRole('columnheader').map((cell) => cell.textContent)).toEqual(['Account', 'Template', 'Currency', 'Balance'])
    expect(within(rows[0]).getByText('Old pots')).toBeInTheDocument()
    expect(within(rows[1]).getByText('Reserva')).toBeInTheDocument()
    expect(within(table).queryByRole('link')).not.toBeInTheDocument()
    expect(within(rows[1]).getByText('Pots')).toBeInTheDocument()
    expect(within(rows[1]).getByText('GBP')).toBeInTheDocument()
    expect(within(rows[1]).getByText('4,210.55')).toBeInTheDocument()
    expect(within(rows[0]).getByText('Inactive')).toBeInTheDocument()
    expect(within(rows[1]).queryByText('Inactive')).not.toBeInTheDocument()
    expect(table.closest('section')?.querySelector('.monthly-page__section-total')).toBeNull()
  })

  it('shows the empty message when there are no other accounts this month', async () => {
    getOtherAccountsMock.mockResolvedValue([])

    renderPage()

    expect(await screen.findByText('No other accounts this month')).toBeInTheDocument()
    expect(screen.getByRole('table', { name: 'Default group' })).toBeInTheDocument()
  })

  it('shows one loading state for both sections while the other accounts are pending', () => {
    getOtherAccountsMock.mockReturnValueOnce(new Promise(() => {}))

    renderPage()

    expect(screen.getAllByText('Loading...')).toHaveLength(1)
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('shows one error for both sections when the other accounts fail, and Try again reloads', async () => {
    getOtherAccountsMock.mockRejectedValueOnce(new Error('boom'))
    renderPage()

    expect(await screen.findByText('Unable to load the accounts')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('table', { name: 'Other accounts' })).toBeInTheDocument()
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

    expect(await screen.findByRole('table', { name: 'Default group' })).toBeInTheDocument()
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
