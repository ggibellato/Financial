import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { MaeLedgerEntryDto, MaeLedgerTotalsDto } from '../api/types'
import { previousYearJanuaryFirst } from '../utils/formatters'

export type CreateFormField = 'createDate' | 'createDescription' | 'createNote' | 'createSourceCurrency' | 'createSourceValue'
export type EditField = 'editBrlValue' | 'editGbpValue'

interface ControleMaeState {
  fromDate: string
  entries: MaeLedgerEntryDto[]
  totals: MaeLedgerTotalsDto | null
  isLoading: boolean
  error: string | null
  retryCount: number
  isCreateFormOpen: boolean
  createDate: string
  createDescription: string
  createNote: string
  createSourceCurrency: string
  createSourceValue: string
  isCreating: boolean
  createError: string | null
  editingId: string | null
  editBrlValue: string
  editGbpValue: string
  isSaving: boolean
  saveError: string | null
  deletingId: string | null
  deleteError: string | null
}

type ControleMaeAction =
  | { type: 'SET_FROM_DATE'; payload: string }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: MaeLedgerEntryDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'FETCH_TOTALS_SUCCESS'; payload: MaeLedgerTotalsDto }
  | { type: 'RETRY' }
  | { type: 'SHOW_CREATE_FORM' }
  | { type: 'CANCEL_CREATE_FORM' }
  | { type: 'SET_CREATE_FIELD'; payload: { field: CreateFormField; value: string } }
  | { type: 'CREATE_START' }
  | { type: 'CREATE_SUCCESS' }
  | { type: 'CREATE_ERROR'; payload: string }
  | { type: 'SHOW_EDIT_FORM'; payload: MaeLedgerEntryDto }
  | { type: 'CANCEL_EDIT' }
  | { type: 'SET_EDIT_FIELD'; payload: { field: EditField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS' }
  | { type: 'SAVE_ERROR'; payload: string }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }

const BLANK_CREATE_FORM = {
  createDate: '',
  createDescription: '',
  createNote: '',
  createSourceCurrency: 'BRL',
  createSourceValue: '',
} as const

const INITIAL_STATE: ControleMaeState = {
  fromDate: previousYearJanuaryFirst(),
  entries: [],
  totals: null,
  isLoading: true,
  error: null,
  retryCount: 0,
  isCreateFormOpen: false,
  ...BLANK_CREATE_FORM,
  isCreating: false,
  createError: null,
  editingId: null,
  editBrlValue: '',
  editGbpValue: '',
  isSaving: false,
  saveError: null,
  deletingId: null,
  deleteError: null,
}

function reducer(state: ControleMaeState, action: ControleMaeAction): ControleMaeState {
  switch (action.type) {
    case 'SET_FROM_DATE':
      return { ...state, fromDate: action.payload }
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, entries: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'FETCH_TOTALS_SUCCESS':
      return { ...state, totals: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'SHOW_CREATE_FORM':
      return { ...state, isCreateFormOpen: true, editingId: null, saveError: null }
    case 'CANCEL_CREATE_FORM':
      return { ...state, ...BLANK_CREATE_FORM, isCreateFormOpen: false, createError: null }
    case 'SET_CREATE_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'CREATE_START':
      return { ...state, isCreating: true, createError: null }
    case 'CREATE_SUCCESS':
      return { ...state, ...BLANK_CREATE_FORM, isCreateFormOpen: false, isCreating: false }
    case 'CREATE_ERROR':
      return { ...state, isCreating: false, createError: action.payload }
    case 'SHOW_EDIT_FORM':
      return {
        ...state,
        isCreateFormOpen: false,
        editingId: action.payload.id,
        editBrlValue: action.payload.brlValue !== null ? String(action.payload.brlValue) : '',
        editGbpValue: action.payload.gbpValue !== null ? String(action.payload.gbpValue) : '',
        saveError: null,
      }
    case 'CANCEL_EDIT':
      return { ...state, editingId: null, editBrlValue: '', editGbpValue: '', saveError: null }
    case 'SET_EDIT_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null }
    case 'SAVE_SUCCESS':
      return { ...state, isSaving: false, editingId: null, editBrlValue: '', editGbpValue: '' }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload }
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

export interface ControleMaeData {
  fromDateInputValue: string
  setFromDateInputValue: (value: string) => void
  entries: MaeLedgerEntryDto[]
  totals: MaeLedgerTotalsDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
  isCreateFormOpen: boolean
  createDate: string
  createDescription: string
  createNote: string
  createSourceCurrency: string
  createSourceValue: string
  isCreating: boolean
  createError: string | null
  showCreateForm: () => void
  cancelCreateForm: () => void
  setCreateField: (field: CreateFormField, value: string) => void
  submitCreate: () => void
  editingId: string | null
  editBrlValue: string
  editGbpValue: string
  isSaving: boolean
  saveError: string | null
  setEditField: (field: EditField, value: string) => void
  showEditForm: (entry: MaeLedgerEntryDto) => void
  cancelEdit: () => void
  saveEdit: () => void
  deletingId: string | null
  deleteError: string | null
  deleteEntry: (id: string) => void
}

export function useControleMae(): ControleMaeData {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getMaeLedgerEntriesFromDate(state.fromDate)
      .then((entries) => dispatch({ type: 'FETCH_SUCCESS', payload: entries }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Unable to load Controle Mae data' })
      })
  }, [apiClient, state.fromDate, state.retryCount])

  useEffect(() => {
    void apiClient
      .getMaeLedgerTotals()
      .then((totals) => dispatch({ type: 'FETCH_TOTALS_SUCCESS', payload: totals }))
      .catch(() => {
        // Totals are supplementary to the ledger list; a failed refresh just keeps the last known values.
      })
  }, [apiClient, state.retryCount])

  const setFromDateInputValue = useCallback((value: string) => {
    if (!value) return
    dispatch({ type: 'SET_FROM_DATE', payload: value })
  }, [])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const showCreateForm = useCallback(() => dispatch({ type: 'SHOW_CREATE_FORM' }), [])

  const cancelCreateForm = useCallback(() => dispatch({ type: 'CANCEL_CREATE_FORM' }), [])

  const setCreateField = useCallback(
    (field: CreateFormField, value: string) => dispatch({ type: 'SET_CREATE_FIELD', payload: { field, value } }),
    [],
  )

  function submitCreate() {
    const { createDate, createDescription, createSourceCurrency, createSourceValue, createNote } = state

    if (!createDate.trim()) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Date is required' })
      return
    }

    if (!createDescription.trim()) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Description is required' })
      return
    }

    const sourceValue = Number(createSourceValue)
    if (!createSourceValue.trim() || !isFinite(sourceValue) || sourceValue === 0) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Value must be a non-zero number' })
      return
    }

    dispatch({ type: 'CREATE_START' })

    void apiClient
      .createMaeLedgerEntry({
        date: createDate,
        description: createDescription,
        note: createNote,
        sourceCurrency: createSourceCurrency,
        sourceValue,
      })
      .then(() => {
        dispatch({ type: 'CREATE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'CREATE_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to create entry',
        })
      })
  }

  const setEditField = useCallback(
    (field: EditField, value: string) => dispatch({ type: 'SET_EDIT_FIELD', payload: { field, value } }),
    [],
  )

  const showEditForm = useCallback(
    (entry: MaeLedgerEntryDto) => dispatch({ type: 'SHOW_EDIT_FORM', payload: entry }),
    [],
  )

  const cancelEdit = useCallback(() => dispatch({ type: 'CANCEL_EDIT' }), [])

  function saveEdit() {
    if (!state.editingId) return

    const brlValue = state.editBrlValue.trim() === '' ? null : Number(state.editBrlValue)
    const gbpValue = state.editGbpValue.trim() === '' ? null : Number(state.editGbpValue)

    dispatch({ type: 'SAVE_START' })

    void apiClient
      .updateMaeLedgerEntryValues(state.editingId, { brlValue, gbpValue })
      .then(() => {
        dispatch({ type: 'SAVE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to update entry',
        })
      })
  }

  function deleteEntry(id: string) {
    dispatch({ type: 'DELETE_START', payload: id })

    void apiClient
      .deleteMaeLedgerEntry(id)
      .then(() => {
        dispatch({ type: 'DELETE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'DELETE_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to delete entry',
        })
      })
  }

  return {
    fromDateInputValue: state.fromDate,
    setFromDateInputValue,
    entries: state.entries,
    totals: state.totals,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    isCreateFormOpen: state.isCreateFormOpen,
    createDate: state.createDate,
    createDescription: state.createDescription,
    createNote: state.createNote,
    createSourceCurrency: state.createSourceCurrency,
    createSourceValue: state.createSourceValue,
    isCreating: state.isCreating,
    createError: state.createError,
    showCreateForm,
    cancelCreateForm,
    setCreateField,
    submitCreate,
    editingId: state.editingId,
    editBrlValue: state.editBrlValue,
    editGbpValue: state.editGbpValue,
    isSaving: state.isSaving,
    saveError: state.saveError,
    setEditField,
    showEditForm,
    cancelEdit,
    saveEdit,
    deletingId: state.deletingId,
    deleteError: state.deleteError,
    deleteEntry,
  }
}
