import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { InvestmentSnapshotDto } from '../api/types'
import { currentYearMonth, getErrorMessage, parseValidatedNumber } from '../utils/formatters'
import { useMonthSelection } from './useMonthSelection'

export type SnapshotEditField = 'editValue'

interface InvestmentSnapshotsState {
  snapshots: InvestmentSnapshotDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  editingId: string | null
  editValue: string
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<SnapshotEditField, string>>
}

type InvestmentSnapshotsAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: InvestmentSnapshotDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SHOW_EDIT_FORM'; payload: InvestmentSnapshotDto }
  | { type: 'CANCEL_EDIT' }
  | { type: 'SET_EDIT_VALUE'; payload: string }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS' }
  | { type: 'SAVE_ERROR'; payload: { message: string | null; fields: Partial<Record<SnapshotEditField, string>> } }

const DEFAULT_MONTH = currentYearMonth()

const INITIAL_STATE: InvestmentSnapshotsState = {
  snapshots: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  editingId: null,
  editValue: '',
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
}

function reducer(state: InvestmentSnapshotsState, action: InvestmentSnapshotsAction): InvestmentSnapshotsState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, snapshots: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'SHOW_EDIT_FORM':
      return {
        ...state,
        editingId: action.payload.id,
        editValue: String(action.payload.value),
        saveError: null,
        saveErrorFields: {},
      }
    case 'CANCEL_EDIT':
      return { ...state, editingId: null, editValue: '', saveError: null, saveErrorFields: {} }
    case 'SET_EDIT_VALUE':
      return { ...state, editValue: action.payload }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null, saveErrorFields: {} }
    case 'SAVE_SUCCESS':
      return { ...state, isSaving: false, editingId: null, editValue: '' }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload.message, saveErrorFields: action.payload.fields }
    default:
      return state
  }
}

export interface InvestmentSnapshotsData {
  year: number
  month: number
  monthInputValue: string
  setMonthInputValue: (value: string) => void
  snapshots: InvestmentSnapshotDto[]
  totalValue: number
  isLoading: boolean
  error: string | null
  retry: () => void
  refresh: () => void
  editingId: string | null
  editValue: string
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<SnapshotEditField, string>>
  setEditValue: (value: string) => void
  showEditForm: (snapshot: InvestmentSnapshotDto) => void
  cancelEdit: () => void
  saveEdit: () => void
}

export function useInvestmentSnapshots(): InvestmentSnapshotsData {
  const { year, month, monthInputValue, setMonthInputValue } = useMonthSelection(DEFAULT_MONTH)
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const fetchSnapshots = useCallback((year: number, month: number) => {
    return apiClient
      .getInvestmentSnapshots(year, month)
      .then((snapshots) => dispatch({ type: 'FETCH_SUCCESS', payload: snapshots }))
      .catch((err: unknown) => {
        dispatch({
          type: 'FETCH_ERROR',
          payload: getErrorMessage(err, 'Unable to load investment snapshots'),
        })
      })
  }, [])

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void fetchSnapshots(year, month)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [year, month, state.retryCount])

  // Re-fetches after a mutation without flipping isLoading, so the table's own sort state
  // (owned by useSortableRows in the page component) survives the refresh.
  const refreshSilently = useCallback(
    () => fetchSnapshots(year, month),
    [fetchSnapshots, year, month],
  )

  const totalValue = useMemo(
    () => state.snapshots.reduce((sum, s) => sum + (s.isLiability ? -s.value : s.value), 0),
    [state.snapshots],
  )

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const setEditValue = useCallback((value: string) => dispatch({ type: 'SET_EDIT_VALUE', payload: value }), [])

  const showEditForm = useCallback(
    (snapshot: InvestmentSnapshotDto) => dispatch({ type: 'SHOW_EDIT_FORM', payload: snapshot }),
    [],
  )

  const cancelEdit = useCallback(() => dispatch({ type: 'CANCEL_EDIT' }), [])

  function saveEdit() {
    if (!state.editingId) return

    const value = parseValidatedNumber(state.editValue)
    if (value === null) {
      dispatch({
        type: 'SAVE_ERROR',
        payload: {
          message: 'Value must be a number',
          fields: { editValue: 'Value must be a number' },
        },
      })
      return
    }

    dispatch({ type: 'SAVE_START' })

    void apiClient
      .updateInvestmentSnapshotValue(state.editingId, { value })
      .then(() => {
        dispatch({ type: 'SAVE_SUCCESS' })
        void refreshSilently()
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: { message: getErrorMessage(err, 'Failed to update snapshot'), fields: {} },
        })
      })
  }

  return {
    year,
    month,
    monthInputValue,
    setMonthInputValue,
    snapshots: state.snapshots,
    totalValue,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    refresh: refreshSilently,
    editingId: state.editingId,
    editValue: state.editValue,
    isSaving: state.isSaving,
    saveError: state.saveError,
    saveErrorFields: state.saveErrorFields,
    setEditValue,
    showEditForm,
    cancelEdit,
    saveEdit,
  }
}
