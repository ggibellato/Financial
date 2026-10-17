import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { CalendarConnectionStatusDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'

interface CalendarConnectionState {
  isConnecting: boolean
  isDisconnecting: boolean
  disconnectError: string | null
}

type CalendarConnectionAction =
  | { type: 'CONNECT_START' }
  | { type: 'CONNECT_COMPLETE' }
  | { type: 'DISCONNECT_START' }
  | { type: 'DISCONNECT_SUCCESS' }
  | { type: 'DISCONNECT_ERROR'; payload: string }

const INITIAL_STATE: CalendarConnectionState = {
  isConnecting: false,
  isDisconnecting: false,
  disconnectError: null,
}

function reducer(state: CalendarConnectionState, action: CalendarConnectionAction): CalendarConnectionState {
  switch (action.type) {
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

  const {
    data: status,
    isLoading,
    error,
    retry,
    refresh,
    setData,
  } = useAsyncResource<CalendarConnectionStatusDto>(
    () => apiClient.getCalendarStatus(),
    [],
    'Unable to load calendar connection status',
    { initialLoading: true, keepDataWhileLoading: true },
  )

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
        .then((latestStatus) => {
          setData(() => latestStatus)
          if (latestStatus.connected || latestStatus.disconnectReason) {
            dispatch({ type: 'CONNECT_COMPLETE' })
          }
        })
        .catch(() => {
          // A transient failure during the focus re-check is retried on the next focus event.
        })
    }

    window.addEventListener('focus', onFocus)
    return () => window.removeEventListener('focus', onFocus)
  }, [state.isConnecting, setData])

  const disconnect = useCallback(async () => {
    dispatch({ type: 'DISCONNECT_START' })

    try {
      await apiClient.disconnectCalendar()
      dispatch({ type: 'DISCONNECT_SUCCESS' })
      await refresh()
    } catch (err: unknown) {
      dispatch({ type: 'DISCONNECT_ERROR', payload: getErrorMessage(err, 'Failed to disconnect Google Calendar') })
      throw err
    }
  }, [refresh])

  return {
    status,
    isLoading,
    error,
    retry,
    isConnecting: state.isConnecting,
    connect,
    isDisconnecting: state.isDisconnecting,
    disconnectError: state.disconnectError,
    disconnect,
  }
}
