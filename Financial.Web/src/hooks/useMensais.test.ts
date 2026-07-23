import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { RecurringBillInstanceDto } from '../api/types'
import { useMensais } from './useMensais'

const NOW = new Date()
const CURRENT_YEAR = NOW.getFullYear()
const CURRENT_MONTH = NOW.getMonth() + 1
const CURRENT_MONTH_INPUT = `${CURRENT_YEAR}-${String(CURRENT_MONTH).padStart(2, '0')}`
const NEXT_MONTH = CURRENT_MONTH === 12 ? 1 : CURRENT_MONTH + 1
const NEXT_MONTH_YEAR = CURRENT_MONTH === 12 ? CURRENT_YEAR + 1 : CURRENT_YEAR
const NEXT_MONTH_INPUT = `${NEXT_MONTH_YEAR}-${String(NEXT_MONTH).padStart(2, '0')}`

const getMensaisInstancesMock = vi.fn<FinancialApiClient['getMensaisInstances']>()
const updateMensaisInstanceMock = vi.fn<FinancialApiClient['updateMensaisInstance']>()
const createMensaisTemplateMock = vi.fn<FinancialApiClient['createMensaisTemplate']>()
const deleteMensaisTemplateMock = vi.fn<FinancialApiClient['deleteMensaisTemplate']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getMensaisInstances: getMensaisInstancesMock,
    updateMensaisInstance: updateMensaisInstanceMock,
    createMensaisTemplate: createMensaisTemplateMock,
    deleteMensaisTemplate: deleteMensaisTemplateMock,
  }),
}))

const INSTANCES: RecurringBillInstanceDto[] = [
  {
    id: 'i1',
    templateId: 't1',
    year: CURRENT_YEAR,
    month: CURRENT_MONTH,
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
    id: 'i2',
    templateId: 't2',
    year: CURRENT_YEAR,
    month: CURRENT_MONTH,
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

describe('useMensais', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getMensaisInstancesMock.mockResolvedValue(INSTANCES)
  })

  it('fetches instances for the current month on mount', async () => {
    const { result } = renderHook(() => useMensais())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getMensaisInstancesMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(result.current.monthInputValue).toBe(CURRENT_MONTH_INPUT)
  })

  it('groups instances into brasil and uk sections', async () => {
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.brasilInstances).toHaveLength(1)
    expect(result.current.brasilInstances[0].description).toBe('INSS')
    expect(result.current.ukInstances).toHaveLength(1)
    expect(result.current.ukInstances[0].description).toBe('Council Tax')
  })

  it('re-fetches for a new month when the month input changes', async () => {
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setMonthInputValue(NEXT_MONTH_INPUT))

    await waitFor(() => expect(getMensaisInstancesMock).toHaveBeenCalledWith(NEXT_MONTH_YEAR, NEXT_MONTH))
  })

  it('surfaces a fetch error', async () => {
    getMensaisInstancesMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useMensais())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('saves an edit and re-fetches the current month', async () => {
    updateMensaisInstanceMock.mockResolvedValue({ ...INSTANCES[0], status: 'Paid', value: 900 })
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(INSTANCES[0]))
    act(() => result.current.setEditField('editStatus', 'Paid'))
    act(() => result.current.setEditField('editValue', '900'))
    act(() => result.current.saveEdit())

    await waitFor(() => expect(updateMensaisInstanceMock).toHaveBeenCalledWith('i1', { status: 'Paid', value: 900 }))
    await waitFor(() => expect(getMensaisInstancesMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a save error without crashing', async () => {
    updateMensaisInstanceMock.mockRejectedValue(new Error('Status is not recognized.'))
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(INSTANCES[0]))
    act(() => result.current.setEditField('editValue', '900'))
    act(() => result.current.saveEdit())

    await waitFor(() => expect(result.current.saveError).toBe('Status is not recognized.'))
  })

  it('adds a new bill and re-fetches the current month', async () => {
    createMensaisTemplateMock.mockResolvedValue({
      id: 't3',
      dueDay: 5,
      description: 'Aluguel',
      value: 1000,
      area: 'Brasil',
      note: '',
      nitNumber: null,
      minimumWageValue: null,
      isActive: true,
    })
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showAddForm())
    act(() => result.current.setAddField('newDescription', 'Aluguel'))
    act(() => result.current.setAddField('newDueDay', '5'))
    act(() => result.current.setAddField('newValue', '1000'))
    act(() => result.current.submitAdd())

    await waitFor(() =>
      expect(createMensaisTemplateMock).toHaveBeenCalledWith({
        dueDay: 5,
        description: 'Aluguel',
        value: 1000,
        area: 'Brasil',
        note: '',
        nitNumber: null,
        minimumWageValue: null,
      }),
    )
    await waitFor(() => expect(getMensaisInstancesMock).toHaveBeenCalledTimes(2))
    expect(result.current.isAddFormOpen).toBe(false)
  })

  it('surfaces an add error without crashing', async () => {
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showAddForm())
    act(() => result.current.submitAdd())

    await waitFor(() => expect(result.current.addError).toBe('Description is required'))
    expect(createMensaisTemplateMock).not.toHaveBeenCalled()
  })

  it('deletes a bill template and re-fetches the current month', async () => {
    deleteMensaisTemplateMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteTemplate('t1'))

    await waitFor(() => expect(deleteMensaisTemplateMock).toHaveBeenCalledWith('t1'))
    await waitFor(() => expect(getMensaisInstancesMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a delete error without crashing', async () => {
    deleteMensaisTemplateMock.mockRejectedValue(new Error('Recurring bill template not found.'))
    const { result } = renderHook(() => useMensais())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteTemplate('unknown'))

    await waitFor(() => expect(result.current.deleteError).toBe('Recurring bill template not found.'))
  })
})
