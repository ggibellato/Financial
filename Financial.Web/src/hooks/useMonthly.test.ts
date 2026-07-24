import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { BankDto, CardStatementDto, CategoryTotalDto, ExpenseDto, IncomeDto } from '../api/types'
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
const getBanksMock = vi.fn<FinancialApiClient['getBanks']>()
const createExpenseMock = vi.fn<FinancialApiClient['createExpense']>()
const updateExpenseMock = vi.fn<FinancialApiClient['updateExpense']>()
const deleteExpenseMock = vi.fn<FinancialApiClient['deleteExpense']>()
const markCardStatementPaidMock = vi.fn<FinancialApiClient['markCardStatementPaid']>()
const unmarkCardStatementPaidMock = vi.fn<FinancialApiClient['unmarkCardStatementPaid']>()
const getIncomesByMonthMock = vi.fn<FinancialApiClient['getIncomesByMonth']>()
const createIncomeMock = vi.fn<FinancialApiClient['createIncome']>()
const updateIncomeMock = vi.fn<FinancialApiClient['updateIncome']>()
const deleteIncomeMock = vi.fn<FinancialApiClient['deleteIncome']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getExpensesByMonth: getExpensesByMonthMock,
    getCategoryTotalsByMonth: getCategoryTotalsByMonthMock,
    getCardStatementsByMonth: getCardStatementsByMonthMock,
    getBanks: getBanksMock,
    createExpense: createExpenseMock,
    updateExpense: updateExpenseMock,
    deleteExpense: deleteExpenseMock,
    markCardStatementPaid: markCardStatementPaidMock,
    unmarkCardStatementPaid: unmarkCardStatementPaidMock,
    getIncomesByMonth: getIncomesByMonthMock,
    createIncome: createIncomeMock,
    updateIncome: updateIncomeMock,
    deleteIncome: deleteIncomeMock,
  }),
}))

const BANKS: BankDto[] = [
  { name: 'Barclays', roundUpEnabled: false },
  { name: 'Trading212', roundUpEnabled: true },
  { name: 'Chase', roundUpEnabled: true },
]

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
    roundUpAmount: null,
    suggestedRoundUpAmount: null,
  },
]

const CATEGORY_TOTALS: CategoryTotalDto[] = [{ category: 'Mercado', totalValue: 42.5 }]

const CARD_STATEMENTS: CardStatementDto[] = [
  { id: 'c1', card: 'BaAmex', year: CURRENT_YEAR, month: CURRENT_MONTH, isPaid: false, outstandingTotal: 100 },
  { id: 'c2', card: 'ChaseMaster4023', year: CURRENT_YEAR, month: CURRENT_MONTH, isPaid: true, outstandingTotal: 0 },
]

const INCOMES: IncomeDto[] = [
  {
    id: 'i1',
    date: `${CURRENT_YEAR}-${String(CURRENT_MONTH).padStart(2, '0')}-01`,
    incomeSource: 'Gleison',
    grossValue: 3200,
    netValue: 2450,
    bank: 'Barclays',
  },
]

