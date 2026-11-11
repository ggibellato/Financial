import { fireEvent, render as rtlRender, screen, waitFor, within } from '@testing-library/react'
import type { ReactElement } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { FluentProvider, webLightTheme } from '@fluentui/react-components'
import { accountTransactionRow } from '../../test/accountTransactionRow'
import MonthlyPage from '../MonthlyPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type {
  AccountGroupMemberDto,
  AccountGroupSummaryDto,
  CategoryTotalDto,
  MonthTransactionDto,
  TitheSummaryDto,
} from '../../api/types'

const {
  getCategoryTotalsByMonthMock,
  getDefaultGroupSummaryMock,
  getTitheSummaryByMonthMock,
  updateTitheCarryForwardMock,
  getMonthTransactionsMock,
} = vi.hoisted(() => ({
  getCategoryTotalsByMonthMock: vi.fn<FinancialApiClient['getCategoryTotalsByMonth']>(),
  getDefaultGroupSummaryMock: vi.fn<FinancialApiClient['getDefaultGroupSummary']>(),
  getTitheSummaryByMonthMock: vi.fn<FinancialApiClient['getTitheSummaryByMonth']>(),
  updateTitheCarryForwardMock: vi.fn<FinancialApiClient['updateTitheCarryForward']>(),
  getMonthTransactionsMock: vi.fn<FinancialApiClient['getMonthTransactions']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getCategoryTotalsByMonth: getCategoryTotalsByMonthMock,
    getDefaultGroupSummary: getDefaultGroupSummaryMock,
    getTitheSummaryByMonth: getTitheSummaryByMonthMock,
    updateTitheCarryForward: updateTitheCarryForwardMock,
    getMonthTransactions: getMonthTransactionsMock,
  } as Partial<FinancialApiClient>,
}))

function render(ui: ReactElement) {
  return rtlRender(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>{ui}</MemoryRouter>
    </FluentProvider>,
  )
}

const CATEGORY_TOTALS: CategoryTotalDto[] = [{ category: 'Mercado', totalValue: 42.5 }]

function member(accountId: string, name: string, balance: number, roundUpTotal = 0): AccountGroupMemberDto {
  return { accountId, name, templateName: 'Bank', currency: 'GBP', balance, currencyBalances: [{ currency: 'GBP', balance }], otherParticipant: null, roundUpTotal, statement: null, balanceIsOwed: false, isActive: true, totalOwed: null, nextStatement: null }
}

function cardMember(accountId: string, name: string, outstandingTotal: number, isPaid = false): AccountGroupMemberDto {
  return { ...member(accountId, name, -outstandingTotal), templateName: 'CreditCard', roundUpTotal: null, statement: { outstandingTotal, isPaid }, balanceIsOwed: true, totalOwed: outstandingTotal }
}

const SUMMARY: AccountGroupSummaryDto = {
  year: 2026,
  month: 7,
  members: [
    member('bank-barclays', 'Barclays', 42.5),
    member('bank-chase', 'Chase', 0),
    member('bank-trading212', 'Trading212', 8.8, 0.6),
    cardMember('card-baamex', 'BaAmex', 100),
    cardMember('card-chase', 'ChaseMaster4023', 0, true),
  ],
  totals: [{ currency: 'GBP', balance: 51.3, roundUpTotal: 0.6, cardOutstandingTotal: 100 }],
  categoryTotalsSum: 42.5,
  incomeTotals: [{ incomeSourceId: '1', name: 'Gleison', netValue: 2450, grossValue: 3200 }],
  totalIncoming: 2450,
}

const MONTH_TRANSACTIONS: MonthTransactionDto[] = [
  {
    accountId: 'bank-barclays',
    accountName: 'Barclays',
    accountCurrency: 'GBP',
    accountIsActive: true,
    transaction: accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-07-05', description: 'Lidl UK', categoryName: 'Mercado', amount: -42.5 }),
  },
]

const TITHE_SUMMARY: TitheSummaryDto = { calculatedTithe: 245, titheBalance: 245, carryForward: null }

const summaryLoaded = () => screen.findByText('BaAmex')

