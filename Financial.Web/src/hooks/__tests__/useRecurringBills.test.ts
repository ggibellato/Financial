import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { RecurringBillDto } from '../../api/types'
import { useRecurringBills } from '../useRecurringBills'

const { getMensaisBillsMock, createMensaisBillMock, updateMensaisBillMock, deleteMensaisBillMock } = vi.hoisted(() => ({
  getMensaisBillsMock: vi.fn<FinancialApiClient['getMensaisBills']>(),
  createMensaisBillMock: vi.fn<FinancialApiClient['createMensaisBill']>(),
  updateMensaisBillMock: vi.fn<FinancialApiClient['updateMensaisBill']>(),
  deleteMensaisBillMock: vi.fn<FinancialApiClient['deleteMensaisBill']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getMensaisBills: getMensaisBillsMock,
    createMensaisBill: createMensaisBillMock,
    updateMensaisBill: updateMensaisBillMock,
    deleteMensaisBill: deleteMensaisBillMock,
  } as Partial<FinancialApiClient>,
}))

const BILLS: RecurringBillDto[] = [
  { id: 'b1', dueDay: 10, description: 'INSS', value: 850, area: 'Brasil', note: '', nitNumber: null, minimumWageValue: null, status: 'Unset' },
  { id: 'b2', dueDay: 15, description: 'Council Tax', value: 120, area: 'UK', note: '', nitNumber: null, minimumWageValue: null, status: 'Unset' },
]

describe('useRecurringBills', () => {
  beforeEach(() => {
    getMensaisBillsMock.mockReset()
    createMensaisBillMock.mockReset()
    updateMensaisBillMock.mockReset()
    deleteMensaisBillMock.mockReset()
    getMensaisBillsMock.mockResolvedValue(BILLS)
  })

  it('fetches the recurring bill list once on mount', async () => {
    const { result } = renderHook(() => useRecurringBills())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getMensaisBillsMock).toHaveBeenCalledTimes(1)
    expect(result.current.recurringBills).toEqual(BILLS)
  })

  it('surfaces a fetch error', async () => {
    getMensaisBillsMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useRecurringBills())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('retry re-fetches the list', async () => {
    const { result } = renderHook(() => useRecurringBills())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getMensaisBillsMock).toHaveBeenCalledTimes(2))
  })

  it('createRecurringBill calls the API and re-fetches the list', async () => {
    createMensaisBillMock.mockResolvedValue({
      id: 'b3',
      dueDay: 5,
      description: 'Rent',
      value: 1500,
      area: 'Brasil',
      note: '',
      nitNumber: null,
      minimumWageValue: null,
      status: 'Unset',
    })
    const { result } = renderHook(() => useRecurringBills())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.createRecurringBill({ dueDay: 5, description: 'Rent', value: 1500, area: 'Brasil', note: '' })
    })

    expect(createMensaisBillMock).toHaveBeenCalledWith({ dueDay: 5, description: 'Rent', value: 1500, area: 'Brasil', note: '' })
    await waitFor(() => expect(getMensaisBillsMock).toHaveBeenCalledTimes(2))
  })

  it('createRecurringBill propagates a rejected promise to the caller without swallowing it', async () => {
    createMensaisBillMock.mockRejectedValue(new Error('Due day must be between 1 and 31.'))
    const { result } = renderHook(() => useRecurringBills())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await expect(
      result.current.createRecurringBill({ dueDay: 32, description: 'Rent', value: 1500, area: 'Brasil', note: '' }),
    ).rejects.toThrow('Due day must be between 1 and 31.')
  })

  it('updateRecurringBill calls the API and re-fetches the list', async () => {
    updateMensaisBillMock.mockResolvedValue({
      id: 'b1',
      dueDay: 12,
      description: 'INSS Renamed',
      value: 900,
      area: 'Brasil',
      note: '',
      nitNumber: null,
      minimumWageValue: null,
      status: 'Paid',
    })
    const { result } = renderHook(() => useRecurringBills())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.updateRecurringBill('b1', {
        dueDay: 12,
        description: 'INSS Renamed',
        value: 900,
        area: 'Brasil',
        note: '',
        nitNumber: null,
        minimumWageValue: null,
        status: 'Paid',
      })
    })

    expect(updateMensaisBillMock).toHaveBeenCalledWith('b1', {
      dueDay: 12,
      description: 'INSS Renamed',
      value: 900,
      area: 'Brasil',
      note: '',
      nitNumber: null,
      minimumWageValue: null,
      status: 'Paid',
    })
    await waitFor(() => expect(getMensaisBillsMock).toHaveBeenCalledTimes(2))
  })

  it('deleteRecurringBill calls the API and re-fetches the list', async () => {
    deleteMensaisBillMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useRecurringBills())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteRecurringBill('b2'))

    await waitFor(() => expect(result.current.deletingId).toBeNull())
    expect(deleteMensaisBillMock).toHaveBeenCalledWith('b2')
    await waitFor(() => expect(getMensaisBillsMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a delete error without re-fetching', async () => {
    deleteMensaisBillMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useRecurringBills())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteRecurringBill('b1'))

    await waitFor(() => expect(result.current.deleteError).toBe('Network down'))
    expect(getMensaisBillsMock).toHaveBeenCalledTimes(1)
  })
})
