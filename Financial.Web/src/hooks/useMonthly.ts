import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { CardStatementDto, CategoryTotalDto, ExpenseDto } from '../api/types'
import { currentYearMonth, formatMonthInputValue, parseMonthInputValue } from '../utils/formatters'

export type CreateFormField =
  | 'createDate'
  | 'createDescription'
  | 'createValue'
  | 'createCategory'
  | 'createPaymentSource'
  | 'createCardTag'
export type EditField =
  | 'editDate'
  | 'editDescription'
  | 'editValue'
  | 'editCategory'
  | 'editPaymentSource'
  | 'editCardTag'

interface MonthlyState {
  year: number
  month: number
  expenses: ExpenseDto[]
  categoryTotals: CategoryTotalDto[]
  cardStatements: CardStatementDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  isCreateFormOpen: boolean
  createDate: string
  createDescription: string
  createValue: string
  createCategory: string
  createPaymentSource: string
  createCardTag: string
  isCreating: boolean
  createError: string | null
  editingId: string | null
  editDate: string
  editDescription: string
  editValue: string
  editCategory: string
  editPaymentSource: string
  editCardTag: string
  isSaving: boolean
  saveError: string | null
}

type MonthlyAction =
  | { type: 'SET_MONTH'; payload: { year: number; month: number } }
  | { type: 'FETCH_START' }
  | {
      type: 'FETCH_SUCCESS'
      payload: { expenses: ExpenseDto[]; categoryTotals: CategoryTotalDto[]; cardStatements: CardStatementDto[] }
    }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SHOW_CREATE_FORM' }
  | { type: 'CANCEL_CREATE_FORM' }
  | { type: 'SET_CREATE_FIELD'; payload: { field: CreateFormField; value: string } }
  | { type: 'CREATE_START' }
  | { type: 'CREATE_SUCCESS' }
  | { type: 'CREATE_ERROR'; payload: string }
  | { type: 'SHOW_EDIT_FORM'; payload: ExpenseDto }
  | { type: 'CANCEL_EDIT' }
  | { type: 'SET_EDIT_FIELD'; payload: { field: EditField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS' }
  | { type: 'SAVE_ERROR'; payload: string }
  | { type: 'MARK_PAID_ERROR'; payload: string }

const { year: DEFAULT_YEAR, month: DEFAULT_MONTH } = currentYearMonth()

const BLANK_CREATE_FORM = {
  createDate: '',
  createDescription: '',
  createValue: '',
  createCategory: 'Mercado',
  createPaymentSource: 'Barclays',
  createCardTag: '',
} as const

const INITIAL_STATE: MonthlyState = {
  year: DEFAULT_YEAR,
  month: DEFAULT_MONTH,
  expenses: [],
  categoryTotals: [],
  cardStatements: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  isCreateFormOpen: false,
  ...BLANK_CREATE_FORM,
  isCreating: false,
  createError: null,
  editingId: null,
  editDate: '',
  editDescription: '',
  editValue: '',
  editCategory: '',
  editPaymentSource: '',
  editCardTag: '',
  isSaving: false,
  saveError: null,
}

function reducer(state: MonthlyState, action: MonthlyAction): MonthlyState {
  switch (action.type) {
    case 'SET_MONTH':
      return { ...state, year: action.payload.year, month: action.payload.month }
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return {
        ...state,
        isLoading: false,
        expenses: action.payload.expenses,
        categoryTotals: action.payload.categoryTotals,
        cardStatements: action.payload.cardStatements,
      }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
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
        editDate: action.payload.date,
        editDescription: action.payload.description,
        editValue: String(action.payload.value),
        editCategory: action.payload.category,
        editPaymentSource: action.payload.paymentSource,
        editCardTag: action.payload.cardTag ?? '',
        saveError: null,
      }
    case 'CANCEL_EDIT':
      return {
        ...state,
        editingId: null,
        editDate: '',
        editDescription: '',
        editValue: '',
        editCategory: '',
        editPaymentSource: '',
        editCardTag: '',
        saveError: null,
      }
    case 'SET_EDIT_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null }
    case 'SAVE_SUCCESS':
      return {
        ...state,
        isSaving: false,
        editingId: null,
        editDate: '',
        editDescription: '',
        editValue: '',
        editCategory: '',
        editPaymentSource: '',
        editCardTag: '',
      }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload }
    case 'MARK_PAID_ERROR':
      return { ...state, saveError: action.payload }
    default:
      return state
  }
}

export interface MonthlyData {
  monthInputValue: string
  setMonthInputValue: (value: string) => void
  expenses: ExpenseDto[]
  categoryTotals: CategoryTotalDto[]
  cardStatements: CardStatementDto[]
  adjustmentTotal: number
  isLoading: boolean
  error: string | null
  retry: () => void
  isCreateFormOpen: boolean
  createDate: string
  createDescription: string
  createValue: string
  createCategory: string
  createPaymentSource: string
  createCardTag: string
  isCreating: boolean
  createError: string | null
  showCreateForm: () => void
  cancelCreateForm: () => void
  setCreateField: (field: CreateFormField, value: string) => void
  submitCreate: () => void
  editingId: string | null
  editDate: string
  editDescription: string
  editValue: string
  editCategory: string
  editPaymentSource: string
  editCardTag: string
  isSaving: boolean
  saveError: string | null
  setEditField: (field: EditField, value: string) => void
  showEditForm: (expense: ExpenseDto) => void
  cancelEdit: () => void
  saveEdit: () => void
  deleteExpense: (id: string) => void
  markStatementPaid: (id: string) => void
}

