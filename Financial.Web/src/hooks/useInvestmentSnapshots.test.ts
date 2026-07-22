import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { InvestmentSnapshotDto } from '../api/types'
import { useInvestmentSnapshots } from './useInvestmentSnapshots'

const NOW = new Date()
const CURRENT_YEAR = NOW.getFullYear()
const CURRENT_MONTH = NOW.getMonth() + 1
const CURRENT_MONTH_INPUT = `${CURRENT_YEAR}-${String(CURRENT_MONTH).padStart(2, '0')}`
const NEXT_MONTH = CURRENT_MONTH === 12 ? 1 : CURRENT_MONTH + 1
const NEXT_MONTH_YEAR = CURRENT_MONTH === 12 ? CURRENT_YEAR + 1 : CURRENT_YEAR
const NEXT_MONTH_INPUT = `${NEXT_MONTH_YEAR}-${String(NEXT_MONTH).padStart(2, '0')}`

const getInvestmentSnapshotsMock = vi.fn<FinancialApiClient['getInvestmentSnapshots']>()
const updateInvestmentSnapshotValueMock = vi.fn<FinancialApiClient['updateInvestmentSnapshotValue']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getInvestmentSnapshots: getInvestmentSnapshotsMock,
    updateInvestmentSnapshotValue: updateInvestmentSnapshotValueMock,
  }),
}))

const SNAPSHOTS: InvestmentSnapshotDto[] = [
  { id: 's1', account: 'ChaseSave', isLiability: false, year: CURRENT_YEAR, month: CURRENT_MONTH, value: 1000 },
  { id: 's2', account: 'PlatinumVisa8003', isLiability: true, year: CURRENT_YEAR, month: CURRENT_MONTH, value: 250 },
]

describe('useInvestmentSnapshots', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getInvestmentSnapshotsMock.mockResolvedValue(SNAPSHOTS)
  })

  it('fetches the current month snapshots on mount', async () => {
    const { result } = renderHook(() => useInvestmentSnapshots())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getInvestmentSnapshotsMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(result.current.monthInputValue).toBe(CURRENT_MONTH_INPUT)
    expect(result.current.snapshots).toEqual(SNAPSHOTS)
  })

  it('re-fetches for a new month when the month input changes', async () => {
    const { result } = renderHook(() => useInvestmentSnapshots())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setMonthInputValue(NEXT_MONTH_INPUT))

    await waitFor(() => expect(getInvestmentSnapshotsMock).toHaveBeenCalledWith(NEXT_MONTH_YEAR, NEXT_MONTH))
  })

  it('surfaces a fetch error', async () => {
    getInvestmentSnapshotsMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useInvestmentSnapshots())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('saves an edit and re-fetches, leaving other snapshots untouched', async () => {
    updateInvestmentSnapshotValueMock.mockResolvedValue({ ...SNAPSHOTS[0], value: 1500 })
    const { result } = renderHook(() => useInvestmentSnapshots())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(SNAPSHOTS[0]))
    act(() => result.current.setEditValue('1500'))
    act(() => result.current.saveEdit())

    await waitFor(() => expect(updateInvestmentSnapshotValueMock).toHaveBeenCalledWith('s1', { value: 1500 }))
    await waitFor(() => expect(getInvestmentSnapshotsMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a save error without crashing', async () => {
    updateInvestmentSnapshotValueMock.mockRejectedValue(new Error('Value must not be negative.'))
    const { result } = renderHook(() => useInvestmentSnapshots())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(SNAPSHOTS[0]))
    act(() => result.current.setEditValue('-5'))
    act(() => result.current.saveEdit())

    await waitFor(() => expect(result.current.saveError).toBeTruthy())
  })
})
