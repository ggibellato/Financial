import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { PortfolioBreakdownItemDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'

interface BrokerBreakdownState {
  breakdown: PortfolioBreakdownItemDto[] | null
  isLoading: boolean
  error: string | null
  retryCount: number
}

type BrokerBreakdownAction =
  | { type: 'RESET' }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: PortfolioBreakdownItemDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }

const INITIAL_STATE: BrokerBreakdownState = {
  breakdown: null,
  isLoading: false,
  error: null,
  retryCount: 0,
}

function reducer(state: BrokerBreakdownState, action: BrokerBreakdownAction): BrokerBreakdownState {
  switch (action.type) {
    case 'RESET':
      return INITIAL_STATE
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null, breakdown: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, breakdown: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    default:
      return state
  }
}

export interface BrokerBreakdownData {
  breakdown: PortfolioBreakdownItemDto[] | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useBrokerBreakdown(): BrokerBreakdownData {
  const { selectedNode, scope } = useSelectedNode()
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const isBroker = selectedNode?.nodeType === 'Broker'

  useEffect(() => {
    if (!isBroker || !selectedNode) {
      dispatch({ type: 'RESET' })
      return
    }

    dispatch({ type: 'FETCH_START' })

    void apiClient
      .getBrokerBreakdown(selectedNode.brokerName, scope)
      .then((result) => dispatch({ type: 'FETCH_SUCCESS', payload: result }))
      .catch((err: unknown) => {
        dispatch({
          type: 'FETCH_ERROR',
          payload: err instanceof Error ? err.message : 'Unable to load breakdown',
        })
      })
  }, [selectedNode, isBroker, apiClient, scope, state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  return {
    breakdown: state.breakdown,
    isLoading: state.isLoading,
    error: state.error,
    retry,
  }
}
