import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { CreateRecurringBillDto, RecurringBillDto } from '../api/types'
import { currentYearMonth, formatMonthInputValue, parseMonthInputValue } from '../utils/formatters'

export type EditField = 'editStatus' | 'editValue'
export type AddField = 'newDueDay' | 'newDescription' | 'newValue' | 'newArea' | 'newNote'

const EMPTY_ADD_FORM = {
  newDueDay: '',
  newDescription: '',
  newValue: '',
  newArea: 'Brasil',
  newNote: '',
}

interface MensaisState {
  displayYear: number
  displayMonth: number
  bills: RecurringBillDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  editingId: string | null
  editStatus: string
  editValue: string
  isSaving: boolean
  saveError: string | null
  isAddFormOpen: boolean
  newDueDay: string
  newDescription: string
  newValue: string
  newArea: string
  newNote: string
  isAdding: boolean
  addError: string | null
  deletingBillId: string | null
  deleteError: string | null
  isResetting: boolean
  resetError: string | null
}

type MensaisAction =
  | { type: 'SET_DISPLAY_MONTH'; payload: { year: number; month: number } }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: RecurringBillDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SHOW_EDIT_FORM'; payload: RecurringBillDto }
  | { type: 'CANCEL_EDIT' }
  | { type: 'SET_EDIT_FIELD'; payload: { field: EditField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS' }
  | { type: 'SAVE_ERROR'; payload: string }
  | { type: 'SHOW_ADD_FORM' }
  | { type: 'CANCEL_ADD' }
  | { type: 'SET_ADD_FIELD'; payload: { field: AddField; value: string } }
  | { type: 'ADD_START' }
  | { type: 'ADD_SUCCESS' }
  | { type: 'ADD_ERROR'; payload: string }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }
  | { type: 'RESET_START' }
  | { type: 'RESET_SUCCESS'; payload: RecurringBillDto[] }
  | { type: 'RESET_ERROR'; payload: string }

const { year: DEFAULT_YEAR, month: DEFAULT_MONTH } = currentYearMonth()

const INITIAL_STATE: MensaisState = {
  displayYear: DEFAULT_YEAR,
  displayMonth: DEFAULT_MONTH,
  bills: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  editingId: null,
  editStatus: '',
  editValue: '',
  isSaving: false,
  saveError: null,
  isAddFormOpen: false,
  ...EMPTY_ADD_FORM,
  isAdding: false,
  addError: null,
  deletingBillId: null,
  deleteError: null,
  isResetting: false,
  resetError: null,
}

function reducer(state: MensaisState, action: MensaisAction): MensaisState {
  switch (action.type) {
    case 'SET_DISPLAY_MONTH':
      return { ...state, displayYear: action.payload.year, displayMonth: action.payload.month }
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, bills: action.payload }
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
    case 'SHOW_ADD_FORM':
      return { ...state, isAddFormOpen: true, ...EMPTY_ADD_FORM, addError: null }
    case 'CANCEL_ADD':
      return { ...state, isAddFormOpen: false, ...EMPTY_ADD_FORM, addError: null }
    case 'SET_ADD_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'ADD_START':
      return { ...state, isAdding: true, addError: null }
    case 'ADD_SUCCESS':
      return { ...state, isAdding: false, isAddFormOpen: false, ...EMPTY_ADD_FORM }
    case 'ADD_ERROR':
      return { ...state, isAdding: false, addError: action.payload }
    case 'DELETE_START':
      return { ...state, deletingBillId: action.payload, deleteError: null }
    case 'DELETE_SUCCESS':
      return { ...state, deletingBillId: null }
    case 'DELETE_ERROR':
      return { ...state, deletingBillId: null, deleteError: action.payload }
    case 'RESET_START':
      return { ...state, isResetting: true, resetError: null }
    case 'RESET_SUCCESS':
      return { ...state, isResetting: false, bills: action.payload }
    case 'RESET_ERROR':
      return { ...state, isResetting: false, resetError: action.payload }
    default:
      return state
  }
}

