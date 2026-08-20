import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { AssetPriceDto, PortfolioAssetSummaryItemDto, SelectedNode } from '../api/types'
import { createSelectedNodeWrapper } from '../test-utils/selectedNodeTestWrapper'
import { usePortfolioAssetSummary } from './usePortfolioAssetSummary'

const getPortfolioAssetsSummaryMock = vi.fn<FinancialApiClient['getPortfolioAssetsSummary']>()
const getCurrentPriceMock = vi.fn<FinancialApiClient['getCurrentPrice']>()
const calculateXirrMock = vi.fn<FinancialApiClient['calculateXirr']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getPortfolioAssetsSummary: getPortfolioAssetsSummaryMock,
    getCurrentPrice: getCurrentPriceMock,
    calculateXirr: calculateXirrMock,
  }),
}))

const BROKER_NODE: SelectedNode = {
  nodeType: 'Broker',
  brokerName: 'XPI',
  currency: 'BRL',
}

const PORTFOLIO_NODE: SelectedNode = {
  nodeType: 'Portfolio',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
}

const ASSET_NODE: SelectedNode = {
  nodeType: 'Asset',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  assetName: 'KLBN4',
  ticker: 'KLBN4',
  exchange: 'BVMF',
}

const ITEM_1: PortfolioAssetSummaryItemDto = {
  assetName: 'ALZR11',
  ticker: 'ALZR11',
  exchange: 'BVMF',
  class: 'RealEstate',
  firstInvestmentDate: '2021-03-01T00:00:00',
  currentQuantity: 25,
  averagePrice: 100,
  averageSellPrice: null,
  totalBought: 2500,
  totalSold: 0,
  totalInvested: 2500,
  realizedGainLoss: 0,
  portfolioWeight: 71.4,
  totalCredits: 125,
  cashFlows: [
    { date: '2021-03-01T00:00:00', amount: -2500 },
    { date: '2021-09-15T00:00:00', amount: 50 },
    { date: '2022-09-15T00:00:00', amount: 75 },
  ],
  lastMonthCredits: 50,
  lastCreditMonth: '2022-09',
  lastMonthCreditsPercent: 2.0,
  creditFrequencyPerYear: 12,
  estimatedAnnualCredits: 600,
  estimatedAnnualPercent: 24.0,
  currentMonthCredits: 0,
}

const ITEM_2: PortfolioAssetSummaryItemDto = {
  assetName: 'MXRF11',
  ticker: 'MXRF11',
  exchange: 'BVMF',
  class: 'RealEstate',
  firstInvestmentDate: '2021-05-15T00:00:00',
  currentQuantity: 10,
  averagePrice: 100,
  averageSellPrice: null,
  totalBought: 1000,
  totalSold: 0,
  totalInvested: 1000,
  realizedGainLoss: 0,
  portfolioWeight: 28.6,
  totalCredits: 0,
  cashFlows: [
    { date: '2021-05-15T00:00:00', amount: -1000 },
  ],
  lastMonthCredits: 0,
  lastCreditMonth: null,
  lastMonthCreditsPercent: null,
  creditFrequencyPerYear: null,
  estimatedAnnualCredits: null,
  estimatedAnnualPercent: null,
  currentMonthCredits: 0,
}

const PRICE_DTO: AssetPriceDto = {
  exchange: 'BVMF',
  ticker: 'ALZR11',
  name: 'ALZR11',
  price: 100.5,
  asOf: '2024-01-01T10:00:00',
  isManual: false,
}

