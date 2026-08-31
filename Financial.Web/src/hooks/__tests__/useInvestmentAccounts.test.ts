import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { InvestmentAccountDto } from '../../api/types'
import { useInvestmentAccounts } from '../useInvestmentAccounts'

const { getInvestmentAccountsMock, createInvestmentAccountMock, updateInvestmentAccountMock, deleteInvestmentAccountMock } = vi.hoisted(() => ({
  getInvestmentAccountsMock: vi.fn<FinancialApiClient['getInvestmentAccounts']>(),
  createInvestmentAccountMock: vi.fn<FinancialApiClient['createInvestmentAccount']>(),
  updateInvestmentAccountMock: vi.fn<FinancialApiClient['updateInvestmentAccount']>(),
  deleteInvestmentAccountMock: vi.fn<FinancialApiClient['deleteInvestmentAccount']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getInvestmentAccounts: getInvestmentAccountsMock,
    createInvestmentAccount: createInvestmentAccountMock,
    updateInvestmentAccount: updateInvestmentAccountMock,
    deleteInvestmentAccount: deleteInvestmentAccountMock,
  } as Partial<FinancialApiClient>,
}))

const INVESTMENT_ACCOUNTS: InvestmentAccountDto[] = [
  { id: 'a1', name: 'ChaseSave', isActive: true, isLiability: false, aliases: [], latestBalance: 0 },
  { id: 'a2', name: 'PlatinumVisa8003', isActive: true, isLiability: true, aliases: ['Amex'], latestBalance: 500 },
]

describe('useInvestmentAccounts', () => {
  beforeEach(() => {
    getInvestmentAccountsMock.mockReset()
    createInvestmentAccountMock.mockReset()
    updateInvestmentAccountMock.mockReset()
    deleteInvestmentAccountMock.mockReset()
    getInvestmentAccountsMock.mockResolvedValue(INVESTMENT_ACCOUNTS)
  })

  it('fetches the investment account list once on mount', async () => {
    const { result } = renderHook(() => useInvestmentAccounts())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getInvestmentAccountsMock).toHaveBeenCalledTimes(1)
    expect(result.current.investmentAccounts).toEqual(INVESTMENT_ACCOUNTS)
  })

  it('surfaces a fetch error', async () => {
    getInvestmentAccountsMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useInvestmentAccounts())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('retry re-fetches the list', async () => {
    const { result } = renderHook(() => useInvestmentAccounts())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getInvestmentAccountsMock).toHaveBeenCalledTimes(2))
  })

  it('createInvestmentAccount calls the API and re-fetches the list', async () => {
    createInvestmentAccountMock.mockResolvedValue({
      id: 'a3',
      name: 'Monzo Pot',
      isActive: true,
      isLiability: false,
      aliases: ['Monzo'],
      latestBalance: 0,
    })
    const { result } = renderHook(() => useInvestmentAccounts())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.createInvestmentAccount({ name: 'Monzo Pot', isActive: true, isLiability: false, aliases: ['Monzo'] })
    })

    expect(createInvestmentAccountMock).toHaveBeenCalledWith({ name: 'Monzo Pot', isActive: true, isLiability: false, aliases: ['Monzo'] })
    await waitFor(() => expect(getInvestmentAccountsMock).toHaveBeenCalledTimes(2))
  })

  it('createInvestmentAccount propagates a rejected promise to the caller without swallowing it', async () => {
    createInvestmentAccountMock.mockRejectedValue(new Error('An investment account named "ChaseSave" already exists.'))
    const { result } = renderHook(() => useInvestmentAccounts())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await expect(
      result.current.createInvestmentAccount({ name: 'ChaseSave', isActive: true, isLiability: false, aliases: [] }),
    ).rejects.toThrow('An investment account named "ChaseSave" already exists.')
  })

  it('updateInvestmentAccount calls the API and re-fetches the list', async () => {
    updateInvestmentAccountMock.mockResolvedValue({
      id: 'a1',
      name: 'ChaseSaveRenamed',
      isActive: false,
      isLiability: true,
      aliases: ['New alias'],
      latestBalance: 0,
    })
    const { result } = renderHook(() => useInvestmentAccounts())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.updateInvestmentAccount('a1', { name: 'ChaseSaveRenamed', isActive: false, isLiability: true, aliases: ['New alias'] })
    })

    expect(updateInvestmentAccountMock).toHaveBeenCalledWith('a1', {
      name: 'ChaseSaveRenamed',
      isActive: false,
      isLiability: true,
      aliases: ['New alias'],
    })
    await waitFor(() => expect(getInvestmentAccountsMock).toHaveBeenCalledTimes(2))
  })

  it('deleteInvestmentAccount calls the API and re-fetches the list', async () => {
    deleteInvestmentAccountMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useInvestmentAccounts())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteInvestmentAccount('a1'))

    await waitFor(() => expect(result.current.deletingId).toBeNull())
    expect(deleteInvestmentAccountMock).toHaveBeenCalledWith('a1')
    await waitFor(() => expect(getInvestmentAccountsMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a delete error without re-fetching', async () => {
    deleteInvestmentAccountMock.mockRejectedValue(new Error('Cannot delete an investment account with a non-zero balance.'))
    const { result } = renderHook(() => useInvestmentAccounts())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteInvestmentAccount('a2'))

    await waitFor(() =>
      expect(result.current.deleteError).toBe('Cannot delete an investment account with a non-zero balance.'),
    )
    expect(getInvestmentAccountsMock).toHaveBeenCalledTimes(1)
  })
})
