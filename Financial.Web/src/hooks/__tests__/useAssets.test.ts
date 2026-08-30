import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetAdminCreateDto, AssetAdminDto, AssetAdminUpdateDto } from '../../api/types'
import { assetKey, useAssets } from '../useAssets'

const CREATE_REQUEST: AssetAdminCreateDto = {
  brokerName: 'XPI',
  portfolioName: 'Default',
  name: 'NEWASSET',
  isin: '',
  exchange: '',
  ticker: '',
  country: 'Unknown',
  localTypeCode: '',
  class: null,
}

const UPDATE_REQUEST: AssetAdminUpdateDto = {
  name: 'BCIA11B',
  isin: '',
  exchange: '',
  ticker: '',
  country: 'Unknown',
  localTypeCode: '',
  class: 'Unknown',
}

const { getAdminAssetsMock, createAssetMock, updateAssetMock, archiveAssetMock } = vi.hoisted(() => ({
  getAdminAssetsMock: vi.fn<FinancialApiClient['getAdminAssets']>(),
  createAssetMock: vi.fn<FinancialApiClient['createAsset']>(),
  updateAssetMock: vi.fn<FinancialApiClient['updateAsset']>(),
  archiveAssetMock: vi.fn<FinancialApiClient['archiveAsset']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAdminAssets: getAdminAssetsMock,
    createAsset: createAssetMock,
    updateAsset: updateAssetMock,
    archiveAsset: archiveAssetMock,
  } as Partial<FinancialApiClient>,
}))

const ASSETS: AssetAdminDto[] = [
  {
    name: 'BCIA11',
    brokerName: 'XPI',
    portfolioName: 'Default',
    brokerStatus: 'Active',
    isin: 'BR0000000001',
    exchange: 'BVMF',
    ticker: 'BCIA11',
    country: 'BR',
    localTypeCode: 'FII',
    class: 'RealEstate',
    quantity: 100,
  },
  {
    name: 'CLOSEDASSET',
    brokerName: 'XPI',
    portfolioName: 'Uncategorized',
    brokerStatus: 'Historic',
    isin: '',
    exchange: '',
    ticker: 'CLOSEDASSET',
    country: 'Unknown',
    localTypeCode: '',
    class: 'Unknown',
    quantity: 0,
  },
]

describe('useAssets', () => {
  beforeEach(() => {
    getAdminAssetsMock.mockReset()
    createAssetMock.mockReset()
    updateAssetMock.mockReset()
    archiveAssetMock.mockReset()
    getAdminAssetsMock.mockResolvedValue(ASSETS)
  })

  it('fetches the asset list once on mount', async () => {
    const { result } = renderHook(() => useAssets())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getAdminAssetsMock).toHaveBeenCalledTimes(1)
    expect(result.current.assets).toEqual(ASSETS)
  })

  it('surfaces a fetch error', async () => {
    getAdminAssetsMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useAssets())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('retry re-fetches the list', async () => {
    const { result } = renderHook(() => useAssets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getAdminAssetsMock).toHaveBeenCalledTimes(2))
  })

  it('createAsset calls the API and re-fetches the list', async () => {
    createAssetMock.mockResolvedValue({ ...ASSETS[0], name: 'NEWASSET', quantity: 0 })
    const { result } = renderHook(() => useAssets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.createAsset(CREATE_REQUEST)
    })

    expect(createAssetMock).toHaveBeenCalledWith(CREATE_REQUEST)
    await waitFor(() => expect(getAdminAssetsMock).toHaveBeenCalledTimes(2))
  })

  it('createAsset propagates a rejected promise to the caller without swallowing it', async () => {
    createAssetMock.mockRejectedValue(new Error('Portfolio "Default" already has an asset named "BCIA11".'))
    const { result } = renderHook(() => useAssets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await expect(result.current.createAsset(CREATE_REQUEST)).rejects.toThrow(
      'Portfolio "Default" already has an asset named "BCIA11".',
    )
  })

  it('updateAsset calls the API and re-fetches the list', async () => {
    updateAssetMock.mockResolvedValue({ ...ASSETS[0], name: 'BCIA11B' })
    const { result } = renderHook(() => useAssets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.updateAsset('XPI', 'Default', 'BCIA11', UPDATE_REQUEST)
    })

    expect(updateAssetMock).toHaveBeenCalledWith('XPI', 'Default', 'BCIA11', UPDATE_REQUEST)
    await waitFor(() => expect(getAdminAssetsMock).toHaveBeenCalledTimes(2))
  })

  it('deleteAsset archives in the asset\'s own portfolio and re-fetches the list', async () => {
    archiveAssetMock.mockResolvedValue({} as never)
    const { result } = renderHook(() => useAssets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteAsset('XPI', 'Default', 'BCIA11'))

    await waitFor(() => expect(result.current.deletingKey).toBeNull())
    expect(archiveAssetMock).toHaveBeenCalledWith({
      brokerName: 'XPI',
      sourcePortfolioName: 'Default',
      assetName: 'BCIA11',
      destinationPortfolioName: 'Default',
    })
    await waitFor(() => expect(getAdminAssetsMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a delete error without re-fetching', async () => {
    archiveAssetMock.mockRejectedValue(new Error('still holds a position'))
    const { result } = renderHook(() => useAssets())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteAsset('XPI', 'Default', 'BCIA11'))

    await waitFor(() => expect(result.current.deleteError).toBe('still holds a position'))
    expect(getAdminAssetsMock).toHaveBeenCalledTimes(1)
  })

  it('assetKey combines broker, portfolio, and asset names', () => {
    expect(assetKey('XPI', 'Default', 'BCIA11')).toBe('XPI/Default/BCIA11')
  })
})