export function useMonthly(): MonthlyData {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void Promise.all([
      apiClient.getExpensesByMonth(state.year, state.month),
      apiClient.getCategoryTotalsByMonth(state.year, state.month),
      apiClient.getCardStatementsByMonth(state.year, state.month),
    ])
      .then(([expenses, categoryTotals, cardStatements]) =>
        dispatch({ type: 'FETCH_SUCCESS', payload: { expenses, categoryTotals, cardStatements } }),
      )
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Unable to load Monthly data' })
      })
  }, [apiClient, state.year, state.month, state.retryCount])

  const monthInputValue = formatMonthInputValue(state.year, state.month)

  const setMonthInputValue = useCallback((value: string) => {
    const parsed = parseMonthInputValue(value)
    if (!parsed) return
    dispatch({ type: 'SET_MONTH', payload: parsed })
  }, [])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const showCreateForm = useCallback(() => dispatch({ type: 'SHOW_CREATE_FORM' }), [])

  const cancelCreateForm = useCallback(() => dispatch({ type: 'CANCEL_CREATE_FORM' }), [])

  const setCreateField = useCallback(
    (field: CreateFormField, value: string) => dispatch({ type: 'SET_CREATE_FIELD', payload: { field, value } }),
    [],
  )

  function submitCreate() {
    const { createDate, createDescription, createValue, createCategory, createPaymentSource, createCardTag } = state

    if (!createDate.trim()) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Date is required' })
      return
    }

    if (!createDescription.trim()) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Description is required' })
      return
    }

    const value = Number(createValue)
    if (!createValue.trim() || !isFinite(value) || value === 0) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Value must be a non-zero number' })
      return
    }

    dispatch({ type: 'CREATE_START' })

    void apiClient
      .createExpense({
        date: createDate,
        description: createDescription,
        value,
        category: createCategory,
        paymentSource: createPaymentSource,
        cardTag: createCardTag.trim() === '' ? null : createCardTag,
      })
      .then(() => {
        dispatch({ type: 'CREATE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'CREATE_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to create expense',
        })
      })
  }

  const setEditField = useCallback(
    (field: EditField, value: string) => dispatch({ type: 'SET_EDIT_FIELD', payload: { field, value } }),
    [],
  )

  const showEditForm = useCallback((expense: ExpenseDto) => dispatch({ type: 'SHOW_EDIT_FORM', payload: expense }), [])

  const cancelEdit = useCallback(() => dispatch({ type: 'CANCEL_EDIT' }), [])

  function saveEdit() {
    if (!state.editingId) return

    const value = Number(state.editValue)
    if (!state.editValue.trim() || !isFinite(value) || value === 0) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Value must be a non-zero number' })
      return
    }

    dispatch({ type: 'SAVE_START' })

    void apiClient
      .updateExpense(state.editingId, {
        date: state.editDate,
        description: state.editDescription,
        value,
        category: state.editCategory,
        paymentSource: state.editPaymentSource,
        cardTag: state.editCardTag.trim() === '' ? null : state.editCardTag,
      })
      .then(() => {
        dispatch({ type: 'SAVE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to update expense',
        })
      })
  }

  const deleteExpense = useCallback(
    (id: string) => {
      if (!window.confirm('Delete this expense?')) return

      void apiClient
        .deleteExpense(id)
        .then(() => dispatch({ type: 'RETRY' }))
        .catch((err: unknown) => {
          dispatch({
            type: 'SAVE_ERROR',
            payload: err instanceof Error ? err.message : 'Failed to delete expense',
          })
        })
    },
    [apiClient],
  )

  const markStatementPaid = useCallback(
    (id: string) => {
      void apiClient
        .markCardStatementPaid(id)
        .then(() => dispatch({ type: 'RETRY' }))
        .catch((err: unknown) => {
          dispatch({
            type: 'MARK_PAID_ERROR',
            payload: err instanceof Error ? err.message : 'Failed to mark statement paid',
          })
        })
    },
    [apiClient],
  )

  const adjustmentTotal = state.cardStatements.reduce((sum, statement) => sum + statement.outstandingTotal, 0)

  return {
    monthInputValue,
    setMonthInputValue,
    expenses: state.expenses,
    categoryTotals: state.categoryTotals,
    cardStatements: state.cardStatements,
    adjustmentTotal,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    isCreateFormOpen: state.isCreateFormOpen,
    createDate: state.createDate,
    createDescription: state.createDescription,
    createValue: state.createValue,
    createCategory: state.createCategory,
    createPaymentSource: state.createPaymentSource,
    createCardTag: state.createCardTag,
    isCreating: state.isCreating,
    createError: state.createError,
    showCreateForm,
    cancelCreateForm,
    setCreateField,
    submitCreate,
    editingId: state.editingId,
    editDate: state.editDate,
    editDescription: state.editDescription,
    editValue: state.editValue,
    editCategory: state.editCategory,
    editPaymentSource: state.editPaymentSource,
    editCardTag: state.editCardTag,
    isSaving: state.isSaving,
    saveError: state.saveError,
    setEditField,
    showEditForm,
    cancelEdit,
    saveEdit,
    deleteExpense,
    markStatementPaid,
  }
}
