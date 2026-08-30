import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BankDto } from '../../api/types'
import { useBanks } from '../useBanks'

const { getBanksMock, createBankMock, updateBankMock, deleteBankMock } = vi.hoisted(() => ({
  getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
  createBankMock: vi.fn<FinancialApiClient['createBank']>(),
  updateBankMock: vi.fn<FinancialApiClient['updateBank']>(),
  deleteBankMock: vi.fn<FinancialApiClient['deleteBank']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getBanks: getBanksMock,
    createBank: createBankMock,
    updateBank: updateBankMock,
    deleteBank: deleteBankMock,
  } as Partial<FinancialApiClient>,
}))

const BANKS: BankDto[] = [
  { id: 'b1', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true },
  { id: 'b2', name: 'Chase', roundUpEnabled: true, openingBalance: 100, openingBalanceDate: '2026-01-01', hasReferences: false },
]

describe('useBanks', () => {
  beforeEach(() => {
    getBanksMock.mockReset()
    createBankMock.mockReset()
    updateBankMock.mockReset()
    deleteBankMock.mockReset()
    getBanksMock.mockResolvedValue(BANKS)
  })

  it('fetches the bank list once on mount', async () => {
    const { result } = renderHook(() => useBanks())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getBanksMock).toHaveBeenCalledTimes(1)
    expect(result.current.banks).toEqual(BANKS)
  })

  it('surfaces a fetch error', async () => {
    getBanksMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useBanks())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('retry re-fetches the list', async () => {
    const { result } = renderHook(() => useBanks())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getBanksMock).toHaveBeenCalledTimes(2))
  })

  it('createBank calls the API and re-fetches the list', async () => {
    createBankMock.mockResolvedValue({
      id: 'b3',
      name: 'Monzo',
      roundUpEnabled: false,
      openingBalance: 0,
      openingBalanceDate: '2026-01-01',
      hasReferences: false,
    })
    const { result } = renderHook(() => useBanks())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.createBank({ name: 'Monzo', roundUpEnabled: false })
    })

    expect(createBankMock).toHaveBeenCalledWith({ name: 'Monzo', roundUpEnabled: false })
    await waitFor(() => expect(getBanksMock).toHaveBeenCalledTimes(2))
  })

  it('createBank propagates a rejected promise to the caller without swallowing it', async () => {
    createBankMock.mockRejectedValue(new Error('A bank named "Barclays" already exists.'))
    const { result } = renderHook(() => useBanks())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await expect(result.current.createBank({ name: 'Barclays', roundUpEnabled: false })).rejects.toThrow(
      'A bank named "Barclays" already exists.',
    )
  })

  it('updateBank calls the API and re-fetches the list', async () => {
    updateBankMock.mockResolvedValue({
      id: 'b1',
      name: 'Barclays Renamed',
      roundUpEnabled: true,
      openingBalance: 0,
      openingBalanceDate: '2026-01-01',
      hasReferences: true,
    })
    const { result } = renderHook(() => useBanks())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.updateBank('b1', { name: 'Barclays Renamed', roundUpEnabled: true })
    })

    expect(updateBankMock).toHaveBeenCalledWith('b1', { name: 'Barclays Renamed', roundUpEnabled: true })
    await waitFor(() => expect(getBanksMock).toHaveBeenCalledTimes(2))
  })

  it('deleteBank calls the API and re-fetches the list', async () => {
    deleteBankMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useBanks())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteBank('b2'))

    await waitFor(() => expect(result.current.deletingId).toBeNull())
    expect(deleteBankMock).toHaveBeenCalledWith('b2')
    await waitFor(() => expect(getBanksMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a delete error without re-fetching', async () => {
    deleteBankMock.mockRejectedValue(new Error('Cannot delete a bank that still has balance history or transactions.'))
    const { result } = renderHook(() => useBanks())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteBank('b1'))

    await waitFor(() =>
      expect(result.current.deleteError).toBe('Cannot delete a bank that still has balance history or transactions.'),
    )
    expect(getBanksMock).toHaveBeenCalledTimes(1)
  })
})
