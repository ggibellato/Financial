import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type {
  AccountGroupSummaryDto,
  BankDto,
  CardStatementDto,
  CategoryDto,
  CategoryTotalDto,
  ExpenseDto,
  IncomeDto,
  IncomeSourceDto,
  TitheSummaryDto,
} from '../../api/types'
import { useMonthly } from '../useMonthly'

const NOW = new Date()
const CURRENT_YEAR = NOW.getFullYear()
const CURRENT_MONTH = NOW.getMonth() + 1
const CURRENT_MONTH_INPUT = `${CURRENT_YEAR}-${String(CURRENT_MONTH).padStart(2, '0')}`
const NEXT_MONTH = CURRENT_MONTH === 12 ? 1 : CURRENT_MONTH + 1
const NEXT_MONTH_YEAR = CURRENT_MONTH === 12 ? CURRENT_YEAR + 1 : CURRENT_YEAR
const NEXT_MONTH_INPUT = `${NEXT_MONTH_YEAR}-${String(NEXT_MONTH).padStart(2, '0')}`

const {
  getExpensesByMonthMock,
  getUnpaidCardChargesByMonthMock,
  getCategoryTotalsByMonthMock,
  getCardStatementsByMonthMock,
  getBanksMock,
  getIncomeSourcesMock,
  getCategoriesMock,
  deleteExpenseMock,
  markCardStatementPaidMock,
  unmarkCardStatementPaidMock,
  getIncomesByMonthMock,
  deleteIncomeMock,
  getDefaultGroupSummaryMock,
  getTitheSummaryByMonthMock,
  updateTitheCarryForwardMock,
} = vi.hoisted(() => ({
  getExpensesByMonthMock: vi.fn<FinancialApiClient['getExpensesByMonth']>(),
  getUnpaidCardChargesByMonthMock: vi.fn<FinancialApiClient['getUnpaidCardChargesByMonth']>(),
  getCategoryTotalsByMonthMock: vi.fn<FinancialApiClient['getCategoryTotalsByMonth']>(),
  getCardStatementsByMonthMock: vi.fn<FinancialApiClient['getCardStatementsByMonth']>(),
  getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
  getIncomeSourcesMock: vi.fn<FinancialApiClient['getIncomeSources']>(),
  getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
  deleteExpenseMock: vi.fn<FinancialApiClient['deleteExpense']>(),
  markCardStatementPaidMock: vi.fn<FinancialApiClient['markCardStatementPaid']>(),
  unmarkCardStatementPaidMock: vi.fn<FinancialApiClient['unmarkCardStatementPaid']>(),
  getIncomesByMonthMock: vi.fn<FinancialApiClient['getIncomesByMonth']>(),
  deleteIncomeMock: vi.fn<FinancialApiClient['deleteIncome']>(),
  getDefaultGroupSummaryMock: vi.fn<FinancialApiClient['getDefaultGroupSummary']>(),
  getTitheSummaryByMonthMock: vi.fn<FinancialApiClient['getTitheSummaryByMonth']>(),
  updateTitheCarryForwardMock: vi.fn<FinancialApiClient['updateTitheCarryForward']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getExpensesByMonth: getExpensesByMonthMock,
    getUnpaidCardChargesByMonth: getUnpaidCardChargesByMonthMock,
    getCategoryTotalsByMonth: getCategoryTotalsByMonthMock,
    getCardStatementsByMonth: getCardStatementsByMonthMock,
    getBanks: getBanksMock,
    getIncomeSources: getIncomeSourcesMock,
    getCategories: getCategoriesMock,
    deleteExpense: deleteExpenseMock,
    markCardStatementPaid: markCardStatementPaidMock,
    unmarkCardStatementPaid: unmarkCardStatementPaidMock,
    getIncomesByMonth: getIncomesByMonthMock,
    deleteIncome: deleteIncomeMock,
    getDefaultGroupSummary: getDefaultGroupSummaryMock,
    getTitheSummaryByMonth: getTitheSummaryByMonthMock,
    updateTitheCarryForward: updateTitheCarryForwardMock,
  } as Partial<FinancialApiClient>,
}))

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: true },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: true },
  { id: 'bank-chase', name: 'Chase', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: true },
]

