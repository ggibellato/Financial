import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BankDto, CategoryDto, RecurringBillDto } from '../../api/types'
import { useMensais } from '../useMensais'

const NOW = new Date()
const CURRENT_YEAR = NOW.getFullYear()
const CURRENT_MONTH = NOW.getMonth() + 1
const CURRENT_MONTH_INPUT = `${CURRENT_YEAR}-${String(CURRENT_MONTH).padStart(2, '0')}`
const NEXT_MONTH = CURRENT_MONTH === 12 ? 1 : CURRENT_MONTH + 1
const NEXT_MONTH_YEAR = CURRENT_MONTH === 12 ? CURRENT_YEAR + 1 : CURRENT_YEAR
const NEXT_MONTH_INPUT = `${NEXT_MONTH_YEAR}-${String(NEXT_MONTH).padStart(2, '0')}`

const {
  getMensaisBillsMock,
  createMensaisBillMock,
  updateMensaisBillMock,
  updateMensaisBillStatusMock,
  deleteMensaisBillMock,
  resetMensaisToUnsetMock,
  getBanksMock,
  getCategoriesMock,
  createExpenseMock,
} = vi.hoisted(() => ({
  getMensaisBillsMock: vi.fn<FinancialApiClient['getMensaisBills']>(),
  createMensaisBillMock: vi.fn<FinancialApiClient['createMensaisBill']>(),
  updateMensaisBillMock: vi.fn<FinancialApiClient['updateMensaisBill']>(),
  updateMensaisBillStatusMock: vi.fn<FinancialApiClient['updateMensaisBillStatus']>(),
  deleteMensaisBillMock: vi.fn<FinancialApiClient['deleteMensaisBill']>(),
  resetMensaisToUnsetMock: vi.fn<FinancialApiClient['resetMensaisToUnset']>(),
  getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
  getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
  createExpenseMock: vi.fn<FinancialApiClient['createExpense']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getMensaisBills: getMensaisBillsMock,
    createMensaisBill: createMensaisBillMock,
    updateMensaisBill: updateMensaisBillMock,
    updateMensaisBillStatus: updateMensaisBillStatusMock,
    deleteMensaisBill: deleteMensaisBillMock,
    resetMensaisToUnset: resetMensaisToUnsetMock,
    getBanks: getBanksMock,
    getCategories: getCategoriesMock,
    createExpense: createExpenseMock,
  } as Partial<FinancialApiClient>,
}))

const BILLS: RecurringBillDto[] = [
  {
    id: 'b1',
    dueDay: 10,
    description: 'INSS',
    area: 'Brasil',
    note: '',
    nitNumber: null,
    minimumWageValue: null,
    value: 850,
    status: 'Unset',
  },
  {
    id: 'b2',
    dueDay: 15,
    description: 'Council Tax',
    area: 'UK',
    note: '',
    nitNumber: null,
    minimumWageValue: null,
    value: 120,
    status: 'Unset',
  },
]

const BANKS: BankDto[] = [
  {
    id: 'bank-1',
    name: 'Barclays',
    roundUpEnabled: false,
    openingBalance: 0,
    openingBalanceDate: '2026-01-01',
    hasReferences: false,
  },
]

const CATEGORIES: CategoryDto[] = [
  { id: 'cat-1', name: 'Bills', active: true, isInvestment: false, isTithe: false, hasReferences: false },
]

