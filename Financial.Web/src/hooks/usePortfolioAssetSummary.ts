import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { PortfolioAssetSummaryItemDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'

export interface RowPriceState {
  isLoading: boolean
  currentPrice: number | null
  fetchFailed: boolean
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
  | { type: 'ROW_PRICE_SUCCESS'; index: number; currentPrice: number }
  | { type: 'ROW_PRICE_ERROR'; index: number }

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
          ? { isLoading: false, currentPrice: action.currentPrice, fetchFailed: false }
          : row,
      )
      return { ...state, rowPrices }
    }
    case 'ROW_PRICE_ERROR': {
      const rowPrices = state.rowPrices.map((row, i) =>
        i === action.index ? { isLoading: false, currentPrice: null, fetchFailed: true } : row,
      )
      return { ...state, rowPrices }
    }
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
  const apiClient = useMemo(() => createFinancialApiClient(), [])
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
        if (scope === 'historic') return
        items.forEach((item, index) => {
          void apiClient
            .getCurrentPrice(item.exchange, item.ticker, item.class, brokerName, item.assetName)
            .then((priceDto) => {
              dispatch({ type: 'ROW_PRICE_SUCCESS', index, currentPrice: priceDto.price })
            })
            .catch(() => {
              dispatch({ type: 'ROW_PRICE_ERROR', index })
            })
        })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'FETCH_ERROR',
          payload: err instanceof Error ? err.message : 'Unable to load portfolio assets',
        })
      })
  }, [selectedNode, isPortfolio, apiClient, scope, state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  return {
    items: state.items,
    rowPrices: state.rowPrices,
    isLoading: state.isLoading,
    error: state.error,
    retry,
  }
}
