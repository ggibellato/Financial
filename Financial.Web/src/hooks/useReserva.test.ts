import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/apiError'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { ReserveBucketBalanceDto, ReserveMovementDto } from '../api/types'
import { useReserva } from './useReserva'

const getReserveBalancesMock = vi.fn<FinancialApiClient['getReserveBalances']>()
const getReserveMovementsMock = vi.fn<FinancialApiClient['getReserveMovements']>()
const postIncomeSplitMock = vi.fn<FinancialApiClient['postIncomeSplit']>()
const postWithdrawalMock = vi.fn<FinancialApiClient['postWithdrawal']>()
const updateReserveMovementMock = vi.fn<FinancialApiClient['updateReserveMovement']>()
const deleteReserveMovementMock = vi.fn<FinancialApiClient['deleteReserveMovement']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getReserveBalances: getReserveBalancesMock,
    getReserveMovements: getReserveMovementsMock,
    postIncomeSplit: postIncomeSplitMock,
    postWithdrawal: postWithdrawalMock,
    updateReserveMovement: updateReserveMovementMock,
    deleteReserveMovement: deleteReserveMovementMock,
  }),
}))

const BALANCES: ReserveBucketBalanceDto[] = [
  { bucket: 'Investimento', balance: 654.33 },
  { bucket: 'HouseTreats', balance: 654.33 },
  { bucket: 'Ariana', balance: 327.17 },
  { bucket: 'Gleison', balance: 327.17 },
]

const MOVEMENTS: ReserveMovementDto[] = [
  { id: 'm1', bucket: 'Investimento', amount: 654.33, date: '2026-07-17', description: 'Ramsay' },
  { id: 'm2', bucket: 'HouseTreats', amount: 654.33, date: '2026-07-17', description: 'Ramsay' },
  { id: 'm3', bucket: 'Ariana', amount: 327.17, date: '2026-07-17', description: 'Ramsay' },
  { id: 'm4', bucket: 'Gleison', amount: 327.17, date: '2026-07-17', description: 'Ramsay' },
]