export interface MensaisData {
  monthInputValue: string
  setMonthInputValue: (value: string) => void
  brasilBills: RecurringBillDto[]
  ukBills: RecurringBillDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  editingId: string | null
  editStatus: string
  editValue: string
  isSaving: boolean
  saveError: string | null
  setEditField: (field: EditField, value: string) => void
  showEditForm: (bill: RecurringBillDto) => void
  cancelEdit: () => void
  saveEdit: () => void
  isAddFormOpen: boolean
  newDueDay: string
  newDescription: string
  newValue: string
  newArea: string
  newNote: string
  isAdding: boolean
  addError: string | null
  setAddField: (field: AddField, value: string) => void
  showAddForm: () => void
  cancelAdd: () => void
  submitAdd: () => void
  deletingBillId: string | null
  deleteError: string | null
  deleteBill: (id: string) => void
  isResetting: boolean
  resetError: string | null
  resetAllToUnset: () => void
}

export function useMensais(): MensaisData {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getMensaisBills()
      .then((bills) => dispatch({ type: 'FETCH_SUCCESS', payload: bills }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Unable to load Mensais data' })
      })
  }, [apiClient, state.retryCount])

  const monthInputValue = formatMonthInputValue(state.displayYear, state.displayMonth)

  const setMonthInputValue = useCallback((value: string) => {
    const parsed = parseMonthInputValue(value)
    if (!parsed) return
    dispatch({ type: 'SET_DISPLAY_MONTH', payload: parsed })
  }, [])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const setEditField = useCallback(
    (field: EditField, value: string) => dispatch({ type: 'SET_EDIT_FIELD', payload: { field, value } }),
    [],
  )

  const showEditForm = useCallback(
    (bill: RecurringBillDto) => dispatch({ type: 'SHOW_EDIT_FORM', payload: bill }),
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
      .updateMensaisBill(state.editingId, { status: state.editStatus, value })
      .then(() => {
        dispatch({ type: 'SAVE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to update bill',
        })
      })
  }

  const setAddField = useCallback(
    (field: AddField, value: string) => dispatch({ type: 'SET_ADD_FIELD', payload: { field, value } }),
    [],
  )

  const showAddForm = useCallback(() => dispatch({ type: 'SHOW_ADD_FORM' }), [])

  const cancelAdd = useCallback(() => dispatch({ type: 'CANCEL_ADD' }), [])

  function submitAdd() {
    const dueDay = Number(state.newDueDay)
    const value = Number(state.newValue)

    if (!state.newDescription.trim()) {
      dispatch({ type: 'ADD_ERROR', payload: 'Description is required' })
      return
    }
    if (!state.newDueDay.trim() || !isFinite(dueDay)) {
      dispatch({ type: 'ADD_ERROR', payload: 'Due day must be a number' })
      return
    }
    if (!state.newValue.trim() || !isFinite(value)) {
      dispatch({ type: 'ADD_ERROR', payload: 'Value must be a number' })
      return
    }

    dispatch({ type: 'ADD_START' })

    const request: CreateRecurringBillDto = {
      dueDay,
      description: state.newDescription,
      value,
      area: state.newArea,
      note: state.newNote,
    }

    void apiClient
      .createMensaisBill(request)
      .then(() => {
        dispatch({ type: 'ADD_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'ADD_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to add bill',
        })
      })
  }

  function deleteBill(id: string) {
    dispatch({ type: 'DELETE_START', payload: id })

    void apiClient
      .deleteMensaisBill(id)
      .then(() => {
        dispatch({ type: 'DELETE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'DELETE_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to delete bill',
        })
      })
  }

  function resetAllToUnset() {
    dispatch({ type: 'RESET_START' })

    void apiClient
      .resetMensaisToUnset()
      .then((bills) => dispatch({ type: 'RESET_SUCCESS', payload: bills }))
      .catch((err: unknown) => {
        dispatch({
          type: 'RESET_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to reset bills',
        })
      })
  }

  const brasilBills = useMemo(() => state.bills.filter((b) => b.area === 'Brasil'), [state.bills])
  const ukBills = useMemo(() => state.bills.filter((b) => b.area === 'UK'), [state.bills])

  return {
    monthInputValue,
    setMonthInputValue,
    brasilBills,
    ukBills,
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
    isAddFormOpen: state.isAddFormOpen,
    newDueDay: state.newDueDay,
    newDescription: state.newDescription,
    newValue: state.newValue,
    newArea: state.newArea,
    newNote: state.newNote,
    isAdding: state.isAdding,
    addError: state.addError,
    setAddField,
    showAddForm,
    cancelAdd,
    submitAdd,
    deletingBillId: state.deletingBillId,
    deleteError: state.deleteError,
    deleteBill,
    isResetting: state.isResetting,
    resetError: state.resetError,
    resetAllToUnset,
  }
}
