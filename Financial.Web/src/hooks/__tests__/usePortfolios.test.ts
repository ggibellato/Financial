import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { PortfolioDto } from '../../api/types'
import { portfolioKey, usePortfolios } from '../usePortfolios'

const { getAdminPortfoliosMock, createPortfolioMock, updatePortfolioMock, deleteEmptyPortfolioMock } = vi.hoisted(() => ({
  getAdminPortfoliosMock: vi.fn<FinancialApiClient['getAdminPortfolios']>(),
  createPortfolioMock: vi.fn<FinancialApiClient['createPortfolio']>(),
  updatePortfolioMock: vi.fn<FinancialApiClient['updatePortfolio']>(),
  deleteEmptyPortfolioMock: vi.fn<FinancialApiClient['deleteEmptyPortfolio']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAdminPortfolios: getAdminPortfoliosMock,
    createPortfolio: createPortfolioMock,
    updatePortfolio: updatePortfolioMock,
    deleteEmptyPortfolio: deleteEmptyPortfolioMock,
  } as Partial<FinancialApiClient>,
}))

const PORTFOLIOS: PortfolioDto[] = [
  { name: 'Default', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 1 },
  { name: 'Old', brokerName: 'Avenue', brokerStatus: 'Historic', assetCount: 0 },
]

describe('usePortfolios', () => {
  beforeEach(() => {
    getAdminPortfoliosMock.mockReset()
    createPortfolioMock.mockReset()
    updatePortfolioMock.mockReset()
    deleteEmptyPortfolioMock.mockReset()
    getAdminPortfoliosMock.mockResolvedValue(PORTFOLIOS)
  })

  it('fetches the portfolio list once on mount', async () => {
    const { result } = renderHook(() => usePortfolios())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getAdminPortfoliosMock).toHaveBeenCalledTimes(1)
    expect(result.current.portfolios).toEqual(PORTFOLIOS)
  })

  it('surfaces a fetch error', async () => {
    getAdminPortfoliosMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => usePortfolios())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('retry re-fetches the list', async () => {
    const { result } = renderHook(() => usePortfolios())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getAdminPortfoliosMock).toHaveBeenCalledTimes(2))
  })

  it('createPortfolio calls the API and re-fetches the list', async () => {
    createPortfolioMock.mockResolvedValue({ name: 'New Portfolio', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 0 })
    const { result } = renderHook(() => usePortfolios())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.createPortfolio({ brokerName: 'XPI', name: 'New Portfolio' })
    })

    expect(createPortfolioMock).toHaveBeenCalledWith({ brokerName: 'XPI', name: 'New Portfolio' })
    await waitFor(() => expect(getAdminPortfoliosMock).toHaveBeenCalledTimes(2))
  })

  it('createPortfolio propagates a rejected promise to the caller without swallowing it', async () => {
    createPortfolioMock.mockRejectedValue(new Error('Broker "XPI" already has a portfolio named "Default".'))
    const { result } = renderHook(() => usePortfolios())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await expect(result.current.createPortfolio({ brokerName: 'XPI', name: 'Default' })).rejects.toThrow(
      'Broker "XPI" already has a portfolio named "Default".',
    )
  })

  it('updatePortfolio calls the API and re-fetches the list', async () => {
    updatePortfolioMock.mockResolvedValue({ name: 'Default Renamed', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 1 })
    const { result } = renderHook(() => usePortfolios())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.updatePortfolio('XPI', 'Default', { name: 'Default Renamed' })
    })

    expect(updatePortfolioMock).toHaveBeenCalledWith('XPI', 'Default', { name: 'Default Renamed' })
    await waitFor(() => expect(getAdminPortfoliosMock).toHaveBeenCalledTimes(2))
  })

  it('deletePortfolio calls the API and re-fetches the list', async () => {
    deleteEmptyPortfolioMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => usePortfolios())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deletePortfolio('Avenue', 'Old'))

    await waitFor(() => expect(result.current.deletingKey).toBeNull())
    expect(deleteEmptyPortfolioMock).toHaveBeenCalledWith('Avenue', 'Old')
    await waitFor(() => expect(getAdminPortfoliosMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a delete error without re-fetching', async () => {
    deleteEmptyPortfolioMock.mockRejectedValue(new Error('Only an empty portfolio can be deleted.'))
    const { result } = renderHook(() => usePortfolios())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deletePortfolio('XPI', 'Default'))

    await waitFor(() => expect(result.current.deleteError).toBe('Only an empty portfolio can be deleted.'))
    expect(getAdminPortfoliosMock).toHaveBeenCalledTimes(1)
  })

  it('portfolioKey combines broker and portfolio names', () => {
    expect(portfolioKey('XPI', 'Default')).toBe('XPI/Default')
  })
})
