import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { BankDto, CategoryDto, RecurringBillCreateDto, RecurringBillDto } from '../api/types'
import {
  currentYearMonth,
  formatMonthInputValue,
  getErrorMessage,
  parseMonthInputValue,
  parseValidatedNumber,
} from '../utils/formatters'
import { getStoredDefault, setStoredDefault } from '../utils/createFormDefaults'

export type EditField = 'editStatus' | 'editValue'
export type AddField = 'newDueDay' | 'newDescription' | 'newValue' | 'newArea' | 'newNote'

export interface ExpensePromptValues {
  description: string
  value: number
  date: string
  bankId: string
  categoryId: string
}

const AREA_KEY = 'addBill.area'
const AREAS = ['Brasil', 'UK']

const EMPTY_ADD_FORM_FIELDS = {
  newDueDay: '',
  newDescription: '',
  newValue: '',
  newNote: '',
}

function defaultArea(): string {
  const stored = getStoredDefault(AREA_KEY)
  return stored && AREAS.includes(stored) ? stored : 'Brasil'
}

interface MensaisState {
  displayYear: number
  displayMonth: number
  bills: RecurringBillDto[]
  banks: BankDto[]
  categories: CategoryDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  editingId: string | null
  editStatus: string
  editValue: string
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<EditField, string>>
  isAddFormOpen: boolean
  newDueDay: string
  newDescription: string
  newValue: string
  newArea: string
  newNote: string
  isAdding: boolean
  addError: string | null
  addErrorFields: Partial<Record<AddField, string>>
  deletingBillId: string | null
  deleteError: string | null
  isResetting: boolean
  resetError: string | null
  updatingStatusBillId: string | null
  statusUpdateError: string | null
  expensePromptBill: RecurringBillDto | null
  isCreatingExpense: boolean
  expenseCreateError: string | null
  expenseCreatedForRetry: boolean
}