const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: '1', name: 'Gleison', isActive: true, group: 'Salary', autoSplitToReserve: false, hasReferences: false },
  { id: '2', name: 'Ariana', isActive: true, group: 'Salary', autoSplitToReserve: true, hasReferences: false },
  { id: '3', name: 'Lottery', isActive: true, group: 'NonReportable', autoSplitToReserve: false, hasReferences: false },
  { id: '4', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros', autoSplitToReserve: false, hasReferences: false },
]

const CATEGORIES: CategoryDto[] = [
  { id: 'category-mercado', name: 'Mercado', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'category-extras', name: 'Extras', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'category-reserva', name: 'Reserva', active: false, isInvestment: false, isTithe: false, hasReferences: false },
]

const EXPENSES: ExpenseDto[] = [
  {
    id: 'e1',
    date: `${CURRENT_YEAR}-${String(CURRENT_MONTH).padStart(2, '0')}-05`,
    description: 'Lidl',
    value: 42.5,
    categoryId: 'category-mercado',
    categoryName: 'Mercado',
    paymentSourceBankId: 'bank-barclays',
    paymentSourceBankName: 'Barclays',
    creditCardId: null,
    creditCardName: null,
    chargeDate: null,
    invoiceDate: null,
    paymentStatus: 'ImmediatePayment',
    roundUpAmount: null,
    suggestedRoundUpAmount: null,
    countsAsTithe: true,
  },
]

const CATEGORY_TOTALS: CategoryTotalDto[] = [{ category: 'Mercado', totalValue: 42.5 }]

const CARD_STATEMENTS: CardStatementDto[] = [
  { id: 'c1', creditCardId: 'card-baamex', creditCardName: 'BaAmex', year: CURRENT_YEAR, month: CURRENT_MONTH, isPaid: false, outstandingTotal: 100, accumulatedOutstandingTotal: 100 },
  { id: 'c2', creditCardId: 'card-chase', creditCardName: 'ChaseMaster4023', year: CURRENT_YEAR, month: CURRENT_MONTH, isPaid: true, outstandingTotal: 0, accumulatedOutstandingTotal: 0 },
]

const INCOMES: IncomeDto[] = [
  {
    id: 'i1',
    date: `${CURRENT_YEAR}-${String(CURRENT_MONTH).padStart(2, '0')}-01`,
    incomeSourceId: '1',
    incomeSourceName: 'Gleison',
    grossValue: 3200,
    netValue: 2450,
    bankId: 'bank-barclays',
    bankName: 'Barclays',
    description: null,
    splitToReserve: false,
  },
]

const SUMMARY: AccountGroupSummaryDto = {
  year: CURRENT_YEAR,
  month: CURRENT_MONTH,
  members: [
    { accountId: 'bank-barclays', name: 'Barclays', templateName: 'Bank', currency: 'GBP', balance: 1875.32, roundUpTotal: 0, statement: null, balanceIsOwed: false },
    { accountId: 'bank-chase', name: 'Chase', templateName: 'Bank', currency: 'GBP', balance: -50, roundUpTotal: 0.1, statement: null, balanceIsOwed: false },
    { accountId: 'bank-trading212', name: 'Trading212', templateName: 'Bank', currency: 'GBP', balance: 420.1, roundUpTotal: 0.6, statement: null, balanceIsOwed: false },
  ],
  totals: [{ currency: 'GBP', balance: 2245.42, roundUpTotal: 0.7, cardOutstandingTotal: 0 }],
  categoryTotalsSum: 1234.56,
  incomeTotals: [
    { incomeSourceId: '2', name: 'Ariana', netValue: 720, grossValue: 820 },
    { incomeSourceId: '3', name: 'Lottery', netValue: 50, grossValue: null },
  ],
  totalIncoming: 770,
}

