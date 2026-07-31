import { renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { BalanceAdjustmentDto, BankDto, TransferDto } from '../api/types'
import { useBankHistory } from './useBankHistory'

const getTransfersByMonthMock = vi.fn<FinancialApiClient['getTransfersByMonth']>()
const deleteTransferMock = vi.fn<FinancialApiClient['deleteTransfer']>()
const getAdjustmentsByBankMock = vi.fn<FinancialApiClient['getAdjustmentsByBank']>()
const deleteBalanceAdjustmentMock = vi.fn<FinancialApiClient['deleteBalanceAdjustment']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getTransfersByMonth: getTransfersByMonthMock,
    deleteTransfer: deleteTransferMock,
    getAdjustmentsByBank: getAdjustmentsByBankMock,
    deleteBalanceAdjustment: deleteBalanceAdjustmentMock,
  }),
}))

const BANKS: BankDto[] = [
  { name: 'Barclays', roundUpEnabled: false },
  { name: 'Trading212', roundUpEnabled: true },
]

const TRANSFERS: TransferDto[] = [
  {
    id: 't1',
    date: '2026-07-10',
    sourceBank: 'Barclays',
    destinationBank: 'Trading212',
    amount: 500,
    note: 'Top-up',
  },
  {
    id: 't2',
    date: '2026-07-20',
    sourceBank: 'Trading212',
    destinationBank: 'Barclays',
    amount: 100,
    note: null,
  },
]

const BARCLAYS_ADJUSTMENTS: BalanceAdjustmentDto[] = [
  { id: 'a1', date: '2026-07-15', bank: 'Barclays', targetBalance: 150, delta: 50, note: 'Matched statement' },
  { id: 'a2', date: '2026-06-01', bank: 'Barclays', targetBalance: 100, delta: 10, note: 'Old month' },
]

describe('useBankHistory', () => {
  beforeEach(() => {
    getTransfersByMonthMock.mockReset()
    deleteTransferMock.mockReset()
    getAdjustmentsByBankMock.mockReset()
    deleteBalanceAdjustmentMock.mockReset()
    getTransfersByMonthMock.mockResolvedValue(TRANSFERS)
    getAdjustmentsByBankMock.mockImplementation((bankName: string) =>
      Promise.resolve(bankName === 'Barclays' ? BARCLAYS_ADJUSTMENTS : []),
    )
    vi.spyOn(window, 'confirm').mockReturnValue(true)
  })

  it('combines transfers and adjustments per bank, sorted descending by date, scoped to the month', async () => {
    const { result } = renderHook(() => useBankHistory(2026, 7, BANKS, vi.fn()))

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    const barclaysHistory = result.current.historyByBank['Barclays']
    expect(barclaysHistory).toHaveLength(3)
    expect(barclaysHistory.map((e) => e.id)).toEqual(['t2', 'a1', 't1'])
    expect(barclaysHistory[0]).toMatchObject({ kind: 'transferIn', counterpartBank: 'Trading212' })
    expect(barclaysHistory[1]).toMatchObject({ kind: 'adjustment', delta: 50 })
    expect(barclaysHistory[2]).toMatchObject({ kind: 'transferOut', counterpartBank: 'Trading212' })
  })

  it('excludes adjustments outside the selected month', async () => {
    const { result } = renderHook(() => useBankHistory(2026, 7, BANKS, vi.fn()))

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.historyByBank['Barclays'].some((e) => e.id === 'a2')).toBe(false)
  })

  it('classifies transfers for the other bank in the opposite direction', async () => {
    const { result } = renderHook(() => useBankHistory(2026, 7, BANKS, vi.fn()))

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    const trading212History = result.current.historyByBank['Trading212']
    expect(trading212History.find((e) => e.id === 't1')).toMatchObject({ kind: 'transferIn' })
    expect(trading212History.find((e) => e.id === 't2')).toMatchObject({ kind: 'transferOut' })
  })

  it('deleteTransfer confirms, calls the client, refetches, and calls onChanged', async () => {
    deleteTransferMock.mockResolvedValue(undefined)
    const onChanged = vi.fn()
    const { result } = renderHook(() => useBankHistory(2026, 7, BANKS, onChanged))
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    result.current.deleteTransfer('t1')
    await waitFor(() => expect(deleteTransferMock).toHaveBeenCalledWith('t1'))

    expect(onChanged).toHaveBeenCalledOnce()
  })

  it('deleteTransfer does nothing when the user cancels the confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    const { result } = renderHook(() => useBankHistory(2026, 7, BANKS, vi.fn()))
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    result.current.deleteTransfer('t1')

    expect(deleteTransferMock).not.toHaveBeenCalled()
  })

  it('deleteAdjustment confirms, calls the client, refetches, and calls onChanged', async () => {
    deleteBalanceAdjustmentMock.mockResolvedValue(undefined)
    const onChanged = vi.fn()
    const { result } = renderHook(() => useBankHistory(2026, 7, BANKS, onChanged))
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    result.current.deleteAdjustment('Barclays', 'a1')
    await waitFor(() => expect(deleteBalanceAdjustmentMock).toHaveBeenCalledWith('Barclays', 'a1'))

    expect(onChanged).toHaveBeenCalledOnce()
  })

  it('sets error on a failed fetch', async () => {
    getTransfersByMonthMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useBankHistory(2026, 7, BANKS, vi.fn()))

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })
})
