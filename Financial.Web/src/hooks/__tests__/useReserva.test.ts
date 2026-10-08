import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BankDto, CategoryDto, ReserveBucketBalanceDto, ReserveBucketDto, ReserveMovementDto } from '../../api/types'
import { useReserva } from '../useReserva'
import { pinDate } from '../../test-utils/pinDate'

const {
  getReserveBalancesMock,
  getReserveMovementsMock,
  getReserveBucketsMock,
  getReserveSplitStatusMock,
  getBanksMock,
  getCategoriesMock,
  postIncomeSplitMock,
  postWithdrawalMock,
  updateReserveMovementMock,
  deleteReserveMovementMock,
} = vi.hoisted(() => ({
  getReserveBalancesMock: vi.fn<FinancialApiClient['getReserveBalances']>(),
  getReserveMovementsMock: vi.fn<FinancialApiClient['getReserveMovements']>(),
  getReserveBucketsMock: vi.fn<FinancialApiClient['getReserveBuckets']>(),
  getReserveSplitStatusMock: vi.fn<FinancialApiClient['getReserveSplitStatus']>(),
  getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
  getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
  postIncomeSplitMock: vi.fn<FinancialApiClient['postIncomeSplit']>(),
  postWithdrawalMock: vi.fn<FinancialApiClient['postWithdrawal']>(),
  updateReserveMovementMock: vi.fn<FinancialApiClient['updateReserveMovement']>(),
  deleteReserveMovementMock: vi.fn<FinancialApiClient['deleteReserveMovement']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getReserveBalances: getReserveBalancesMock,
    getReserveMovements: getReserveMovementsMock,
    getReserveBuckets: getReserveBucketsMock,
    getReserveSplitStatus: getReserveSplitStatusMock,
    getBanks: getBanksMock,
    getCategories: getCategoriesMock,
    postIncomeSplit: postIncomeSplitMock,
    postWithdrawal: postWithdrawalMock,
    updateReserveMovement: updateReserveMovementMock,
    deleteReserveMovement: deleteReserveMovementMock,
  } as Partial<FinancialApiClient>,
}))

const BALANCES: ReserveBucketBalanceDto[] = [
  { bucketId: 'b1', bucketName: 'Investimento', balance: 654.33 },
  { bucketId: 'b2', bucketName: 'HouseTreats', balance: 654.33 },
  { bucketId: 'b3', bucketName: 'Ariana', balance: 327.17 },
  { bucketId: 'b4', bucketName: 'Gleison', balance: 327.17 },
]

const MOVEMENTS: ReserveMovementDto[] = [
  { id: 'm1', bucketId: 'b1', bucketName: 'Investimento', amount: 654.33, date: '2026-07-17', description: 'Ramsay', incomeId: null },
  { id: 'm2', bucketId: 'b2', bucketName: 'HouseTreats', amount: 654.33, date: '2026-07-17', description: 'Ramsay', incomeId: null },
  { id: 'm3', bucketId: 'b3', bucketName: 'Ariana', amount: 327.17, date: '2026-07-17', description: 'Ramsay', incomeId: null },
  { id: 'm4', bucketId: 'b4', bucketName: 'Gleison', amount: 327.17, date: '2026-07-17', description: 'Ramsay', incomeId: null },
]

const BUCKETS: ReserveBucketDto[] = [
  { id: 'b1', name: 'Investimento', isActive: true, splitPercentage: 33.33, warning: null },
  { id: 'b2', name: 'HouseTreats', isActive: true, splitPercentage: 33.33, warning: null },
  { id: 'b3', name: 'Ariana', isActive: true, splitPercentage: 16.67, warning: null },
  { id: 'b4', name: 'Gleison', isActive: true, splitPercentage: 16.67, warning: null },
]

const BANKS: BankDto[] = [
  { id: 'bk1', name: 'Chase', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false },
  { id: 'bk2', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false },
]

const CATEGORIES: CategoryDto[] = [
  { id: 'c1', name: 'Ariana', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'c2', name: 'Saude', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'c3', name: 'Investimento', active: true, isInvestment: true, isTithe: false, hasReferences: false },
  { id: 'c4', name: 'Reserva', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'c5', name: 'Old', active: false, isInvestment: false, isTithe: false, hasReferences: false },
]

