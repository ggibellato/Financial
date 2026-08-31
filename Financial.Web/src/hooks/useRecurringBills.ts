import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { RecurringBillCreateDto, RecurringBillDto, RecurringBillUpdateDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

interface RecurringBillsState {
  recurringBills: RecurringBillDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  deletingId: string | null
  deleteError: string | null
}

type RecurringBillsAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: RecurringBillDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }

const INITIAL_STATE: RecurringBillsState = {
  recurringBills: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  deletingId: null,
  deleteError: null,
}

function reducer(state: RecurringBillsState, action: RecurringBillsAction): RecurringBillsState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, recurringBills: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'DELETE_START':
      return { ...state, deletingId: action.payload, deleteError: null }
    case 'DELETE_SUCCESS':
      return { ...state, deletingId: null }
    case 'DELETE_ERROR':
      return { ...state, deletingId: null, deleteError: action.payload }
    default:
      return state
  }
}

export interface RecurringBillsData {
  recurringBills: RecurringBillDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createRecurringBill: (request: RecurringBillCreateDto) => Promise<RecurringBillDto>
  updateRecurringBill: (id: string, request: RecurringBillUpdateDto) => Promise<RecurringBillDto>
  deletingId: string | null
  deleteError: string | null
  deleteRecurringBill: (id: string) => void
}

export function useRecurringBills(): RecurringBillsData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getMensaisBills()
      .then((recurringBills) => dispatch({ type: 'FETCH_SUCCESS', payload: recurringBills }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load recurring bills') })
      })
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const createRecurringBill = useCallback(async (request: RecurringBillCreateDto) => {
    const created = await apiClient.createMensaisBill(request)
    dispatch({ type: 'RETRY' })
    return created
  }, [])

  const updateRecurringBill = useCallback(async (id: string, request: RecurringBillUpdateDto) => {
    const updated = await apiClient.updateMensaisBill(id, request)
    dispatch({ type: 'RETRY' })
    return updated
  }, [])

  const deleteRecurringBill = useCallback((id: string) => {
    dispatch({ type: 'DELETE_START', payload: id })

    void apiClient
      .deleteMensaisBill(id)
      .then(() => {
        dispatch({ type: 'DELETE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({ type: 'DELETE_ERROR', payload: getErrorMessage(err, 'Failed to delete recurring bill') })
      })
  }, [])

  return {
    recurringBills: state.recurringBills,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    createRecurringBill,
    updateRecurringBill,
    deletingId: state.deletingId,
    deleteError: state.deleteError,
    deleteRecurringBill,
  }
}
