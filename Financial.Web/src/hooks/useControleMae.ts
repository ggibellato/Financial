import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { MaeLedgerEntryDto, MaeLedgerTotalsDto } from '../api/types'
import { getErrorMessage, previousYearJanuaryFirst, todayIsoDate } from '../utils/formatters'
import { getStoredDefault, setStoredDefault } from '../utils/createFormDefaults'

export type CreateFormField = 'createDate' | 'createDescription' | 'createNote' | 'createSourceCurrency' | 'createSourceValue'
export type EditField = 'editBrlValue' | 'editGbpValue'

const DATE_KEY = 'createEntry.date'
const CURRENCY_KEY = 'createEntry.sourceCurrency'
const CURRENCIES = ['BRL', 'GBP']

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
  createErrorFields: Partial<Record<CreateFormField, string>>
  editingId: string | null
  editBrlValue: string
  editGbpValue: string
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<EditField, string>>
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
  | { type: 'SHOW_CREATE_FORM'; payload: { date: string; sourceCurrency: string } }
  | { type: 'CANCEL_CREATE_FORM' }
  | { type: 'SET_CREATE_FIELD'; payload: { field: CreateFormField; value: string } }
  | { type: 'CREATE_START' }
  | { type: 'CREATE_SUCCESS' }
  | { type: 'CREATE_ERROR'; payload: { message: string | null; fields: Partial<Record<CreateFormField, string>> } }
  | { type: 'SHOW_EDIT_FORM'; payload: MaeLedgerEntryDto }
  | { type: 'CANCEL_EDIT' }
  | { type: 'SET_EDIT_FIELD'; payload: { field: EditField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS' }
  | { type: 'SAVE_ERROR'; payload: { message: string | null; fields: Partial<Record<EditField, string>> } }
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
  createErrorFields: {},
  editingId: null,
  editBrlValue: '',
  editGbpValue: '',
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
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
      return {
        ...state,
        isCreateFormOpen: true,
        editingId: null,
        saveError: null,
        saveErrorFields: {},
        createDate: action.payload.date,
        createSourceCurrency: action.payload.sourceCurrency,
      }
    case 'CANCEL_CREATE_FORM':
      return { ...state, ...BLANK_CREATE_FORM, isCreateFormOpen: false, createError: null, createErrorFields: {} }
    case 'SET_CREATE_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'CREATE_START':
      return { ...state, isCreating: true, createError: null, createErrorFields: {} }
    case 'CREATE_SUCCESS':
      return { ...state, ...BLANK_CREATE_FORM, isCreateFormOpen: false, isCreating: false }
    case 'CREATE_ERROR':
      return { ...state, isCreating: false, createError: action.payload.message, createErrorFields: action.payload.fields }
    case 'SHOW_EDIT_FORM':
      return {
        ...state,
        isCreateFormOpen: false,
        editingId: action.payload.id,
        editBrlValue: action.payload.brlValue !== null ? String(action.payload.brlValue) : '',
        editGbpValue: action.payload.gbpValue !== null ? String(action.payload.gbpValue) : '',
        saveError: null,
        saveErrorFields: {},
      }
    case 'CANCEL_EDIT':
      return { ...state, editingId: null, editBrlValue: '', editGbpValue: '', saveError: null, saveErrorFields: {} }
    case 'SET_EDIT_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null, saveErrorFields: {} }
    case 'SAVE_SUCCESS':
      return { ...state, isSaving: false, editingId: null, editBrlValue: '', editGbpValue: '' }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload.message, saveErrorFields: action.payload.fields }
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
  createErrorFields: Partial<Record<CreateFormField, string>>
  showCreateForm: () => void
  cancelCreateForm: () => void
  setCreateField: (field: CreateFormField, value: string) => void
  submitCreate: () => void
  editingId: string | null
  editBrlValue: string
  editGbpValue: string
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<EditField, string>>
  setEditField: (field: EditField, value: string) => void
  showEditForm: (entry: MaeLedgerEntryDto) => void
  cancelEdit: () => void
  saveEdit: () => void
  deletingId: string | null
  deleteError: string | null
  deleteEntry: (id: string) => void
}

