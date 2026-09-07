import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { CalendarConnectionStatusDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

interface CalendarConnectionState {
  status: CalendarConnectionStatusDto | null
  isLoading: boolean
  error: string | null
  retryCount: number
  isConnecting: boolean
  isDisconnecting: boolean
  disconnectError: string | null
}

type CalendarConnectionAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: CalendarConnectionStatusDto }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'CONNECT_START' }
  | { type: 'CONNECT_COMPLETE' }
  | { type: 'DISCONNECT_START' }
  | { type: 'DISCONNECT_SUCCESS' }
  | { type: 'DISCONNECT_ERROR'; payload: string }

const INITIAL_STATE: CalendarConnectionState = {
  status: null,
  isLoading: true,
  error: null,
  retryCount: 0,
  isConnecting: false,
  isDisconnecting: false,
  disconnectError: null,
}

function reducer(state: CalendarConnectionState, action: CalendarConnectionAction): CalendarConnectionState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, status: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'CONNECT_START':
      return { ...state, isConnecting: true }
    case 'CONNECT_COMPLETE':
      return { ...state, isConnecting: false }
    case 'DISCONNECT_START':
      return { ...state, isDisconnecting: true, disconnectError: null }
    case 'DISCONNECT_SUCCESS':
      return { ...state, isDisconnecting: false }
    case 'DISCONNECT_ERROR':
      return { ...state, isDisconnecting: false, disconnectError: action.payload }
    default:
      return state
  }
}

export interface CalendarConnectionData {
  status: CalendarConnectionStatusDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
  isConnecting: boolean
  connect: () => void
  isDisconnecting: boolean
  disconnectError: string | null
  disconnect: () => Promise<void>
}

export function useCalendarConnection(): CalendarConnectionData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const fetchStatus = useCallback(() => {
    return apiClient
      .getCalendarStatus()
      .then((status) => dispatch({ type: 'FETCH_SUCCESS', payload: status }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load calendar connection status') })
      })
  }, [])

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void fetchStatus()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const connect = useCallback(() => {
    window.open(apiClient.buildCalendarConnectUrl(), '_blank', 'noopener,noreferrer')
    dispatch({ type: 'CONNECT_START' })
  }, [])

  // While connecting, re-check status every time the app tab regains focus - the OAuth
  // consent flow completes in a separate tab (see spec.md Technical Decisions), so there is
  // no in-app navigation event to hook into.
  useEffect(() => {
    if (!state.isConnecting) return

    const onFocus = () => {
      void apiClient
        .getCalendarStatus()
        .then((status) => {
          dispatch({ type: 'FETCH_SUCCESS', payload: status })
          if (status.connected || status.disconnectReason) {
            dispatch({ type: 'CONNECT_COMPLETE' })
          }
        })
        .catch(() => {
          // A transient failure during the focus re-check is retried on the next focus event.
        })
    }

    window.addEventListener('focus', onFocus)
    return () => window.removeEventListener('focus', onFocus)
  }, [state.isConnecting])

  const disconnect = useCallback(async () => {
    dispatch({ type: 'DISCONNECT_START' })

    try {
      await apiClient.disconnectCalendar()
      dispatch({ type: 'DISCONNECT_SUCCESS' })
      await fetchStatus()
    } catch (err: unknown) {
      dispatch({ type: 'DISCONNECT_ERROR', payload: getErrorMessage(err, 'Failed to disconnect Google Calendar') })
      throw err
    }
  }, [fetchStatus])

  return {
    status: state.status,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    isConnecting: state.isConnecting,
    connect,
    isDisconnecting: state.isDisconnecting,
    disconnectError: state.disconnectError,
    disconnect,
  }
}