const TITHE_SUMMARY: TitheSummaryDto = { calculatedTithe: 245, titheBalance: 245, carryForward: null }

describe('useMonthly', () => {
  beforeEach(() => {
    getExpensesByMonthMock.mockReset()
    getUnpaidCardChargesByMonthMock.mockReset()
    getCategoryTotalsByMonthMock.mockReset()
    getCardStatementsByMonthMock.mockReset()
    getBanksMock.mockReset()
    getIncomeSourcesMock.mockReset()
    getCategoriesMock.mockReset()
    deleteExpenseMock.mockReset()
    markCardStatementPaidMock.mockReset()
    unmarkCardStatementPaidMock.mockReset()
    getIncomesByMonthMock.mockReset()
    deleteIncomeMock.mockReset()
    getDefaultGroupSummaryMock.mockReset()
    getTitheSummaryByMonthMock.mockReset()
    updateTitheCarryForwardMock.mockReset()
    getExpensesByMonthMock.mockResolvedValue(EXPENSES)
    getUnpaidCardChargesByMonthMock.mockResolvedValue([])
    getCategoryTotalsByMonthMock.mockResolvedValue(CATEGORY_TOTALS)
    getCardStatementsByMonthMock.mockResolvedValue(CARD_STATEMENTS)
    getBanksMock.mockResolvedValue(BANKS)
    getIncomeSourcesMock.mockResolvedValue(INCOME_SOURCES)
    getCategoriesMock.mockResolvedValue(CATEGORIES)
    getIncomesByMonthMock.mockResolvedValue(INCOMES)
    getDefaultGroupSummaryMock.mockResolvedValue(SUMMARY)
    getTitheSummaryByMonthMock.mockResolvedValue(TITHE_SUMMARY)
  })

  it('fetches expenses, category totals, card statements, and banks for the current month on mount', async () => {
    const { result } = renderHook(() => useMonthly())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getExpensesByMonthMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(getCategoryTotalsByMonthMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(getCardStatementsByMonthMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(getBanksMock).toHaveBeenCalledOnce()
    expect(getIncomeSourcesMock).toHaveBeenCalledOnce()
    expect(getCategoriesMock).toHaveBeenCalledOnce()
    expect(result.current.monthInputValue).toBe(CURRENT_MONTH_INPUT)
    expect(result.current.expenses).toEqual(EXPENSES)
    expect(result.current.categoryTotals).toEqual(CATEGORY_TOTALS)
    expect(result.current.cardStatements).toEqual(CARD_STATEMENTS)
    expect(result.current.banks).toEqual(BANKS)
    expect(result.current.incomeSources).toEqual(INCOME_SOURCES)
    expect(result.current.categories).toEqual(CATEGORIES)
  })

  it('leaves the income source list empty when the fetch fails', async () => {
    getIncomeSourcesMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useMonthly())

    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(result.current.error).toBe('Network down')
    expect(result.current.incomeSources).toEqual([])
  })

  it('[AC P60-F07-credit-card-template-and-card-migration-09] reads the card total from the summary and makes no statement sum', async () => {
    getDefaultGroupSummaryMock.mockResolvedValue({
      ...SUMMARY,
      totals: [{ currency: 'GBP', balance: 2245.42, roundUpTotal: 0.7, cardOutstandingTotal: 64.1 }],
    })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.currencyTotals).toEqual([{ currency: 'GBP', balance: 2245.42, roundUpTotal: 0.7, cardOutstandingTotal: 64.1 }])
    expect(result.current).not.toHaveProperty('adjustmentTotal')
  })

  it('leaves card members out of the bank rows', async () => {
    getDefaultGroupSummaryMock.mockResolvedValue({
      ...SUMMARY,
      members: [
        ...SUMMARY.members,
        { accountId: 'card-baamex', name: 'BaAmex', templateName: 'CreditCard', currency: 'GBP', balance: -812.33, roundUpTotal: null, statement: { outstandingTotal: 64.1, isPaid: false }, balanceIsOwed: true },
      ],
    })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.bankTotals.map((row) => row.bank)).toEqual(['Barclays', 'Chase', 'Trading212'])
  })

  it('[AC P60-F04-default-account-group-and-summary-query-06] reads the summary figures from one summary request without summing in the browser', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getDefaultGroupSummaryMock).toHaveBeenCalledOnce()
    expect(getDefaultGroupSummaryMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(result.current.categoryTotalsSum).toBe(1234.56)
    expect(result.current.bankTotals).toEqual([
      { bankId: 'bank-barclays', bank: 'Barclays', currency: 'GBP', balance: 1875.32, roundUpTotal: 0 },
      { bankId: 'bank-chase', bank: 'Chase', currency: 'GBP', balance: -50, roundUpTotal: 0.1 },
      { bankId: 'bank-trading212', bank: 'Trading212', currency: 'GBP', balance: 420.1, roundUpTotal: 0.6 },
    ])
    expect(result.current.currencyTotals).toEqual(SUMMARY.totals)
    expect(result.current.incomeTotals).toEqual([
      { incomeSourceId: '2', name: 'Ariana', netValue: 720, grossValue: 820 },
      { incomeSourceId: '3', name: 'Lottery', netValue: 50, grossValue: null },
    ])
    expect(result.current.totalIncoming).toBe(770)
  })

  it('keys bank rows by the member account Id, not by matching a bank name', async () => {
    getDefaultGroupSummaryMock.mockResolvedValue({
      ...SUMMARY,
      members: [{ accountId: 'bank-new', name: 'Barclays', templateName: 'Bank', currency: 'GBP', balance: 10, roundUpTotal: 0, statement: null, balanceIsOwed: false }],
    })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.bankTotals).toEqual([{ bankId: 'bank-new', bank: 'Barclays', currency: 'GBP', balance: 10, roundUpTotal: 0 }])
  })

  it('shows a member without a round-up total as 0', async () => {
    getDefaultGroupSummaryMock.mockResolvedValue({
      ...SUMMARY,
      members: [{ accountId: 'bank-barclays', name: 'Barclays', templateName: 'Bank', currency: 'GBP', balance: 10, roundUpTotal: null, statement: null, balanceIsOwed: false }],
    })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.bankTotals[0].roundUpTotal).toBe(0)
  })

  it('exposes empty summary figures before the summary has loaded', () => {
    getDefaultGroupSummaryMock.mockReturnValue(new Promise(() => {}))
    const { result } = renderHook(() => useMonthly())

    expect(result.current.bankTotals).toEqual([])
    expect(result.current.currencyTotals).toEqual([])
    expect(result.current.incomeTotals).toEqual([])
    expect(result.current.categoryTotalsSum).toBe(0)
    expect(result.current.totalIncoming).toBe(0)
  })

  it('re-fetches for a new month when the month input changes', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setMonthInputValue(NEXT_MONTH_INPUT))

    await waitFor(() => expect(getExpensesByMonthMock).toHaveBeenCalledWith(NEXT_MONTH_YEAR, NEXT_MONTH))
  })

  it('deletes an expense and re-fetches', async () => {
    deleteExpenseMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteExpense('e1'))

    await waitFor(() => expect(deleteExpenseMock).toHaveBeenCalledWith('e1'))
    await waitFor(() => expect(getExpensesByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('marks a card statement paid with the selected bank and re-fetches', async () => {
    markCardStatementPaidMock.mockResolvedValue({ ...CARD_STATEMENTS[0], isPaid: true, outstandingTotal: 0 })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.markStatementPaid('c1', 'bank-trading212'))

    await waitFor(() =>
      expect(markCardStatementPaidMock).toHaveBeenCalledWith('c1', { paymentSourceBankId: 'bank-trading212' }),
    )
    await waitFor(() => expect(getCardStatementsByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('shows the server message when a mark-paid is refused', async () => {
    markCardStatementPaidMock.mockRejectedValue(new Error('This statement is already paid.'))
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.markStatementPaid('c1', 'bank-trading212'))

    await waitFor(() => expect(result.current.listActionError).toBe('This statement is already paid.'))
  })

  it('shows the server message when an unmark is refused', async () => {
    unmarkCardStatementPaidMock.mockRejectedValue(new Error('This statement is not paid.'))
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.unmarkStatementPaid('c2'))

    await waitFor(() => expect(result.current.listActionError).toBe('This statement is not paid.'))
  })

  it('clears the previous action error once a mark-paid succeeds', async () => {
    markCardStatementPaidMock.mockRejectedValueOnce(new Error('There is nothing to pay on this statement.'))
    markCardStatementPaidMock.mockResolvedValueOnce({ ...CARD_STATEMENTS[0], isPaid: true, outstandingTotal: 0 })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.markStatementPaid('c1', 'bank-trading212'))
    await waitFor(() => expect(result.current.listActionError).not.toBeNull())
    act(() => result.current.markStatementPaid('c1', 'bank-trading212'))

    await waitFor(() => expect(result.current.listActionError).toBeNull())
  })

  it('tracks the selected paying bank per statement', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setMarkPaidSource('c1', 'bank-chase'))

    expect(result.current.markPaidSources).toEqual({ c1: 'bank-chase' })
  })

  it('unmarks a paid statement and re-fetches', async () => {
    unmarkCardStatementPaidMock.mockResolvedValue({ ...CARD_STATEMENTS[1], isPaid: false, outstandingTotal: 45 })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.unmarkStatementPaid('c2'))

    await waitFor(() => expect(unmarkCardStatementPaidMock).toHaveBeenCalledWith('c2'))
    await waitFor(() => expect(getCardStatementsByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('fetches the tithe summary for the current month and exposes it', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getTitheSummaryByMonthMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(result.current.titheSummary).toEqual(TITHE_SUMMARY)
  })

  it('toggles carry-forward inclusion and re-fetches the tithe summary', async () => {
    updateTitheCarryForwardMock.mockResolvedValue({
      calculatedTithe: 245,
      titheBalance: 195,
      carryForward: { amount: 50, included: false, fromYear: CURRENT_YEAR, fromMonth: CURRENT_MONTH },
    })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.updateCarryForwardInclusion(false))

    await waitFor(() =>
      expect(updateTitheCarryForwardMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH, { included: false }),
    )
    await waitFor(() => expect(getTitheSummaryByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('sets carryForwardUpdating while the toggle request is in flight, then clears it', async () => {
    let resolveUpdate!: (value: TitheSummaryDto) => void
    updateTitheCarryForwardMock.mockReturnValue(
      new Promise((resolve) => {
        resolveUpdate = resolve
      }),
    )
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.updateCarryForwardInclusion(false))
    expect(result.current.carryForwardUpdating).toBe(true)

    await act(async () => {
      resolveUpdate({ calculatedTithe: 245, titheBalance: 195, carryForward: null })
    })

    await waitFor(() => expect(result.current.carryForwardUpdating).toBe(false))
  })

  it('exposes a failed carry-forward toggle as an action error and clears the busy flag', async () => {
    updateTitheCarryForwardMock.mockRejectedValue(new Error('No carry-forward is available for this month.'))
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.updateCarryForwardInclusion(false))

    await waitFor(() => expect(result.current.listActionError).toBe('No carry-forward is available for this month.'))
    expect(result.current.carryForwardUpdating).toBe(false)
  })

  it('deletes an income entry and re-fetches', async () => {
    deleteIncomeMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteIncome('i1'))

    await waitFor(() => expect(deleteIncomeMock).toHaveBeenCalledWith('i1'))
    await waitFor(() => expect(getIncomesByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('refetches the summary after a silent refresh', async () => {
    deleteIncomeMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteIncome('i1'))

    await waitFor(() => expect(getDefaultGroupSummaryMock).toHaveBeenCalledTimes(2))
  })
})
