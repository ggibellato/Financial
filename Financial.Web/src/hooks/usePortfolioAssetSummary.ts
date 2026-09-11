import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { PortfolioAssetSummaryItemDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { getErrorMessage } from '../utils/formatters'

export interface RowPriceState {
  isLoading: boolean
  currentPrice: number | null
  fetchFailed: boolean
  isManual: boolean
}

interface PortfolioAssetSummaryState {
  items: PortfolioAssetSummaryItemDto[] | null
  rowPrices: RowPriceState[]
  isLoading: boolean
  error: string | null
  retryCount: number
}

type PortfolioAssetSummaryAction =
  | { type: 'RESET' }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: PortfolioAssetSummaryItemDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'ROW_PRICE_SUCCESS'; index: number; currentPrice: number; isManual: boolean }
  | { type: 'ROW_PRICE_ERROR'; index: number }
  | { type: 'VALUATION_REFRESHED'; payload: PortfolioAssetSummaryItemDto[] }

const INITIAL_STATE: PortfolioAssetSummaryState = {
  items: null,
  rowPrices: [],
  isLoading: false,
  error: null,
  retryCount: 0,
}

function reducer(
  state: PortfolioAssetSummaryState,
  action: PortfolioAssetSummaryAction,
): PortfolioAssetSummaryState {
  switch (action.type) {
    case 'RESET':
      return INITIAL_STATE
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null, items: null, rowPrices: [] }
    case 'FETCH_SUCCESS': {
      const rowPrices: RowPriceState[] = action.payload.map(() => ({
        isLoading: true,
        currentPrice: null,
        fetchFailed: false,
        isManual: false,
      }))
      return { ...state, isLoading: false, items: action.payload, rowPrices }
    }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'ROW_PRICE_SUCCESS': {
      const rowPrices = state.rowPrices.map((row, i) =>
        i === action.index
          ? { ...row, isLoading: false, currentPrice: action.currentPrice, fetchFailed: false, isManual: action.isManual }
          : row,
      )
      return { ...state, rowPrices }
    }
    case 'ROW_PRICE_ERROR': {
      const rowPrices = state.rowPrices.map((row, i) =>
        i === action.index ? { ...row, isLoading: false, currentPrice: null, fetchFailed: true, isManual: false } : row,
      )
      return { ...state, rowPrices }
    }
    case 'VALUATION_REFRESHED':
      return { ...state, items: action.payload }
    default:
      return state
  }
}

export interface PortfolioAssetSummaryData {
  items: PortfolioAssetSummaryItemDto[] | null
  rowPrices: RowPriceState[]
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function usePortfolioAssetSummary(): PortfolioAssetSummaryData {
  const { selectedNode, scope } = useSelectedNode()
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const isPortfolio = selectedNode?.nodeType === 'Portfolio'

  useEffect(() => {
    if (!isPortfolio || !selectedNode) {
      dispatch({ type: 'RESET' })
      return
    }

    const { brokerName, portfolioName } = selectedNode
    if (!portfolioName) {
      dispatch({ type: 'RESET' })
      return
    }

    dispatch({ type: 'FETCH_START' })

    void apiClient
      .getPortfolioAssetsSummary(brokerName, portfolioName, scope)
      .then((items) => {
        dispatch({ type: 'FETCH_SUCCESS', payload: items })
        if (scope === 'historic') {
          items.forEach((_, index) => dispatch({ type: 'ROW_PRICE_ERROR', index }))
          return
        }

        const rowFetches = items.map((item, index) =>
          apiClient
            .getCurrentPrice(item.exchange, item.ticker, item.class, brokerName, item.assetName, portfolioName, item.assetName)
            .then(
              (priceDto) => {
                dispatch({ type: 'ROW_PRICE_SUCCESS', index, currentPrice: priceDto.price, isManual: priceDto.isManual })
              },
              () => {
                dispatch({ type: 'ROW_PRICE_ERROR', index })
              },
            ),
        )

        // Each fetch also records the price server-side, so once every row has settled the
        // grid re-reads the server-computed valuation rather than deriving it here.
        void Promise.allSettled(rowFetches).then(() =>
          apiClient
            .getPortfolioAssetsSummary(brokerName, portfolioName, scope)
            .then((refreshed) => dispatch({ type: 'VALUATION_REFRESHED', payload: refreshed }))
            .catch(() => undefined),
        )
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'FETCH_ERROR',
          payload: getErrorMessage(err, 'Unable to load portfolio assets'),
        })
      })
  }, [selectedNode, isPortfolio, scope, state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  return {
    items: state.items,
    rowPrices: state.rowPrices,
    isLoading: state.isLoading,
    error: state.error,
    retry,
  }
}
