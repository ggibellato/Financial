import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { CreditCardCalendarSyncStatusDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

export interface CalendarSyncStatusRow {
  creditCardId: string
  name: string
  dueDate: string
  state: CreditCardCalendarSyncStatusDto['state']
  lastSuccessfulSyncUtc: string | null
  lastError: string | null
}

interface CalendarSyncStatusesState {
  rows: CalendarSyncStatusRow[]
  isLoading: boolean
  error: string | null
  retryCount: number
  retryingCardId: string | null
  retryError: string | null
}

type CalendarSyncStatusesAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: CalendarSyncStatusRow[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'RESYNC_START'; payload: string }
  | { type: 'RESYNC_SUCCESS'; payload: CreditCardCalendarSyncStatusDto }
  | { type: 'RESYNC_ERROR'; payload: string }

const INITIAL_STATE: CalendarSyncStatusesState = {
  rows: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  retryingCardId: null,
  retryError: null,
}

function mergeStatus(row: CalendarSyncStatusRow, status: CreditCardCalendarSyncStatusDto): CalendarSyncStatusRow {
  return { ...row, state: status.state, lastSuccessfulSyncUtc: status.lastSuccessfulSyncUtc, lastError: status.lastError }
}

function reducer(state: CalendarSyncStatusesState, action: CalendarSyncStatusesAction): CalendarSyncStatusesState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, rows: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'RESYNC_START':
      return { ...state, retryingCardId: action.payload, retryError: null }
    case 'RESYNC_SUCCESS':
      return {
        ...state,
        retryingCardId: null,
        rows: state.rows.map((row) => (row.creditCardId === action.payload.creditCardId ? mergeStatus(row, action.payload) : row)),
      }
    case 'RESYNC_ERROR':
      return { ...state, retryingCardId: null, retryError: action.payload }
    default:
      return state
  }
}

export interface CalendarSyncStatusesData {
  rows: CalendarSyncStatusRow[]
  isLoading: boolean
  error: string | null
  retry: () => void
  retryingCardId: string | null
  retryError: string | null
  resyncCard: (id: string) => void
}

export function useCalendarSyncStatuses(): CalendarSyncStatusesData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const fetchRows = useCallback(() => {
    return Promise.all([apiClient.getCreditCards(), apiClient.getCalendarSyncStatuses()])
      .then(([creditCards, statuses]) => {
        const statusByCardId = new Map(statuses.map((status) => [status.creditCardId, status]))
        const rows = creditCards
          .filter((card) => card.isActive && card.nextInvoiceDueDate != null)
          .map((card): CalendarSyncStatusRow => {
            const status = statusByCardId.get(card.id)
            return {
              creditCardId: card.id,
              name: card.name,
              dueDate: card.nextInvoiceDueDate as string,
              state: status?.state ?? 'Pending',
              lastSuccessfulSyncUtc: status?.lastSuccessfulSyncUtc ?? null,
              lastError: status?.lastError ?? null,
            }
          })
        dispatch({ type: 'FETCH_SUCCESS', payload: rows })
      })
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load the calendar sync status list') })
      })
  }, [])

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void fetchRows()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const resyncCard = useCallback((id: string) => {
    dispatch({ type: 'RESYNC_START', payload: id })

    void apiClient
      .resyncCreditCardCalendar(id)
      .then((status) => dispatch({ type: 'RESYNC_SUCCESS', payload: status }))
      .catch((err: unknown) => {
        dispatch({ type: 'RESYNC_ERROR', payload: getErrorMessage(err, 'Failed to retry the calendar sync') })
      })
  }, [])

  return {
    rows: state.rows,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    retryingCardId: state.retryingCardId,
    retryError: state.retryError,
    resyncCard,
  }
}
