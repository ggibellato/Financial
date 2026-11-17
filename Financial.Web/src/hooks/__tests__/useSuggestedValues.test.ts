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

const SNAPSHOT = { id: 's1', accountId: 'a1', accountName: 'PlatinumVisa8003', isLiability: true, year: 2026, month: 8, value: 142.17 }

async function renderOpened(onApplied = vi.fn(), suggestions: InvestmentSnapshotSuggestionsDto = SUGGESTIONS) {
  getInvestmentSnapshotSuggestionsMock.mockResolvedValue(suggestions)
  const rendered = renderHook(() => useSuggestedValues(2026, 8, onApplied))
  act(() => rendered.result.current.open())
  await waitFor(() => expect(rendered.result.current.phase).toBe('ready'))
  return rendered.result
}

describe('useSuggestedValues', () => {
  beforeEach(() => {
    getInvestmentSnapshotSuggestionsMock.mockReset()
    updateInvestmentSnapshotValueMock.mockReset()
    getInvestmentSnapshotSuggestionsMock.mockResolvedValue(SUGGESTIONS)
  })

  it('defaults zero current value rows to included', async () => {
    const result = await renderOpened()

    expect(result.current.rows.find((r) => r.accountId === 'a1')?.included).toBe(true)
  })

  it('defaults nonzero current value rows to unchecked', async () => {
    const result = await renderOpened()

    expect(result.current.rows.find((r) => r.accountId === 'a2')?.included).toBe(false)
  })

  it('sets an error phase with a message when the fetch fails, and retryFetch re-attempts it', async () => {
    getInvestmentSnapshotSuggestionsMock.mockRejectedValueOnce(new Error('Network down'))
    const { result } = renderHook(() => useSuggestedValues(2026, 8, vi.fn()))

    act(() => result.current.open())
    await waitFor(() => expect(result.current.phase).toBe('error'))
    expect(result.current.fetchError).toBe('Network down')

    getInvestmentSnapshotSuggestionsMock.mockResolvedValueOnce(SUGGESTIONS)
    act(() => result.current.retryFetch())
    await waitFor(() => expect(result.current.phase).toBe('ready'))
    expect(result.current.rows).toHaveLength(2)
  })

  it('applies only checked rows sequentially', async () => {
    updateInvestmentSnapshotValueMock.mockResolvedValue(SNAPSHOT)
    const onApplied = vi.fn()
    const result = await renderOpened(onApplied)

    await act(async () => {
      await result.current.apply()
    })

    expect(updateInvestmentSnapshotValueMock).toHaveBeenCalledTimes(1)
    expect(updateInvestmentSnapshotValueMock).toHaveBeenCalledWith('s1', { value: 142.17 })
    expect(onApplied).toHaveBeenCalledTimes(1)
  })

  it('continues past a failed row, counting only the applied rows that succeeded or failed', async () => {
    const result = await renderOpened(vi.fn(), {
      ...SUGGESTIONS,
      suggestions: [
        ...SUGGESTIONS.suggestions,
        { snapshotId: 's3', accountId: 'a3', accountName: 'Untouched', currentValue: 10, suggestedValue: 12, sourceDescription: 'x' },
      ],
    })
    act(() => result.current.toggleIncluded('a2'))
    updateInvestmentSnapshotValueMock.mockRejectedValueOnce(new Error('boom')).mockResolvedValueOnce({ ...SNAPSHOT, id: 's2', accountId: 'a2' })

    await act(async () => {
      await result.current.apply()
    })

    expect(updateInvestmentSnapshotValueMock).toHaveBeenCalledTimes(2)
    expect(result.current.phase).toBe('completed')
    expect(result.current.failedRows.map((row) => row.accountId)).toEqual(['a1'])
    expect(result.current.succeededCount).toBe(1)
  })

  it('retry failed resubmits prior values without refetching', async () => {
    const result = await renderOpened()

    updateInvestmentSnapshotValueMock.mockRejectedValueOnce(new Error('boom'))
    await act(async () => {
      await result.current.apply()
    })
    expect(result.current.phase).toBe('completed')
    expect(getInvestmentSnapshotSuggestionsMock).toHaveBeenCalledTimes(1)

    updateInvestmentSnapshotValueMock.mockResolvedValueOnce(SNAPSHOT)
    await act(async () => {
      await result.current.retryFailed()
    })

    expect(getInvestmentSnapshotSuggestionsMock).toHaveBeenCalledTimes(1)
    expect(updateInvestmentSnapshotValueMock).toHaveBeenCalledTimes(2)
    expect(updateInvestmentSnapshotValueMock).toHaveBeenLastCalledWith('s1', { value: 142.17 })
    expect(result.current.phase).toBe('closed')
  })

  it('is closed until opened', () => {
    const { result } = renderHook(() => useSuggestedValues(2026, 8, vi.fn()))

    expect(result.current.isOpen).toBe(false)
  })

  it('loads the suggestions of the month it is opened for', async () => {
    const { result, rerender } = renderHook(({ month }) => useSuggestedValues(2026, month, vi.fn()), { initialProps: { month: 8 } })

    rerender({ month: 9 })
    act(() => result.current.open())

    expect(getInvestmentSnapshotSuggestionsMock).toHaveBeenCalledWith(2026, 9)
    await waitFor(() => expect(result.current.phase).toBe('ready'))
  })

  it('shows the load fallback message when the load fails without one', async () => {
    getInvestmentSnapshotSuggestionsMock.mockRejectedValue('unexpected')
    const { result } = renderHook(() => useSuggestedValues(2026, 8, vi.fn()))

    act(() => result.current.open())

    await waitFor(() => expect(result.current.fetchError).toBe("Couldn't load suggestions"))
  })

  it('counts the rows that are ticked', async () => {
    const result = await renderOpened()
    const before = result.current.checkedCount

    act(() => result.current.toggleIncluded('a2'))

    expect(before).toBe(1)
    expect(result.current.checkedCount).toBe(2)
  })

  it('applies an edited value to its own row only', async () => {
    updateInvestmentSnapshotValueMock.mockResolvedValue(SNAPSHOT)
    const result = await renderOpened()

    act(() => result.current.setValue('a1', '150'))

    expect(result.current.rows.map((row) => row.suggestedValue)).toEqual(['150', '5612.3'])
    await act(async () => {
      await result.current.apply()
    })
    expect(updateInvestmentSnapshotValueMock).toHaveBeenCalledWith('s1', { value: 150 })
  })

  it('marks a negative edited value as failed without sending it, and reports nothing applied', async () => {
    const onApplied = vi.fn()
    const result = await renderOpened(onApplied)
    act(() => result.current.setValue('a1', '-5'))

    await act(async () => {
      await result.current.apply()
    })

    expect(updateInvestmentSnapshotValueMock).not.toHaveBeenCalled()
    expect(result.current.failedRows.map((row) => row.accountId)).toEqual(['a1'])
    expect(onApplied).not.toHaveBeenCalled()
  })

  it('shows which row is being applied out of how many', async () => {
    updateInvestmentSnapshotValueMock.mockReturnValue(new Promise(() => {}))
    const result = await renderOpened()

    act(() => {
      void result.current.apply()
    })

    expect(result.current.applyProgress).toEqual({ current: 1, total: 1, accountName: 'PlatinumVisa8003' })
  })

  it('reports the applied values to the latest callback it was given', async () => {
    updateInvestmentSnapshotValueMock.mockResolvedValue(SNAPSHOT)
    const first = vi.fn()
    const latest = vi.fn()
    const { result, rerender } = renderHook(({ onApplied }) => useSuggestedValues(2026, 8, onApplied), { initialProps: { onApplied: first } })
    act(() => result.current.open())
    await waitFor(() => expect(result.current.phase).toBe('ready'))

    rerender({ onApplied: latest })
    await act(async () => {
      await result.current.apply()
    })

    expect(latest).toHaveBeenCalledOnce()
    expect(first).not.toHaveBeenCalled()
  })
})