describe('useReserva', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getReserveBalancesMock.mockResolvedValue(BALANCES)
    getReserveMovementsMock.mockResolvedValue(MOVEMENTS)
  })

  it('loads balances and movements on mount', async () => {
    const { result } = renderHook(() => useReserva())

    expect(result.current.isLoading).toBe(true)

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.balances).toEqual(BALANCES)
    expect(result.current.movements).toEqual(MOVEMENTS)
    expect(result.current.error).toBeNull()
  })

  it('marks the last movement of a same date+description group with the group total', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    const groupTotals = result.current.movementRows.map((m) => m.groupTotal)
    expect(groupTotals.slice(0, 3)).toEqual([null, null, null])
    expect(groupTotals[3]).toBeCloseTo(1963, 2)
  })

  it('does not attach a group total to a lone movement', async () => {
    getReserveMovementsMock.mockResolvedValue([
      { id: 'm5', bucket: 'Investimento', amount: -30, date: '2026-07-18', description: 'Groceries top-up' },
    ])
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.movementRows[0].groupTotal).toBeNull()
    expect(result.current.movementRows[0].isPartOfGroup).toBe(false)
  })

  it('marks every movement of a split group as part of a group, not just the last', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.movementRows.map((m) => m.isPartOfGroup)).toEqual([true, true, true, true])
  })

  it('computes the total balance across all buckets', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    // 654.33 + 654.33 + 327.17 + 327.17 = 1963.00
    expect(result.current.totalBalance).toBeCloseTo(1963, 2)
  })

  it('surfaces a fetch error', async () => {
    getReserveBalancesMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useReserva())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('submits an income split and re-fetches balances/movements on success', async () => {
    postIncomeSplitMock.mockResolvedValue({
      investimento: 654.33,
      houseTreats: 654.33,
      ariana: 327.17,
      gleison: 327.17,
      total: 1963,
    })
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setSplitField('splitDate', '2026-07-01'))
    act(() => result.current.setSplitField('splitAmount', '1963'))
    act(() => result.current.setSplitField('splitDescription', 'Ramsay'))
    act(() => result.current.submitIncomeSplit())

    await waitFor(() => expect(postIncomeSplitMock).toHaveBeenCalledTimes(1))
    expect(postIncomeSplitMock).toHaveBeenCalledWith({ date: '2026-07-01', amount: 1963, description: 'Ramsay' })
    await waitFor(() => expect(getReserveBalancesMock).toHaveBeenCalledTimes(2))
    expect(result.current.lastSplitResult).toEqual({
      investimento: 654.33,
      houseTreats: 654.33,
      ariana: 327.17,
      gleison: 327.17,
      total: 1963,
    })
  })

  it('rejects an income split with a non-positive amount before calling the API', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setSplitField('splitDate', '2026-07-01'))
    act(() => result.current.setSplitField('splitAmount', '0'))
    act(() => result.current.submitIncomeSplit())

    await waitFor(() => expect(result.current.splitError).toBe('Amount must be a positive number'))
    expect(postIncomeSplitMock).not.toHaveBeenCalled()
  })

  it('rejects an income split with a missing description before calling the API', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setSplitField('splitDate', '2026-07-01'))
    act(() => result.current.setSplitField('splitAmount', '1963'))
    act(() => result.current.submitIncomeSplit())

    await waitFor(() => expect(result.current.splitError).toBe('Description is required'))
    expect(postIncomeSplitMock).not.toHaveBeenCalled()
  })

  it('surfaces a validation error from the backend on income split failure', async () => {
    postIncomeSplitMock.mockRejectedValue(new Error('Amount must be greater than zero.'))
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setSplitField('splitDate', '2026-07-01'))
    act(() => result.current.setSplitField('splitAmount', '1963'))
    act(() => result.current.setSplitField('splitDescription', 'Ramsay'))
    act(() => result.current.submitIncomeSplit())

    await waitFor(() => expect(result.current.splitError).toBe('Amount must be greater than zero.'))
  })

  it('submits a withdrawal and re-fetches on success', async () => {
    postWithdrawalMock.mockResolvedValue({
      id: 'm2',
      bucket: 'Investimento',
      amount: -30,
      date: '2026-07-01',
      description: 'Groceries top-up',
    })
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setWithdrawalField('withdrawalAmount', '30'))
    act(() => result.current.setWithdrawalField('withdrawalDate', '2026-07-01'))
    act(() => result.current.setWithdrawalField('withdrawalDescription', 'Groceries top-up'))
    act(() => result.current.submitWithdrawal())

    await waitFor(() => expect(postWithdrawalMock).toHaveBeenCalledTimes(1))
    expect(postWithdrawalMock).toHaveBeenCalledWith(
      expect.objectContaining({ amount: 30, confirmed: false }),
    )
    await waitFor(() => expect(getReserveBalancesMock).toHaveBeenCalledTimes(2))
  })

  it('prompts for confirmation on a 409 and resubmits confirmed when accepted', async () => {
    vi.stubGlobal('confirm', vi.fn(() => true))
    postWithdrawalMock
      .mockRejectedValueOnce(new ApiError('This withdrawal exceeds the balance.', 409))
      .mockResolvedValueOnce({
        id: 'm3',
        bucket: 'Ariana',
        amount: -100,
        date: '2026-07-01',
        description: 'Big purchase',
      })
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setWithdrawalField('withdrawalBucket', 'Ariana'))
    act(() => result.current.setWithdrawalField('withdrawalAmount', '100'))
    act(() => result.current.setWithdrawalField('withdrawalDate', '2026-07-01'))
    act(() => result.current.setWithdrawalField('withdrawalDescription', 'Big purchase'))
    act(() => result.current.submitWithdrawal())

    await waitFor(() => expect(postWithdrawalMock).toHaveBeenCalledTimes(2))
    expect(postWithdrawalMock).toHaveBeenNthCalledWith(1, expect.objectContaining({ confirmed: false }))
    expect(postWithdrawalMock).toHaveBeenNthCalledWith(2, expect.objectContaining({ confirmed: true }))
  })

  it('does not resubmit a 409 withdrawal when the user declines the confirmation', async () => {
    vi.stubGlobal('confirm', vi.fn(() => false))
    postWithdrawalMock.mockRejectedValue(new ApiError('This withdrawal exceeds the balance.', 409))
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setWithdrawalField('withdrawalAmount', '100'))
    act(() => result.current.setWithdrawalField('withdrawalDate', '2026-07-01'))
    act(() => result.current.setWithdrawalField('withdrawalDescription', 'Big purchase'))
    act(() => result.current.submitWithdrawal())

    await waitFor(() => expect(result.current.withdrawalError).toBe('This withdrawal exceeds the balance.'))
    expect(postWithdrawalMock).toHaveBeenCalledTimes(1)
  })

  it('saves a movement edit and re-fetches on success', async () => {
    updateReserveMovementMock.mockResolvedValue({ ...MOVEMENTS[0], amount: 700 })
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditMovementForm(MOVEMENTS[0]))
    act(() => result.current.setEditMovementField('editMovementAmount', '700'))
    act(() => result.current.saveMovementEdit())

    await waitFor(() =>
      expect(updateReserveMovementMock).toHaveBeenCalledWith('m1', {
        bucket: 'Investimento',
        amount: 700,
        date: '2026-07-17',
        description: 'Ramsay',
      }),
    )
    await waitFor(() => expect(getReserveBalancesMock).toHaveBeenCalledTimes(2))
    expect(result.current.editingMovementId).toBeNull()
  })

  it('surfaces a movement-edit error without crashing', async () => {
    updateReserveMovementMock.mockRejectedValue(new Error('Description is required.'))
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditMovementForm(MOVEMENTS[0]))
    act(() => result.current.setEditMovementField('editMovementDescription', ''))
    act(() => result.current.saveMovementEdit())

    await waitFor(() => expect(result.current.saveMovementError).toBe('Description is required'))
    expect(updateReserveMovementMock).not.toHaveBeenCalled()
  })

  it('deletes a movement and re-fetches on success', async () => {
    deleteReserveMovementMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteMovement('m1'))

    await waitFor(() => expect(deleteReserveMovementMock).toHaveBeenCalledWith('m1'))
    await waitFor(() => expect(getReserveBalancesMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a delete error without crashing', async () => {
    deleteReserveMovementMock.mockRejectedValue(new Error('Reserve movement not found.'))
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteMovement('unknown'))

    await waitFor(() => expect(result.current.deleteMovementError).toBe('Reserve movement not found.'))
  })
})