type MensaisAction =
  | { type: 'SET_DISPLAY_MONTH'; payload: { year: number; month: number } }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: { bills: RecurringBillDto[]; banks: BankDto[]; categories: CategoryDto[] } }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SHOW_EDIT_FORM'; payload: RecurringBillDto }
  | { type: 'CANCEL_EDIT' }
  | { type: 'SET_EDIT_FIELD'; payload: { field: EditField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS' }
  | { type: 'SAVE_ERROR'; payload: { message: string | null; fields: Partial<Record<EditField, string>> } }
  | { type: 'SHOW_ADD_FORM'; payload: { area: string } }
  | { type: 'CANCEL_ADD' }
  | { type: 'SET_ADD_FIELD'; payload: { field: AddField; value: string } }
  | { type: 'ADD_START' }
  | { type: 'ADD_SUCCESS' }
  | { type: 'ADD_ERROR'; payload: { message: string | null; fields: Partial<Record<AddField, string>> } }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }
  | { type: 'RESET_START' }
  | { type: 'RESET_SUCCESS'; payload: RecurringBillDto[] }
  | { type: 'RESET_ERROR'; payload: string }
  | { type: 'UPDATE_STATUS_START'; payload: string }
  | { type: 'UPDATE_STATUS_SUCCESS'; payload: RecurringBillDto }
  | { type: 'UPDATE_STATUS_ERROR'; payload: string }
  | { type: 'OPEN_EXPENSE_PROMPT'; payload: RecurringBillDto }
  | { type: 'CLOSE_EXPENSE_PROMPT' }
  | { type: 'EXPENSE_CREATE_START' }
  | { type: 'EXPENSE_CREATE_SUCCESS' }
  | { type: 'EXPENSE_CREATE_ERROR'; payload: string }

const { year: DEFAULT_YEAR, month: DEFAULT_MONTH } = currentYearMonth()

const INITIAL_STATE: MensaisState = {
  displayYear: DEFAULT_YEAR,
  displayMonth: DEFAULT_MONTH,
  bills: [],
  banks: [],
  categories: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  editingId: null,
  editStatus: '',
  editValue: '',
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
  isAddFormOpen: false,
  ...EMPTY_ADD_FORM_FIELDS,
  newArea: 'Brasil',
  isAdding: false,
  addError: null,
  addErrorFields: {},
  deletingBillId: null,
  deleteError: null,
  isResetting: false,
  resetError: null,
  updatingStatusBillId: null,
  statusUpdateError: null,
  expensePromptBill: null,
  isCreatingExpense: false,
  expenseCreateError: null,
  expenseCreatedForRetry: false,
}

function reducer(state: MensaisState, action: MensaisAction): MensaisState {
  switch (action.type) {
    case 'SET_DISPLAY_MONTH':
      return { ...state, displayYear: action.payload.year, displayMonth: action.payload.month }
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return {
        ...state,
        isLoading: false,
        bills: action.payload.bills,
        banks: action.payload.banks,
        categories: action.payload.categories,
      }
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
        saveErrorFields: {},
      }
    case 'CANCEL_EDIT':
      return { ...state, editingId: null, editStatus: '', editValue: '', saveError: null, saveErrorFields: {} }
    case 'SET_EDIT_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null, saveErrorFields: {} }
    case 'SAVE_SUCCESS':
      return { ...state, isSaving: false, editingId: null, editStatus: '', editValue: '' }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload.message, saveErrorFields: action.payload.fields }
    case 'SHOW_ADD_FORM':
      return { ...state, isAddFormOpen: true, ...EMPTY_ADD_FORM_FIELDS, newArea: action.payload.area, addError: null, addErrorFields: {} }
    case 'CANCEL_ADD':
      return { ...state, isAddFormOpen: false, ...EMPTY_ADD_FORM_FIELDS, addError: null, addErrorFields: {} }
    case 'SET_ADD_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'ADD_START':
      return { ...state, isAdding: true, addError: null, addErrorFields: {} }
    case 'ADD_SUCCESS':
      return { ...state, isAdding: false, isAddFormOpen: false, ...EMPTY_ADD_FORM_FIELDS }
    case 'ADD_ERROR':
      return { ...state, isAdding: false, addError: action.payload.message, addErrorFields: action.payload.fields }
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
    case 'UPDATE_STATUS_START':
      return { ...state, updatingStatusBillId: action.payload, statusUpdateError: null }
    case 'UPDATE_STATUS_SUCCESS':
      return {
        ...state,
        updatingStatusBillId: null,
        bills: state.bills.map((b) => (b.id === action.payload.id ? action.payload : b)),
        ...(state.expensePromptBill?.id === action.payload.id
          ? { expensePromptBill: null, isCreatingExpense: false, expenseCreateError: null, expenseCreatedForRetry: false }
          : null),
      }
    case 'UPDATE_STATUS_ERROR':
      return { ...state, updatingStatusBillId: null, statusUpdateError: action.payload }
    case 'OPEN_EXPENSE_PROMPT':
      return {
        ...state,
        expensePromptBill: action.payload,
        expenseCreateError: null,
        statusUpdateError: null,
        expenseCreatedForRetry: false,
      }
    case 'CLOSE_EXPENSE_PROMPT':
      return {
        ...state,
        expensePromptBill: null,
        isCreatingExpense: false,
        expenseCreateError: null,
        expenseCreatedForRetry: false,
        statusUpdateError: null,
      }
    case 'EXPENSE_CREATE_START':
      return { ...state, isCreatingExpense: true, expenseCreateError: null }
    case 'EXPENSE_CREATE_SUCCESS':
      return { ...state, isCreatingExpense: false, expenseCreatedForRetry: true }
    case 'EXPENSE_CREATE_ERROR':
      return { ...state, isCreatingExpense: false, expenseCreateError: action.payload }
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
  saveErrorFields: Partial<Record<EditField, string>>
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
  addErrorFields: Partial<Record<AddField, string>>
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
  updatingStatusBillId: string | null
  statusUpdateError: string | null
  updateBillStatus: (id: string, status: string) => void
  banks: BankDto[]
  categories: CategoryDto[]
  expensePromptBill: RecurringBillDto | null
  isCreatingExpense: boolean
  expenseCreateError: string | null
  expenseCreatedForRetry: boolean
  confirmExpensePrompt: (values: ExpensePromptValues) => void
  skipOrRetryExpensePrompt: () => void
  closeExpensePrompt: () => void
}

