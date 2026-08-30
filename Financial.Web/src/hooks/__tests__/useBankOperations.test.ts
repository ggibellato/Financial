import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BalanceAdjustmentDto, BankDto, TransferDto } from '../../api/types'
import { useBankOperations } from '../useBankOperations'

const {
  getTransfersByMonthMock,
  deleteTransferMock,
  getAdjustmentsByBankMock,
  deleteBalanceAdjustmentMock,
} = vi.hoisted(() => ({
  getTransfersByMonthMock: vi.fn<FinancialApiClient['getTransfersByMonth']>(),
  deleteTransferMock: vi.fn<FinancialApiClient['deleteTransfer']>(),
  getAdjustmentsByBankMock: vi.fn<FinancialApiClient['getAdjustmentsByBank']>(),
  deleteBalanceAdjustmentMock: vi.fn<FinancialApiClient['deleteBalanceAdjustment']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getTransfersByMonth: getTransfersByMonthMock,
    deleteTransfer: deleteTransferMock,
    getAdjustmentsByBank: getAdjustmentsByBankMock,
    deleteBalanceAdjustment: deleteBalanceAdjustmentMock,
  } as Partial<FinancialApiClient>,
}))

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false },
]

const TRANSFERS: TransferDto[] = [
  {
    id: 't1',
    date: '2026-07-10',
    sourceBankId: 'bank-barclays',
    sourceBankName: 'Barclays',
    destinationBankId: 'bank-trading212',
    destinationBankName: 'Trading212',
    amount: 500,
    note: 'Top-up',
  },
  {
    id: 't2',
    date: '2026-07-20',
    sourceBankId: 'bank-trading212',
    sourceBankName: 'Trading212',
    destinationBankId: 'bank-barclays',
    destinationBankName: 'Barclays',
    amount: 100,
    note: null,
  },
]

const BARCLAYS_ADJUSTMENTS: BalanceAdjustmentDto[] = [
  {
    id: 'a1',
    date: '2026-07-15',
    bankId: 'bank-barclays',
    bankName: 'Barclays',
    targetBalance: 150,
    delta: 50,
    note: 'Matched statement',
  },
  {
    id: 'a2',
    date: '2026-06-01',
    bankId: 'bank-barclays',
    bankName: 'Barclays',
    targetBalance: 100,
    delta: 10,
    note: 'Old month',
  },
]

describe('useBankOperations', () => {
  beforeEach(() => {
    getTransfersByMonthMock.mockReset()
    deleteTransferMock.mockReset()
    getAdjustmentsByBankMock.mockReset()
    deleteBalanceAdjustmentMock.mockReset()
    getTransfersByMonthMock.mockResolvedValue(TRANSFERS)
    getAdjustmentsByBankMock.mockImplementation((bankId: string) =>
      Promise.resolve(bankId === 'bank-barclays' ? BARCLAYS_ADJUSTMENTS : []),
    )
  })

  it('combines transfers and adjustments into one flat list sorted newest-first', async () => {
    const { result } = renderHook(() => useBankOperations(2026, 7, BANKS, vi.fn()))

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.operations.map((e) => e.id)).toEqual(['t2', 'a1', 't1'])
  })

  it('filters adjustments to the selected month only, leaving transfers unaffected', async () => {
    const { result } = renderHook(() => useBankOperations(2026, 7, BANKS, vi.fn()))

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.operations.some((e) => e.id === 'a2')).toBe(false)
    expect(result.current.operations.some((e) => e.id === 't1')).toBe(true)
  })

  it('represents each transfer once, with both source and destination banks', async () => {
    const { result } = renderHook(() => useBankOperations(2026, 7, BANKS, vi.fn()))

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    const t1 = result.current.operations.find((e) => e.id === 't1')
    expect(t1).toMatchObject({ kind: 'transfer', sourceBank: 'Barclays', destinationBank: 'Trading212' })
  })

  it('returns every operation regardless of bank — filtering is the component/UI\'s job now', async () => {
    const { result } = renderHook(() => useBankOperations(2026, 7, BANKS, vi.fn()))

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.operations.map((e) => e.id).sort()).toEqual(['a1', 't1', 't2'])
  })

  it('deleteTransfer calls the client, refetches, and calls onChanged', async () => {
    deleteTransferMock.mockResolvedValue(undefined)
    const onChanged = vi.fn()
    const { result } = renderHook(() => useBankOperations(2026, 7, BANKS, onChanged))
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    result.current.deleteTransfer('t1')
    await waitFor(() => expect(deleteTransferMock).toHaveBeenCalledWith('t1'))

    expect(onChanged).toHaveBeenCalledOnce()
  })

  it('deleteAdjustment calls the client, refetches, and calls onChanged', async () => {
    deleteBalanceAdjustmentMock.mockResolvedValue(undefined)
    const onChanged = vi.fn()
    const { result } = renderHook(() => useBankOperations(2026, 7, BANKS, onChanged))
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    result.current.deleteAdjustment('bank-barclays', 'a1')
    await waitFor(() => expect(deleteBalanceAdjustmentMock).toHaveBeenCalledWith('bank-barclays', 'a1'))

    expect(onChanged).toHaveBeenCalledOnce()
  })

  it('surfaces a fetch error with retry', async () => {
    getTransfersByMonthMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useBankOperations(2026, 7, BANKS, vi.fn()))

    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(result.current.error).toBe('Network down')

    getTransfersByMonthMock.mockResolvedValue(TRANSFERS)
    act(() => result.current.retry())

    await waitFor(() => expect(result.current.error).toBeNull())
  })

  it('surfaces a delete error inline without discarding the list', async () => {
    deleteTransferMock.mockRejectedValue(new Error('Already deleted'))
    const { result } = renderHook(() => useBankOperations(2026, 7, BANKS, vi.fn()))
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    result.current.deleteTransfer('t1')

    await waitFor(() => expect(result.current.error).toBe('Already deleted'))
    expect(result.current.operations).toHaveLength(3)
  })
})
