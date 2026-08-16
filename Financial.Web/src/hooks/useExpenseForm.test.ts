import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { BankDto, CategoryDto, ExpenseDto } from '../api/types'
import { useExpenseForm } from './useExpenseForm'

const createExpenseMock = vi.fn<FinancialApiClient['createExpense']>()
const updateExpenseMock = vi.fn<FinancialApiClient['updateExpense']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    createExpense: createExpenseMock,
    updateExpense: updateExpenseMock,
  }),
}))

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true },
  { id: 'bank-chase', name: 'Chase', roundUpEnabled: true },
]

const CATEGORIES: CategoryDto[] = [
  { id: 'category-mercado', name: 'Mercado', active: true, isInvestment: false, isTithe: false },
  { id: 'category-extras', name: 'Extras', active: true, isInvestment: false, isTithe: false },
  { id: 'category-reserva', name: 'Reserva', active: false, isInvestment: false, isTithe: false },
  { id: 'category-dizimo', name: 'Dizimo', active: true, isInvestment: false, isTithe: true },
]

const EXPENSE: ExpenseDto = {
  id: 'e1',
  date: '2026-07-05',
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
}

describe('useExpenseForm', () => {
  let onSaved: () => void

  beforeEach(() => {
    createExpenseMock.mockReset()
    updateExpenseMock.mockReset()
    onSaved = vi.fn<() => void>()
  })

  it('defaults the new-expense category field to the first active category once the form opens', () => {
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showCreateForm('bank'))

    expect(result.current.createCategoryId).toBe('category-mercado')
  })

  it('creates an expense and calls onSaved on success', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSE, id: 'e2' })
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'Waitrose'))
    act(() => result.current.setCreateField('createValue', '15.5'))
    act(() => result.current.setCreateField('createCategoryId', 'category-mercado'))
    await act(() => result.current.submitCreate())

    expect(createExpenseMock).toHaveBeenCalledWith(
      expect.objectContaining({ description: 'Waitrose', value: 15.5, creditCardId: null, categoryId: 'category-mercado' }),
    )
    expect(onSaved).toHaveBeenCalledOnce()
  })

  it('creates an expense with countsAsTithe defaulting to true', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSE, id: 'e2' })
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'Tithe payment'))
    act(() => result.current.setCreateField('createValue', '200'))
    act(() => result.current.setCreateField('createCategoryId', 'category-dizimo'))
    await act(() => result.current.submitCreate())

    expect(createExpenseMock).toHaveBeenCalledWith(expect.objectContaining({ countsAsTithe: true }))
  })

  it('creates an expense with countsAsTithe unchecked', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSE, id: 'e2' })
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'Charitable offer'))
    act(() => result.current.setCreateField('createValue', '50'))
    act(() => result.current.setCreateField('createCategoryId', 'category-dizimo'))
    act(() => result.current.setCreateField('createCountsAsTithe', 'false'))
    await act(() => result.current.submitCreate())

    expect(createExpenseMock).toHaveBeenCalledWith(expect.objectContaining({ countsAsTithe: false }))
  })

  it('populates editCountsAsTithe from the edited expense', () => {
    const offer: ExpenseDto = { ...EXPENSE, id: 'e5', categoryId: 'category-dizimo', countsAsTithe: false }
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showEditForm(offer))

    expect(result.current.editCountsAsTithe).toBe('false')
  })

  it('saves an edit toggling countsAsTithe to false', async () => {
    const dizimoExpense: ExpenseDto = { ...EXPENSE, id: 'e6', categoryId: 'category-dizimo' }
    updateExpenseMock.mockResolvedValue({ ...dizimoExpense, countsAsTithe: false })
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showEditForm(dizimoExpense))
    act(() => result.current.setEditField('editCountsAsTithe', 'false'))
    await act(() => result.current.saveEdit())

    expect(updateExpenseMock).toHaveBeenCalledWith('e6', expect.objectContaining({ countsAsTithe: false }))
  })

  it('surfaces a backend validation error on create failure without crashing', async () => {
    createExpenseMock.mockRejectedValue(new Error('Unrecognized category.'))
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'Waitrose'))
    act(() => result.current.setCreateField('createValue', '15.5'))
    await act(() => result.current.submitCreate())

    expect(result.current.createError).toBe('Unrecognized category.')
    expect(onSaved).not.toHaveBeenCalled()
  })

  it('saves an edit and calls onSaved on success', async () => {
    updateExpenseMock.mockResolvedValue({ ...EXPENSE, value: 50 })
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showEditForm(EXPENSE))
    act(() => result.current.setEditField('editValue', '50'))
    await act(() => result.current.saveEdit())

    expect(updateExpenseMock).toHaveBeenCalledWith(
      'e1',
      expect.objectContaining({ description: 'Lidl', value: 50, categoryId: 'category-mercado' }),
    )
    expect(onSaved).toHaveBeenCalledOnce()
  })

  it('creates in bank mode with a null card tag by default', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSE, id: 'e2' })
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'Waitrose'))
    act(() => result.current.setCreateField('createValue', '15.5'))
    act(() => result.current.setCreateField('createPaymentSource', 'bank-barclays'))
    await act(() => result.current.submitCreate())

    expect(result.current.createPaymentMode).toBe('bank')
    expect(createExpenseMock).toHaveBeenCalledWith(
      expect.objectContaining({ paymentSourceBankId: 'bank-barclays', creditCardId: null }),
    )
  })

  it('creates in card mode with a null payment source', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSE, id: 'e2' })
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showCreateForm('card'))
    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'Amazon'))
    act(() => result.current.setCreateField('createValue', '9.99'))
    act(() => result.current.setCreateField('createCreditCardId', 'card-chase'))
    await act(() => result.current.submitCreate())

    expect(createExpenseMock).toHaveBeenCalledWith(
      expect.objectContaining({ paymentSourceBankId: null, creditCardId: 'card-chase' }),
    )
  })

  it('rejects card-mode create without a card before calling the API', async () => {
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showCreateForm('card'))
    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'Amazon'))
    act(() => result.current.setCreateField('createValue', '9.99'))
    await act(() => result.current.submitCreate())

    expect(result.current.createError).toBe('Card is required')
    expect(createExpenseMock).not.toHaveBeenCalled()
  })

  it("showCreateForm('bank') defaults to the first bank and an empty card tag", () => {
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showCreateForm('bank'))

    expect(result.current.createPaymentMode).toBe('bank')
    expect(result.current.createPaymentSource).toBe('bank-barclays')
    expect(result.current.createCreditCardId).toBe('')
  })

  it("showCreateForm('card') defaults to an empty payment source and card tag", () => {
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showCreateForm('card'))

    expect(result.current.createPaymentMode).toBe('card')
    expect(result.current.createPaymentSource).toBe('')
    expect(result.current.createCreditCardId).toBe('')
  })

  it('opens edit in card mode for a credit card charge', () => {
    const charge: ExpenseDto = {
      ...EXPENSE,
      id: 'e3',
      paymentSourceBankId: null,
      paymentSourceBankName: null,
      creditCardId: 'card-baamex',
      creditCardName: 'BaAmex',
      paymentStatus: 'CreditCardCharge',
    }
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showEditForm(charge))

    expect(result.current.editPaymentMode).toBe('card')
    expect(result.current.editIsSettled).toBe(false)
  })

  it('saves a settled expense with its payment fields unchanged', async () => {
    const settled: ExpenseDto = {
      ...EXPENSE,
      id: 'e4',
      paymentSourceBankId: 'bank-trading212',
      paymentSourceBankName: 'Trading212',
      creditCardId: 'card-baamex',
      creditCardName: 'BaAmex',
      chargeDate: EXPENSE.date,
      invoiceDate: `${EXPENSE.date.slice(0, 7)}-01`,
      paymentStatus: 'CreditCardSettled',
    }
    updateExpenseMock.mockResolvedValue(settled)
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showEditForm(settled))
    expect(result.current.editIsSettled).toBe(true)

    act(() => result.current.setEditField('editDescription', 'Renamed'))
    await act(() => result.current.saveEdit())

    expect(updateExpenseMock).toHaveBeenCalledWith(
      'e4',
      expect.objectContaining({ description: 'Renamed', paymentSourceBankId: 'bank-trading212', creditCardId: 'card-baamex' }),
    )
  })

  it('picking a round-up-enabled bank auto-suggests when the field is blank', () => {
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.setCreateField('createValue', '9.40'))
    act(() => result.current.setCreateField('createPaymentSource', 'bank-trading212'))

    expect(result.current.createRoundUpAmount).toBe('0.60')
  })

  it('picking a round-up-enabled bank does not overwrite an amount the user already typed', () => {
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.setCreateField('createValue', '9.40'))
    act(() => result.current.setCreateField('createRoundUpAmount', '0.10'))
    act(() => result.current.setCreateField('createPaymentSource', 'bank-chase'))

    expect(result.current.createRoundUpAmount).toBe('0.10')
  })

  it('picking a non-round-up bank does not fill a suggestion', () => {
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.setCreateField('createValue', '9.40'))
    act(() => result.current.setCreateField('createPaymentSource', 'bank-barclays'))

    expect(result.current.createRoundUpAmount).toBe('')
  })

  it('a negative (reimbursement) value does not fill a round-up suggestion', () => {
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.setCreateField('createValue', '-9.40'))
    act(() => result.current.setCreateField('createPaymentSource', 'bank-trading212'))

    expect(result.current.createRoundUpAmount).toBe('')
  })

  it('sends the round-up amount on create for a round-up-enabled bank', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSE, id: 'e2' })
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'TfL'))
    act(() => result.current.setCreateField('createValue', '9.40'))
    act(() => result.current.setCreateField('createPaymentSource', 'bank-trading212'))
    await act(() => result.current.submitCreate())

    expect(createExpenseMock).toHaveBeenCalledWith(expect.objectContaining({ roundUpAmount: 0.6 }))
  })

  it('sends a null round-up amount when charging to card', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSE, id: 'e2' })
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showCreateForm('card'))
    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'Amazon'))
    act(() => result.current.setCreateField('createValue', '9.99'))
    act(() => result.current.setCreateField('createCreditCardId', 'card-chase'))
    await act(() => result.current.submitCreate())

    expect(createExpenseMock).toHaveBeenCalledWith(expect.objectContaining({ roundUpAmount: null }))
  })

  it('rejects a round-up amount outside £0.00-£0.99 before calling the API', async () => {
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.setCreateField('createDate', '2026-07-16'))
    act(() => result.current.setCreateField('createDescription', 'TfL'))
    act(() => result.current.setCreateField('createValue', '9.40'))
    act(() => result.current.setCreateField('createPaymentSource', 'bank-trading212'))
    act(() => result.current.setCreateField('createRoundUpAmount', '1.50'))
    await act(() => result.current.submitCreate())

    expect(result.current.createError).toContain('between £0.00 and £0.99')
    expect(createExpenseMock).not.toHaveBeenCalled()
  })

  it('pre-fills the edit round-up field from the saved amount, not the suggestion', () => {
    const expense: ExpenseDto = {
      ...EXPENSE,
      id: 'e7',
      value: 9.4,
      paymentSourceBankId: 'bank-trading212',
      paymentSourceBankName: 'Trading212',
      roundUpAmount: 0.1,
      suggestedRoundUpAmount: null,
    }
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showEditForm(expense))

    expect(result.current.editRoundUpAmount).toBe('0.1')
  })

  it('leaves a saved round-up amount unchanged when only Value is edited', async () => {
    const expense: ExpenseDto = {
      ...EXPENSE,
      id: 'e8',
      value: 9.4,
      paymentSourceBankId: 'bank-trading212',
      paymentSourceBankName: 'Trading212',
      roundUpAmount: 0.1,
      suggestedRoundUpAmount: null,
    }
    updateExpenseMock.mockResolvedValue(expense)
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showEditForm(expense))
    act(() => result.current.setEditField('editValue', '20'))
    await act(() => result.current.saveEdit())

    expect(updateExpenseMock).toHaveBeenCalledWith('e8', expect.objectContaining({ value: 20, roundUpAmount: 0.1 }))
  })

  it('clears a saved round-up amount when the edit field is emptied', async () => {
    const expense: ExpenseDto = {
      ...EXPENSE,
      id: 'e9',
      paymentSourceBankId: 'bank-trading212',
      paymentSourceBankName: 'Trading212',
      roundUpAmount: 0.1,
      suggestedRoundUpAmount: null,
    }
    updateExpenseMock.mockResolvedValue(expense)
    const { result } = renderHook(() => useExpenseForm(BANKS, CATEGORIES, onSaved))

    act(() => result.current.showEditForm(expense))
    act(() => result.current.setEditField('editRoundUpAmount', ''))
    await act(() => result.current.saveEdit())

    expect(updateExpenseMock).toHaveBeenCalledWith('e9', expect.objectContaining({ roundUpAmount: null }))
  })
})
