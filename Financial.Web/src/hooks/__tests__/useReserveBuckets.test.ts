import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { ReserveBucketDto } from '../../api/types'
import { useReserveBuckets } from '../useReserveBuckets'

const { getReserveBucketsMock, createReserveBucketMock, updateReserveBucketMock } = vi.hoisted(() => ({
  getReserveBucketsMock: vi.fn<FinancialApiClient['getReserveBuckets']>(),
  createReserveBucketMock: vi.fn<FinancialApiClient['createReserveBucket']>(),
  updateReserveBucketMock: vi.fn<FinancialApiClient['updateReserveBucket']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getReserveBuckets: getReserveBucketsMock,
    createReserveBucket: createReserveBucketMock,
    updateReserveBucket: updateReserveBucketMock,
  } as Partial<FinancialApiClient>,
}))

const BUCKETS: ReserveBucketDto[] = [
  { id: 'b1', name: 'Investimento', isActive: true, splitPercentage: 60, warning: null },
  { id: 'b2', name: 'Ferias', isActive: true, splitPercentage: 40, warning: null },
]

describe('useReserveBuckets', () => {
  beforeEach(() => {
    getReserveBucketsMock.mockReset()
    createReserveBucketMock.mockReset()
    updateReserveBucketMock.mockReset()
    getReserveBucketsMock.mockResolvedValue(BUCKETS)
  })

  it('fetches the reserve bucket list once on mount', async () => {
    const { result } = renderHook(() => useReserveBuckets())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getReserveBucketsMock).toHaveBeenCalledTimes(1)
    expect(result.current.reserveBuckets).toEqual(BUCKETS)
  })

  it('surfaces a fetch error', async () => {
    getReserveBucketsMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useReserveBuckets())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('retry re-fetches the list', async () => {
    const { result } = renderHook(() => useReserveBuckets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getReserveBucketsMock).toHaveBeenCalledTimes(2))
  })

  it('reports no warning when active buckets sum to 100', async () => {
    const { result } = renderHook(() => useReserveBuckets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.activeSplitWarning).toBeNull()
  })

  it('reports a warning when active buckets do not sum to 100', async () => {
    getReserveBucketsMock.mockResolvedValue([
      { id: 'b1', name: 'Investimento', isActive: true, splitPercentage: 60, warning: null },
    ])
    const { result } = renderHook(() => useReserveBuckets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.activeSplitWarning).toContain('60')
    expect(result.current.activeSplitWarning).toContain('review your split percentages')
  })

  it('createReserveBucket calls the API and re-fetches the list', async () => {
    createReserveBucketMock.mockResolvedValue({
      id: 'b3',
      name: 'Emergencia',
      isActive: true,
      splitPercentage: 10,
      warning: 'Active buckets currently sum to 110% — review your split percentages',
    })
    const { result } = renderHook(() => useReserveBuckets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.createReserveBucket({ name: 'Emergencia', splitPercentage: 10, isActive: true })
    })

    expect(createReserveBucketMock).toHaveBeenCalledWith({ name: 'Emergencia', splitPercentage: 10, isActive: true })
    await waitFor(() => expect(getReserveBucketsMock).toHaveBeenCalledTimes(2))
  })

  it('createReserveBucket propagates a rejected promise to the caller without swallowing it', async () => {
    createReserveBucketMock.mockRejectedValue(new Error('A reserve bucket named "Investimento" already exists.'))
    const { result } = renderHook(() => useReserveBuckets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await expect(
      result.current.createReserveBucket({ name: 'Investimento', splitPercentage: 10, isActive: true }),
    ).rejects.toThrow('A reserve bucket named "Investimento" already exists.')
  })

  it('updateReserveBucket calls the API and re-fetches the list', async () => {
    updateReserveBucketMock.mockResolvedValue({
      id: 'b1',
      name: 'InvestimentoRenamed',
      isActive: true,
      splitPercentage: 70,
      warning: null,
    })
    const { result } = renderHook(() => useReserveBuckets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.updateReserveBucket('b1', { name: 'InvestimentoRenamed', splitPercentage: 70, isActive: true })
    })

    expect(updateReserveBucketMock).toHaveBeenCalledWith('b1', { name: 'InvestimentoRenamed', splitPercentage: 70, isActive: true })
    await waitFor(() => expect(getReserveBucketsMock).toHaveBeenCalledTimes(2))
  })

  it('deactivateReserveBucket calls update with isActive false and re-fetches the list', async () => {
    updateReserveBucketMock.mockResolvedValue({ ...BUCKETS[0], isActive: false })
    const { result } = renderHook(() => useReserveBuckets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deactivateReserveBucket(BUCKETS[0]))

    await waitFor(() => expect(result.current.savingId).toBeNull())
    expect(updateReserveBucketMock).toHaveBeenCalledWith('b1', { name: 'Investimento', splitPercentage: 60, isActive: false })
    await waitFor(() => expect(getReserveBucketsMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a deactivate error without re-fetching', async () => {
    updateReserveBucketMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useReserveBuckets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deactivateReserveBucket(BUCKETS[0]))

    await waitFor(() => expect(result.current.saveError).toBe('Network down'))
    expect(getReserveBucketsMock).toHaveBeenCalledTimes(1)
  })
})