// Only the 409 tests should reach the confirmation policy. Everywhere else, being asked at all
// is the bug - so the default stub fails the test instead of quietly answering.
const rejectUnexpectedConfirm = (): boolean => {
  throw new Error('submitWithdrawal consulted confirmProceed when no 409 was returned')
}

describe('useReserva', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  beforeEach(() => {
    getReserveBalancesMock.mockReset()
    getReserveMovementsMock.mockReset()
    getReserveBucketsMock.mockReset()
    getReserveSplitStatusMock.mockReset()
    getBanksMock.mockReset()
    getCategoriesMock.mockReset()
    postIncomeSplitMock.mockReset()
    postWithdrawalMock.mockReset()
    updateReserveMovementMock.mockReset()
    deleteReserveMovementMock.mockReset()
    getReserveBalancesMock.mockResolvedValue(BALANCES)
    getReserveMovementsMock.mockResolvedValue(MOVEMENTS)
    getReserveBucketsMock.mockResolvedValue(BUCKETS)
    getReserveSplitStatusMock.mockResolvedValue({ activeTotal: 100, warning: null })
    getBanksMock.mockResolvedValue(BANKS)
    getCategoriesMock.mockResolvedValue(CATEGORIES)
    sessionStorage.clear()
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
      { id: 'm5', bucketId: 'b1', bucketName: 'Investimento', amount: -30, date: '2026-07-18', description: 'Groceries top-up', incomeId: null },
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

  it('marks a movement with a non-null incomeId as locked', async () => {
    getReserveMovementsMock.mockResolvedValue([
      { id: 'm1', bucketId: 'b1', bucketName: 'Investimento', amount: 100, date: '2026-07-25', description: 'Salary', incomeId: 'income-1' },
    ])
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.movementRows[0].isLocked).toBe(true)
  })

  it('does not mark a movement with a null incomeId as locked', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.movementRows.every((m) => !m.isLocked)).toBe(true)
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
    const splitResult = {
      buckets: [
        { bucketId: 'b1', bucketName: 'Investimento', amount: 654.33 },
        { bucketId: 'b2', bucketName: 'HouseTreats', amount: 654.33 },
        { bucketId: 'b3', bucketName: 'Ariana', amount: 327.17 },
        { bucketId: 'b4', bucketName: 'Gleison', amount: 327.17 },
      ],
      total: 1963,
    }
    postIncomeSplitMock.mockResolvedValue(splitResult)
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setSplitField('splitDate', '2026-07-01'))
    act(() => result.current.setSplitField('splitAmount', '1963'))
    act(() => result.current.setSplitField('splitDescription', 'Ramsay'))
    act(() => result.current.submitIncomeSplit())

    await waitFor(() => expect(postIncomeSplitMock).toHaveBeenCalledTimes(1))
    expect(postIncomeSplitMock).toHaveBeenCalledWith({ date: '2026-07-01', amount: 1963, description: 'Ramsay' })
    await waitFor(() => expect(getReserveBalancesMock).toHaveBeenCalledTimes(2))
    expect(result.current.lastSplitResult).toEqual(splitResult)
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

  it('showSplitForm defaults the date to today when nothing was persisted yet', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    pinDate('2026-07-01T00:30:00+01:00')
    act(() => result.current.showSplitForm())

    expect(result.current.splitDate).toBe('2026-07-01')
  })

  it('persists the split date after a successful submit, for the next split form', async () => {
    postIncomeSplitMock.mockResolvedValue({ buckets: [], total: 1963 })
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    act(() => result.current.showSplitForm())
    act(() => result.current.setSplitField('splitDate', '2026-07-01'))
    act(() => result.current.setSplitField('splitAmount', '1963'))
    act(() => result.current.setSplitField('splitDescription', 'Ramsay'))
    act(() => result.current.submitIncomeSplit())
    await waitFor(() => expect(postIncomeSplitMock).toHaveBeenCalledTimes(1))

    act(() => result.current.showSplitForm())

    expect(result.current.splitDate).toBe('2026-07-01')
    expect(result.current.splitAmount).toBe('')
    expect(result.current.splitDescription).toBe('')
  })

  it('submits a withdrawal and re-fetches on success', async () => {
    postWithdrawalMock.mockResolvedValue({
      id: 'm2',
      bucketId: 'b1', bucketName: 'Investimento',
      amount: -30,
      date: '2026-07-01',
      description: 'Groceries top-up',
      incomeId: null,
    })
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setWithdrawalField('withdrawalAmount', '30'))
    act(() => result.current.setWithdrawalField('withdrawalDate', '2026-07-01'))
    act(() => result.current.setWithdrawalField('withdrawalDescription', 'Groceries top-up'))
    act(() => result.current.submitWithdrawal(rejectUnexpectedConfirm))

    await waitFor(() => expect(postWithdrawalMock).toHaveBeenCalledTimes(1))
    expect(postWithdrawalMock).toHaveBeenCalledWith(
      expect.objectContaining({ amount: 30, confirmed: false }),
    )
    await waitFor(() => expect(getReserveBalancesMock).toHaveBeenCalledTimes(2))
  })

  it('asks the caller on a 409 and resubmits confirmed when it accepts', async () => {
    const confirmProceed = vi.fn(() => true)
    postWithdrawalMock
      .mockRejectedValueOnce(new ApiError('This withdrawal exceeds the balance.', 409))
      .mockResolvedValueOnce({
        id: 'm3',
        bucketId: 'b3', bucketName: 'Ariana',
        amount: -100,
        date: '2026-07-01',
        description: 'Big purchase',
        incomeId: null,
      })
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setWithdrawalField('withdrawalBucketId', 'Ariana'))
    act(() => result.current.setWithdrawalField('withdrawalAmount', '100'))
    act(() => result.current.setWithdrawalField('withdrawalDate', '2026-07-01'))
    act(() => result.current.setWithdrawalField('withdrawalDescription', 'Big purchase'))
    act(() => result.current.submitWithdrawal(confirmProceed))

    await waitFor(() => expect(postWithdrawalMock).toHaveBeenCalledTimes(2))
    expect(postWithdrawalMock).toHaveBeenNthCalledWith(1, expect.objectContaining({ confirmed: false }))
    expect(postWithdrawalMock).toHaveBeenNthCalledWith(2, expect.objectContaining({ confirmed: true }))
  })

  it('does not resubmit a 409 withdrawal when the caller declines', async () => {
    const confirmProceed = vi.fn(() => false)
    postWithdrawalMock.mockRejectedValue(new ApiError('This withdrawal exceeds the balance.', 409))
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setWithdrawalField('withdrawalAmount', '100'))
    act(() => result.current.setWithdrawalField('withdrawalDate', '2026-07-01'))
    act(() => result.current.setWithdrawalField('withdrawalDescription', 'Big purchase'))
    act(() => result.current.submitWithdrawal(confirmProceed))

    await waitFor(() => expect(result.current.withdrawalError).toBe('This withdrawal exceeds the balance.'))
    expect(postWithdrawalMock).toHaveBeenCalledTimes(1)
  })

  it('showWithdrawalForm defaults the date to today when nothing was persisted yet', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    pinDate('2026-07-01T00:30:00+01:00')
    act(() => result.current.showWithdrawalForm())

    expect(result.current.withdrawalDate).toBe('2026-07-01')
  })

  it('persists the withdrawal date and bucket after a successful submit, for the next withdrawal form', async () => {
    postWithdrawalMock.mockResolvedValue({
      id: 'm3', bucketId: 'b3', bucketName: 'Ariana',
      amount: -100, date: '2026-07-01', description: 'Big purchase', incomeId: null,
    })
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    act(() => result.current.showWithdrawalForm())
    act(() => result.current.setWithdrawalField('withdrawalBucketId', 'b3'))
    act(() => result.current.setWithdrawalField('withdrawalAmount', '100'))
    act(() => result.current.setWithdrawalField('withdrawalDate', '2026-07-01'))
    act(() => result.current.setWithdrawalField('withdrawalDescription', 'Big purchase'))
    act(() => result.current.submitWithdrawal(rejectUnexpectedConfirm))
    await waitFor(() => expect(postWithdrawalMock).toHaveBeenCalledTimes(1))

    act(() => result.current.showWithdrawalForm())

    expect(result.current.withdrawalDate).toBe('2026-07-01')
    expect(result.current.withdrawalBucketId).toBe('b3')
    expect(result.current.withdrawalAmount).toBe('')
    expect(result.current.withdrawalDescription).toBe('')
  })

  it('falls back to the first active bucket when the persisted withdrawal bucket no longer exists', async () => {
    sessionStorage.setItem('financial.createFormDefault.withdrawal.bucketId', 'bucket-deleted')
    const { result } = renderHook(() => useReserva())

    await waitFor(() => expect(result.current.withdrawalBucketId).toBe('b1'))
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
        bucketId: 'b1',
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

  it('loads the reserve bucket list and defaults the withdrawal bucket to the first one', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.buckets).toEqual(BUCKETS)
    expect(result.current.withdrawalBucketId).toBe('b1')
  })

  it('defaults the withdrawal bucket to the first active bucket, skipping a leading inactive one', async () => {
    getReserveBucketsMock.mockResolvedValue([
      { id: 'b0', name: 'Retired', isActive: false, splitPercentage: 0, warning: null },
      { id: 'b1', name: 'Investimento', isActive: true, splitPercentage: 100, warning: null },
    ])
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.withdrawalBucketId).toBe('b1')
  })

  it('does not re-fetch the bucket list after a mutation-triggered refresh', async () => {
    postWithdrawalMock.mockResolvedValue({
      id: 'm2',
      bucketId: 'b1', bucketName: 'Investimento',
      amount: -30,
      date: '2026-07-01',
      description: 'Groceries top-up',
      incomeId: null,
    })
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(getReserveBucketsMock).toHaveBeenCalledTimes(1)

    act(() => result.current.setWithdrawalField('withdrawalAmount', '30'))
    act(() => result.current.setWithdrawalField('withdrawalDate', '2026-07-01'))
    act(() => result.current.setWithdrawalField('withdrawalDescription', 'Groceries top-up'))
    act(() => result.current.submitWithdrawal(rejectUnexpectedConfirm))

    await waitFor(() => expect(getReserveBalancesMock).toHaveBeenCalledTimes(2))
    expect(getReserveBucketsMock).toHaveBeenCalledTimes(1)
    expect(getReserveSplitStatusMock).toHaveBeenCalledTimes(1)
    expect(result.current.buckets).toEqual(BUCKETS)
  })

  it('reports no split-percentage warning when active buckets sum to 100%', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.splitPercentageWarning).toBeNull()
  })

  it('shows the split-percentage warning the server reports', async () => {
    getReserveSplitStatusMock.mockResolvedValue({
      activeTotal: 83.33,
      warning: 'Active buckets currently sum to 83.33% — review your split percentages',
    })
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.splitPercentageWarning).toBe('Active buckets currently sum to 83.33% — review your split percentages')
  })

  it('shows no split-percentage warning, and no page-level error, when only the split status fetch fails', async () => {
    getReserveSplitStatusMock.mockRejectedValue(new Error('Status unavailable'))
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBeNull()
    expect(result.current.splitPercentageWarning).toBeNull()
    expect(result.current.balances).toEqual(BALANCES)
  })

  it('degrades to an empty bucket list without a page-level error when only the buckets fetch fails', async () => {
    getReserveBucketsMock.mockRejectedValue(new Error('Buckets unavailable'))
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBeNull()
    expect(result.current.buckets).toEqual([])
    expect(result.current.balances).toEqual(BALANCES)
  })

  it('rejects a withdrawal with no bucket selected before calling the API', async () => {
    getReserveBucketsMock.mockRejectedValue(new Error('Buckets unavailable'))
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setWithdrawalField('withdrawalAmount', '30'))
    act(() => result.current.setWithdrawalField('withdrawalDate', '2026-07-01'))
    act(() => result.current.setWithdrawalField('withdrawalDescription', 'Groceries top-up'))
    act(() => result.current.submitWithdrawal(rejectUnexpectedConfirm))

    await waitFor(() => expect(result.current.withdrawalError).toBe('Bucket is required'))
    expect(postWithdrawalMock).not.toHaveBeenCalled()
  })

  it('rejects a movement edit with no bucket selected before calling the API', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditMovementForm(MOVEMENTS[0]))
    act(() => result.current.setEditMovementField('editMovementBucketId', ''))
    act(() => result.current.saveMovementEdit())

    await waitFor(() => expect(result.current.saveMovementError).toBe('Bucket is required'))
    expect(updateReserveMovementMock).not.toHaveBeenCalled()
  })

  it('rejects an income split with a missing date before calling the API', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setSplitField('splitDate', ''))
    act(() => result.current.submitIncomeSplit())

    await waitFor(() => expect(result.current.splitError).toBe('Date is required'))
    expect(postIncomeSplitMock).not.toHaveBeenCalled()
  })

  it('cancelSplitForm closes the split form and clears its fields', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showSplitForm())
    act(() => result.current.setSplitField('splitDescription', 'Draft'))
    act(() => result.current.cancelSplitForm())

    expect(result.current.isSplitFormOpen).toBe(false)
    expect(result.current.splitDescription).toBe('')
  })

  it('rejects a withdrawal with a missing date before calling the API', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setWithdrawalField('withdrawalDate', ''))
    act(() => result.current.setWithdrawalField('withdrawalDescription', 'Groceries'))
    act(() => result.current.setWithdrawalField('withdrawalAmount', '10'))
    act(() => result.current.submitWithdrawal(rejectUnexpectedConfirm))

    await waitFor(() => expect(result.current.withdrawalError).toBe('Date is required'))
    expect(postWithdrawalMock).not.toHaveBeenCalled()
  })

  it('rejects a withdrawal with a missing description before calling the API', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setWithdrawalField('withdrawalDate', '2026-07-01'))
    act(() => result.current.setWithdrawalField('withdrawalDescription', ''))
    act(() => result.current.setWithdrawalField('withdrawalAmount', '10'))
    act(() => result.current.submitWithdrawal(rejectUnexpectedConfirm))

    await waitFor(() => expect(result.current.withdrawalError).toBe('Description is required'))
    expect(postWithdrawalMock).not.toHaveBeenCalled()
  })

  it('surfaces a non-conflict withdrawal error from the backend', async () => {
    postWithdrawalMock.mockRejectedValue(new Error('Withdrawal failed'))
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setWithdrawalField('withdrawalDate', '2026-07-01'))
    act(() => result.current.setWithdrawalField('withdrawalDescription', 'Groceries'))
    act(() => result.current.setWithdrawalField('withdrawalAmount', '10'))
    act(() => result.current.submitWithdrawal(rejectUnexpectedConfirm))

    await waitFor(() => expect(result.current.withdrawalError).toBe('Withdrawal failed'))
  })

  it('cancelWithdrawalForm closes the withdrawal form and resets its fields', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showWithdrawalForm())
    act(() => result.current.setWithdrawalField('withdrawalDescription', 'Draft'))
    act(() => result.current.cancelWithdrawalForm())

    expect(result.current.isWithdrawalFormOpen).toBe(false)
    expect(result.current.withdrawalDescription).toBe('')
  })

  it('rejects a movement edit with a missing date before calling the API', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditMovementForm(MOVEMENTS[0]))
    act(() => result.current.setEditMovementField('editMovementDate', ''))
    act(() => result.current.saveMovementEdit())

    await waitFor(() => expect(result.current.saveMovementError).toBe('Date is required'))
    expect(updateReserveMovementMock).not.toHaveBeenCalled()
  })

  it('rejects a movement edit with a non-numeric amount before calling the API', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditMovementForm(MOVEMENTS[0]))
    act(() => result.current.setEditMovementField('editMovementAmount', 'not-a-number'))
    act(() => result.current.saveMovementEdit())

    await waitFor(() => expect(result.current.saveMovementError).toBe('Amount must be a number'))
    expect(updateReserveMovementMock).not.toHaveBeenCalled()
  })

  it('surfaces a movement-edit error returned by the backend', async () => {
    updateReserveMovementMock.mockRejectedValue(new Error('Movement not found'))
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditMovementForm(MOVEMENTS[0]))
    act(() => result.current.saveMovementEdit())

    await waitFor(() => expect(result.current.saveMovementError).toBe('Movement not found'))
  })

  it('cancelEditMovement clears the editing movement and its form fields', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditMovementForm(MOVEMENTS[0]))
    act(() => result.current.cancelEditMovement())

    expect(result.current.editingMovementId).toBeNull()
    expect(result.current.editMovementAmount).toBe('')
  })

  it('retry re-fetches balances and movements', async () => {
    const { result } = renderHook(() => useReserva())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getReserveBalancesMock).toHaveBeenCalledTimes(2))
    await waitFor(() => expect(getReserveMovementsMock).toHaveBeenCalledTimes(2))
  })

  describe('withdrawal through a bank', () => {
    const MOVEMENT = { ...MOVEMENTS[0], id: 'm9', amount: -30, description: 'Dentist' }

    async function loadWithBucket(bucketId: string) {
      const { result } = renderHook(() => useReserva())
      await waitFor(() => expect(result.current.isLoading).toBe(false))
      act(() => result.current.setWithdrawalField('withdrawalBucketId', bucketId))
      act(() => result.current.setWithdrawalField('withdrawalAmount', '30'))
      act(() => result.current.setWithdrawalField('withdrawalDate', '2026-07-01'))
      act(() => result.current.setWithdrawalField('withdrawalDescription', 'Dentist'))
      return result
    }

    it('loads banks and eligible category options with the reserve data', async () => {
      const { result } = renderHook(() => useReserva())
      await waitFor(() => expect(result.current.isLoading).toBe(false))

      expect(result.current.withdrawalBanks).toEqual(BANKS)
      expect(result.current.withdrawalCategoryOptions.map((c) => c.name)).toEqual(['Ariana', 'Saude'])
      expect(getBanksMock).toHaveBeenCalledTimes(1)
      expect(getCategoriesMock).toHaveBeenCalledTimes(1)
    })

    it('still loads the reserve data when banks or categories fail to load', async () => {
      getBanksMock.mockRejectedValue(new Error('down'))
      getCategoriesMock.mockRejectedValue(new Error('down'))

      const { result } = renderHook(() => useReserva())
      await waitFor(() => expect(result.current.isLoading).toBe(false))

      expect(result.current.error).toBeNull()
      expect(result.current.withdrawalBanks).toEqual([])
      expect(result.current.withdrawalCategoryOptions).toEqual([])
    })

    it('posts null bank and category when no bank is selected', async () => {
      postWithdrawalMock.mockResolvedValue(MOVEMENT)
      const result = await loadWithBucket('b3')

      act(() => result.current.submitWithdrawal(rejectUnexpectedConfirm))

      await waitFor(() => expect(postWithdrawalMock).toHaveBeenCalledTimes(1))
      expect(postWithdrawalMock).toHaveBeenCalledWith(
        expect.objectContaining({ paymentSourceBankId: null, expenseCategoryId: null }),
      )
    })

    it('defaults the category to the one named after the selected bucket', async () => {
      const result = await loadWithBucket('b3')

      act(() => result.current.setWithdrawalField('withdrawalBankId', 'bk1'))

      expect(result.current.withdrawalExpenseCategoryId).toBe('c1')
    })

    it('leaves the category empty when no eligible category matches the bucket name', async () => {
      const result = await loadWithBucket('b1')

      act(() => result.current.setWithdrawalField('withdrawalBankId', 'bk1'))

      expect(result.current.withdrawalExpenseCategoryId).toBe('')
    })

    it('follows the bucket for the default until the user picks a category', async () => {
      const result = await loadWithBucket('b3')
      act(() => result.current.setWithdrawalField('withdrawalBankId', 'bk1'))

      act(() => result.current.setWithdrawalField('withdrawalBucketId', 'b1'))
      expect(result.current.withdrawalExpenseCategoryId).toBe('')

      act(() => result.current.setWithdrawalField('withdrawalExpenseCategoryId', 'c2'))
      act(() => result.current.setWithdrawalField('withdrawalBucketId', 'b3'))
      expect(result.current.withdrawalExpenseCategoryId).toBe('c2')
    })

    it('drops the chosen category and posts nulls after the bank is cleared', async () => {
      postWithdrawalMock.mockResolvedValue(MOVEMENT)
      const result = await loadWithBucket('b3')
      act(() => result.current.setWithdrawalField('withdrawalBankId', 'bk1'))
      act(() => result.current.setWithdrawalField('withdrawalExpenseCategoryId', 'c2'))

      act(() => result.current.setWithdrawalField('withdrawalBankId', ''))
      act(() => result.current.submitWithdrawal(rejectUnexpectedConfirm))

      expect(result.current.withdrawalExpenseCategoryId).toBe('c1')
      await waitFor(() => expect(postWithdrawalMock).toHaveBeenCalledTimes(1))
      expect(postWithdrawalMock).toHaveBeenCalledWith(
        expect.objectContaining({ paymentSourceBankId: null, expenseCategoryId: null }),
      )
    })

    it('requires a category when a bank is selected and posts nothing', async () => {
      const result = await loadWithBucket('b1')
      act(() => result.current.setWithdrawalField('withdrawalBankId', 'bk1'))

      act(() => result.current.submitWithdrawal(rejectUnexpectedConfirm))

      expect(result.current.withdrawalErrorFields.withdrawalExpenseCategoryId).toBe(
        'Category is required when a bank is selected.',
      )
      expect(postWithdrawalMock).not.toHaveBeenCalled()
    })

    it('posts both ids when a bank and category are selected', async () => {
      postWithdrawalMock.mockResolvedValue(MOVEMENT)
      const result = await loadWithBucket('b1')
      act(() => result.current.setWithdrawalField('withdrawalBankId', 'bk2'))
      act(() => result.current.setWithdrawalField('withdrawalExpenseCategoryId', 'c2'))

      act(() => result.current.submitWithdrawal(rejectUnexpectedConfirm))

      await waitFor(() => expect(postWithdrawalMock).toHaveBeenCalledTimes(1))
      expect(postWithdrawalMock).toHaveBeenCalledWith(
        expect.objectContaining({ paymentSourceBankId: 'bk2', expenseCategoryId: 'c2', confirmed: false }),
      )
    })

    it('clears the bank and category after cancel', async () => {
      const result = await loadWithBucket('b1')
      act(() => result.current.setWithdrawalField('withdrawalBankId', 'bk1'))
      act(() => result.current.setWithdrawalField('withdrawalExpenseCategoryId', 'c2'))

      act(() => result.current.cancelWithdrawalForm())

      expect(result.current.withdrawalBankId).toBe('')
      expect(result.current.withdrawalExpenseCategoryId).toBe('')
    })

    it('clears the bank and category after a successful submit', async () => {
      postWithdrawalMock.mockResolvedValue(MOVEMENT)
      const result = await loadWithBucket('b1')
      act(() => result.current.setWithdrawalField('withdrawalBankId', 'bk1'))
      act(() => result.current.setWithdrawalField('withdrawalExpenseCategoryId', 'c2'))

      act(() => result.current.submitWithdrawal(rejectUnexpectedConfirm))

      await waitFor(() => expect(result.current.withdrawalBankId).toBe(''))
      expect(postWithdrawalMock).toHaveBeenCalledTimes(1)
      expect(result.current.withdrawalExpenseCategoryId).toBe('')
    })

    it('replays a 409 with confirmed and keeps both ids', async () => {
      postWithdrawalMock
        .mockRejectedValueOnce(new ApiError('This withdrawal exceeds the balance.', 409))
        .mockResolvedValueOnce(MOVEMENT)
      const result = await loadWithBucket('b1')
      act(() => result.current.setWithdrawalField('withdrawalBankId', 'bk1'))
      act(() => result.current.setWithdrawalField('withdrawalExpenseCategoryId', 'c2'))

      act(() => result.current.submitWithdrawal(() => true))

      await waitFor(() => expect(postWithdrawalMock).toHaveBeenCalledTimes(2))
      expect(postWithdrawalMock).toHaveBeenNthCalledWith(
        2,
        expect.objectContaining({ confirmed: true, paymentSourceBankId: 'bk1', expenseCategoryId: 'c2' }),
      )
    })
  })
})