export function useControleMae(): ControleMaeData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const fetchEntries = useCallback((fromDate: string) => {
    return apiClient
      .getMaeLedgerEntriesFromDate(fromDate)
      .then((entries) => dispatch({ type: 'FETCH_SUCCESS', payload: entries }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load Controle Mae data') })
      })
  }, [])

  const fetchTotals = useCallback(() => {
    return apiClient
      .getMaeLedgerTotals()
      .then((totals) => dispatch({ type: 'FETCH_TOTALS_SUCCESS', payload: totals }))
      .catch(() => {
        // Totals are supplementary to the ledger list; a failed refresh just keeps the last known values.
      })
  }, [])

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void fetchEntries(state.fromDate)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [state.fromDate, state.retryCount])

  useEffect(() => {
    void fetchTotals()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [state.retryCount])

  const setFromDateInputValue = useCallback((value: string) => {
    if (!value) return
    dispatch({ type: 'SET_FROM_DATE', payload: value })
  }, [])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  // Re-fetches after a mutation without flipping isLoading, so the entries grid's own
  // sort/filter state survives the refresh.
  const refreshSilently = useCallback(
    () => Promise.all([fetchEntries(state.fromDate), fetchTotals()]),
    [fetchEntries, fetchTotals, state.fromDate],
  )

  const showCreateForm = useCallback(() => {
    const storedCurrency = getStoredDefault(CURRENCY_KEY)
    dispatch({
      type: 'SHOW_CREATE_FORM',
      payload: {
        date: getStoredDefault(DATE_KEY) ?? todayIsoDate(),
        sourceCurrency: storedCurrency && CURRENCIES.includes(storedCurrency) ? storedCurrency : 'BRL',
      },
    })
  }, [])

  const cancelCreateForm = useCallback(() => dispatch({ type: 'CANCEL_CREATE_FORM' }), [])

  const setCreateField = useCallback(
    (field: CreateFormField, value: string) => dispatch({ type: 'SET_CREATE_FIELD', payload: { field, value } }),
    [],
  )

  function submitCreate() {
    const { createDate, createDescription, createSourceCurrency, createSourceValue, createNote } = state
    const errors: Partial<Record<CreateFormField, string>> = {}

    if (!createDate.trim()) {
      errors.createDate = 'Date is required'
    }

    if (!createDescription.trim()) {
      errors.createDescription = 'Description is required'
    }

    const sourceValue = Number(createSourceValue)
    if (!createSourceValue.trim() || !isFinite(sourceValue) || sourceValue === 0) {
      errors.createSourceValue = 'Value must be a non-zero number'
    }

    if (Object.keys(errors).length > 0) {
      dispatch({ type: 'CREATE_ERROR', payload: { message: Object.values(errors)[0] ?? null, fields: errors } })
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
        setStoredDefault(DATE_KEY, createDate)
        setStoredDefault(CURRENCY_KEY, createSourceCurrency)
        dispatch({ type: 'CREATE_SUCCESS' })
        void refreshSilently()
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'CREATE_ERROR',
          payload: { message: getErrorMessage(err, 'Failed to create entry'), fields: {} },
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

    const errors: Partial<Record<EditField, string>> = {}

    const brlValue = state.editBrlValue.trim() === '' ? null : Number(state.editBrlValue)
    if (brlValue !== null && !isFinite(brlValue)) {
      errors.editBrlValue = 'BRL value must be a number'
    }

    const gbpValue = state.editGbpValue.trim() === '' ? null : Number(state.editGbpValue)
    if (gbpValue !== null && !isFinite(gbpValue)) {
      errors.editGbpValue = 'GBP value must be a number'
    }

    if (Object.keys(errors).length > 0) {
      dispatch({ type: 'SAVE_ERROR', payload: { message: Object.values(errors)[0] ?? null, fields: errors } })
      return
    }

    dispatch({ type: 'SAVE_START' })

    void apiClient
      .updateMaeLedgerEntryValues(state.editingId, { brlValue, gbpValue })
      .then(() => {
        dispatch({ type: 'SAVE_SUCCESS' })
        void refreshSilently()
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: { message: getErrorMessage(err, 'Failed to update entry'), fields: {} },
        })
      })
  }

  function deleteEntry(id: string) {
    dispatch({ type: 'DELETE_START', payload: id })

    void apiClient
      .deleteMaeLedgerEntry(id)
      .then(() => {
        dispatch({ type: 'DELETE_SUCCESS' })
        void refreshSilently()
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'DELETE_ERROR',
          payload: getErrorMessage(err, 'Failed to delete entry'),
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
    createErrorFields: state.createErrorFields,
    showCreateForm,
    cancelCreateForm,
    setCreateField,
    submitCreate,
    editingId: state.editingId,
    editBrlValue: state.editBrlValue,
    editGbpValue: state.editGbpValue,
    isSaving: state.isSaving,
    saveError: state.saveError,
    saveErrorFields: state.saveErrorFields,
    setEditField,
    showEditForm,
    cancelEdit,
    saveEdit,
    deletingId: state.deletingId,
    deleteError: state.deleteError,
    deleteEntry,
  }
}
