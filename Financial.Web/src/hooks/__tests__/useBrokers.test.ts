import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BrokerDto } from '../../api/types'
import { useBrokers } from '../useBrokers'

const { getAdminBrokersMock, createBrokerMock, updateBrokerMock, deleteBrokerMock } = vi.hoisted(() => ({
  getAdminBrokersMock: vi.fn<FinancialApiClient['getAdminBrokers']>(),
  createBrokerMock: vi.fn<FinancialApiClient['createBroker']>(),
  updateBrokerMock: vi.fn<FinancialApiClient['updateBroker']>(),
  deleteBrokerMock: vi.fn<FinancialApiClient['deleteBroker']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAdminBrokers: getAdminBrokersMock,
    createBroker: createBrokerMock,
    updateBroker: updateBrokerMock,
    deleteBroker: deleteBrokerMock,
  } as Partial<FinancialApiClient>,
}))

const BROKERS: BrokerDto[] = [
  { name: 'XPI', currency: 'BRL', status: 'Active', portfolioCount: 2 },
  { name: 'Avenue', currency: 'USD', status: 'Historic', portfolioCount: 0 },
]

describe('useBrokers', () => {
  beforeEach(() => {
    getAdminBrokersMock.mockReset()
    createBrokerMock.mockReset()
    updateBrokerMock.mockReset()
    deleteBrokerMock.mockReset()
    getAdminBrokersMock.mockResolvedValue(BROKERS)
  })

  it('fetches the broker list once on mount', async () => {
    const { result } = renderHook(() => useBrokers())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getAdminBrokersMock).toHaveBeenCalledTimes(1)
    expect(result.current.brokers).toEqual(BROKERS)
  })

  it('surfaces a fetch error', async () => {
    getAdminBrokersMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useBrokers())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('retry re-fetches the list', async () => {
    const { result } = renderHook(() => useBrokers())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getAdminBrokersMock).toHaveBeenCalledTimes(2))
  })

  it('createBroker calls the API and re-fetches the list', async () => {
    createBrokerMock.mockResolvedValue({ name: 'New Broker', currency: 'BRL', status: 'Active', portfolioCount: 0 })
    const { result } = renderHook(() => useBrokers())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.createBroker({ name: 'New Broker', currency: 'BRL' })
    })

    expect(createBrokerMock).toHaveBeenCalledWith({ name: 'New Broker', currency: 'BRL' })
    await waitFor(() => expect(getAdminBrokersMock).toHaveBeenCalledTimes(2))
  })

  it('createBroker propagates a rejected promise to the caller without swallowing it', async () => {
    createBrokerMock.mockRejectedValue(new Error('A broker named "XPI" already exists.'))
    const { result } = renderHook(() => useBrokers())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await expect(result.current.createBroker({ name: 'XPI', currency: 'BRL' })).rejects.toThrow(
      'A broker named "XPI" already exists.',
    )
  })

  it('updateBroker calls the API and re-fetches the list', async () => {
    updateBrokerMock.mockResolvedValue({ name: 'XPI Renamed', currency: 'USD', status: 'Active', portfolioCount: 2 })
    const { result } = renderHook(() => useBrokers())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.updateBroker('XPI', { name: 'XPI Renamed', currency: 'USD' })
    })

    expect(updateBrokerMock).toHaveBeenCalledWith('XPI', { name: 'XPI Renamed', currency: 'USD' })
    await waitFor(() => expect(getAdminBrokersMock).toHaveBeenCalledTimes(2))
  })

  it('deleteBroker calls the API and re-fetches the list', async () => {
    deleteBrokerMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useBrokers())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteBroker('Avenue'))

    await waitFor(() => expect(result.current.deletingName).toBeNull())
    expect(deleteBrokerMock).toHaveBeenCalledWith('Avenue')
    await waitFor(() => expect(getAdminBrokersMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a delete error without re-fetching', async () => {
    deleteBrokerMock.mockRejectedValue(new Error('Cannot delete a broker that still has portfolios.'))
    const { result } = renderHook(() => useBrokers())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteBroker('XPI'))

    await waitFor(() => expect(result.current.deleteError).toBe('Cannot delete a broker that still has portfolios.'))
    expect(getAdminBrokersMock).toHaveBeenCalledTimes(1)
  })
})
