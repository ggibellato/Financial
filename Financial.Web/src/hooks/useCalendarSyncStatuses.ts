import { useCallback, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { CreditCardCalendarSyncStatusDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'

export interface CalendarSyncStatusRow {
  creditCardId: string
  name: string
  dueDate: string
  state: CreditCardCalendarSyncStatusDto['state']
  lastSuccessfulSyncUtc: string | null
  lastError: string | null
}

interface ResyncState {
  retryingCardId: string | null
  retryError: string | null
}

type ResyncAction =
  | { type: 'RESYNC_START'; payload: string }
  | { type: 'RESYNC_SUCCESS' }
  | { type: 'RESYNC_ERROR'; payload: string }

const INITIAL_RESYNC_STATE: ResyncState = {
  retryingCardId: null,
  retryError: null,
}

const NO_ROWS: CalendarSyncStatusRow[] = []

function mergeStatus(row: CalendarSyncStatusRow, status: CreditCardCalendarSyncStatusDto): CalendarSyncStatusRow {
  return { ...row, state: status.state, lastSuccessfulSyncUtc: status.lastSuccessfulSyncUtc, lastError: status.lastError }
}

function resyncReducer(state: ResyncState, action: ResyncAction): ResyncState {
  switch (action.type) {
    case 'RESYNC_START':
      return { ...state, retryingCardId: action.payload, retryError: null }
    case 'RESYNC_SUCCESS':
      return { ...state, retryingCardId: null }
    case 'RESYNC_ERROR':
      return { ...state, retryingCardId: null, retryError: action.payload }
    default:
      return state
  }
}

function fetchRows(): Promise<CalendarSyncStatusRow[]> {
  return Promise.all([apiClient.getCreditCards(), apiClient.getCalendarSyncStatuses()]).then(([creditCards, statuses]) => {
    const statusByCardId = new Map(statuses.map((status) => [status.creditCardId, status]))
    return creditCards
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
  })
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
  const [state, dispatch] = useReducer(resyncReducer, INITIAL_RESYNC_STATE)

  const { data, isLoading, error, retry, setData } = useAsyncResource<CalendarSyncStatusRow[]>(
    fetchRows,
    [],
    'Unable to load the calendar sync status list',
    { initialLoading: true, keepDataWhileLoading: true },
  )

  const resyncCard = useCallback(
    (id: string) => {
      dispatch({ type: 'RESYNC_START', payload: id })

      void apiClient
        .resyncCreditCardCalendar(id)
        .then((status) => {
          setData((rows) => (rows ?? NO_ROWS).map((row) => (row.creditCardId === status.creditCardId ? mergeStatus(row, status) : row)))
          dispatch({ type: 'RESYNC_SUCCESS' })
        })
        .catch((err: unknown) => {
          dispatch({ type: 'RESYNC_ERROR', payload: getErrorMessage(err, 'Failed to retry the calendar sync') })
        })
    },
    [setData],
  )

  return {
    rows: data ?? NO_ROWS,
    isLoading,
    error,
    retry,
    retryingCardId: state.retryingCardId,
    retryError: state.retryError,
    resyncCard,
  }
}
