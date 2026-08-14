import { useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { SyncStatusResponseDto } from '../api/types'

const POLL_INTERVAL_MS = 15000

interface SyncStatusState {
  status: SyncStatusResponseDto | null
}

type SyncStatusAction = { type: 'FETCH_SUCCESS'; payload: SyncStatusResponseDto }

const INITIAL_STATE: SyncStatusState = {
  status: null,
}

function reducer(state: SyncStatusState, action: SyncStatusAction): SyncStatusState {
  switch (action.type) {
    case 'FETCH_SUCCESS':
      return { ...state, status: action.payload }
    default:
      return state
  }
}

export interface SyncStatusData {
  status: SyncStatusResponseDto | null
}

export function useSyncStatus(): SyncStatusData {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    let cancelled = false

    const poll = () => {
      void apiClient
        .getSyncStatus()
        .then((result) => {
          if (!cancelled) {
            dispatch({ type: 'FETCH_SUCCESS', payload: result })
          }
        })
        .catch(() => {
          // A failed poll is retried on the next tick; the previous status is retained.
        })
    }

    poll()
    const intervalId = setInterval(poll, POLL_INTERVAL_MS)

    return () => {
      cancelled = true
      clearInterval(intervalId)
    }
  }, [apiClient])

  return { status: state.status }
}
