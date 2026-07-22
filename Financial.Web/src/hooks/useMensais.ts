import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { RecurringBillInstanceDto } from '../api/types'
import { currentYearMonth, formatMonthInputValue, parseMonthInputValue } from '../utils/formatters'

export type EditField = 'editStatus' | 'editValue'

interface MensaisState {
  year: number
  month: number
  instances: RecurringBillInstanceDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  editingId: string | null
  editStatus: string
  editValue: string
  isSaving: boolean
  saveError: string | null
}

type MensaisAction =
  | { type: 'SET_MONTH'; payload: { year: number; month: number } }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: RecurringBillInstanceDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SHOW_EDIT_FORM'; payload: RecurringBillInstanceDto }
  | { type: 'CANCEL_EDIT' }
  | { type: 'SET_EDIT_FIELD'; payload: { field: EditField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS' }
  | { type: 'SAVE_ERROR'; payload: string }

const { year: DEFAULT_YEAR, month: DEFAULT_MONTH } = currentYearMonth()

const INITIAL_STATE: MensaisState = {
  year: DEFAULT_YEAR,
  month: DEFAULT_MONTH,
  instances: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  editingId: null,
  editStatus: '',
  editValue: '',
  isSaving: false,
  saveError: null,
}

function reducer(state: MensaisState, action: MensaisAction): MensaisState {
  switch (action.type) {
    case 'SET_MONTH':
      return { ...state, year: action.payload.year, month: action.payload.month }
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, instances: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'SHOW_EDIT_FORM':
      return {
        ...state,
        editingId: action.payload.id,
        editStatus: action.payload.status,
        editValue: String(action.payload.value),
        saveError: null,
      }
    case 'CANCEL_EDIT':
      return { ...state, editingId: null, editStatus: '', editValue: '', saveError: null }
    case 'SET_EDIT_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null }
    case 'SAVE_SUCCESS':
      return { ...state, isSaving: false, editingId: null, editStatus: '', editValue: '' }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload }
    default:
      return state
  }
}

export interface MensaisData {
  year: number
  month: number
  monthInputValue: string
  setMonthInputValue: (value: string) => void
  brasilInstances: RecurringBillInstanceDto[]
  ukInstances: RecurringBillInstanceDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  editingId: string | null
  editStatus: string
  editValue: string
  isSaving: boolean
  saveError: string | null
  setEditField: (field: EditField, value: string) => void
  showEditForm: (instance: RecurringBillInstanceDto) => void
  cancelEdit: () => void
  saveEdit: () => void
}

export function useMensais(): MensaisData {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getMensaisInstances(state.year, state.month)
      .then((instances) => dispatch({ type: 'FETCH_SUCCESS', payload: instances }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Unable to load Mensais data' })
      })
  }, [apiClient, state.year, state.month, state.retryCount])

  const monthInputValue = formatMonthInputValue(state.year, state.month)

  const setMonthInputValue = useCallback((value: string) => {
    const parsed = parseMonthInputValue(value)
    if (!parsed) return
    dispatch({ type: 'SET_MONTH', payload: parsed })
  }, [])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const setEditField = useCallback(
    (field: EditField, value: string) => dispatch({ type: 'SET_EDIT_FIELD', payload: { field, value } }),
    [],
  )

  const showEditForm = useCallback(
    (instance: RecurringBillInstanceDto) => dispatch({ type: 'SHOW_EDIT_FORM', payload: instance }),
    [],
  )

  const cancelEdit = useCallback(() => dispatch({ type: 'CANCEL_EDIT' }), [])

  function saveEdit() {
    if (!state.editingId) return

    const value = Number(state.editValue)
    if (!state.editValue.trim() || !isFinite(value)) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Value must be a number' })
      return
    }

    dispatch({ type: 'SAVE_START' })

    void apiClient
      .updateMensaisInstance(state.editingId, { status: state.editStatus, value })
      .then(() => {
        dispatch({ type: 'SAVE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to update instance',
        })
      })
  }

  const brasilInstances = useMemo(
    () => state.instances.filter((i) => i.area === 'Brasil'),
    [state.instances],
  )
  const ukInstances = useMemo(() => state.instances.filter((i) => i.area === 'UK'), [state.instances])

  return {
    year: state.year,
    month: state.month,
    monthInputValue,
    setMonthInputValue,
    brasilInstances,
    ukInstances,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    editingId: state.editingId,
    editStatus: state.editStatus,
    editValue: state.editValue,
    isSaving: state.isSaving,
    saveError: state.saveError,
    setEditField,
    showEditForm,
    cancelEdit,
    saveEdit,
  }
}
