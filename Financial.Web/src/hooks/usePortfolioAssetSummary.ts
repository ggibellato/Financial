import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AssetCashFlowDto, PortfolioAssetSummaryItemDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { getErrorMessage } from '../utils/formatters'

export interface RowPriceState {
  isLoading: boolean
  currentPrice: number | null
  fetchFailed: boolean
  isManual: boolean
  /** Annualised rate as a fraction, from POST /xirr/calculate. Null once resolved with no rate. */
  xirr: number | null
  isLoadingXirr: boolean
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
  | { type: 'ROW_XIRR_SETTLED'; index: number; xirr: number | null }

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
        xirr: null,
        isLoadingXirr: true,
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
    case 'ROW_XIRR_SETTLED': {
      const rowPrices = state.rowPrices.map((row, i) =>
        i === action.index ? { ...row, xirr: action.xirr, isLoadingXirr: false } : row,
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

    // One rate per row, from the same solver the asset tab uses. The row is not held back
    // waiting for its neighbours: each rate is requested as soon as that row's price lands.
    const settleXirr = (cashFlows: AssetCashFlowDto[], terminalValue: number, index: number) =>
      Promise.resolve()
        .then(() => apiClient.calculateXirr(cashFlows, terminalValue))
        .then((result) => dispatch({ type: 'ROW_XIRR_SETTLED', index, xirr: result.xirr }))
        .catch(() => dispatch({ type: 'ROW_XIRR_SETTLED', index, xirr: null }))

    void apiClient
      .getPortfolioAssetsSummary(brokerName, portfolioName, scope)
      .then((items) => {
        dispatch({ type: 'FETCH_SUCCESS', payload: items })
        if (scope === 'historic') {
          // Historic positions are closed — there's no live price to fetch, so rows resolve
          // immediately rather than sitting in a perpetual loading state. Every buy, sell and
          // credit is already a dated entry, so the rate uses a terminal value of 0.
          items.forEach((item, index) => {
            dispatch({ type: 'ROW_PRICE_ERROR', index })
            void settleXirr(item.cashFlows, 0, index)
          })
          return
        }
        items.forEach((item, index) => {
          void apiClient
            .getCurrentPrice(item.exchange, item.ticker, item.class, brokerName, item.assetName, portfolioName, item.assetName)
            // Two-argument then, not then().catch(): the rejection handler must see only a price
            // failure. Chaining a catch would also swallow a fault from the rate request below
            // and blank out a price that had already arrived.
            .then(
              (priceDto) => {
                dispatch({ type: 'ROW_PRICE_SUCCESS', index, currentPrice: priceDto.price, isManual: priceDto.isManual })
                return priceDto.price * item.currentQuantity
              },
              () => {
                dispatch({ type: 'ROW_PRICE_ERROR', index })
                // No price means no terminal value, so there is no rate to ask for.
                dispatch({ type: 'ROW_XIRR_SETTLED', index, xirr: null })
                return null
              },
            )
            .then((terminalValue) => (terminalValue === null ? undefined : settleXirr(item.cashFlows, terminalValue, index)))
        })
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
