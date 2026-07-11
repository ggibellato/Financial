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

const INITIAL_STATE: SummaryState = {
  asset: null,
  isLoadingAsset: false,
  assetError: null,
  assetRetryCount: 0,
  price: null,
  isLoadingPrice: false,
  priceError: null,
}

function reducer(state: SummaryState, action: SummaryAction): SummaryState {
  switch (action.type) {
    case 'RESET':
      return INITIAL_STATE
    case 'ASSET_FETCH_START':
      return { ...state, isLoadingAsset: true, assetError: null, asset: null, price: null, priceError: null }
    case 'ASSET_FETCH_SUCCESS':
      return { ...state, isLoadingAsset: false, asset: action.payload }
    case 'ASSET_FETCH_ERROR':
      return { ...state, isLoadingAsset: false, assetError: action.payload }
    case 'ASSET_RETRY':
      return { ...state, assetRetryCount: state.assetRetryCount + 1 }
    case 'PRICE_FETCH_START':
      return { ...state, isLoadingPrice: true, priceError: null, price: null }
    case 'PRICE_FETCH_SUCCESS':
      return { ...state, isLoadingPrice: false, price: action.payload }
    case 'PRICE_FETCH_ERROR':
      return { ...state, isLoadingPrice: false, priceError: action.payload, price: null }
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
}

export function useAssetSummary(): AssetSummaryData {
  const { selectedNode } = useSelectedNode()
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const isAsset =
    selectedNode?.nodeType === 'Asset' &&
    !!selectedNode.portfolioName &&
    !!selectedNode.assetName

  const fetchPrice = useCallback(
    (exchange: string, ticker: string, assetClass?: string, brokerName?: string) => {
      dispatch({ type: 'PRICE_FETCH_START' })
      void apiClient
        .getCurrentPrice(exchange, ticker, assetClass, brokerName)
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

    if (ticker && (exchange || assetClass === 'Cryptocurrency')) {
      fetchPrice(exchange ?? '', ticker, assetClass, brokerName)
    }

    void apiClient
      .getAssetDetails(brokerName, portfolioName, assetName)
      .then((result) => dispatch({ type: 'ASSET_FETCH_SUCCESS', payload: result }))
      .catch((err: unknown) => {
        dispatch({
          type: 'ASSET_FETCH_ERROR',
          payload: err instanceof Error ? err.message : 'Unable to load asset details',
        })
      })
  }, [selectedNode, isAsset, apiClient, fetchPrice, state.assetRetryCount])

  const retryAsset = useCallback(() => dispatch({ type: 'ASSET_RETRY' }), [])

  const refresh = useCallback(() => {
    if (!isAsset || !selectedNode?.ticker) return
    if (!selectedNode.exchange && selectedNode.assetClass !== 'Cryptocurrency') return
    fetchPrice(selectedNode.exchange ?? '', selectedNode.ticker, selectedNode.assetClass, selectedNode.brokerName)
  }, [isAsset, selectedNode, fetchPrice])

  const canRefresh = !state.isLoadingPrice

  const showCurrentSection =
    !!state.asset && state.asset.quantity !== 0 && state.asset.averagePrice !== 0

  const totalCurrentValue =
    state.price && state.asset ? state.price.price * state.asset.quantity : 0

  const resultPercent =
    state.asset && state.asset.totalBought !== 0
      ? (totalCurrentValue - state.asset.totalBought) / state.asset.totalBought
      : 0

  const totalCurrentPlusCredits = state.asset ? totalCurrentValue + state.asset.totalCredits : 0

  const resultWithCreditsPercent =
    state.asset && state.asset.totalBought !== 0
      ? (totalCurrentPlusCredits - state.asset.totalBought) / state.asset.totalBought
      : 0

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
  }
}
