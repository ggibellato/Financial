import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { InvestmentSnapshotSuggestionsDto } from '../../api/types'
import { useSuggestedValues } from '../useSuggestedValues'

const { getInvestmentSnapshotSuggestionsMock, updateInvestmentSnapshotValueMock } = vi.hoisted(() => ({
  getInvestmentSnapshotSuggestionsMock: vi.fn<FinancialApiClient['getInvestmentSnapshotSuggestions']>(),
  updateInvestmentSnapshotValueMock: vi.fn<FinancialApiClient['updateInvestmentSnapshotValue']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getInvestmentSnapshotSuggestions: getInvestmentSnapshotSuggestionsMock,
    updateInvestmentSnapshotValue: updateInvestmentSnapshotValueMock,
  } as Partial<FinancialApiClient>,
}))

const SUGGESTIONS: InvestmentSnapshotSuggestionsDto = {
  suggestions: [
    {
      snapshotId: 's1',
      accountId: 'a1',
      accountName: 'PlatinumVisa8003',
      currentValue: 0,
      suggestedValue: 142.17,
      sourceDescription: 'BarclaysPlatinumVisa8003 — Aug 2026 statement',
    },
    {
      snapshotId: 's2',
      accountId: 'a2',
      accountName: 'ReservasPessoais',
      currentValue: 5400,
      suggestedValue: 5612.3,
      sourceDescription: 'Sum of reserve buckets — as of Jul 2026',
    },
  ],
  notUpdated: [{ accountId: 'a3', accountName: 'ChaseMaster4023', reason: 'No statement for this month yet' }],
}

describe('useSuggestedValues', () => {
  beforeEach(() => {
    getInvestmentSnapshotSuggestionsMock.mockReset()
    updateInvestmentSnapshotValueMock.mockReset()
    getInvestmentSnapshotSuggestionsMock.mockResolvedValue(SUGGESTIONS)
  })

  it('defaults zero current value rows to included', async () => {
    const { result } = renderHook(() => useSuggestedValues(2026, 8, vi.fn()))

    act(() => result.current.open())
    await waitFor(() => expect(result.current.phase).toBe('ready'))

    expect(result.current.rows.find((r) => r.accountId === 'a1')?.included).toBe(true)
  })

  it('defaults nonzero current value rows to unchecked', async () => {
    const { result } = renderHook(() => useSuggestedValues(2026, 8, vi.fn()))

    act(() => result.current.open())
    await waitFor(() => expect(result.current.phase).toBe('ready'))

    expect(result.current.rows.find((r) => r.accountId === 'a2')?.included).toBe(false)
  })

  it('applies only checked rows sequentially', async () => {
    updateInvestmentSnapshotValueMock.mockResolvedValue({
      id: 's1',
      accountId: 'a1',
      accountName: 'PlatinumVisa8003',
      isLiability: true,
      year: 2026,
      month: 8,
      value: 142.17,
    })
    const onApplied = vi.fn()
    const { result } = renderHook(() => useSuggestedValues(2026, 8, onApplied))

    act(() => result.current.open())
    await waitFor(() => expect(result.current.phase).toBe('ready'))

    await act(async () => {
      await result.current.apply()
    })

    expect(updateInvestmentSnapshotValueMock).toHaveBeenCalledTimes(1)
    expect(updateInvestmentSnapshotValueMock).toHaveBeenCalledWith('s1', { value: 142.17 })
    expect(onApplied).toHaveBeenCalledTimes(1)
  })

  it('continues past a failed row', async () => {
    const { result } = renderHook(() => useSuggestedValues(2026, 8, vi.fn()))

    act(() => result.current.open())
    await waitFor(() => expect(result.current.phase).toBe('ready'))

    act(() => result.current.toggleIncluded('a2'))
    updateInvestmentSnapshotValueMock.mockRejectedValueOnce(new Error('boom')).mockResolvedValueOnce({
      id: 's2',
      accountId: 'a2',
      accountName: 'ReservasPessoais',
      isLiability: true,
      year: 2026,
      month: 8,
      value: 5612.3,
    })

    await act(async () => {
      await result.current.apply()
    })

    expect(updateInvestmentSnapshotValueMock).toHaveBeenCalledTimes(2)
    expect(result.current.phase).toBe('completed')
    expect(result.current.failedRows).toHaveLength(1)
    expect(result.current.succeededCount).toBe(1)
  })

  it('retry failed resubmits prior values without refetching', async () => {
    const { result } = renderHook(() => useSuggestedValues(2026, 8, vi.fn()))

    act(() => result.current.open())
    await waitFor(() => expect(result.current.phase).toBe('ready'))

    updateInvestmentSnapshotValueMock.mockRejectedValueOnce(new Error('boom'))
    await act(async () => {
      await result.current.apply()
    })
    expect(result.current.phase).toBe('completed')
    expect(getInvestmentSnapshotSuggestionsMock).toHaveBeenCalledTimes(1)

    updateInvestmentSnapshotValueMock.mockResolvedValueOnce({
      id: 's1',
      accountId: 'a1',
      accountName: 'PlatinumVisa8003',
      isLiability: true,
      year: 2026,
      month: 8,
      value: 142.17,
    })
    await act(async () => {
      await result.current.retryFailed()
    })

    expect(getInvestmentSnapshotSuggestionsMock).toHaveBeenCalledTimes(1)
    expect(updateInvestmentSnapshotValueMock).toHaveBeenLastCalledWith('s1', { value: 142.17 })
    expect(result.current.phase).toBe('closed')
  })
})
