import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetAdminDto, SelectedNode } from '../../api/types'
import { createSelectedNodeWrapper } from '../../test-utils/selectedNodeTestWrapper'
import { useAssetSearchOptions } from '../useAssetSearchOptions'

const { getAdminAssetsMock } = vi.hoisted(() => ({
  getAdminAssetsMock: vi.fn<FinancialApiClient['getAdminAssets']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAdminAssets: getAdminAssetsMock,
  } as Partial<FinancialApiClient>,
}))

const ASSET_NODE: SelectedNode = {
  nodeType: 'Asset',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  assetName: 'KLBN4',
  ticker: 'KLBN4',
  exchange: 'BVMF',
}

const BROKER_NODE: SelectedNode = {
  nodeType: 'Broker',
  brokerName: 'XPI',
  currency: 'BRL',
}

function makeAsset(overrides: Partial<AssetAdminDto>): AssetAdminDto {
  return {
    name: 'ASSET',
    brokerName: 'XPI',
    portfolioName: 'Acoes',
    brokerStatus: 'Active',
    isin: '',
    exchange: '',
    ticker: '',
    country: 'Unknown',
    localTypeCode: '',
    class: 'Unknown',
    valuationMethod: 'Unspecified',
    incomePolicy: 'Unknown',
    quantity: 0,
    ...overrides,
  }
}

const SAME_PORTFOLIO_OTHER_ASSET = makeAsset({ name: 'PETR4' })
const SAME_ASSET = makeAsset({ name: 'KLBN4' })
const OTHER_PORTFOLIO_ASSET = makeAsset({ name: 'BCIA11', portfolioName: 'Fundos' })
const OTHER_BROKER_ASSET = makeAsset({ name: 'AAPL', brokerName: 'Trading212', portfolioName: 'Acoes' })

const ALL_ASSETS = [SAME_PORTFOLIO_OTHER_ASSET, SAME_ASSET, OTHER_PORTFOLIO_ASSET, OTHER_BROKER_ASSET]

describe('useAssetSearchOptions', () => {
  beforeEach(() => {
    getAdminAssetsMock.mockReset()
    getAdminAssetsMock.mockResolvedValue(ALL_ASSETS)
  })

  it('does_not_fetch_when_no_node_selected', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSearchOptions(), { wrapper })
    setNode(null)
    await act(async () => {})
    expect(getAdminAssetsMock).not.toHaveBeenCalled()
    expect(result.current.options).toEqual([])
  })

  it('does_not_fetch_for_a_non_asset_node', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => useAssetSearchOptions(), { wrapper })
    setNode(BROKER_NODE)
    await act(async () => {})
    expect(getAdminAssetsMock).not.toHaveBeenCalled()
  })

  it('sets_isLoading_true_while_fetch_is_in_progress', async () => {
    getAdminAssetsMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSearchOptions(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(true))
  })

  it('filters_to_the_current_broker_and_portfolio_and_excludes_the_current_asset', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSearchOptions(), { wrapper })
    setNode(ASSET_NODE)

    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(result.current.options).toEqual([SAME_PORTFOLIO_OTHER_ASSET])
  })

  it('sets_error_on_fetch_failure', async () => {
    getAdminAssetsMock.mockRejectedValue(new Error('Network error'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSearchOptions(), { wrapper })
    setNode(ASSET_NODE)

    await waitFor(() => expect(result.current.error).toBe('Network error'))
    expect(result.current.options).toEqual([])
  })

  it('retry_re_fetches', async () => {
    getAdminAssetsMock.mockRejectedValueOnce(new Error('Fail'))
    getAdminAssetsMock.mockResolvedValue(ALL_ASSETS)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSearchOptions(), { wrapper })
    setNode(ASSET_NODE)

    await waitFor(() => expect(result.current.error).toBe('Fail'))

    act(() => result.current.retry())
    await waitFor(() => expect(result.current.options).toEqual([SAME_PORTFOLIO_OTHER_ASSET]))
    expect(getAdminAssetsMock).toHaveBeenCalledTimes(2)
  })
})