describe('useMensais', () => {
  beforeEach(() => {
    getMensaisBillsMock.mockReset()
    createMensaisBillMock.mockReset()
    updateMensaisBillMock.mockReset()
    updateMensaisBillStatusMock.mockReset()
    deleteMensaisBillMock.mockReset()
    resetMensaisToUnsetMock.mockReset()
    getBanksMock.mockReset()
    getCategoriesMock.mockReset()
    createExpenseMock.mockReset()
    getMensaisBillsMock.mockResolvedValue(BILLS)
    getBanksMock.mockResolvedValue(BANKS)
    getCategoriesMock.mockResolvedValue(CATEGORIES)
    sessionStorage.clear()
  })

  it('fetches the bill list once on mount, defaulting the display month to today', async () => {
    const { result } = renderHook(() => useMensais())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getMensaisBillsMock).toHaveBeenCalledTimes(1)
    expect(result.current.monthInputValue).toBe(CURRENT_MONTH_INPUT)
  })

  it('groups bills into brasil and uk sections', async () => {
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.brasilBills).toHaveLength(1)
    expect(result.current.brasilBills[0].description).toBe('INSS')
    expect(result.current.ukBills).toHaveLength(1)
    expect(result.current.ukBills[0].description).toBe('Council Tax')
  })

  it('changing the display month is purely local and does not re-fetch', async () => {
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setMonthInputValue(NEXT_MONTH_INPUT))

    expect(result.current.monthInputValue).toBe(NEXT_MONTH_INPUT)
    expect(getMensaisBillsMock).toHaveBeenCalledTimes(1)
  })

  it('surfaces a fetch error', async () => {
    getMensaisBillsMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useMensais())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('saves an edit and re-fetches the bill list', async () => {
    updateMensaisBillMock.mockResolvedValue({ ...BILLS[0], status: 'Paid', value: 900 })
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(BILLS[0]))
    act(() => result.current.setEditField('editStatus', 'Paid'))
    act(() => result.current.setEditField('editValue', '900'))
    act(() => result.current.saveEdit())

    await waitFor(() =>
      expect(updateMensaisBillMock).toHaveBeenCalledWith('b1', {
        dueDay: 10,
        description: 'INSS',
        value: 900,
        area: 'Brasil',
        note: '',
        nitNumber: null,
        minimumWageValue: null,
        status: 'Paid',
      }),
    )
    await waitFor(() => expect(getMensaisBillsMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a save error without crashing', async () => {
    updateMensaisBillMock.mockRejectedValue(new Error('Status is not recognized.'))
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(BILLS[0]))
    act(() => result.current.setEditField('editValue', '900'))
    act(() => result.current.saveEdit())

    await waitFor(() => expect(result.current.saveError).toBe('Status is not recognized.'))
  })

  it('adds a new bill and re-fetches the bill list', async () => {
    createMensaisBillMock.mockResolvedValue({
      id: 'b3',
      dueDay: 5,
      description: 'Aluguel',
      value: 1000,
      area: 'Brasil',
      note: '',
      nitNumber: null,
      minimumWageValue: null,
      status: 'Unset',
    })
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showAddForm())
    act(() => result.current.setAddField('newDescription', 'Aluguel'))
    act(() => result.current.setAddField('newDueDay', '5'))
    act(() => result.current.setAddField('newValue', '1000'))
    act(() => result.current.submitAdd())

    await waitFor(() =>
      expect(createMensaisBillMock).toHaveBeenCalledWith({
        dueDay: 5,
        description: 'Aluguel',
        value: 1000,
        area: 'Brasil',
        note: '',
      }),
    )
    await waitFor(() => expect(getMensaisBillsMock).toHaveBeenCalledTimes(2))
    expect(result.current.isAddFormOpen).toBe(false)
  })

  it('surfaces an add error without crashing', async () => {
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showAddForm())
    act(() => result.current.submitAdd())

    await waitFor(() => expect(result.current.addError).toBe('Description is required'))
    expect(createMensaisBillMock).not.toHaveBeenCalled()
  })

  it('showAddForm defaults area to Brasil when nothing was persisted yet', async () => {
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showAddForm())

    expect(result.current.newArea).toBe('Brasil')
  })

  it('persists the area after a successful add, for the next add form', async () => {
    createMensaisBillMock.mockResolvedValue({
      id: 'b3', dueDay: 5, description: 'Council Tax top-up', value: 100, area: 'UK', note: '',
      nitNumber: null, minimumWageValue: null, status: 'Unset',
    })
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    act(() => result.current.showAddForm())
    act(() => result.current.setAddField('newArea', 'UK'))
    act(() => result.current.setAddField('newDescription', 'Council Tax top-up'))
    act(() => result.current.setAddField('newDueDay', '5'))
    act(() => result.current.setAddField('newValue', '100'))
    act(() => result.current.submitAdd())
    await waitFor(() => expect(createMensaisBillMock).toHaveBeenCalledTimes(1))

    act(() => result.current.showAddForm())

    expect(result.current.newArea).toBe('UK')
    expect(result.current.newDescription).toBe('')
    expect(result.current.newValue).toBe('')
    expect(result.current.newDueDay).toBe('')
    expect(result.current.newNote).toBe('')
  })

  it('deletes a bill and re-fetches the bill list', async () => {
    deleteMensaisBillMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteBill('b1'))

    await waitFor(() => expect(deleteMensaisBillMock).toHaveBeenCalledWith('b1'))
    await waitFor(() => expect(getMensaisBillsMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a delete error without crashing', async () => {
    deleteMensaisBillMock.mockRejectedValue(new Error('Recurring bill not found.'))
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteBill('unknown'))

    await waitFor(() => expect(result.current.deleteError).toBe('Recurring bill not found.'))
  })

  it('resets all bills to Unset using the server response directly, without an extra fetch', async () => {
    resetMensaisToUnsetMock.mockResolvedValue([
      { ...BILLS[0], status: 'Unset' },
      { ...BILLS[1], status: 'Unset' },
    ])
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.resetAllToUnset())

    await waitFor(() => expect(resetMensaisToUnsetMock).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(result.current.brasilBills[0].status).toBe('Unset'))
    expect(getMensaisBillsMock).toHaveBeenCalledTimes(1)
  })

  it('surfaces a reset error without crashing', async () => {
    resetMensaisToUnsetMock.mockRejectedValue(new Error('Failed to reset bills'))
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.resetAllToUnset())

    await waitFor(() => expect(result.current.resetError).toBe('Failed to reset bills'))
  })

  it('updates a bill status in place, without refetching the bill list', async () => {
    updateMensaisBillStatusMock.mockResolvedValue({ ...BILLS[0], status: 'Paid' })
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.updateBillStatus('b1', 'Paid'))

    await waitFor(() => expect(updateMensaisBillStatusMock).toHaveBeenCalledWith('b1', { status: 'Paid' }))
    await waitFor(() => expect(result.current.brasilBills[0].status).toBe('Paid'))
    expect(result.current.ukBills[0].status).toBe('Unset')
    expect(getMensaisBillsMock).toHaveBeenCalledTimes(1)
  })

  it('tracks which bill is updating while the status call is in flight', async () => {
    let resolveUpdate: (bill: RecurringBillDto) => void = () => {}
    updateMensaisBillStatusMock.mockReturnValue(
      new Promise((resolve) => {
        resolveUpdate = resolve
      }),
    )
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.updateBillStatus('b1', 'Paid'))

    expect(result.current.updatingStatusBillId).toBe('b1')

    await act(async () => resolveUpdate({ ...BILLS[0], status: 'Paid' }))

    expect(result.current.updatingStatusBillId).toBeNull()
  })

  it('surfaces a status update error and leaves the bill list untouched', async () => {
    updateMensaisBillStatusMock.mockRejectedValue(new Error('Status is not recognized.'))
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.updateBillStatus('b1', 'NotAStatus'))

    await waitFor(() => expect(result.current.statusUpdateError).toBe('Status is not recognized.'))
    expect(result.current.brasilBills[0].status).toBe('Unset')
    expect(result.current.updatingStatusBillId).toBeNull()
  })

  describe('UK Paid-to-Expense prompt', () => {
    const ukBill = BILLS[1]

    it.each(['Unset', 'Scheduled'])(
      'opens the expense prompt instead of calling the API when a UK bill transitions from %s into Paid',
      async (priorStatus) => {
        getMensaisBillsMock.mockResolvedValue([BILLS[0], { ...ukBill, status: priorStatus }])
        const { result } = renderHook(() => useMensais())
        await waitFor(() => expect(result.current.isLoading).toBe(false))

        act(() => result.current.updateBillStatus(ukBill.id, 'Paid'))

        expect(result.current.expensePromptBill?.id).toBe(ukBill.id)
        expect(updateMensaisBillStatusMock).not.toHaveBeenCalled()
      },
    )

    it('updates a Brasil bill directly, without opening the prompt', async () => {
      updateMensaisBillStatusMock.mockResolvedValue({ ...BILLS[0], status: 'Paid' })
      const { result } = renderHook(() => useMensais())
      await waitFor(() => expect(result.current.isLoading).toBe(false))

      act(() => result.current.updateBillStatus(BILLS[0].id, 'Paid'))

      expect(result.current.expensePromptBill).toBeNull()
      await waitFor(() => expect(updateMensaisBillStatusMock).toHaveBeenCalledWith(BILLS[0].id, { status: 'Paid' }))
    })

    it('updates an already-Paid UK bill directly, without opening the prompt', async () => {
      const paidUkBill = { ...ukBill, status: 'Paid' }
      getMensaisBillsMock.mockResolvedValue([BILLS[0], paidUkBill])
      updateMensaisBillStatusMock.mockResolvedValue({ ...paidUkBill, status: 'Unset' })
      const { result } = renderHook(() => useMensais())
      await waitFor(() => expect(result.current.isLoading).toBe(false))

      act(() => result.current.updateBillStatus(paidUkBill.id, 'Unset'))

      expect(result.current.expensePromptBill).toBeNull()
      await waitFor(() => expect(updateMensaisBillStatusMock).toHaveBeenCalledWith(paidUkBill.id, { status: 'Unset' }))
    })

    it('confirming the prompt creates the expense, then updates the status, and closes the prompt', async () => {
      createExpenseMock.mockResolvedValue({
        id: 'exp-1', date: '2026-09-01', description: 'Council Tax', value: 120,
        categoryId: 'cat-1', categoryName: 'Bills', paymentSourceBankId: 'bank-1', paymentSourceBankName: 'Barclays',
        creditCardId: null, creditCardName: null, chargeDate: null, invoiceDate: null,
        paymentStatus: 'ImmediatePayment', roundUpAmount: null, suggestedRoundUpAmount: null, countsAsTithe: true,
      })
      updateMensaisBillStatusMock.mockResolvedValue({ ...ukBill, status: 'Paid' })
      const { result } = renderHook(() => useMensais())
      await waitFor(() => expect(result.current.isLoading).toBe(false))
      act(() => result.current.updateBillStatus(ukBill.id, 'Paid'))

      act(() =>
        result.current.confirmExpensePrompt({
          description: 'Council Tax',
          value: 120,
          date: '2026-09-01',
          bankId: 'bank-1',
          categoryId: 'cat-1',
        }),
      )

      await waitFor(() =>
        expect(createExpenseMock).toHaveBeenCalledWith(
          expect.objectContaining({
            description: 'Council Tax',
            value: 120,
            date: '2026-09-01',
            categoryId: 'cat-1',
            paymentSourceBankId: 'bank-1',
            creditCardId: null,
          }),
        ),
      )
      await waitFor(() => expect(updateMensaisBillStatusMock).toHaveBeenCalledWith(ukBill.id, { status: 'Paid' }))
      await waitFor(() => expect(result.current.expensePromptBill).toBeNull())
      expect(result.current.ukBills.find((b) => b.id === ukBill.id)?.status).toBe('Paid')
    })

    it('keeps the prompt open with an error when expense creation fails', async () => {
      createExpenseMock.mockRejectedValue(new Error('Category is required.'))
      const { result } = renderHook(() => useMensais())
      await waitFor(() => expect(result.current.isLoading).toBe(false))
      act(() => result.current.updateBillStatus(ukBill.id, 'Paid'))

      act(() =>
        result.current.confirmExpensePrompt({
          description: 'Council Tax', value: 120, date: '2026-09-01', bankId: 'bank-1', categoryId: '',
        }),
      )

      await waitFor(() => expect(result.current.expenseCreateError).toBe('Category is required.'))
      expect(result.current.expensePromptBill?.id).toBe(ukBill.id)
      expect(updateMensaisBillStatusMock).not.toHaveBeenCalled()
    })

    it('enters retry-only mode without creating a second expense when the status update fails after a successful expense', async () => {
      createExpenseMock.mockResolvedValue({
        id: 'exp-1', date: '2026-09-01', description: 'Council Tax', value: 120,
        categoryId: 'cat-1', categoryName: 'Bills', paymentSourceBankId: 'bank-1', paymentSourceBankName: 'Barclays',
        creditCardId: null, creditCardName: null, chargeDate: null, invoiceDate: null,
        paymentStatus: 'ImmediatePayment', roundUpAmount: null, suggestedRoundUpAmount: null, countsAsTithe: true,
      })
      updateMensaisBillStatusMock.mockRejectedValue(new Error('Recurring bill not found.'))
      const { result } = renderHook(() => useMensais())
      await waitFor(() => expect(result.current.isLoading).toBe(false))
      act(() => result.current.updateBillStatus(ukBill.id, 'Paid'))

      act(() =>
        result.current.confirmExpensePrompt({
          description: 'Council Tax', value: 120, date: '2026-09-01', bankId: 'bank-1', categoryId: 'cat-1',
        }),
      )

      await waitFor(() => expect(result.current.expenseCreatedForRetry).toBe(true))
      await waitFor(() => expect(result.current.statusUpdateError).toBe('Recurring bill not found.'))
      expect(createExpenseMock).toHaveBeenCalledTimes(1)

      updateMensaisBillStatusMock.mockResolvedValue({ ...ukBill, status: 'Paid' })
      act(() => result.current.skipOrRetryExpensePrompt())

      await waitFor(() => expect(result.current.expensePromptBill).toBeNull())
      expect(createExpenseMock).toHaveBeenCalledTimes(1)
      expect(updateMensaisBillStatusMock).toHaveBeenCalledTimes(2)
    })

    it('skipping the prompt updates the status without creating an expense', async () => {
      updateMensaisBillStatusMock.mockResolvedValue({ ...ukBill, status: 'Paid' })
      const { result } = renderHook(() => useMensais())
      await waitFor(() => expect(result.current.isLoading).toBe(false))
      act(() => result.current.updateBillStatus(ukBill.id, 'Paid'))

      act(() => result.current.skipOrRetryExpensePrompt())

      await waitFor(() => expect(result.current.expensePromptBill).toBeNull())
      expect(createExpenseMock).not.toHaveBeenCalled()
      expect(updateMensaisBillStatusMock).toHaveBeenCalledWith(ukBill.id, { status: 'Paid' })
    })

    it('canceling the prompt makes no API calls and clears the prompt', async () => {
      const { result } = renderHook(() => useMensais())
      await waitFor(() => expect(result.current.isLoading).toBe(false))
      act(() => result.current.updateBillStatus(ukBill.id, 'Paid'))

      act(() => result.current.closeExpensePrompt())

      expect(result.current.expensePromptBill).toBeNull()
      expect(createExpenseMock).not.toHaveBeenCalled()
      expect(updateMensaisBillStatusMock).not.toHaveBeenCalled()
    })
  })
})
