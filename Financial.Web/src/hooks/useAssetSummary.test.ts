import { act, renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { createElement } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { AssetDetailsDto, AssetPriceDto, SelectedNode } from '../api/types'
import { SelectedNodeProvider, useSelectedNode } from '../context/SelectedNodeContext'
import { useAssetSummary } from './useAssetSummary'

const getAssetDetailsMock = vi.fn<FinancialApiClient['getAssetDetails']>()
const getCurrentPriceMock = vi.fn<FinancialApiClient['getCurrentPrice']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getAssetDetails: getAssetDetailsMock,
    getCurrentPrice: getCurrentPriceMock,
  }),
}))

const ASSET_NODE: SelectedNode = {
  nodeType: 'Asset',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  assetName: 'KLBN4',
  ticker: 'KLBN4',
  exchange: 'BVMF',
  isActive: true,
}

const OTHER_ASSET_NODE: SelectedNode = {
  ...ASSET_NODE,
  assetName: 'TRPL4',
  ticker: 'TRPL4',
}

const ASSET_DETAILS: AssetDetailsDto = {
  name: 'KLBN4',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  ticker: 'KLBN4',
  isin: 'BRKLBN',
  exchange: 'BVMF',
  country: 'BR',
  localTypeCode: 'ON',
  class: 'Equity',
  quantity: 100,
  averagePrice: 20,
  isActive: true,
  totalBought: 2000,
  totalSold: 0,
  totalCredits: 50,
  transactions: [],
  credits: [],
}

const PRICE: AssetPriceDto = {
  exchange: 'BVMF',
  ticker: 'KLBN4',
  name: 'Klabin',
  price: 25,
  asOf: '2026-06-26T10:00:00',
}

function createWrapper() {
  let setNodeRef: ((node: SelectedNode | null) => void) | undefined

  function NodeControl() {
    const { setSelectedNode } = useSelectedNode()
    setNodeRef = setSelectedNode
    return null
  }

  function Wrapper({ children }: { children: ReactNode }) {
    return createElement(SelectedNodeProvider, null, createElement(NodeControl), children)
  }

  return {
    wrapper: Wrapper,
    setNode: (node: SelectedNode | null) => act(() => { setNodeRef?.(node) }),
  }
}

describe('useAssetSummary', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset()
    getCurrentPriceMock.mockReset()
  })

  it('fetches_asset_details_on_asset_selection', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createWrapper()
    renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => {
      expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4')
    })
  })

  it('fetches_current_price_simultaneously_with_asset_details', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createWrapper()
    renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => {
      expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'KLBN4')
      expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4')
    })
  })

  it('returns_isLoadingAsset_true_while_fetching', async () => {
    getAssetDetailsMock.mockReturnValue(new Promise(() => {}))
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoadingAsset).toBe(true))
  })

  it('computes_total_current_value', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    await waitFor(() => expect(result.current.price).not.toBeNull())
    expect(result.current.totalCurrentValue).toBeCloseTo(PRICE.price * ASSET_DETAILS.quantity, 2)
  })

  it('computes_result_percent', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.price).not.toBeNull())
    const tcv = PRICE.price * ASSET_DETAILS.quantity
    const expected = (tcv - ASSET_DETAILS.totalBought) / ASSET_DETAILS.totalBought
    expect(result.current.resultPercent).toBeCloseTo(expected, 5)
  })

  it('computes_total_current_plus_credits', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.price).not.toBeNull())
    const tcv = PRICE.price * ASSET_DETAILS.quantity
    expect(result.current.totalCurrentPlusCredits).toBeCloseTo(tcv + ASSET_DETAILS.totalCredits, 2)
  })

  it('computes_result_percent_with_credits', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.price).not.toBeNull())
    const tcv = PRICE.price * ASSET_DETAILS.quantity
    const tcc = tcv + ASSET_DETAILS.totalCredits
    const expected = (tcc - ASSET_DETAILS.totalBought) / ASSET_DETAILS.totalBought
    expect(result.current.resultWithCreditsPercent).toBeCloseTo(expected, 5)
  })

  it('sets_asset_error_on_load_failure', async () => {
    getAssetDetailsMock.mockRejectedValue(new Error('Network error'))
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.assetError).toBe('Network error'))
    expect(result.current.asset).toBeNull()
  })

  it('sets_price_error_on_price_fetch_failure', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockRejectedValue(new Error('Price unavailable'))
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    await waitFor(() => expect(result.current.priceError).toBe('Price unavailable'))
    expect(result.current.assetError).toBeNull()
  })

  it('refresh_triggers_new_price_fetch', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.price).not.toBeNull())
    expect(getCurrentPriceMock).toHaveBeenCalledTimes(1)
    act(() => result.current.refresh())
    await waitFor(() => expect(getCurrentPriceMock).toHaveBeenCalledTimes(2))
  })

  it('disables_refresh_while_price_is_loading', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoadingPrice).toBe(true))
    expect(result.current.canRefresh).toBe(false)
  })

  it('resets_state_on_node_change', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset?.name).toBe('KLBN4'))
    getAssetDetailsMock.mockReturnValue(new Promise(() => {}))
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    setNode(OTHER_ASSET_NODE)
    await waitFor(() => expect(result.current.isLoadingAsset).toBe(true))
    expect(result.current.asset).toBeNull()
  })

  it('showCurrentSection_false_when_quantity_is_zero', async () => {
    const zeroQty = { ...ASSET_DETAILS, quantity: 0 }
    getAssetDetailsMock.mockResolvedValue(zeroQty)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    expect(result.current.showCurrentSection).toBe(false)
  })

  it('showCurrentSection_false_when_average_price_is_zero', async () => {
    const zeroPx = { ...ASSET_DETAILS, averagePrice: 0 }
    getAssetDetailsMock.mockResolvedValue(zeroPx)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    expect(result.current.showCurrentSection).toBe(false)
  })

  it('showCurrentSection_true_when_both_nonzero', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    expect(result.current.showCurrentSection).toBe(true)
  })
})
