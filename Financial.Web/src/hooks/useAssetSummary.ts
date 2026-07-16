import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { AssetDetailsDto, AssetPriceDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'

interface SummaryState {
  asset: AssetDetailsDto | null
  isLoadingAsset: boolean
  assetError: string | null
  assetRetryCount: number
  price: AssetPriceDto | null
  isLoadingPrice: boolean
  priceError: string | null
  xirr: number | null
  xirrWithCredits: number | null
  portfolioWeight: number | null
}

type SummaryAction =
  | { type: 'RESET' }
  | { type: 'ASSET_FETCH_START' }
  | { type: 'ASSET_FETCH_SUCCESS'; payload: AssetDetailsDto }
  | { type: 'ASSET_FETCH_ERROR'; payload: string }
  | { type: 'ASSET_RETRY' }
  | { type: 'PRICE_FETCH_START' }
  | { type: 'PRICE_FETCH_SUCCESS'; payload: AssetPriceDto }
  | { type: 'PRICE_FETCH_ERROR'; payload: string }
  | { type: 'XIRR_RESET' }
  | { type: 'XIRR_FETCH_SUCCESS'; xirr: number | null; xirrWithCredits: number | null }
  | { type: 'PORTFOLIO_WEIGHT_SUCCESS'; portfolioWeight: number | null }

const INITIAL_STATE: SummaryState = {
  asset: null,
  isLoadingAsset: false,
  assetError: null,
  assetRetryCount: 0,
  price: null,
  isLoadingPrice: false,
  priceError: null,
  xirr: null,
  xirrWithCredits: null,
  portfolioWeight: null,
}

function reducer(state: SummaryState, action: SummaryAction): SummaryState {
  switch (action.type) {
    case 'RESET':
      return INITIAL_STATE
    case 'ASSET_FETCH_START':
      return {
        ...state,
        isLoadingAsset: true,
        assetError: null,
        asset: null,
        price: null,
        priceError: null,
        portfolioWeight: null,
      }
    case 'ASSET_FETCH_SUCCESS':
      return { ...state, isLoadingAsset: false, asset: action.payload }
    case 'ASSET_FETCH_ERROR':
      return { ...state, isLoadingAsset: false, assetError: action.payload }
    case 'ASSET_RETRY':
      return { ...state, assetRetryCount: state.assetRetryCount + 1 }
    case 'PRICE_FETCH_START':
      return { ...state, isLoadingPrice: true, priceError: null, price: null, xirr: null, xirrWithCredits: null }
    case 'PRICE_FETCH_SUCCESS':
      return { ...state, isLoadingPrice: false, price: action.payload }
    case 'PRICE_FETCH_ERROR':
      return { ...state, isLoadingPrice: false, priceError: action.payload, price: null }
    case 'XIRR_RESET':
      return { ...state, xirr: null, xirrWithCredits: null }
    case 'XIRR_FETCH_SUCCESS':
      return { ...state, xirr: action.xirr, xirrWithCredits: action.xirrWithCredits }
    case 'PORTFOLIO_WEIGHT_SUCCESS':
      return { ...state, portfolioWeight: action.portfolioWeight }
    default:
      return state
  }
}

export interface AssetSummaryData {
  asset: AssetDetailsDto | null
  isLoadingAsset: boolean
  assetError: string | null
  retryAsset: () => void
  price: AssetPriceDto | null
  isLoadingPrice: boolean
  priceError: string | null
  canRefresh: boolean
  refresh: () => void
  showCurrentSection: boolean
  totalCurrentValue: number
  resultPercent: number
  totalCurrentPlusCredits: number
  resultWithCreditsPercent: number
  xirr: number | null
  xirrWithCredits: number | null
  portfolioWeight: number | null
}

export function useAssetSummary(): AssetSummaryData {
  const { selectedNode, scope } = useSelectedNode()
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const isAsset =
    selectedNode?.nodeType === 'Asset' &&
    !!selectedNode.portfolioName &&
    !!selectedNode.assetName

  const fetchPrice = useCallback(
    (exchange: string, ticker: string, assetClass?: string, brokerName?: string, name?: string) => {
      dispatch({ type: 'PRICE_FETCH_START' })
      void apiClient
        .getCurrentPrice(exchange, ticker, assetClass, brokerName, name)
        .then((result) => dispatch({ type: 'PRICE_FETCH_SUCCESS', payload: result }))
        .catch((err: unknown) => {
          dispatch({
            type: 'PRICE_FETCH_ERROR',
            payload: err instanceof Error ? err.message : 'Unable to fetch current price',
          })
        })
    },
    [apiClient],
  )

  useEffect(() => {
    if (!isAsset || !selectedNode) {
      dispatch({ type: 'RESET' })
      return
    }

    const { brokerName, portfolioName, assetName, exchange, ticker, assetClass } = selectedNode

    if (!portfolioName || !assetName) {
      dispatch({ type: 'RESET' })
      return
    }

    dispatch({ type: 'ASSET_FETCH_START' })

    if (
      scope === 'active' &&
      ticker &&
      (exchange || assetClass === 'Cryptocurrency' || (assetClass === 'Bond' && assetName))
    ) {
      fetchPrice(exchange ?? '', ticker, assetClass, brokerName, assetClass === 'Bond' ? assetName : undefined)
    }

    void apiClient
      .getAssetDetails(brokerName, portfolioName, assetName, scope)
      .then((result) => dispatch({ type: 'ASSET_FETCH_SUCCESS', payload: result }))
      .catch((err: unknown) => {
        dispatch({
          type: 'ASSET_FETCH_ERROR',
          payload: err instanceof Error ? err.message : 'Unable to load asset details',
        })
      })

    if (scope === 'historic') {
      void apiClient
        .getPortfolioAssetsSummary(brokerName, portfolioName, 'historic')
        .then((items) => {
          const match = items.find((item) => item.assetName === assetName)
          dispatch({ type: 'PORTFOLIO_WEIGHT_SUCCESS', portfolioWeight: match?.portfolioWeight ?? null })
        })
        .catch(() => {
          dispatch({ type: 'PORTFOLIO_WEIGHT_SUCCESS', portfolioWeight: null })
        })
    }
  }, [selectedNode, isAsset, apiClient, fetchPrice, scope, state.assetRetryCount])

  const retryAsset = useCallback(() => dispatch({ type: 'ASSET_RETRY' }), [])

  const refresh = useCallback(() => {
    if (!isAsset || !selectedNode?.ticker) return
    const isBondWithName = selectedNode.assetClass === 'Bond' && !!selectedNode.assetName
    if (!selectedNode.exchange && selectedNode.assetClass !== 'Cryptocurrency' && !isBondWithName) return
    fetchPrice(
      selectedNode.exchange ?? '',
      selectedNode.ticker,
      selectedNode.assetClass,
      selectedNode.brokerName,
      isBondWithName ? selectedNode.assetName : undefined,
    )
  }, [isAsset, selectedNode, fetchPrice])

  useEffect(() => {
    if (!state.asset) {
      dispatch({ type: 'XIRR_RESET' })
      return
    }

    if (scope === 'active' && !state.price) {
      dispatch({ type: 'XIRR_RESET' })
      return
    }

    // Historic positions are fully closed: every buy/sell/credit is already a dated entry
    // in the cash flow series, so there is no remaining position to mark-to-market — the
    // terminal value is 0 rather than a live price.
    const currentValue =
      scope === 'active' && state.price ? state.price.price * state.asset.quantity : 0
    const currentValueWithCredits = scope === 'active' ? currentValue + state.asset.totalCredits : currentValue
    let cancelled = false

    void Promise.all([
      apiClient.calculateXirr(state.asset.cashFlowsWithoutCredits, currentValue),
      apiClient.calculateXirr(state.asset.cashFlowsWithCredits, currentValueWithCredits),
    ])
      .then(([withoutCredits, withCredits]) => {
        if (cancelled) return
        dispatch({ type: 'XIRR_FETCH_SUCCESS', xirr: withoutCredits.xirr, xirrWithCredits: withCredits.xirr })
      })
      .catch(() => {
        if (!cancelled) dispatch({ type: 'XIRR_RESET' })
      })

    return () => {
      cancelled = true
    }
  }, [state.asset, state.price, scope, apiClient])

  const canRefresh = !state.isLoadingPrice

  const showCurrentSection =
    !!state.asset && state.asset.quantity !== 0 && state.asset.averagePrice !== 0

  const totalCurrentValue =
    state.price && state.asset ? state.price.price * state.asset.quantity : 0

  const costBasis = state.asset ? state.asset.quantity * state.asset.averagePrice : 0

  const resultPercent =
    costBasis !== 0 ? (totalCurrentValue - costBasis) / costBasis : 0

  const totalCurrentPlusCredits = state.asset ? totalCurrentValue + state.asset.totalCredits : 0

  const resultWithCreditsPercent =
    costBasis !== 0 ? (totalCurrentPlusCredits - costBasis) / costBasis : 0

  return {
    asset: state.asset,
    isLoadingAsset: state.isLoadingAsset,
    assetError: state.assetError,
    retryAsset,
    price: state.price,
    isLoadingPrice: state.isLoadingPrice,
    priceError: state.priceError,
    canRefresh,
    refresh,
    showCurrentSection,
    totalCurrentValue,
    resultPercent,
    totalCurrentPlusCredits,
    resultWithCreditsPercent,
    xirr: state.xirr,
    xirrWithCredits: state.xirrWithCredits,
    portfolioWeight: state.portfolioWeight,
  }
}