describe('MonthlyPage', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    vi.setSystemTime(new Date('2026-07-15T12:00:00Z'))
    getCategoryTotalsByMonthMock.mockReset().mockResolvedValue(CATEGORY_TOTALS)
    getDefaultGroupSummaryMock.mockReset().mockResolvedValue(SUMMARY)
    getTitheSummaryByMonthMock.mockReset().mockResolvedValue(TITHE_SUMMARY)
    updateTitheCarryForwardMock.mockReset()
    getMonthTransactionsMock.mockReset().mockResolvedValue(MONTH_TRANSACTIONS)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('shows a loading state before data arrives', () => {
    render(<MonthlyPage />)

    expect(screen.getByText('Loading…')).toBeInTheDocument()
  })

  it('[AC P60-F04-default-account-group-and-summary-query-07] shows loading while the summary request is pending', () => {
    getDefaultGroupSummaryMock.mockReturnValue(new Promise(() => {}))

    render(<MonthlyPage />)

    expect(screen.getByText('Loading…')).toBeInTheDocument()
    expect(screen.queryByText(/Bank balance:/, { selector: 'p' })).not.toBeInTheDocument()
  })

  it('[AC P60-F04-default-account-group-and-summary-query-07] shows an error with Try again when the summary request fails, and Try again reloads it', async () => {
    getDefaultGroupSummaryMock.mockRejectedValueOnce(new Error('Summary down'))

    render(<MonthlyPage />)

    expect(await screen.findByText('Summary down')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText(/Bank balance:/, { selector: 'p' })).toBeInTheDocument()
    expect(getDefaultGroupSummaryMock).toHaveBeenCalledTimes(2)
  })

  it('[AC P60-F04-default-account-group-and-summary-query-07] shows an empty message in each summary grid for a month with nothing recorded', async () => {
    getCategoryTotalsByMonthMock.mockResolvedValue([])
    getDefaultGroupSummaryMock.mockResolvedValue({ ...SUMMARY, members: [], totals: [], categoryTotalsSum: 0, incomeTotals: [], totalIncoming: 0 })

    render(<MonthlyPage />)

    expect(await screen.findByText('No accounts in the default group for this month.')).toBeInTheDocument()
    expect(screen.getByText('No cards in the default group for this month.')).toBeInTheDocument()
    expect(screen.getByText('No expenses recorded for this month.')).toBeInTheDocument()
    expect(screen.getByText('No income recorded for this month.')).toBeInTheDocument()
    expect(screen.getByText(/Bank balance:/, { selector: 'p' })).toHaveTextContent('Bank balance: 0.00 · Round-up: 0.00')
  })

  it('[AC P60-F13-consolidated-navigation-and-retirement-02] shows Summary and All transactions with no entry forms', async () => {
    render(<MonthlyPage />)
    await summaryLoaded()

    expect(screen.getAllByRole('tab')).toHaveLength(2)
    expect(screen.getByRole('tab', { name: 'All transactions' })).toHaveAttribute('aria-selected', 'false')
    expect(screen.getByRole('tab', { name: 'Summary' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.queryByRole('button', { name: /^(New expense|New income|Transfer to|Transfer from|Adjust balance|Mark paid|Unmark paid)$/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('form')).not.toBeInTheDocument()
  })

  it('wires the Summary and All transactions tabs to the panel they control', async () => {
    render(<MonthlyPage />)
    await summaryLoaded()

    const panel = screen.getByRole('tabpanel', { name: 'Summary' })
    expect(screen.getByRole('tab', { name: 'All transactions' })).toHaveAttribute('aria-controls', panel.id)
    fireEvent.click(screen.getByRole('tab', { name: 'All transactions' }))
    expect(await screen.findByRole('tabpanel', { name: 'All transactions' })).toHaveAttribute('id', panel.id)
  })

  it('[AC P60-F13-consolidated-navigation-and-retirement-02] links bank and card rows to their account screen at the viewed month', async () => {
    render(<MonthlyPage />)
    await summaryLoaded()

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })
    await waitFor(() => expect(getDefaultGroupSummaryMock).toHaveBeenCalledWith(2026, 8))

    expect(await screen.findByRole('link', { name: 'Barclays' })).toHaveAttribute('href', '/cashflow/accounts/bank-barclays?month=2026-08')
    expect(screen.getByRole('link', { name: 'BaAmex' })).toHaveAttribute('href', '/cashflow/accounts/card-baamex?month=2026-08')
  })

  it('shows the card total from the summary and keeps cards out of the bank rows', async () => {
    render(<MonthlyPage />)
    await summaryLoaded()

    expect(screen.getByText(/Combined adjustment figure/)).toHaveTextContent('Combined adjustment figure: 100.00')
    expect(within(screen.getByRole('table', { name: 'Bank balances' })).queryByText('BaAmex')).not.toBeInTheDocument()
  })

  it('renders Category totals and Cards in the first Summary row, Banks and Incoming in the second', async () => {
    render(<MonthlyPage />)
    await summaryLoaded()

    const rows = document.querySelectorAll('.monthly-page__summary-groups > .monthly-page__grids-row')
    expect(rows).toHaveLength(2)

    const firstRow = within(rows[0] as HTMLElement)
    expect(firstRow.getByText(/^Total:/, { selector: 'p' })).toBeInTheDocument()
    expect(firstRow.getByText(/Combined adjustment figure/)).toBeInTheDocument()

    const secondRow = within(rows[1] as HTMLElement)
    expect(secondRow.getByText(/Bank balance:/, { selector: 'p' })).toBeInTheDocument()
    expect(secondRow.getByText(/Total incoming:/)).toBeInTheDocument()
  })

  it('shows each bank balance with its round-up in a separate column', async () => {
    render(<MonthlyPage />)
    await summaryLoaded()

    const trading212Row = screen.getByRole('row', { name: /Trading212/ })
    expect(within(trading212Row).getByText('8.80')).toBeInTheDocument()
    expect(within(trading212Row).getByText('0.60')).toBeInTheDocument()
  })

  it('renders the Incoming grid with one row per source and the calculated tithe and tithe balance', async () => {
    render(<MonthlyPage />)

    const incomingSection = within((await screen.findByText(/Total incoming:/)).closest('section')!)
    expect(incomingSection.getByText('Gleison')).toBeInTheDocument()
    expect(incomingSection.getByText('3,200.00')).toBeInTheDocument()
    expect(incomingSection.getByText(/Calculated tithe:/)).toBeInTheDocument()
    expect(incomingSection.getByText(/Tithe balance:/)).toBeInTheDocument()
  })

  it('toggles carry-forward inclusion from the Summary checkbox', async () => {
    getTitheSummaryByMonthMock.mockResolvedValue({
      calculatedTithe: 245,
      titheBalance: 195,
      carryForward: { amount: 50, included: true, fromYear: 2026, fromMonth: 6 },
    })
    updateTitheCarryForwardMock.mockResolvedValue(TITHE_SUMMARY)
    render(<MonthlyPage />)
    await summaryLoaded()

    fireEvent.click(screen.getByRole('checkbox', { name: /^50\.00 from/ }))

    await waitFor(() => expect(updateTitheCarryForwardMock).toHaveBeenCalledWith(2026, 7, { included: false }))
    await waitFor(() => expect(getTitheSummaryByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('shows a refused carry-forward toggle as an alert and keeps the grids', async () => {
    getTitheSummaryByMonthMock.mockResolvedValue({
      calculatedTithe: 245,
      titheBalance: 195,
      carryForward: { amount: 50, included: true, fromYear: 2026, fromMonth: 6 },
    })
    updateTitheCarryForwardMock.mockRejectedValue(new Error('No carry-forward is available for this month.'))
    render(<MonthlyPage />)
    await summaryLoaded()

    fireEvent.click(screen.getByRole('checkbox', { name: /^50\.00 from/ }))

    expect(await screen.findByRole('alert')).toHaveTextContent('No carry-forward is available for this month.')
    expect(screen.getByText('BaAmex')).toBeInTheDocument()
  })

  it('re-scopes every Summary grid when the month value changes', async () => {
    render(<MonthlyPage />)
    await summaryLoaded()

    getCategoryTotalsByMonthMock.mockResolvedValue([{ category: 'Viagem', totalValue: 300 }])
    getDefaultGroupSummaryMock.mockResolvedValue({
      ...SUMMARY,
      month: 8,
      members: [member('bank-barclays', 'Barclays', 300), cardMember('card-baamex', 'BaAmex', 55)],
      totals: [{ currency: 'GBP', balance: 300, roundUpTotal: 0, cardOutstandingTotal: 55 }],
      categoryTotalsSum: 300,
      incomeTotals: [{ incomeSourceId: '3', name: 'Lottery', netValue: 50, grossValue: null }],
      totalIncoming: 50,
    })

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })

    expect(await screen.findByText('Viagem')).toBeInTheDocument()
    expect(screen.queryByText('Mercado')).not.toBeInTheDocument()
    expect(screen.getByRole('row', { name: /BaAmex/ })).toHaveTextContent('55.00')
    expect(screen.getByText('Lottery')).toBeInTheDocument()
  })

  it('keeps the month and refetches nothing when switching tabs', async () => {
    render(<MonthlyPage />)
    await summaryLoaded()
    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })
    await waitFor(() => expect(getDefaultGroupSummaryMock).toHaveBeenCalledTimes(2))
    await summaryLoaded()

    fireEvent.click(screen.getByRole('tab', { name: 'All transactions' }))
    fireEvent.click(screen.getByRole('tab', { name: 'Summary' }))

    expect(screen.getByLabelText('Month')).toHaveValue('2026-08')
    expect(getDefaultGroupSummaryMock).toHaveBeenCalledTimes(2)
  })

  describe('All transactions tab', () => {
    async function openAllTransactions() {
      fireEvent.click(screen.getByRole('tab', { name: 'All transactions' }))
      const grid = await screen.findByRole('table', { name: 'All transactions' })
      await waitFor(() => expect(within(grid).getByText('Lidl UK')).toBeInTheDocument())
    }

    it('requests nothing until the tab is opened, then lists the month', async () => {
      render(<MonthlyPage />)
      await summaryLoaded()
      expect(getMonthTransactionsMock).not.toHaveBeenCalled()

      await openAllTransactions()
      expect(getMonthTransactionsMock).toHaveBeenCalledWith(2026, 7)
    })

    it('[AC P60-F16-month-transactions-view-04] works while the Monthly load is pending', async () => {
      getDefaultGroupSummaryMock.mockReturnValue(new Promise(() => {}))
      render(<MonthlyPage />)

      await openAllTransactions()
      expect(screen.queryByText('Loading…')).not.toBeInTheDocument()
    })

    it('[AC P60-F16-month-transactions-view-04] works while the Monthly load has failed', async () => {
      getCategoryTotalsByMonthMock.mockRejectedValue(new Error('Network down'))
      render(<MonthlyPage />)
      await screen.findByText('Network down')

      await openAllTransactions()
      expect(screen.queryByText('Network down')).not.toBeInTheDocument()
    })

    it('[AC P60-F16-month-transactions-view-04] keeps its load error to the tab and leaves Summary working', async () => {
      getMonthTransactionsMock.mockRejectedValue({ status: 503 })
      render(<MonthlyPage />)
      await summaryLoaded()

      fireEvent.click(screen.getByRole('tab', { name: 'All transactions' }))

      await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Unable to load the transactions.'))

      fireEvent.click(screen.getByRole('tab', { name: 'Summary' }))

      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
      expect(screen.getByText(/Bank balance:/, { selector: 'p' })).toBeInTheDocument()
    })

    it('reloads on a month change and starts again with no filter', async () => {
      render(<MonthlyPage />)
      await openAllTransactions()
      fireEvent.click(screen.getByRole('button', { name: 'Filter by Account' }))
      fireEvent.click(screen.getByRole('checkbox', { name: '(All)' }))
      expect(screen.getByRole('button', { name: 'Clear filters' })).toBeInTheDocument()

      fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })

      await waitFor(() => expect(getMonthTransactionsMock).toHaveBeenLastCalledWith(2026, 8))
      await screen.findByText('Lidl UK')
      expect(screen.queryByRole('button', { name: 'Clear filters' })).not.toBeInTheDocument()
    })

    it('reloads every time the tab is reopened', async () => {
      render(<MonthlyPage />)
      await openAllTransactions()

      fireEvent.click(screen.getByRole('tab', { name: 'Summary' }))
      fireEvent.click(screen.getByRole('tab', { name: 'All transactions' }))

      await waitFor(() => expect(getMonthTransactionsMock).toHaveBeenCalledTimes(2))
    })
  })
})
