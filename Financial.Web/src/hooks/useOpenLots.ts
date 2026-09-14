import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { OpenLotDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { getErrorMessage } from '../utils/formatters'

interface OpenLotsState {
  openLots: OpenLotDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
}

type OpenLotsAction =
  | { type: 'RESET' }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: OpenLotDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }

const INITIAL_STATE: OpenLotsState = { openLots: [], isLoading: false, error: null, retryCount: 0 }

function reducer(state: OpenLotsState, action: OpenLotsAction): OpenLotsState {
  switch (action.type) {
    case 'RESET':
      return { ...INITIAL_STATE, retryCount: state.retryCount }
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, openLots: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1, error: null }
    default:
      return state
  }
}

export interface OpenLotsData {
  openLots: OpenLotDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
}

/** Fetches open lots only while `enabled` (a SpecificId sale is being entered) - never on every
 * asset-details load, since AverageCost/FIFO brokers never need this data. */
export function useOpenLots(enabled: boolean): OpenLotsData {
  const { selectedNode, scope } = useSelectedNode()
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    if (!enabled || !selectedNode || selectedNode.nodeType !== 'Asset' || !selectedNode.portfolioName || !selectedNode.assetName) {
      dispatch({ type: 'RESET' })
      return
    }

    dispatch({ type: 'FETCH_START' })

    void apiClient
      .getOpenLots(selectedNode.brokerName, selectedNode.portfolioName, selectedNode.assetName, scope)
      .then((result) => dispatch({ type: 'FETCH_SUCCESS', payload: result }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load open lots') })
      })
  }, [enabled, selectedNode, scope, state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  return { openLots: state.openLots, isLoading: state.isLoading, error: state.error, retry }
}
