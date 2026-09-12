import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetDetailsDto, AssetPriceDto, PortfolioAssetSummaryItemDto, SelectedNode } from '../../api/types'
import { createSelectedNodeWrapper } from '../../test-utils/selectedNodeTestWrapper'
import { useAssetSummary } from '../useAssetSummary'

const { getAssetDetailsMock, getCurrentPriceMock, getPortfolioAssetsSummaryMock } = vi.hoisted(() => ({
  getAssetDetailsMock: vi.fn<FinancialApiClient['getAssetDetails']>(),
  getCurrentPriceMock: vi.fn<FinancialApiClient['getCurrentPrice']>(),
  getPortfolioAssetsSummaryMock: vi.fn<FinancialApiClient['getPortfolioAssetsSummary']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAssetDetails: getAssetDetailsMock,
    getCurrentPrice: getCurrentPriceMock,
    getPortfolioAssetsSummary: getPortfolioAssetsSummaryMock,
  } as Partial<FinancialApiClient>,
}))

const ASSET_NODE: SelectedNode = {
  nodeType: 'Asset',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  assetName: 'KLBN4',
  ticker: 'KLBN4',
  exchange: 'BVMF',
  positionType: 'Long',
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
  averageSellPrice: null,
  positionType: 'Long',
  totalBought: 2000,
  totalSold: 0,
  totalCredits: 50,
  realizedGainLoss: 0,
  marketValue: null,
  costOfUnitsHeld: 2000,
  unrealisedGain: null,
  priceAsOfDate: null,
  isPriceStale: false,
  priceOnlyReturn: null,
  totalReturn: null,
  transactions: [],
  credits: [],
  priceSnapshots: [],
  cashFlowsWithCredits: [{ date: '2024-01-01T00:00:00', amount: -2000 }],
  cashFlowsWithoutCredits: [{ date: '2024-01-01T00:00:00', amount: -2000 }],
}

const PRICE: AssetPriceDto = {
  exchange: 'BVMF',
  ticker: 'KLBN4',
  name: 'Klabin',
  price: 25,
  asOf: '2026-06-26T10:00:00',
  asOfDate: null,
  isManual: false,
}

const REFRESHED_ASSET_DETAILS: AssetDetailsDto = {
  ...ASSET_DETAILS,
  marketValue: 2500,
  costOfUnitsHeld: 2000,
  unrealisedGain: 500,
  priceAsOfDate: '2026-06-26',
  isPriceStale: false,
  priceOnlyReturn: 0.12,
  totalReturn: 0.15,
}