describe('useMonthly', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getExpensesByMonthMock.mockResolvedValue(EXPENSES)
    getCategoryTotalsByMonthMock.mockResolvedValue(CATEGORY_TOTALS)
    getCardStatementsByMonthMock.mockResolvedValue(CARD_STATEMENTS)
    getBanksMock.mockResolvedValue(BANKS)
    getIncomesByMonthMock.mockResolvedValue(INCOMES)
    vi.spyOn(window, 'confirm').mockReturnValue(true)
  })

  it('fetches expenses, category totals, card statements, and banks for the current month on mount', async () => {
    const { result } = renderHook(() => useMonthly())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getExpensesByMonthMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(getCategoryTotalsByMonthMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(getCardStatementsByMonthMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(getBanksMock).toHaveBeenCalledOnce()
    expect(result.current.monthInputValue).toBe(CURRENT_MONTH_INPUT)
    expect(result.current.expenses).toEqual(EXPENSES)
    expect(result.current.categoryTotals).toEqual(CATEGORY_TOTALS)
    expect(result.current.cardStatements).toEqual(CARD_STATEMENTS)
    expect(result.current.banks).toEqual(BANKS)
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
      { bank: 'Barclays', balance: 42.5, roundUpTotal: 0 },
      { bank: 'Trading212', balance: 0, roundUpTotal: 0 },
      { bank: 'Chase', balance: 0, roundUpTotal: 0 },
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

  it('marks a card statement paid with the selected bank and re-fetches', async () => {
    markCardStatementPaidMock.mockResolvedValue({ ...CARD_STATEMENTS[0], isPaid: true, outstandingTotal: 0 })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.markStatementPaid('c1', 'Trading212'))

    await waitFor(() => expect(markCardStatementPaidMock).toHaveBeenCalledWith('c1', { paymentSource: 'Trading212' }))
    await waitFor(() => expect(getCardStatementsByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('tracks the selected paying bank per statement', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setMarkPaidSource('c1', 'Chase'))

    expect(result.current.markPaidSources).toEqual({ c1: 'Chase' })
  })

  it('unmarks a paid statement after confirmation and re-fetches', async () => {
    unmarkCardStatementPaidMock.mockResolvedValue({ ...CARD_STATEMENTS[1], isPaid: false, outstandingTotal: 45 })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.unmarkStatementPaid('c2'))

    await waitFor(() => expect(unmarkCardStatementPaidMock).toHaveBeenCalledWith('c2'))
    await waitFor(() => expect(getCardStatementsByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('does not unmark when the user cancels the confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.unmarkStatementPaid('c2'))

    expect(unmarkCardStatementPaidMock).not.toHaveBeenCalled()
  })

  it('creates in bank mode with a null card tag by default', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'Waitrose'))
    act(() => result.current.setCreateField('createValue', '15.5'))
    act(() => result.current.submitCreate())

    expect(result.current.createPaymentMode).toBe('bank')
    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(
        expect.objectContaining({ paymentSource: 'Barclays', cardTag: null }),
      ),
    )
  })

  it('creates in card mode with a null payment source', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'Amazon'))
    act(() => result.current.setCreateField('createValue', '9.99'))
    act(() => result.current.setCreatePaymentMode('card'))
    act(() => result.current.setCreateField('createCardTag', 'ChaseMaster4023'))
    act(() => result.current.submitCreate())

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(
        expect.objectContaining({ paymentSource: null, cardTag: 'ChaseMaster4023' }),
      ),
    )
  })

  it('rejects card-mode create without a card before calling the API', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'Amazon'))
    act(() => result.current.setCreateField('createValue', '9.99'))
    act(() => result.current.setCreatePaymentMode('card'))
    act(() => result.current.submitCreate())

    expect(result.current.createError).toBe('Card is required')
    expect(createExpenseMock).not.toHaveBeenCalled()
  })

  it('switching create mode clears the field made irrelevant by the switch', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreatePaymentMode('card'))
    act(() => result.current.setCreateField('createCardTag', 'BaAmex'))
    act(() => result.current.setCreatePaymentMode('bank'))

    expect(result.current.createCardTag).toBe('')
    expect(result.current.createPaymentSource).toBe('Barclays')

    act(() => result.current.setCreatePaymentMode('card'))
    expect(result.current.createPaymentSource).toBe('')
  })

  it('opens edit in card mode for a credit card charge', async () => {
    const charge: ExpenseDto = {
      ...EXPENSES[0],
      id: 'e3',
      paymentSource: null,
      cardTag: 'BaAmex',
      paymentStatus: 'CreditCardCharge',
    }
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(charge))

    expect(result.current.editPaymentMode).toBe('card')
    expect(result.current.editIsSettled).toBe(false)
  })

  it('saves a settled expense with its payment fields unchanged', async () => {
    const settled: ExpenseDto = {
      ...EXPENSES[0],
      id: 'e4',
      paymentSource: 'Trading212',
      cardTag: 'BaAmex',
      settledAt: '2026-07-20',
      paymentStatus: 'CreditCardSettled',
    }
    updateExpenseMock.mockResolvedValue(settled)
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(settled))
    expect(result.current.editIsSettled).toBe(true)

    act(() => result.current.setEditField('editDescription', 'Renamed'))
    act(() => result.current.saveEdit())

    await waitFor(() =>
      expect(updateExpenseMock).toHaveBeenCalledWith(
        'e4',
        expect.objectContaining({ description: 'Renamed', paymentSource: 'Trading212', cardTag: 'BaAmex' }),
      ),
    )
  })

  it('bank totals count immediate and settled expenses per bank and exclude charges', async () => {
    getExpensesByMonthMock.mockResolvedValue([
      EXPENSES[0],
      {
        ...EXPENSES[0],
        id: 'e5',
        value: 20,
        paymentSource: 'Barclays',
        cardTag: 'BaAmex',
        settledAt: '2026-07-20',
        paymentStatus: 'CreditCardSettled',
      },
      {
        ...EXPENSES[0],
        id: 'e6',
        value: 99,
        paymentSource: null,
        cardTag: 'ChaseMaster4023',
        paymentStatus: 'CreditCardCharge',
      },
    ])
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.bankTotals).toEqual([
      { bank: 'Barclays', balance: 62.5, roundUpTotal: 0 },
      { bank: 'Trading212', balance: 0, roundUpTotal: 0 },
      { bank: 'Chase', balance: 0, roundUpTotal: 0 },
    ])
    expect(result.current.bankTotalsSum).toBe(62.5)
    expect(result.current.roundUpTotalsSum).toBe(0)
  })

  it("subtracts a bank's round-up total from its balance and sums round-up totals separately", async () => {
    getExpensesByMonthMock.mockResolvedValue([
      { ...EXPENSES[0], id: 'e7', value: 9.4, paymentSource: 'Trading212', roundUpAmount: 0.6 },
      { ...EXPENSES[0], id: 'e8', value: 5, paymentSource: 'Trading212', roundUpAmount: null },
      { ...EXPENSES[0], id: 'e9', value: 20, paymentSource: 'Chase', roundUpAmount: 0.1 },
    ])
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.bankTotals).toEqual([
      { bank: 'Barclays', balance: 0, roundUpTotal: 0 },
      { bank: 'Trading212', balance: 13.8, roundUpTotal: 0.6 },
      { bank: 'Chase', balance: 19.9, roundUpTotal: 0.1 },
    ])
    expect(result.current.bankTotalsSum).toBeCloseTo(33.7)
    expect(result.current.roundUpTotalsSum).toBeCloseTo(0.7)
  })

  it('picking a round-up-enabled bank auto-suggests when the field is blank', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreateField('createValue', '9.40'))
    act(() => result.current.setCreateField('createPaymentSource', 'Trading212'))

    expect(result.current.createRoundUpAmount).toBe('0.60')
  })

  it('picking a round-up-enabled bank does not overwrite an amount the user already typed', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreateField('createValue', '9.40'))
    act(() => result.current.setCreateField('createRoundUpAmount', '0.10'))
    act(() => result.current.setCreateField('createPaymentSource', 'Chase'))

    expect(result.current.createRoundUpAmount).toBe('0.10')
  })

  it('picking a non-round-up bank does not fill a suggestion', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreateField('createValue', '9.40'))
    act(() => result.current.setCreateField('createPaymentSource', 'Barclays'))

    expect(result.current.createRoundUpAmount).toBe('')
  })

  it('sends the round-up amount on create for a round-up-enabled bank', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'TfL'))
    act(() => result.current.setCreateField('createValue', '9.40'))
    act(() => result.current.setCreateField('createPaymentSource', 'Trading212'))
    act(() => result.current.submitCreate())

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(expect.objectContaining({ roundUpAmount: 0.6 })),
    )
  })

  it('sends a null round-up amount when charging to card', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'Amazon'))
    act(() => result.current.setCreateField('createValue', '9.99'))
    act(() => result.current.setCreatePaymentMode('card'))
    act(() => result.current.setCreateField('createCardTag', 'ChaseMaster4023'))
    act(() => result.current.submitCreate())

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(expect.objectContaining({ roundUpAmount: null })),
    )
  })

  it('rejects a round-up amount outside £0.00-£0.99 before calling the API', async () => {
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'TfL'))
    act(() => result.current.setCreateField('createValue', '9.40'))
    act(() => result.current.setCreateField('createPaymentSource', 'Trading212'))
    act(() => result.current.setCreateField('createRoundUpAmount', '1.50'))
    act(() => result.current.submitCreate())

    expect(result.current.createError).toContain('between £0.00 and £0.99')
    expect(createExpenseMock).not.toHaveBeenCalled()
  })

  it('pre-fills the edit round-up field from the saved amount, not the suggestion', async () => {
    const expense: ExpenseDto = {
      ...EXPENSES[0],
      id: 'e7',
      value: 9.4,
      paymentSource: 'Trading212',
      roundUpAmount: 0.1,
      suggestedRoundUpAmount: null,
    }
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(expense))

    expect(result.current.editRoundUpAmount).toBe('0.1')
  })

  it('leaves a saved round-up amount unchanged when only Value is edited', async () => {
    const expense: ExpenseDto = {
      ...EXPENSES[0],
      id: 'e8',
      value: 9.4,
      paymentSource: 'Trading212',
      roundUpAmount: 0.1,
      suggestedRoundUpAmount: null,
    }
    updateExpenseMock.mockResolvedValue(expense)
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(expense))
    act(() => result.current.setEditField('editValue', '20'))
    act(() => result.current.saveEdit())

    await waitFor(() =>
      expect(updateExpenseMock).toHaveBeenCalledWith('e8', expect.objectContaining({ value: 20, roundUpAmount: 0.1 })),
    )
  })

  it('clears a saved round-up amount when the edit field is emptied', async () => {
    const expense: ExpenseDto = {
      ...EXPENSES[0],
      id: 'e9',
      paymentSource: 'Trading212',
      roundUpAmount: 0.1,
      suggestedRoundUpAmount: null,
    }
    updateExpenseMock.mockResolvedValue(expense)
    const { result } = renderHook(() => useMonthly())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(expense))
    act(() => result.current.setEditField('editRoundUpAmount', ''))
    act(() => result.current.saveEdit())

    await waitFor(() =>
      expect(updateExpenseMock).toHaveBeenCalledWith('e9', expect.objectContaining({ roundUpAmount: null })),
    )
  })
})
