import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { CreateRecurringBillTemplateDto, RecurringBillInstanceDto } from '../api/types'
import { currentYearMonth, formatMonthInputValue, parseMonthInputValue } from '../utils/formatters'

export type EditField = 'editStatus' | 'editValue'
export type AddField = 'newDueDay' | 'newDescription' | 'newValue' | 'newArea' | 'newNote' | 'newNitNumber' | 'newMinimumWageValue'

const EMPTY_ADD_FORM = {
  newDueDay: '',
  newDescription: '',
  newValue: '',
  newArea: 'Brasil',
  newNote: '',
  newNitNumber: '',
  newMinimumWageValue: '',
}

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
  isAddFormOpen: boolean
  newDueDay: string
  newDescription: string
  newValue: string
  newArea: string
  newNote: string
  newNitNumber: string
  newMinimumWageValue: string
  isAdding: boolean
  addError: string | null
  deletingTemplateId: string | null
  deleteError: string | null
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
  | { type: 'SHOW_ADD_FORM' }
  | { type: 'CANCEL_ADD' }
  | { type: 'SET_ADD_FIELD'; payload: { field: AddField; value: string } }
  | { type: 'ADD_START' }
  | { type: 'ADD_SUCCESS' }
  | { type: 'ADD_ERROR'; payload: string }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }

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
  isAddFormOpen: false,
  ...EMPTY_ADD_FORM,
  isAdding: false,
  addError: null,
  deletingTemplateId: null,
  deleteError: null,
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
      return { ...state, deletingTemplateId: action.payload, deleteError: null }
    case 'DELETE_SUCCESS':
      return { ...state, deletingTemplateId: null }
    case 'DELETE_ERROR':
      return { ...state, deletingTemplateId: null, deleteError: action.payload }
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
  isAddFormOpen: boolean
  newDueDay: string
  newDescription: string
  newValue: string
  newArea: string
  newNote: string
  newNitNumber: string
  newMinimumWageValue: string
  isAdding: boolean
  addError: string | null
  setAddField: (field: AddField, value: string) => void
  showAddForm: () => void
  cancelAdd: () => void
  submitAdd: () => void
  deletingTemplateId: string | null
  deleteError: string | null
  deleteTemplate: (templateId: string) => void
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

    const minimumWageValue = state.newMinimumWageValue.trim() ? Number(state.newMinimumWageValue) : null
    if (minimumWageValue !== null && !isFinite(minimumWageValue)) {
      dispatch({ type: 'ADD_ERROR', payload: 'Minimum wage value must be a number' })
      return
    }

    dispatch({ type: 'ADD_START' })

    const request: CreateRecurringBillTemplateDto = {
      dueDay,
      description: state.newDescription,
      value,
      area: state.newArea,
      note: state.newNote,
      nitNumber: state.newNitNumber.trim() || null,
      minimumWageValue,
    }

    void apiClient
      .createMensaisTemplate(request)
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

  function deleteTemplate(templateId: string) {
    dispatch({ type: 'DELETE_START', payload: templateId })

    void apiClient
      .deleteMensaisTemplate(templateId)
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
    isAddFormOpen: state.isAddFormOpen,
    newDueDay: state.newDueDay,
    newDescription: state.newDescription,
    newValue: state.newValue,
    newArea: state.newArea,
    newNote: state.newNote,
    newNitNumber: state.newNitNumber,
    newMinimumWageValue: state.newMinimumWageValue,
    isAdding: state.isAdding,
    addError: state.addError,
    setAddField,
    showAddForm,
    cancelAdd,
    submitAdd,
    deletingTemplateId: state.deletingTemplateId,
    deleteError: state.deleteError,
    deleteTemplate,
  }
}
