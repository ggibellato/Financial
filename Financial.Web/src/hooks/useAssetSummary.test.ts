import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { AssetDetailsDto, AssetPriceDto, PortfolioAssetSummaryItemDto, SelectedNode, XirrResultDto } from '../api/types'
import { createSelectedNodeWrapper } from '../test-utils/selectedNodeTestWrapper'
import { useAssetSummary } from './useAssetSummary'

const getAssetDetailsMock = vi.fn<FinancialApiClient['getAssetDetails']>()
const getCurrentPriceMock = vi.fn<FinancialApiClient['getCurrentPrice']>()
const calculateXirrMock = vi.fn<FinancialApiClient['calculateXirr']>()
const getPortfolioAssetsSummaryMock = vi.fn<FinancialApiClient['getPortfolioAssetsSummary']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getAssetDetails: getAssetDetailsMock,
    getCurrentPrice: getCurrentPriceMock,
    calculateXirr: calculateXirrMock,
    getPortfolioAssetsSummary: getPortfolioAssetsSummaryMock,
  }),
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
  transactions: [],
  credits: [],
  cashFlowsWithCredits: [{ date: '2024-01-01T00:00:00', amount: -2000 }],
  cashFlowsWithoutCredits: [{ date: '2024-01-01T00:00:00', amount: -2000 }],
}

const PRICE: AssetPriceDto = {
  exchange: 'BVMF',
  ticker: 'KLBN4',
  name: 'Klabin',
  price: 25,
  asOf: '2026-06-26T10:00:00',
}

describe('useAssetSummary', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset()
    getCurrentPriceMock.mockReset()
    calculateXirrMock.mockReset()
    calculateXirrMock.mockResolvedValue({ xirr: null })
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

  it('computes_xirr_for_historic_scope_using_zero_terminal_value_without_waiting_for_price', async () => {
    const historicAsset = { ...ASSET_DETAILS, quantity: 0, totalSold: 250 }
    getAssetDetailsMock.mockResolvedValue(historicAsset)
    getPortfolioAssetsSummaryMock.mockResolvedValue([])
    calculateXirrMock.mockResolvedValue({ xirr: 0.08 })
    const { wrapper, setNode } = createSelectedNodeWrapper('historic')
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.xirr).toBe(0.08))
    expect(calculateXirrMock).toHaveBeenCalledWith(historicAsset.cashFlowsWithoutCredits, 0)
    expect(calculateXirrMock).toHaveBeenCalledWith(historicAsset.cashFlowsWithCredits, 0)
  })

  it('fetches_current_price_simultaneously_with_asset_details', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => {
      expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'KLBN4', undefined, 'XPI', undefined)
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
      expect(getCurrentPriceMock).toHaveBeenCalledWith('', 'BTC', 'Cryptocurrency', 'Coinbase', undefined)
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

  it('computes_total_current_value', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.asset).not.toBeNull())
    await waitFor(() => expect(result.current.price).not.toBeNull())
    expect(result.current.totalCurrentValue).toBeCloseTo(PRICE.price * ASSET_DETAILS.quantity, 2)
  })

  it('computes_result_percent', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.price).not.toBeNull())
    const tcv = PRICE.price * ASSET_DETAILS.quantity
    const costBasis = ASSET_DETAILS.quantity * ASSET_DETAILS.averagePrice
    const expected = (tcv - costBasis) / costBasis
    expect(result.current.resultPercent).toBeCloseTo(expected, 5)
  })

  it('computes_total_current_plus_credits', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.price).not.toBeNull())
    const tcv = PRICE.price * ASSET_DETAILS.quantity
    expect(result.current.totalCurrentPlusCredits).toBeCloseTo(tcv + ASSET_DETAILS.totalCredits, 2)
  })

  it('computes_result_percent_with_credits', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.price).not.toBeNull())
    const tcv = PRICE.price * ASSET_DETAILS.quantity
    const tcc = tcv + ASSET_DETAILS.totalCredits
    const costBasis = ASSET_DETAILS.quantity * ASSET_DETAILS.averagePrice
    const expected = (tcc - costBasis) / costBasis
    expect(result.current.resultWithCreditsPercent).toBeCloseTo(expected, 5)
  })

  it('computes_result_percent_using_current_cost_basis_not_gross_total_bought', async () => {
    // Partial sell scenario: totalBought (2000) no longer reflects the current position's
    // cost basis once some quantity has been sold — quantity x averagePrice (60 x 20 = 1200) does.
    const partiallySold: AssetDetailsDto = { ...ASSET_DETAILS, quantity: 60, totalBought: 2000, totalSold: 800 }
    getAssetDetailsMock.mockResolvedValue(partiallySold)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.price).not.toBeNull())
    const tcv = PRICE.price * partiallySold.quantity
    const costBasis = partiallySold.quantity * partiallySold.averagePrice
    const expected = (tcv - costBasis) / costBasis
    expect(result.current.resultPercent).toBeCloseTo(expected, 5)
    expect(result.current.resultPercent).not.toBeCloseTo((tcv - partiallySold.totalBought) / partiallySold.totalBought, 5)
  })

  it('fetches_xirr_for_both_totals_once_asset_and_price_are_loaded', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    const responses: XirrResultDto[] = [{ xirr: 0.12 }, { xirr: 0.15 }]
    calculateXirrMock.mockImplementation(() => Promise.resolve(responses.shift() ?? { xirr: null }))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.xirr).toBe(0.12))
    expect(result.current.xirrWithCredits).toBe(0.15)
    expect(calculateXirrMock).toHaveBeenCalledWith(
      ASSET_DETAILS.cashFlowsWithoutCredits,
      PRICE.price * ASSET_DETAILS.quantity,
    )
    expect(calculateXirrMock).toHaveBeenCalledWith(
      ASSET_DETAILS.cashFlowsWithCredits,
      PRICE.price * ASSET_DETAILS.quantity + ASSET_DETAILS.totalCredits,
    )
  })

  it('resets_xirr_to_null_when_xirr_calculation_fails', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET_DETAILS)
    getCurrentPriceMock.mockResolvedValue(PRICE)
    calculateXirrMock.mockRejectedValue(new Error('XIRR unavailable'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => useAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await waitFor(() => expect(result.current.price).not.toBeNull())
    await waitFor(() => expect(calculateXirrMock).toHaveBeenCalled())
    expect(result.current.xirr).toBeNull()
    expect(result.current.xirrWithCredits).toBeNull()
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