describe('usePortfolioAssetSummary', () => {
  beforeEach(() => {
    getPortfolioAssetsSummaryMock.mockReset()
    getCurrentPriceMock.mockReset()
    calculateXirrMock.mockReset()
    calculateXirrMock.mockResolvedValue({ xirr: null })
  })

  it('calls_getPortfolioAssetsSummary_on_portfolio_node_selection', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1])
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => {
      expect(getPortfolioAssetsSummaryMock).toHaveBeenCalledWith('XPI', 'Acoes', 'active')
    })
  })

  it('forwards_historic_scope_from_context', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([{ ...ITEM_1, realizedGainLoss: -50 }])
    const { wrapper, setNode } = createSelectedNodeWrapper('historic')
    renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => {
      expect(getPortfolioAssetsSummaryMock).toHaveBeenCalledWith('XPI', 'Acoes', 'historic')
    })
  })

  it('skips_current_price_fetch_for_historic_scope', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([{ ...ITEM_1, realizedGainLoss: -50 }])
    const { wrapper, setNode } = createSelectedNodeWrapper('historic')
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(result.current.items).not.toBeNull())
    expect(getCurrentPriceMock).not.toHaveBeenCalled()
    expect(result.current.rowPrices[0].isLoading).toBe(false)
    expect(result.current.rowPrices[0].currentPrice).toBeNull()
  })

  it('does_not_fetch_when_broker_node_selected', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(BROKER_NODE)
    await act(async () => {})
    expect(getPortfolioAssetsSummaryMock).not.toHaveBeenCalled()
  })

  it('does_not_fetch_when_asset_node_selected', async () => {
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(ASSET_NODE)
    await act(async () => {})
    expect(getPortfolioAssetsSummaryMock).not.toHaveBeenCalled()
  })

  it('sets_isLoading_true_while_fetch_in_progress', async () => {
    getPortfolioAssetsSummaryMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(result.current.isLoading).toBe(true))
  })

  it('populates_items_on_successful_fetch', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1, ITEM_2])
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(result.current.items).not.toBeNull())
    expect(result.current.items).toHaveLength(2)
    expect(result.current.items![0].assetName).toBe('ALZR11')
    expect(result.current.items![0].totalCredits).toBe(125)
    expect(result.current.items![0].cashFlows).toHaveLength(3)
  })

  it('sets_error_on_fetch_failure', async () => {
    getPortfolioAssetsSummaryMock.mockRejectedValue(new Error('Network error'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(result.current.error).toBe('Network error'))
    expect(result.current.items).toBeNull()
  })

  it('resets_state_on_node_change', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1])
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(result.current.items).not.toBeNull())

    setNode(BROKER_NODE)
    await waitFor(() => {
      expect(result.current.items).toBeNull()
      expect(result.current.isLoading).toBe(false)
    })
  })

  it('retry_re_triggers_fetch', async () => {
    getPortfolioAssetsSummaryMock.mockRejectedValueOnce(new Error('Fail'))
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1])
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(result.current.error).toBe('Fail'))

    act(() => result.current.retry())
    await waitFor(() => expect(result.current.items).not.toBeNull())
    expect(getPortfolioAssetsSummaryMock).toHaveBeenCalledTimes(2)
  })

  it('fires_getCurrentPrice_for_each_item_after_fetch', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1, ITEM_2])
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(getCurrentPriceMock).toHaveBeenCalledTimes(2))
    expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'ALZR11', 'RealEstate', 'XPI', 'ALZR11', 'Acoes', 'ALZR11')
    expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'MXRF11', 'RealEstate', 'XPI', 'MXRF11', 'Acoes', 'MXRF11')
  })

  it('fires_getCurrentPrice_for_cryptocurrency_item_with_blank_exchange', async () => {
    const cryptoItem: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'Bitcoin', ticker: 'BTC', exchange: '', class: 'Cryptocurrency' }
    const coinbaseNode: SelectedNode = { nodeType: 'Portfolio', brokerName: 'Coinbase', portfolioName: 'Cryptocurrency' }
    getPortfolioAssetsSummaryMock.mockResolvedValue([cryptoItem])
    getCurrentPriceMock.mockResolvedValue(PRICE_DTO)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(coinbaseNode)
    await waitFor(() =>
      expect(getCurrentPriceMock).toHaveBeenCalledWith('', 'BTC', 'Cryptocurrency', 'Coinbase', 'Bitcoin', 'Cryptocurrency', 'Bitcoin'),
    )
  })

  it('fires_getCurrentPrice_with_name_for_bond_item_with_blank_exchange', async () => {
    const bondItem: PortfolioAssetSummaryItemDto = {
      ...ITEM_1,
      assetName: 'TESOURO IPCA+ 2029',
      ticker: 'TESOURO IPCA+ 2029',
      exchange: '',
      class: 'Bond',
    }
    const reservaNode: SelectedNode = { nodeType: 'Portfolio', brokerName: 'XPI', portfolioName: 'Reserva' }
    getPortfolioAssetsSummaryMock.mockResolvedValue([bondItem])
    getCurrentPriceMock.mockResolvedValue({ ...PRICE_DTO, ticker: 'TESOURO IPCA+ 2029', exchange: '', price: 3775.97 })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(reservaNode)
    await waitFor(() =>
      expect(getCurrentPriceMock).toHaveBeenCalledWith('', 'TESOURO IPCA+ 2029', 'Bond', 'XPI', 'TESOURO IPCA+ 2029', 'Reserva', 'TESOURO IPCA+ 2029'),
    )
    await waitFor(() => expect(result.current.rowPrices[0]?.isLoading).toBe(false))
    expect(result.current.rowPrices[0].currentPrice).toBe(3775.97)
    expect(result.current.rowPrices[0].fetchFailed).toBe(false)
  })

  it('sets_row_price_isManual_true_when_price_falls_back_to_a_manual_entry', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1])
    getCurrentPriceMock.mockResolvedValue({ ...PRICE_DTO, isManual: true })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)

    await waitFor(() => expect(result.current.rowPrices[0]?.isLoading).toBe(false))
    expect(result.current.rowPrices[0].isManual).toBe(true)
  })

  it('sets_row_price_isManual_false_when_price_comes_from_a_live_fetch', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1])
    getCurrentPriceMock.mockResolvedValue(PRICE_DTO)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)

    await waitFor(() => expect(result.current.rowPrices[0]?.isLoading).toBe(false))
    expect(result.current.rowPrices[0].isManual).toBe(false)
  })

  it('sets_row_price_loading_true_after_items_arrive', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1])
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(result.current.items).not.toBeNull())
    expect(result.current.rowPrices[0].isLoading).toBe(true)
  })

  it('populates_row_price_on_price_success', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1])
    getCurrentPriceMock.mockResolvedValue(PRICE_DTO)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(result.current.rowPrices[0]?.isLoading).toBe(false))
    expect(result.current.rowPrices[0].currentPrice).toBe(100.5)
    expect(result.current.rowPrices[0].fetchFailed).toBe(false)
  })

  it('sets_row_fetch_failed_on_price_error', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1])
    getCurrentPriceMock.mockRejectedValue(new Error('Price fetch failed'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => expect(result.current.rowPrices[0]?.fetchFailed).toBe(true))
    expect(result.current.rowPrices[0].currentPrice).toBeNull()
    expect(result.current.rowPrices[0].isLoading).toBe(false)
  })

  it('failed_price_for_one_row_does_not_affect_other_rows', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1, ITEM_2])
    getCurrentPriceMock
      .mockRejectedValueOnce(new Error('Price fetch failed'))
      .mockResolvedValueOnce(PRICE_DTO)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)
    await waitFor(() => {
      expect(result.current.rowPrices[0]?.isLoading).toBe(false)
      expect(result.current.rowPrices[1]?.isLoading).toBe(false)
    })
    expect(result.current.rowPrices[0].fetchFailed).toBe(true)
    expect(result.current.rowPrices[1].currentPrice).toBe(100.5)
    expect(result.current.rowPrices[1].fetchFailed).toBe(false)
  })

  it('requests_the_row_rate_against_price_times_quantity_once_the_price_lands', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1])
    getCurrentPriceMock.mockResolvedValue({ ...PRICE_DTO, price: 100.5 })
    calculateXirrMock.mockResolvedValue({ xirr: 0.1234 })
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)

    await waitFor(() => expect(result.current.rowPrices[0].xirr).toBe(0.1234))
    expect(calculateXirrMock).toHaveBeenCalledWith(ITEM_1.cashFlows, 100.5 * ITEM_1.currentQuantity)
  })

  it('requests_the_row_rate_with_a_zero_terminal_value_for_historic_rows', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1])
    calculateXirrMock.mockResolvedValue({ xirr: 0.15 })
    const { wrapper, setNode } = createSelectedNodeWrapper('historic')
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)

    await waitFor(() => expect(result.current.rowPrices[0].xirr).toBe(0.15))
    expect(calculateXirrMock).toHaveBeenCalledWith(ITEM_1.cashFlows, 0)
    expect(getCurrentPriceMock).not.toHaveBeenCalled()
  })

  it('settles_the_row_rate_to_null_when_the_calculation_fails', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1])
    getCurrentPriceMock.mockResolvedValue({ ...PRICE_DTO, price: 100.5 })
    calculateXirrMock.mockRejectedValue(new Error('XIRR unavailable'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)

    await waitFor(() => expect(result.current.rowPrices[0].isLoadingXirr).toBe(false))
    expect(result.current.rowPrices[0].xirr).toBeNull()
    expect(result.current.rowPrices[0].currentPrice).toBe(100.5)
  })

  it('asks_for_no_rate_when_the_price_fetch_fails', async () => {
    getPortfolioAssetsSummaryMock.mockResolvedValue([ITEM_1])
    getCurrentPriceMock.mockRejectedValue(new Error('boom'))
    const { wrapper, setNode } = createSelectedNodeWrapper()
    const { result } = renderHook(() => usePortfolioAssetSummary(), { wrapper })
    setNode(PORTFOLIO_NODE)

    await waitFor(() => expect(result.current.rowPrices[0].fetchFailed).toBe(true))
    expect(result.current.rowPrices[0].isLoadingXirr).toBe(false)
    expect(calculateXirrMock).not.toHaveBeenCalled()
  })
})
