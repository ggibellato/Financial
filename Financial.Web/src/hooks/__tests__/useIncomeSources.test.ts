import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { IncomeSourceDto } from '../../api/types'
import { useIncomeSources } from '../useIncomeSources'

const { getIncomeSourcesMock, createIncomeSourceMock, updateIncomeSourceMock, deleteIncomeSourceMock } = vi.hoisted(() => ({
  getIncomeSourcesMock: vi.fn<FinancialApiClient['getIncomeSources']>(),
  createIncomeSourceMock: vi.fn<FinancialApiClient['createIncomeSource']>(),
  updateIncomeSourceMock: vi.fn<FinancialApiClient['updateIncomeSource']>(),
  deleteIncomeSourceMock: vi.fn<FinancialApiClient['deleteIncomeSource']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getIncomeSources: getIncomeSourcesMock,
    createIncomeSource: createIncomeSourceMock,
    updateIncomeSource: updateIncomeSourceMock,
    deleteIncomeSource: deleteIncomeSourceMock,
  } as Partial<FinancialApiClient>,
}))

const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: 's1', name: 'Gleison', isActive: true, group: 'Salary', potsAccountId: null, hasReferences: true },
  { id: 's2', name: 'Ariana', isActive: true, group: 'Salary', potsAccountId: 'p1', hasReferences: false },
]

describe('useIncomeSources', () => {
  beforeEach(() => {
    getIncomeSourcesMock.mockReset()
    createIncomeSourceMock.mockReset()
    updateIncomeSourceMock.mockReset()
    deleteIncomeSourceMock.mockReset()
    getIncomeSourcesMock.mockResolvedValue(INCOME_SOURCES)
  })

  it('fetches the income source list once on mount', async () => {
    const { result } = renderHook(() => useIncomeSources())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getIncomeSourcesMock).toHaveBeenCalledTimes(1)
    expect(result.current.incomeSources).toEqual(INCOME_SOURCES)
  })

  it('surfaces a fetch error', async () => {
    getIncomeSourcesMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useIncomeSources())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('retry re-fetches the list', async () => {
    const { result } = renderHook(() => useIncomeSources())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getIncomeSourcesMock).toHaveBeenCalledTimes(2))
  })

  it('createIncomeSource calls the API and re-fetches the list', async () => {
    createIncomeSourceMock.mockResolvedValue({
      id: 's3',
      name: 'Freelance',
      isActive: true,
      group: 'NonReportable',
      potsAccountId: null,
      hasReferences: false,
    })
    const { result } = renderHook(() => useIncomeSources())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.createIncomeSource({
        name: 'Freelance',
        group: 'NonReportable',
        isActive: true,
        potsAccountId: null,
      })
    })

    expect(createIncomeSourceMock).toHaveBeenCalledWith({
      name: 'Freelance',
      group: 'NonReportable',
      isActive: true,
      potsAccountId: null,
    })
    await waitFor(() => expect(getIncomeSourcesMock).toHaveBeenCalledTimes(2))
  })

  it('createIncomeSource propagates a rejected promise to the caller without swallowing it', async () => {
    createIncomeSourceMock.mockRejectedValue(new Error('An income source named "Gleison" already exists.'))
    const { result } = renderHook(() => useIncomeSources())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await expect(
      result.current.createIncomeSource({ name: 'Gleison', group: 'Salary', isActive: true, potsAccountId: null }),
    ).rejects.toThrow('An income source named "Gleison" already exists.')
  })

  it('updateIncomeSource calls the API and re-fetches the list', async () => {
    updateIncomeSourceMock.mockResolvedValue({
      id: 's1',
      name: 'Gleison Renamed',
      isActive: false,
      group: 'NonReportable',
      potsAccountId: 'p1',
      hasReferences: true,
    })
    const { result } = renderHook(() => useIncomeSources())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.updateIncomeSource('s1', {
        name: 'Gleison Renamed',
        group: 'NonReportable',
        isActive: false,
        potsAccountId: 'p1',
      })
    })

    expect(updateIncomeSourceMock).toHaveBeenCalledWith('s1', {
      name: 'Gleison Renamed',
      group: 'NonReportable',
      isActive: false,
      potsAccountId: 'p1',
    })
    await waitFor(() => expect(getIncomeSourcesMock).toHaveBeenCalledTimes(2))
  })

  it('deleteIncomeSource calls the API and re-fetches the list', async () => {
    deleteIncomeSourceMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useIncomeSources())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteIncomeSource('s2'))

    await waitFor(() => expect(result.current.deletingId).toBeNull())
    expect(deleteIncomeSourceMock).toHaveBeenCalledWith('s2')
    await waitFor(() => expect(getIncomeSourcesMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a delete error without re-fetching', async () => {
    deleteIncomeSourceMock.mockRejectedValue(new Error('Cannot delete an income source that is still used by an income entry.'))
    const { result } = renderHook(() => useIncomeSources())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteIncomeSource('s1'))

    await waitFor(() =>
      expect(result.current.deleteError).toBe('Cannot delete an income source that is still used by an income entry.'),
    )
    expect(getIncomeSourcesMock).toHaveBeenCalledTimes(1)
  })
})
