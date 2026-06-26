import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { AggregatedSummaryDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'

interface AggregatedSummaryState {
  summary: AggregatedSummaryDto | null
  isLoading: boolean
  error: string | null
  retryCount: number
}

type AggregatedSummaryAction =
  | { type: 'RESET' }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: AggregatedSummaryDto }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }

const INITIAL_STATE: AggregatedSummaryState = {
  summary: null,
  isLoading: false,
  error: null,
  retryCount: 0,
}

function reducer(state: AggregatedSummaryState, action: AggregatedSummaryAction): AggregatedSummaryState {
  switch (action.type) {
    case 'RESET':
      return INITIAL_STATE
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null, summary: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, summary: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    default:
      return state
  }
}

export interface AggregatedSummaryData {
  summary: AggregatedSummaryDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useAggregatedSummary(): AggregatedSummaryData {
  const { selectedNode } = useSelectedNode()
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const isBroker = selectedNode?.nodeType === 'Broker'
  const isPortfolio = selectedNode?.nodeType === 'Portfolio'
  const shouldFetch = isBroker || isPortfolio

  useEffect(() => {
    if (!shouldFetch || !selectedNode) {
      dispatch({ type: 'RESET' })
      return
    }

    dispatch({ type: 'FETCH_START' })

    const fetchPromise = isBroker
      ? apiClient.getSummaryByBroker(selectedNode.brokerName)
      : apiClient.getSummaryByPortfolio(selectedNode.brokerName, selectedNode.portfolioName!)

    void fetchPromise
      .then((result) => dispatch({ type: 'FETCH_SUCCESS', payload: result }))
      .catch((err: unknown) => {
        dispatch({
          type: 'FETCH_ERROR',
          payload: err instanceof Error ? err.message : 'Unable to load summary',
        })
      })
  }, [selectedNode, shouldFetch, isBroker, apiClient, state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  return {
    summary: state.summary,
    isLoading: state.isLoading,
    error: state.error,
    retry,
  }
}
