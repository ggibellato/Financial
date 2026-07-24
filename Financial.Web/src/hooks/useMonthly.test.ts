import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { CardStatementDto, CategoryTotalDto, ExpenseDto } from '../api/types'
import { useMonthly } from './useMonthly'

const NOW = new Date()
const CURRENT_YEAR = NOW.getFullYear()
const CURRENT_MONTH = NOW.getMonth() + 1
const CURRENT_MONTH_INPUT = `${CURRENT_YEAR}-${String(CURRENT_MONTH).padStart(2, '0')}`
const NEXT_MONTH = CURRENT_MONTH === 12 ? 1 : CURRENT_MONTH + 1
const NEXT_MONTH_YEAR = CURRENT_MONTH === 12 ? CURRENT_YEAR + 1 : CURRENT_YEAR
const NEXT_MONTH_INPUT = `${NEXT_MONTH_YEAR}-${String(NEXT_MONTH).padStart(2, '0')}`

const getExpensesByMonthMock = vi.fn<FinancialApiClient['getExpensesByMonth']>()
const getCategoryTotalsByMonthMock = vi.fn<FinancialApiClient['getCategoryTotalsByMonth']>()
const getCardStatementsByMonthMock = vi.fn<FinancialApiClient['getCardStatementsByMonth']>()
const createExpenseMock = vi.fn<FinancialApiClient['createExpense']>()
const updateExpenseMock = vi.fn<FinancialApiClient['updateExpense']>()
const deleteExpenseMock = vi.fn<FinancialApiClient['deleteExpense']>()
const markCardStatementPaidMock = vi.fn<FinancialApiClient['markCardStatementPaid']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getExpensesByMonth: getExpensesByMonthMock,
    getCategoryTotalsByMonth: getCategoryTotalsByMonthMock,
    getCardStatementsByMonth: getCardStatementsByMonthMock,
    createExpense: createExpenseMock,
    updateExpense: updateExpenseMock,
    deleteExpense: deleteExpenseMock,
    markCardStatementPaid: markCardStatementPaidMock,
  }),
}))

const EXPENSES: ExpenseDto[] = [
  {
    id: 'e1',
    date: `${CURRENT_YEAR}-${String(CURRENT_MONTH).padStart(2, '0')}-05`,
    description: 'Lidl',
    value: 42.5,
    category: 'Mercado',
    paymentSource: 'Barclays',
    cardTag: null,
    settledAt: null,
    paymentStatus: 'ImmediatePayment',
  },
]

const CATEGORY_TOTALS: CategoryTotalDto[] = [{ category: 'Mercado', totalValue: 42.5 }]

const CARD_STATEMENTS: CardStatementDto[] = [
  { id: 'c1', card: 'BaAmex', year: CURRENT_YEAR, month: CURRENT_MONTH, isPaid: false, outstandingTotal: 100 },
  { id: 'c2', card: 'ChaseMaster4023', year: CURRENT_YEAR, month: CURRENT_MONTH, isPaid: true, outstandingTotal: 0 },
]

describe('useMonthly', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getExpensesByMonthMock.mockResolvedValue(EXPENSES)
    getCategoryTotalsByMonthMock.mockResolvedValue(CATEGORY_TOTALS)
    getCardStatementsByMonthMock.mockResolvedValue(CARD_STATEMENTS)
    vi.spyOn(window, 'confirm').mockReturnValue(true)
  })

  it('fetches expenses, category totals, and card statements for the current month on mount', async () => {
    const { result } = renderHook(() => useMonthly())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getExpensesByMonthMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(getCategoryTotalsByMonthMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(getCardStatementsByMonthMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(result.current.monthInputValue).toBe(CURRENT_MONTH_INPUT)
    expect(result.current.expenses).toEqual(EXPENSES)
    expect(result.current.categoryTotals).toEqual(CATEGORY_TOTALS)
    expect(result.current.cardStatements).toEqual(CARD_STATEMENTS)
  })

  it('computes the combined adjustment figure as the sum of outstanding totals', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.adjustmentTotal).toBe(100)
  })

  it('computes category totals sum and per-bank totals from the fetched expenses', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.categoryTotalsSum).toBe(42.5)
    expect(result.current.bankTotals).toEqual([
      { bank: 'Barclays', totalValue: 42.5 },
      { bank: 'Trading212', totalValue: 0 },
      { bank: 'Chase', totalValue: 0 },
    ])
    expect(result.current.bankTotalsSum).toBe(42.5)
  })

  it('re-fetches for a new month when the month input changes', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setMonthInputValue(NEXT_MONTH_INPUT))

    await waitFor(() => expect(getExpensesByMonthMock).toHaveBeenCalledWith(NEXT_MONTH_YEAR, NEXT_MONTH))
  })

  it('creates an expense and re-fetches on success', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreateField('createDate', `${CURRENT_YEAR}-07-16`))
    act(() => result.current.setCreateField('createDescription', 'Waitrose'))
    act(() => result.current.setCreateField('createValue', '15.5'))
    act(() => result.current.submitCreate())

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(
        expect.objectContaining({ description: 'Waitrose', value: 15.5, cardTag: null }),
      ),
    )
    await waitFor(() => expect(getExpensesByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a backend validation error on create failure without crashing', async () => {
    createExpenseMock.mockRejectedValue(new Error('Unrecognized category.'))
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreateField('createDate', `${CURRENT_YEAR}-07-16`))
    act(() => result.current.setCreateField('createDescription', 'Waitrose'))
    act(() => result.current.setCreateField('createValue', '15.5'))
    act(() => result.current.submitCreate())

    await waitFor(() => expect(result.current.createError).toBe('Unrecognized category.'))
  })

  it('saves an edit and re-fetches on success', async () => {
    updateExpenseMock.mockResolvedValue({ ...EXPENSES[0], value: 50 })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(EXPENSES[0]))
    act(() => result.current.setEditField('editValue', '50'))
    act(() => result.current.saveEdit())

    await waitFor(() =>
      expect(updateExpenseMock).toHaveBeenCalledWith(
        'e1',
        expect.objectContaining({ description: 'Lidl', value: 50, category: 'Mercado' }),
      ),
    )
    await waitFor(() => expect(getExpensesByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('deletes an expense after confirmation and re-fetches', async () => {
    deleteExpenseMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteExpense('e1'))

    await waitFor(() => expect(deleteExpenseMock).toHaveBeenCalledWith('e1'))
    await waitFor(() => expect(getExpensesByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('does not delete when the user cancels the confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteExpense('e1'))

    expect(deleteExpenseMock).not.toHaveBeenCalled()
  })

  it('marks a card statement paid and re-fetches', async () => {
    markCardStatementPaidMock.mockResolvedValue({ ...CARD_STATEMENTS[0], isPaid: true, outstandingTotal: 0 })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.markStatementPaid('c1'))

    await waitFor(() => expect(markCardStatementPaidMock).toHaveBeenCalledWith('c1'))
    await waitFor(() => expect(getCardStatementsByMonthMock).toHaveBeenCalledTimes(2))
  })
})
