import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CategoryDto, RecurringBillDto } from '../../api/types'
import { account, cardAccount } from '../../test/account'
import { useMensais } from '../useMensais'
import { never } from '../../test-utils/deferred'

const {
  getMensaisBillsMock,
  createMensaisBillMock,
  updateMensaisBillMock,
  updateMensaisBillStatusMock,
  deleteMensaisBillMock,
  resetMensaisToUnsetMock,
  getAccountsMock,
  getCategoriesMock,
  markMensaisBillPaidMock,
} = vi.hoisted(() => ({
  getMensaisBillsMock: vi.fn<FinancialApiClient['getMensaisBills']>(),
  createMensaisBillMock: vi.fn<FinancialApiClient['createMensaisBill']>(),
  updateMensaisBillMock: vi.fn<FinancialApiClient['updateMensaisBill']>(),
  updateMensaisBillStatusMock: vi.fn<FinancialApiClient['updateMensaisBillStatus']>(),
  deleteMensaisBillMock: vi.fn<FinancialApiClient['deleteMensaisBill']>(),
  resetMensaisToUnsetMock: vi.fn<FinancialApiClient['resetMensaisToUnset']>(),
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
  markMensaisBillPaidMock: vi.fn<FinancialApiClient['markMensaisBillPaid']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getMensaisBills: getMensaisBillsMock,
    createMensaisBill: createMensaisBillMock,
    updateMensaisBill: updateMensaisBillMock,
    updateMensaisBillStatus: updateMensaisBillStatusMock,
    deleteMensaisBill: deleteMensaisBillMock,
    resetMensaisToUnset: resetMensaisToUnsetMock,
    getAccounts: getAccountsMock,
    getCategories: getCategoriesMock,
    markMensaisBillPaid: markMensaisBillPaidMock,
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

const BARCLAYS = account({ id: 'bank-1', name: 'Barclays' })
const CLOSED = account({ id: 'bank-2', name: 'Closed', isActive: false })
const ACCOUNTS = [BARCLAYS, cardAccount({ id: 'card-1', name: 'Amex' }), CLOSED]

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
    getAccountsMock.mockReset()
    getCategoriesMock.mockReset()
    markMensaisBillPaidMock.mockReset()
    getMensaisBillsMock.mockResolvedValue(BILLS)
    getAccountsMock.mockResolvedValue(ACCOUNTS)
    getCategoriesMock.mockResolvedValue(CATEGORIES)
    sessionStorage.clear()
  })

  it('lists every account that can pay bills, inactive included', async () => {
    const { result } = renderHook(() => useMensais())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.banks).toEqual([BARCLAYS, CLOSED])
  })

  it('fetches the bill list once on mount', async () => {
    const { result } = renderHook(() => useMensais())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getMensaisBillsMock).toHaveBeenCalledTimes(1)
  })

  it('groups bills into brasil and uk sections', async () => {
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.brasilBills).toHaveLength(1)
    expect(result.current.brasilBills[0].description).toBe('INSS')
    expect(result.current.ukBills).toHaveLength(1)
    expect(result.current.ukBills[0].description).toBe('Council Tax')
  })


  it.each([
    [new Error('Network down'), 'Network down'],
    ['unexpected', 'Unable to load Mensais data.'],
  ])('surfaces a fetch error %s as %s', async (reason, message) => {
    getMensaisBillsMock.mockRejectedValue(reason)
    const { result } = renderHook(() => useMensais())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe(message)
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

  it('does not swap in another form while a bill is saving', async () => {
    updateMensaisBillMock.mockReturnValue(never())
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(BILLS[0]))
    act(() => result.current.setEditField('editValue', '900'))
    act(() => result.current.saveEdit())
    act(() => result.current.showEditForm(BILLS[1]))
    act(() => result.current.showAddForm())

    expect(result.current.editingId).toBe(BILLS[0].id)
    expect(result.current.editValue).toBe('900')
    expect(result.current.isAddFormOpen).toBe(false)
  })

  it('reports each successful add, save, delete and reset, and nothing for a failure', async () => {
    const notifySaved = vi.fn()
    createMensaisBillMock.mockResolvedValueOnce({ ...BILLS[0], id: 'b3' }).mockRejectedValueOnce(new Error('down'))
    updateMensaisBillMock.mockResolvedValue({ ...BILLS[0], value: 900 })
    deleteMensaisBillMock.mockResolvedValue(undefined)
    resetMensaisToUnsetMock.mockResolvedValue(BILLS)
    const { result } = renderHook(() => useMensais(notifySaved))
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    const addBill = () => {
      act(() => result.current.showAddForm())
      act(() => result.current.setAddField('newDescription', 'Aluguel'))
      act(() => result.current.setAddField('newDueDay', '5'))
      act(() => result.current.setAddField('newValue', '1000'))
      act(() => result.current.submitAdd())
    }
    addBill()
    await waitFor(() => expect(notifySaved).toHaveBeenLastCalledWith('Bill added'))
    addBill()
    await waitFor(() => expect(result.current.addError).toBe('down'))

    act(() => result.current.showEditForm(BILLS[0]))
    act(() => result.current.saveEdit())
    await waitFor(() => expect(notifySaved).toHaveBeenLastCalledWith('Bill saved'))
    act(() => result.current.deleteBill(BILLS[0].id))
    await waitFor(() => expect(notifySaved).toHaveBeenLastCalledWith('Bill deleted'))
    act(() => result.current.resetAllToUnset())
    await waitFor(() => expect(notifySaved).toHaveBeenLastCalledWith('All bills reset to Unset'))
    expect(notifySaved).toHaveBeenCalledTimes(4)
  })

  it('surfaces an add error without crashing', async () => {
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showAddForm())
    act(() => result.current.submitAdd())

    await waitFor(() => expect(result.current.addError).toBe('Description is required.'))
    expect(createMensaisBillMock).not.toHaveBeenCalled()
  })

  it('surfaces a backend add error without crashing', async () => {
    createMensaisBillMock.mockRejectedValue(new Error('Due day must be between 1 and 31.'))
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showAddForm())
    act(() => result.current.setAddField('newDescription', 'Aluguel'))
    act(() => result.current.setAddField('newDueDay', '5'))
    act(() => result.current.setAddField('newValue', '1000'))
    act(() => result.current.submitAdd())

    await waitFor(() => expect(result.current.addError).toBe('Due day must be between 1 and 31.'))
  })

  it('cancelAdd closes the add form and clears its fields', async () => {
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showAddForm())
    act(() => result.current.setAddField('newDescription', 'Draft'))
    act(() => result.current.cancelAdd())

    expect(result.current.isAddFormOpen).toBe(false)
    expect(result.current.newDescription).toBe('')
  })

  it.each(['', '  '])('rejects an edit with a non-numeric value and the status %j, without calling the API', async (status) => {
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(BILLS[0]))
    act(() => result.current.setEditField('editValue', 'not-a-number'))
    act(() => result.current.setEditField('editStatus', status))
    act(() => result.current.saveEdit())

    expect(updateMensaisBillMock).not.toHaveBeenCalled()
    expect(result.current.saveErrorFields.editValue).toBe('Value must be a number.')
    expect(result.current.saveErrorFields.editStatus).toBe('Status is required.')
    expect(result.current.saveError).toBe('Value must be a number.')
  })

  it('cancelEdit clears the editing bill and its form fields', async () => {
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(BILLS[0]))
    act(() => result.current.cancelEdit())

    expect(result.current.editingId).toBeNull()
    expect(result.current.editValue).toBe('')
  })

  it('retry re-fetches the bill list', async () => {
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getMensaisBillsMock).toHaveBeenCalledTimes(2))
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

  it('clears a previous load error once a reload after a later delete succeeds', async () => {
    deleteMensaisBillMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    getMensaisBillsMock.mockRejectedValueOnce(new Error('Network down'))
    act(() => result.current.deleteBill('b1'))
    await waitFor(() => expect(result.current.error).toBe('Network down'))

    act(() => result.current.deleteBill('b2'))

    await waitFor(() => expect(result.current.error).toBeNull())
    expect(getMensaisBillsMock).toHaveBeenCalledTimes(3)
  })

  it('ignores an older reload that settles after a newer one failed', async () => {
    deleteMensaisBillMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    const loadedBills = [...result.current.brasilBills, ...result.current.ukBills]
    let resolveOlderReload!: (bills: typeof BILLS) => void
    getMensaisBillsMock.mockReturnValueOnce(new Promise((resolve) => (resolveOlderReload = resolve)))
    act(() => result.current.deleteBill('b1'))
    await waitFor(() => expect(getMensaisBillsMock).toHaveBeenCalledTimes(2))
    getMensaisBillsMock.mockRejectedValueOnce(new Error('Network down'))

    act(() => result.current.deleteBill('b2'))
    await waitFor(() => expect(result.current.error).toBe('Network down'))
    await act(async () => resolveOlderReload([]))

    expect(result.current.error).toBe('Network down')
    expect([...result.current.brasilBills, ...result.current.ukBills]).toEqual(loadedBills)
  })

  it.each([
    ['closes the edit form when its bill is deleted', 'b1', null],
    ['keeps the edit form open when another bill is deleted', 'b2', 'b1'],
  ])('%s', async (_, deletedId, expectedEditingId) => {
    deleteMensaisBillMock.mockResolvedValue(undefined)
    const result = await renderLoaded()
    act(() => result.current.showEditForm(BILLS[0]))

    act(() => result.current.deleteBill(deletedId))

    await waitFor(() => expect(result.current.deletingBillId).toBeNull())
    expect(result.current.editingId).toBe(expectedEditingId)
  })

  it('ignores a second delete while one is still running', async () => {
    deleteMensaisBillMock.mockReturnValue(never())
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    act(() => result.current.deleteBill('b1'))

    act(() => result.current.deleteBill('b2'))

    expect(deleteMensaisBillMock).toHaveBeenCalledExactlyOnceWith('b1')
    expect(result.current.deletingBillId).toBe('b1')
  })

  it('surfaces a delete error without crashing', async () => {
    deleteMensaisBillMock.mockRejectedValue(new Error('Recurring bill not found.'))
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteBill('unknown'))

    await waitFor(() => expect(result.current.deleteError).toBe('Recurring bill not found.'))
  })

  async function renderLoaded() {
    const rendered = renderHook(() => useMensais())
    await waitFor(() => expect(rendered.result.current.isLoading).toBe(false))
    return rendered.result
  }

  it.each([
    ['succeeds', () => updateMensaisBillMock.mockResolvedValue({ ...BILLS[0], value: 900 })],
    ['fails', () => updateMensaisBillMock.mockRejectedValue(new Error('down'))],
  ])('clears the saving flag when a save %s', async (_, arrange) => {
    arrange()
    const result = await renderLoaded()
    act(() => result.current.showEditForm(BILLS[0]))

    act(() => result.current.saveEdit())

    expect(result.current.isSaving).toBe(true)
    await waitFor(() => expect(result.current.isSaving).toBe(false))
  })

  it('clears the adding flag when an add succeeds', async () => {
    createMensaisBillMock.mockResolvedValue({ ...BILLS[0], id: 'b3' })
    const result = await renderLoaded()
    act(() => result.current.showAddForm())
    act(() => {
      result.current.setAddField('newDescription', 'Aluguel')
      result.current.setAddField('newDueDay', '5')
      result.current.setAddField('newValue', '1000')
    })

    act(() => result.current.submitAdd())

    expect(result.current.isAdding).toBe(true)
    await waitFor(() => expect(result.current.isAdding).toBe(false))
  })

  it.each([
    ['succeeds', () => resetMensaisToUnsetMock.mockResolvedValue(BILLS)],
    ['fails', () => resetMensaisToUnsetMock.mockRejectedValue(new Error('down'))],
  ])('clears the resetting flag when a reset %s', async (_, arrange) => {
    arrange()
    const result = await renderLoaded()

    act(() => result.current.resetAllToUnset())

    expect(result.current.isResetting).toBe(true)
    await waitFor(() => expect(result.current.isResetting).toBe(false))
  })

  it('keeps the expense prompt open when another bill status is updated', async () => {
    updateMensaisBillStatusMock.mockResolvedValue({ ...BILLS[0], status: 'Paid' })
    const result = await renderLoaded()
    act(() => result.current.updateBillStatus('b2', 'Paid'))

    act(() => result.current.updateBillStatus('b1', 'Paid'))
    await waitFor(() => expect(result.current.updatingStatusBillId).toBeNull())

    expect(result.current.expensePromptBill?.id).toBe('b2')
  })

  it('ignores a load error from an earlier request that settles after a later one loaded', async () => {
    let rejectFirst: (reason: unknown) => void = () => {}
    getMensaisBillsMock.mockReturnValueOnce(new Promise((_, reject) => { rejectFirst = reject }))
    const { result } = renderHook(() => useMensais())

    act(() => result.current.retry())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    await act(async () => rejectFirst(new Error('stale')))

    expect(result.current.error).toBeNull()
  })

  it('saves the bill being edited, not the first one listed', async () => {
    updateMensaisBillMock.mockResolvedValue(BILLS[1])
    const result = await renderLoaded()
    act(() => result.current.showEditForm(BILLS[1]))

    act(() => result.current.saveEdit())

    expect(updateMensaisBillMock).toHaveBeenCalledWith('b2', expect.objectContaining({ description: 'Council Tax', area: 'UK', dueDay: 15 }))
  })

  it('refuses a whitespace-only description and a value that is not a number', async () => {
    const result = await renderLoaded()
    act(() => result.current.showAddForm())
    act(() => {
      result.current.setAddField('newDescription', '  ')
      result.current.setAddField('newDueDay', '5')
      result.current.setAddField('newValue', 'abc')
    })

    act(() => result.current.submitAdd())

    expect(result.current.addErrorFields).toEqual({ newDescription: 'Description is required.', newValue: 'Value must be a number.' })
    expect(createMensaisBillMock).not.toHaveBeenCalled()
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
    resetMensaisToUnsetMock.mockRejectedValue(new Error('Failed to reset bills.'))
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.resetAllToUnset())

    await waitFor(() => expect(result.current.resetError).toBe('Failed to reset bills.'))
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

    it.each([
      ['a Brasil bill to Paid', BILLS[0], 'Paid'],
      ['an already-Paid UK bill to Unset', { ...ukBill, status: 'Paid' }, 'Unset'],
      ['an already-Paid UK bill to Paid again', { ...ukBill, status: 'Paid' }, 'Paid'],
      ['an unpaid UK bill to Scheduled', ukBill, 'Scheduled'],
    ])('updates %s directly, without opening the prompt', async (_, bill, status) => {
      getMensaisBillsMock.mockResolvedValue([bill])
      updateMensaisBillStatusMock.mockResolvedValue({ ...bill, status })
      const result = await renderLoaded()

      act(() => result.current.updateBillStatus(bill.id, status))

      expect(result.current.expensePromptBill).toBeNull()
      expect(updateMensaisBillStatusMock).toHaveBeenCalledWith(bill.id, { status })
      await waitFor(() => expect(result.current.updatingStatusBillId).toBeNull())
    })

    it('confirming the prompt marks the bill Paid with its expense in one call and closes the prompt', async () => {
      markMensaisBillPaidMock.mockResolvedValue({ ...ukBill, status: 'Paid' })
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
        expect(markMensaisBillPaidMock).toHaveBeenCalledWith(ukBill.id, {
          expense: {
            description: 'Council Tax',
            value: 120,
            date: '2026-09-01',
            categoryId: 'cat-1',
            paymentSourceBankId: 'bank-1',
          },
        }),
      )
      await waitFor(() => expect(result.current.expensePromptBill).toBeNull())
      expect(result.current.ukBills.find((b) => b.id === ukBill.id)?.status).toBe('Paid')
      expect(updateMensaisBillStatusMock).not.toHaveBeenCalled()
    })

    it('keeps the prompt open with the error when marking Paid fails, and a second confirm sends the same single call', async () => {
      markMensaisBillPaidMock.mockRejectedValueOnce(new Error('Category is required.'))
      markMensaisBillPaidMock.mockResolvedValue({ ...ukBill, status: 'Paid' })
      const { result } = renderHook(() => useMensais())
      await waitFor(() => expect(result.current.isLoading).toBe(false))
      act(() => result.current.updateBillStatus(ukBill.id, 'Paid'))
      const values = { description: 'Council Tax', value: 120, date: '2026-09-01', bankId: 'bank-1', categoryId: 'cat-1' }

      act(() => result.current.confirmExpensePrompt(values))

      await waitFor(() => expect(result.current.statusUpdateError).toBe('Category is required.'))
      expect(result.current.expensePromptBill?.id).toBe(ukBill.id)
      expect(result.current.ukBills.find((b) => b.id === ukBill.id)?.status).toBe('Unset')

      act(() => result.current.confirmExpensePrompt(values))

      await waitFor(() => expect(result.current.expensePromptBill).toBeNull())
      expect(markMensaisBillPaidMock).toHaveBeenCalledTimes(2)
      expect(result.current.statusUpdateError).toBeNull()
    })

    it('skipping the prompt marks the bill Paid with no expense', async () => {
      markMensaisBillPaidMock.mockResolvedValue({ ...ukBill, status: 'Paid' })
      const { result } = renderHook(() => useMensais())
      await waitFor(() => expect(result.current.isLoading).toBe(false))
      act(() => result.current.updateBillStatus(ukBill.id, 'Paid'))

      act(() => result.current.skipExpensePrompt())

      await waitFor(() => expect(result.current.expensePromptBill).toBeNull())
      expect(markMensaisBillPaidMock).toHaveBeenCalledWith(ukBill.id, { expense: null })
      expect(updateMensaisBillStatusMock).not.toHaveBeenCalled()
    })

    it('canceling the prompt makes no API calls and clears the prompt', async () => {
      const { result } = renderHook(() => useMensais())
      await waitFor(() => expect(result.current.isLoading).toBe(false))
      act(() => result.current.updateBillStatus(ukBill.id, 'Paid'))

      act(() => result.current.closeExpensePrompt())

      expect(result.current.expensePromptBill).toBeNull()
      expect(markMensaisBillPaidMock).not.toHaveBeenCalled()
      expect(updateMensaisBillStatusMock).not.toHaveBeenCalled()
    })

    it('unmarking a Paid UK bill only calls the status endpoint, never touching expenses', async () => {
      const paidUkBill = { ...ukBill, status: 'Paid' }
      getMensaisBillsMock.mockResolvedValue([BILLS[0], paidUkBill])
      updateMensaisBillStatusMock.mockResolvedValue({ ...paidUkBill, status: 'Unset' })
      const { result } = renderHook(() => useMensais())
      await waitFor(() => expect(result.current.isLoading).toBe(false))

      act(() => result.current.updateBillStatus(paidUkBill.id, 'Unset'))

      expect(result.current.expensePromptBill).toBeNull()
      await waitFor(() => expect(updateMensaisBillStatusMock).toHaveBeenCalledWith(paidUkBill.id, { status: 'Unset' }))
      expect(markMensaisBillPaidMock).not.toHaveBeenCalled()
    })
  })
})
