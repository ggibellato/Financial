import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { InvestmentSnapshotDto } from '../api/types'
import { currentYearMonth, formatMonthInputValue, parseMonthInputValue } from '../utils/formatters'

interface InvestmentSnapshotsState {
  year: number
  month: number
  snapshots: InvestmentSnapshotDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  editingId: string | null
  editValue: string
  isSaving: boolean
  saveError: string | null
}

type InvestmentSnapshotsAction =
  | { type: 'SET_MONTH'; payload: { year: number; month: number } }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: InvestmentSnapshotDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SHOW_EDIT_FORM'; payload: InvestmentSnapshotDto }
  | { type: 'CANCEL_EDIT' }
  | { type: 'SET_EDIT_VALUE'; payload: string }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS' }
  | { type: 'SAVE_ERROR'; payload: string }

const { year: DEFAULT_YEAR, month: DEFAULT_MONTH } = currentYearMonth()

const INITIAL_STATE: InvestmentSnapshotsState = {
  year: DEFAULT_YEAR,
  month: DEFAULT_MONTH,
  snapshots: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  editingId: null,
  editValue: '',
  isSaving: false,
  saveError: null,
}

function reducer(state: InvestmentSnapshotsState, action: InvestmentSnapshotsAction): InvestmentSnapshotsState {
  switch (action.type) {
    case 'SET_MONTH':
      return { ...state, year: action.payload.year, month: action.payload.month }
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, snapshots: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'SHOW_EDIT_FORM':
      return { ...state, editingId: action.payload.id, editValue: String(action.payload.value), saveError: null }
    case 'CANCEL_EDIT':
      return { ...state, editingId: null, editValue: '', saveError: null }
    case 'SET_EDIT_VALUE':
      return { ...state, editValue: action.payload }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null }
    case 'SAVE_SUCCESS':
      return { ...state, isSaving: false, editingId: null, editValue: '' }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload }
    default:
      return state
  }
}

export interface InvestmentSnapshotsData {
  monthInputValue: string
  setMonthInputValue: (value: string) => void
  snapshots: InvestmentSnapshotDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  editingId: string | null
  editValue: string
  isSaving: boolean
  saveError: string | null
  setEditValue: (value: string) => void
  showEditForm: (snapshot: InvestmentSnapshotDto) => void
  cancelEdit: () => void
  saveEdit: () => void
}

export function useInvestmentSnapshots(): InvestmentSnapshotsData {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getInvestmentSnapshots(state.year, state.month)
      .then((snapshots) => dispatch({ type: 'FETCH_SUCCESS', payload: snapshots }))
      .catch((err: unknown) => {
        dispatch({
          type: 'FETCH_ERROR',
          payload: err instanceof Error ? err.message : 'Unable to load investment snapshots',
        })
      })
  }, [apiClient, state.year, state.month, state.retryCount])

  const monthInputValue = formatMonthInputValue(state.year, state.month)

  const setMonthInputValue = useCallback((value: string) => {
    const parsed = parseMonthInputValue(value)
    if (!parsed) return
    dispatch({ type: 'SET_MONTH', payload: parsed })
  }, [])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const setEditValue = useCallback((value: string) => dispatch({ type: 'SET_EDIT_VALUE', payload: value }), [])

  const showEditForm = useCallback(
    (snapshot: InvestmentSnapshotDto) => dispatch({ type: 'SHOW_EDIT_FORM', payload: snapshot }),
    [],
  )

  const cancelEdit = useCallback(() => dispatch({ type: 'CANCEL_EDIT' }), [])

  function saveEdit() {
    if (!state.editingId) return

    const value = Number(state.editValue)
    if (!state.editValue.trim() || !isFinite(value) || value < 0) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Value must be a non-negative number' })
      return
    }

    dispatch({ type: 'SAVE_START' })

    void apiClient
      .updateInvestmentSnapshotValue(state.editingId, { value })
      .then(() => {
        dispatch({ type: 'SAVE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to update snapshot',
        })
      })
  }

  return {
    monthInputValue,
    setMonthInputValue,
    snapshots: state.snapshots,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    editingId: state.editingId,
    editValue: state.editValue,
    isSaving: state.isSaving,
    saveError: state.saveError,
    setEditValue,
    showEditForm,
    cancelEdit,
    saveEdit,
  }
}
