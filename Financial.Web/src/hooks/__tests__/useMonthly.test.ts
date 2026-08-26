import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type {
  BankBalanceDto,
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
  getBankBalancesByMonthMock,
  getTitheSummaryByMonthMock,
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
  getBankBalancesByMonthMock: vi.fn<FinancialApiClient['getBankBalancesByMonth']>(),
  getTitheSummaryByMonthMock: vi.fn<FinancialApiClient['getTitheSummaryByMonth']>(),
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
    getBankBalancesByMonth: getBankBalancesByMonthMock,
    getTitheSummaryByMonth: getTitheSummaryByMonthMock,
  } as Partial<FinancialApiClient>,
}))

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01' },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01' },
  { id: 'bank-chase', name: 'Chase', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01' },
]

const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: '1', name: 'Gleison', isActive: true, group: 'Salary', autoSplitToReserve: false },
  { id: '2', name: 'Ariana', isActive: true, group: 'Salary', autoSplitToReserve: true },
  { id: '3', name: 'Lottery', isActive: true, group: 'NonReportable', autoSplitToReserve: false },
  { id: '4', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros', autoSplitToReserve: false },
]

const CATEGORIES: CategoryDto[] = [
  { id: 'category-mercado', name: 'Mercado', active: true, isInvestment: false, isTithe: false },
  { id: 'category-extras', name: 'Extras', active: true, isInvestment: false, isTithe: false },
  { id: 'category-reserva', name: 'Reserva', active: false, isInvestment: false, isTithe: false },
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
  { id: 'c1', creditCardId: 'card-baamex', creditCardName: 'BaAmex', year: CURRENT_YEAR, month: CURRENT_MONTH, isPaid: false, outstandingTotal: 100, warning: null },
  { id: 'c2', creditCardId: 'card-chase', creditCardName: 'ChaseMaster4023', year: CURRENT_YEAR, month: CURRENT_MONTH, isPaid: true, outstandingTotal: 0, warning: null },
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

const BANK_BALANCES: BankBalanceDto[] = [
  { bank: 'Barclays', balance: 42.5 },
  { bank: 'Trading212', balance: 0 },
  { bank: 'Chase', balance: 0 },
]

const TITHE_SUMMARY: TitheSummaryDto = { calculatedTithe: 245, titheBalance: 245 }

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
    getBankBalancesByMonthMock.mockReset()
    getTitheSummaryByMonthMock.mockReset()
    getExpensesByMonthMock.mockResolvedValue(EXPENSES)
    getUnpaidCardChargesByMonthMock.mockResolvedValue([])
    getCategoryTotalsByMonthMock.mockResolvedValue(CATEGORY_TOTALS)
    getCardStatementsByMonthMock.mockResolvedValue(CARD_STATEMENTS)
    getBanksMock.mockResolvedValue(BANKS)
    getIncomeSourcesMock.mockResolvedValue(INCOME_SOURCES)
    getCategoriesMock.mockResolvedValue(CATEGORIES)
    getIncomesByMonthMock.mockResolvedValue(INCOMES)
    getBankBalancesByMonthMock.mockResolvedValue(BANK_BALANCES)
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

  it('computes the combined adjustment figure as the sum of outstanding totals', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.adjustmentTotal).toBe(100)
  })

  it('computes category totals sum and per-bank totals from the fetched banks and expenses', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.categoryTotalsSum).toBe(42.5)
    expect(result.current.bankTotals).toEqual([
      { bankId: 'bank-barclays', bank: 'Barclays', balance: 42.5, roundUpTotal: 0 },
      { bankId: 'bank-trading212', bank: 'Trading212', balance: 0, roundUpTotal: 0 },
      { bankId: 'bank-chase', bank: 'Chase', balance: 0, roundUpTotal: 0 },
    ])
    expect(result.current.bankTotalsSum).toBe(42.5)
    expect(result.current.roundUpTotalsSum).toBe(0)
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

  it('surfaces a warning the server returned for a mark-paid that changed nothing', async () => {
    markCardStatementPaidMock.mockResolvedValue({
      ...CARD_STATEMENTS[0],
      isPaid: true,
      warning: 'This statement was already marked paid; nothing changed.',
    })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.markStatementPaid('c1', 'bank-trading212'))

    await waitFor(() =>
      expect(result.current.listActionWarning).toBe('This statement was already marked paid; nothing changed.'),
    )
  })

  it('reports no warning for a mark-paid that did change something', async () => {
    markCardStatementPaidMock.mockResolvedValue({ ...CARD_STATEMENTS[0], isPaid: true, outstandingTotal: 0 })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.markStatementPaid('c1', 'bank-trading212'))

    await waitFor(() => expect(getCardStatementsByMonthMock).toHaveBeenCalledTimes(2))
    expect(result.current.listActionWarning).toBeNull()
  })

  it('surfaces a warning the server returned for an unmark that changed nothing', async () => {
    unmarkCardStatementPaidMock.mockResolvedValue({
      ...CARD_STATEMENTS[1],
      isPaid: false,
      warning: 'This statement was not marked paid; nothing changed.',
    })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.unmarkStatementPaid('c2'))

    await waitFor(() =>
      expect(result.current.listActionWarning).toBe('This statement was not marked paid; nothing changed.'),
    )
  })

  it('exposes a failed mark-paid as an action error', async () => {
    markCardStatementPaidMock.mockRejectedValue(new Error('boom'))
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.markStatementPaid('c1', 'bank-trading212'))

    await waitFor(() => expect(result.current.listActionError).not.toBeNull())
    expect(result.current.listActionWarning).toBeNull()
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

  it("sources each bank's balance from the fetched bank-balances data, not from summing the month's expenses", async () => {
    getBankBalancesByMonthMock.mockResolvedValue([
      { bank: 'Barclays', balance: 1875.32 },
      { bank: 'Trading212', balance: 420.1 },
      { bank: 'Chase', balance: -50 },
    ])
    getExpensesByMonthMock.mockResolvedValue([
      { ...EXPENSES[0], value: 999999, paymentSourceBankId: 'bank-barclays', paymentSourceBankName: 'Barclays' },
    ])
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getBankBalancesByMonthMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(result.current.bankTotals).toEqual([
      { bankId: 'bank-barclays', bank: 'Barclays', balance: 1875.32, roundUpTotal: 0 },
      { bankId: 'bank-trading212', bank: 'Trading212', balance: 420.1, roundUpTotal: 0 },
      { bankId: 'bank-chase', bank: 'Chase', balance: -50, roundUpTotal: 0 },
    ])
    expect(result.current.bankTotalsSum).toBeCloseTo(2245.42)
  })

  it("sums each bank's round-up total from the month's expenses independent of the fetched balance", async () => {
    getBankBalancesByMonthMock.mockResolvedValue([
      { bank: 'Barclays', balance: 0 },
      { bank: 'Trading212', balance: 100 },
      { bank: 'Chase', balance: 200 },
    ])
    getExpensesByMonthMock.mockResolvedValue([
      {
        ...EXPENSES[0],
        id: 'e7',
        value: 9.4,
        paymentSourceBankId: 'bank-trading212',
        paymentSourceBankName: 'Trading212',
        roundUpAmount: 0.6,
      },
      {
        ...EXPENSES[0],
        id: 'e8',
        value: 5,
        paymentSourceBankId: 'bank-trading212',
        paymentSourceBankName: 'Trading212',
        roundUpAmount: null,
      },
      {
        ...EXPENSES[0],
        id: 'e9',
        value: 20,
        paymentSourceBankId: 'bank-chase',
        paymentSourceBankName: 'Chase',
        roundUpAmount: 0.1,
      },
    ])
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.bankTotals).toEqual([
      { bankId: 'bank-barclays', bank: 'Barclays', balance: 0, roundUpTotal: 0 },
      { bankId: 'bank-trading212', bank: 'Trading212', balance: 100, roundUpTotal: 0.6 },
      { bankId: 'bank-chase', bank: 'Chase', balance: 200, roundUpTotal: 0.1 },
    ])
    expect(result.current.roundUpTotalsSum).toBeCloseTo(0.7)
  })

  it('fetches the tithe summary for the current month and exposes it', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getTitheSummaryByMonthMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(result.current.titheSummary).toEqual(TITHE_SUMMARY)
  })

  it('deletes an income entry and re-fetches', async () => {
    deleteIncomeMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteIncome('i1'))

    await waitFor(() => expect(deleteIncomeMock).toHaveBeenCalledWith('i1'))
    await waitFor(() => expect(getIncomesByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('groups income totals by source, summing net values and gross values only when present', async () => {
    getIncomesByMonthMock.mockResolvedValue([
      {
        id: 'i1',
        date: `${CURRENT_YEAR}-07-01`,
        incomeSourceId: '2',
        incomeSourceName: 'Ariana',
        grossValue: 400,
        netValue: 350,
        bankId: 'bank-chase',
        bankName: 'Chase',
        description: null,
        splitToReserve: false,
      },
      {
        id: 'i2',
        date: `${CURRENT_YEAR}-07-08`,
        incomeSourceId: '2',
        incomeSourceName: 'Ariana',
        grossValue: 420,
        netValue: 370,
        bankId: 'bank-chase',
        bankName: 'Chase',
        description: null,
        splitToReserve: false,
      },
      {
        id: 'i3',
        date: `${CURRENT_YEAR}-07-10`,
        incomeSourceId: '3',
        incomeSourceName: 'Lottery',
        grossValue: null,
        netValue: 50,
        bankId: 'bank-chase',
        bankName: 'Chase',
        description: null,
        splitToReserve: false,
      },
    ])
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.incomeTotals).toEqual(
      expect.arrayContaining([
        { source: 'Ariana', netValue: 720, grossValue: 820 },
        { source: 'Lottery', netValue: 50, grossValue: null },
      ]),
    )
    expect(result.current.incomeTotals).toHaveLength(2)
    expect(result.current.totalIncoming).toBe(770)
  })
})