export function useMensais(): MensaisData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void Promise.all([apiClient.getMensaisBills(), apiClient.getBanks(), apiClient.getCategories()])
      .then(([bills, banks, categories]) => dispatch({ type: 'FETCH_SUCCESS', payload: { bills, banks, categories } }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load Mensais data') })
      })
  }, [state.retryCount])

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

    const errors: Partial<Record<EditField, string>> = {}

    const value = parseValidatedNumber(state.editValue)
    if (value === null) {
      errors.editValue = 'Value must be a number'
    }

    if (!state.editStatus.trim()) {
      errors.editStatus = 'Status is required'
    }

    if (Object.keys(errors).length > 0) {
      dispatch({ type: 'SAVE_ERROR', payload: { message: Object.values(errors)[0] ?? null, fields: errors } })
      return
    }

    const bill = state.bills.find((b) => b.id === state.editingId)
    if (!bill) return

    dispatch({ type: 'SAVE_START' })

    void apiClient
      .updateMensaisBill(state.editingId, {
        dueDay: bill.dueDay,
        description: bill.description,
        value: value as number,
        area: bill.area,
        note: bill.note,
        nitNumber: bill.nitNumber,
        minimumWageValue: bill.minimumWageValue,
        status: state.editStatus,
      })
      .then(() => {
        dispatch({ type: 'SAVE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: { message: getErrorMessage(err, 'Failed to update bill'), fields: {} },
        })
      })
  }

  const setAddField = useCallback(
    (field: AddField, value: string) => dispatch({ type: 'SET_ADD_FIELD', payload: { field, value } }),
    [],
  )

  const showAddForm = useCallback(() => dispatch({ type: 'SHOW_ADD_FORM', payload: { area: defaultArea() } }), [])

  const cancelAdd = useCallback(() => dispatch({ type: 'CANCEL_ADD' }), [])

  function submitAdd() {
    const errors: Partial<Record<AddField, string>> = {}

    if (!state.newDescription.trim()) {
      errors.newDescription = 'Description is required'
    }

    const dueDay = parseValidatedNumber(state.newDueDay)
    if (dueDay === null) {
      errors.newDueDay = 'Due day must be a number'
    }

    const value = parseValidatedNumber(state.newValue)
    if (value === null) {
      errors.newValue = 'Value must be a number'
    }

    if (Object.keys(errors).length > 0) {
      dispatch({ type: 'ADD_ERROR', payload: { message: Object.values(errors)[0] ?? null, fields: errors } })
      return
    }

    dispatch({ type: 'ADD_START' })

    const request: RecurringBillCreateDto = {
      dueDay: dueDay as number,
      description: state.newDescription,
      value: value as number,
      area: state.newArea,
      note: state.newNote,
    }

    void apiClient
      .createMensaisBill(request)
      .then(() => {
        setStoredDefault(AREA_KEY, state.newArea)
        dispatch({ type: 'ADD_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'ADD_ERROR',
          payload: { message: getErrorMessage(err, 'Failed to add bill'), fields: {} },
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
          payload: getErrorMessage(err, 'Failed to delete bill'),
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
          payload: getErrorMessage(err, 'Failed to reset bills'),
        })
      })
  }

  function performStatusUpdate(id: string, status: string) {
    dispatch({ type: 'UPDATE_STATUS_START', payload: id })

    return apiClient
      .updateMensaisBillStatus(id, { status })
      .then((bill) => {
        dispatch({ type: 'UPDATE_STATUS_SUCCESS', payload: bill })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'UPDATE_STATUS_ERROR',
          payload: getErrorMessage(err, 'Failed to update status'),
        })
        throw err
      })
  }

  function updateBillStatus(id: string, status: string) {
    const bill = state.bills.find((b) => b.id === id)
    const isUkPaidTransition = bill?.area === 'UK' && status === 'Paid' && bill.status !== 'Paid'

    if (bill && isUkPaidTransition) {
      dispatch({ type: 'OPEN_EXPENSE_PROMPT', payload: bill })
      return
    }

    void performStatusUpdate(id, status).catch(() => {
      // Error already recorded in statusUpdateError via performStatusUpdate's own dispatch.
    })
  }

  function confirmExpensePrompt(values: ExpensePromptValues) {
    const bill = state.expensePromptBill
    if (!bill) return

    dispatch({ type: 'EXPENSE_CREATE_START' })

    void apiClient
      .createExpense({
        date: values.date,
        description: values.description,
        value: values.value,
        categoryId: values.categoryId,
        paymentSourceBankId: values.bankId,
        creditCardId: null,
        invoiceDate: null,
        roundUpAmount: null,
        countsAsTithe: true,
      })
      .then(async () => {
        dispatch({ type: 'EXPENSE_CREATE_SUCCESS' })
        try {
          await performStatusUpdate(bill.id, 'Paid')
        } catch {
          // Error already recorded in statusUpdateError; the prompt switches to retry-only mode
          // because expenseCreatedForRetry is now true, and must not create a second Expense.
        }
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'EXPENSE_CREATE_ERROR',
          payload: getErrorMessage(err, 'Failed to create the expense'),
        })
      })
  }

  function skipOrRetryExpensePrompt() {
    const bill = state.expensePromptBill
    if (!bill) return

    void performStatusUpdate(bill.id, 'Paid').catch(() => {
      // Error already recorded in statusUpdateError; the prompt stays open for another attempt.
    })
  }

  const closeExpensePrompt = useCallback(() => dispatch({ type: 'CLOSE_EXPENSE_PROMPT' }), [])

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
    saveErrorFields: state.saveErrorFields,
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
    addErrorFields: state.addErrorFields,
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
    updatingStatusBillId: state.updatingStatusBillId,
    statusUpdateError: state.statusUpdateError,
    updateBillStatus,
    banks: state.banks,
    categories: state.categories,
    expensePromptBill: state.expensePromptBill,
    isCreatingExpense: state.isCreatingExpense,
    expenseCreateError: state.expenseCreateError,
    expenseCreatedForRetry: state.expenseCreatedForRetry,
    confirmExpensePrompt,
    skipOrRetryExpensePrompt,
    closeExpensePrompt,
  }
}