describe('useAssetSummary', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset()
    getCurrentPriceMock.mockReset()
    getPortfolioAssetsSummaryMock.mockReset()
  })

  it('fetches_asset_details_on_asset_selection', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => {
      expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4', 'active')
    })
  })

  it('fetches_asset_details_with_historic_scope_when_context_scope_is_historic', async () => {
    const historicAsset = { ...ASSET_DETAILS, quantity: 0, totalSold: 250 }
    getAssetDetailsMock.mockResolvedValue(historicAsset)
    getPortfolioAssetsSummaryMock.mockResolvedValue([])
    const { wrapper, setNode } = createSelectedNodeWrapper('historic')
    renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => {
      expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4', 'historic')
    })
  })

  it('fetches_portfolio_weight_from_portfolio_summary_for_historic_asset', async () => {
    const historicAsset = { ...ASSET_DETAILS, quantity: 0, totalSold: 250, realizedGainLoss: -1750 }
    getAssetDetailsMock.mockResolvedValue(historicAsset)
    const summaryItem: PortfolioAssetSummaryItemDto = {
      assetName: 'KLBN4',
      ticker: 'KLBN4',
      exchange: 'BVMF',
      class: 'Equity',
      firstInvestmentDate: '2024-01-01T00:00:00',
      currentQuantity: 0,
      averagePrice: 20,
      averageSellPrice: 21,
      totalBought: 2000,
      totalSold: 250,
      totalInvested: 1750,
      realizedGainLoss: -1750,
      portfolioWeight: 100,
      marketValue: null,
      costOfUnitsHeld: 0,
      unrealisedGain: null,
      priceAsOfDate: null,
      isPriceStale: false,
      priceOnlyReturn: null,
      totalReturn: null,
      totalReturnNetOfTax: null,
      totalCredits: 50,
      cashFlows: [],
      lastMonthCredits: 0,
      lastCreditMonth: null,
      lastMonthCreditsPercent: null,
      creditFrequencyPerYear: null,
      estimatedAnnualCredits: null,
      estimatedAnnualPercent: null,
      currentMonthCredits: 0,
    }
    getPortfolioAssetsSummaryMock.mockResolvedValue([summaryItem, { ...summaryItem, assetName: 'OTHER' }])
    const { wrapper, setNode } = createSelectedNodeWrapper('historic')
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.portfolioWeight).not.toBeNull())
    expect(getPortfolioAssetsSummaryMock).toHaveBeenCalledWith('XPI', 'Acoes', 'historic')
    expect(result.current.portfolioWeight).toBe(100)
    // realizedGainLoss comes directly from the asset details response, not a second round-trip
    expect(result.current.asset?.realizedGainLoss).toBe(-1750)
  })

  it('skips_current_price_fetch_for_historic_scope', async () => {
    const historicAsset = { ...ASSET_DETAILS, quantity: 0, totalSold: 250 }
    getAssetDetailsMock.mockResolvedValue(historicAsset)
    getPortfolioAssetsSummaryMock.mockResolvedValue([])
    const { wrapper, setNode } = createSelectedNodeWrapper('historic')
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    expect(getCurrentPriceMock).not.toHaveBeenCalled()
  })

  it('reports_the_server_computed_return_for_historic_scope_without_a_price_fetch', async () => {
    const historicAsset = { ...ASSET_DETAILS, quantity: 0, totalSold: 250, priceOnlyReturn: 0.08, totalReturn: 0.08 }
    getAssetDetailsMock.mockResolvedValue(historicAsset)
    getPortfolioAssetsSummaryMock.mockResolvedValue([])
    const { wrapper, setNode } = createSelectedNodeWrapper('historic')
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset?.priceOnlyReturn).toBe(0.08))
    expect(getCurrentPriceMock).not.toHaveBeenCalled()
  })

  it('fetches_current_price_simultaneously_with_asset_details', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => {
      expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'KLBN4', undefined, 'XPI', undefined, 'Acoes', 'KLBN4')
      expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Acoes', 'KLBN4', 'active')
    })
  })

  it('fetches_current_price_for_cryptocurrency_asset_with_blank_exchange', async () => {
    const cryptoNode: SelectedNode = {
      nodeType: 'Asset',
      brokerName: 'Coinbase',
      portfolioName: 'Cryptocurrency',
      assetName: 'Bitcoin',
      ticker: 'BTC',
      exchange: '',
      positionType: 'Long',
      assetClass: 'Cryptocurrency',
    }
    getAssetDetailsMock.mockResolvedValue({ ...ASSET_DETAILS, name: 'Bitcoin', ticker: 'BTC', class: 'Cryptocurrency' })
    getCurrentPriceMock.mockResolvedValue({ ...PRICE, ticker: 'BTC', exchange: '' })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => useAssetSummary(), { wrapper })
    setNode(cryptoNode)
    await waitFor(() => {
      expect(getCurrentPriceMock).toHaveBeenCalledWith('', 'BTC', 'Cryptocurrency', 'Coinbase', undefined, 'Cryptocurrency', 'Bitcoin')
    })
  })

  it('fetches_current_price_for_bond_asset_without_exchange', async () => {
    const bondNode: SelectedNode = {
      nodeType: 'Asset',
      brokerName: 'XPI',
      portfolioName: 'Reserva',
      assetName: 'TESOURO IPCA+ 2029',
      ticker: 'TESOURO IPCA+ 2029',
      exchange: '',
      positionType: 'Long',
      assetClass: 'Bond',
    }
    getAssetDetailsMock.mockResolvedValue({
      ...ASSET_DETAILS,
      name: 'TESOURO IPCA+ 2029',
      ticker: 'TESOURO IPCA+ 2029',
      class: 'Bond',
    })
    getCurrentPriceMock.mockResolvedValue({ ...PRICE, ticker: 'TESOURO IPCA+ 2029', exchange: '' })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => useAssetSummary(), { wrapper })
    setNode(bondNode)
    await waitFor(() => {
      expect(getCurrentPriceMock).toHaveBeenCalledWith(
        '',
        'TESOURO IPCA+ 2029',
        'Bond',
        'XPI',
        'TESOURO IPCA+ 2029',
        'Reserva',
        'TESOURO IPCA+ 2029',
      )
    })
  })

  it('returns_isLoadingAsset_true_while_fetching', async () => {
    getAssetDetailsMock.mockReturnValue(new Promise(() => {}))
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoadingAsset).toBe(true))
  })

  it('refetches_asset_details_after_a_successful_price_fetch', async () => {
    getAssetDetailsMock.mockResolvedValueOnce(ASSET_DETAILS).mockResolvedValueOnce(REFRESHED_ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset?.marketValue).toBe(REFRESHED_ASSET_DETAILS.marketValue))
    expect(result.current.asset?.unrealisedGain).toBe(REFRESHED_ASSET_DETAILS.unrealisedGain)
    expect(result.current.asset?.priceOnlyReturn).toBe(REFRESHED_ASSET_DETAILS.priceOnlyReturn)
    expect(result.current.asset?.totalReturn).toBe(REFRESHED_ASSET_DETAILS.totalReturn)
    expect(getAssetDetailsMock).toHaveBeenCalledTimes(2)
    expect(getAssetDetailsMock).toHaveBeenNthCalledWith(2, 'XPI', 'Acoes', 'KLBN4', 'active')
  })

  it('sets_asset_error_on_load_failure', async () => {
    getAssetDetailsMock.mockRejectedValue(new Error('Network error'))
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.assetError).toBe('Network error'))
    expect(result.current.asset).toBeNull()
  })

  it('sets_price_error_on_price_fetch_failure', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockRejectedValue(new Error('Price unavailable'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    await waitFor(() => expect(result.current.priceError).toBe('Price unavailable'))
    expect(result.current.assetError).toBeNull()
    expect(getAssetDetailsMock).toHaveBeenCalledTimes(1)
  })

  it('refresh_triggers_new_price_fetch', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createSelectedNodeWrapper()
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
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.isLoadingPrice).toBe(true))
    expect(result.current.canRefresh).toBe(false)
  })

  it('resets_state_on_node_change', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createSelectedNodeWrapper()
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
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    expect(result.current.showCurrentSection).toBe(false)
  })

  it('showCurrentSection_false_when_average_price_is_zero', async () => {
    const zeroPx = { ...ASSET_DETAILS, averagePrice: 0 }
    getAssetDetailsMock.mockResolvedValue(zeroPx)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    expect(result.current.showCurrentSection).toBe(false)
  })

  it('showCurrentSection_true_when_both_nonzero', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    expect(result.current.showCurrentSection).toBe(true)
  })
})
