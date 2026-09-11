import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AssetDetailsDto, AssetPriceDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { getErrorMessage } from '../utils/formatters'

interface SummaryState {
  asset: AssetDetailsDto | null
  isLoadingAsset: boolean
  assetError: string | null
  assetRetryCount: number
  price: AssetPriceDto | null
  isLoadingPrice: boolean
  priceError: string | null
  portfolioWeight: number | null
}

type SummaryAction =
  | { type: 'RESET' }
  | { type: 'ASSET_FETCH_START' }
  | { type: 'ASSET_FETCH_SUCCESS'; payload: AssetDetailsDto }
  | { type: 'ASSET_FETCH_ERROR'; payload: string }
  | { type: 'ASSET_RETRY' }
  | { type: 'ASSET_REFRESHED'; payload: AssetDetailsDto }
  | { type: 'PRICE_FETCH_START' }
  | { type: 'PRICE_FETCH_SUCCESS'; payload: AssetPriceDto }
  | { type: 'PRICE_FETCH_ERROR'; payload: string }
  | { type: 'PORTFOLIO_WEIGHT_SUCCESS'; portfolioWeight: number | null }

function resolvePriceFetchArgs(
  ticker: string | null | undefined,
  exchange: string | null | undefined,
  assetClass: string | undefined,
  assetName: string | null | undefined,
): { exchange: string; bondAssetName: string | undefined } | null {
  if (!ticker) return null
  const isBondWithName = assetClass === 'Bond' && !!assetName
  if (!exchange && assetClass !== 'Cryptocurrency' && !isBondWithName) return null
  return { exchange: exchange ?? '', bondAssetName: isBondWithName ? (assetName ?? undefined) : undefined }
}

const INITIAL_STATE: SummaryState = {
  asset: null,
  isLoadingAsset: false,
  assetError: null,
  assetRetryCount: 0,
  price: null,
  isLoadingPrice: false,
  priceError: null,
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
    case 'ASSET_REFRESHED':
      return { ...state, asset: action.payload }
    case 'PRICE_FETCH_START':
      return { ...state, isLoadingPrice: true, priceError: null, price: null }
    case 'PRICE_FETCH_SUCCESS':
      return { ...state, isLoadingPrice: false, price: action.payload }
    case 'PRICE_FETCH_ERROR':
      return { ...state, isLoadingPrice: false, priceError: action.payload, price: null }
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
  portfolioWeight: number | null
}

export function useAssetSummary(): AssetSummaryData {
  const { selectedNode, scope } = useSelectedNode()
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const isAsset =
    selectedNode?.nodeType === 'Asset' &&
    !!selectedNode.portfolioName &&
    !!selectedNode.assetName

  // Fetching a price also records it into the asset's price history server-side, so once it
  // settles the just-fetched market value/return figures are re-read from the server rather
  // than derived here - a fetch that fails leaves the prior valuation in place.
  const fetchPrice = useCallback(
    (
      exchange: string,
      ticker: string,
      assetClass: string | undefined,
      brokerName: string | undefined,
      name: string | undefined,
      portfolioName: string | undefined,
      assetName: string | undefined,
    ) => {
      dispatch({ type: 'PRICE_FETCH_START' })
      void apiClient
        .getCurrentPrice(exchange, ticker, assetClass, brokerName, name, portfolioName, assetName)
        .then((result) => {
          dispatch({ type: 'PRICE_FETCH_SUCCESS', payload: result })
          return brokerName && portfolioName && assetName
            ? apiClient.getAssetDetails(brokerName, portfolioName, assetName, 'active')
            : null
        })
        .then((refreshed) => {
          if (refreshed) dispatch({ type: 'ASSET_REFRESHED', payload: refreshed })
        })
        .catch((err: unknown) => {
          dispatch({
            type: 'PRICE_FETCH_ERROR',
            payload: getErrorMessage(err, 'Unable to fetch current price'),
          })
        })
    },
    [],
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

    const priceArgs = scope === 'active' ? resolvePriceFetchArgs(ticker, exchange, assetClass, assetName) : null
    if (priceArgs && ticker) {
      fetchPrice(priceArgs.exchange, ticker, assetClass, brokerName, priceArgs.bondAssetName, portfolioName, assetName)
    }

    void apiClient
      .getAssetDetails(brokerName, portfolioName, assetName, scope)
      .then((result) => dispatch({ type: 'ASSET_FETCH_SUCCESS', payload: result }))
      .catch((err: unknown) => {
        dispatch({
          type: 'ASSET_FETCH_ERROR',
          payload: getErrorMessage(err, 'Unable to load asset details'),
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
  }, [selectedNode, isAsset, fetchPrice, scope, state.assetRetryCount])

  const retryAsset = useCallback(() => dispatch({ type: 'ASSET_RETRY' }), [])

  const refresh = useCallback(() => {
    if (!isAsset || !selectedNode?.ticker) return
    const priceArgs = resolvePriceFetchArgs(
      selectedNode.ticker,
      selectedNode.exchange,
      selectedNode.assetClass,
      selectedNode.assetName,
    )
    if (!priceArgs) return
    fetchPrice(
      priceArgs.exchange,
      selectedNode.ticker,
      selectedNode.assetClass,
      selectedNode.brokerName,
      priceArgs.bondAssetName,
      selectedNode.portfolioName,
      selectedNode.assetName,
    )
  }, [isAsset, selectedNode, fetchPrice])

  const canRefresh = !state.isLoadingPrice

  const showCurrentSection =
    !!state.asset && state.asset.quantity !== 0 && state.asset.averagePrice !== 0

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
    portfolioWeight: state.portfolioWeight,
  }
}
