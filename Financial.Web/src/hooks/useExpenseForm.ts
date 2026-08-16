import { useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { BankDto, CategoryDto, ExpenseDto } from '../api/types'
import { getErrorMessage, parseValidatedNumber } from '../utils/formatters'

export type PaymentMode = 'bank' | 'card'

const SETTLED_STATUS = 'CreditCardSettled'
const CHARGE_STATUS = 'CreditCardCharge'

const MIN_ROUND_UP_AMOUNT = 0
const MAX_ROUND_UP_AMOUNT = 0.99

function computeRoundUpSuggestion(value: number): number {
  return Math.round((Math.ceil(value) - value) * 100) / 100
}

/** Suggests a round-up amount for the given bank/value, or null if not eligible or no value yet. */
function suggestRoundUpAmount(banks: BankDto[], bankId: string, value: string): string | null {
  const bank = banks.find((b) => b.id === bankId)
  if (!bank?.roundUpEnabled) return null

  const parsedValue = Number(value)
  if (!value.trim() || !isFinite(parsedValue) || parsedValue <= 0) return null

  return computeRoundUpSuggestion(parsedValue).toFixed(2)
}

export type CreateFormField =
  | 'createDate'
  | 'createDescription'
  | 'createValue'
  | 'createCategoryId'
  | 'createPaymentSource'
  | 'createCreditCardId'
  | 'createInvoiceDate'
  | 'createRoundUpAmount'
  | 'createCountsAsTithe'
export type EditField =
  | 'editDate'
  | 'editDescription'
  | 'editValue'
  | 'editCategoryId'
  | 'editPaymentSource'
  | 'editCreditCardId'
  | 'editInvoiceDate'
  | 'editRoundUpAmount'
  | 'editCountsAsTithe'

interface ExpenseFormState {
  isCreateFormOpen: boolean
  createDate: string
  createDescription: string
  createValue: string
  createCategoryId: string
  createPaymentSource: string
  createCreditCardId: string
  createInvoiceDate: string
  createRoundUpAmount: string
  createCountsAsTithe: string
  createPaymentMode: PaymentMode
  isCreating: boolean
  createError: string | null
  editingId: string | null
  editDate: string
  editDescription: string
  editValue: string
  editCategoryId: string
  editPaymentSource: string
  editCreditCardId: string
  editCreditCardName: string
  editInvoiceDate: string
  editRoundUpAmount: string
  editCountsAsTithe: string
  editPaymentMode: PaymentMode
  editIsSettled: boolean
  isSaving: boolean
  saveError: string | null
}

type ExpenseFormAction =
  | { type: 'SHOW_CREATE_FORM'; payload: { mode: PaymentMode; paymentSource: string; roundUpAmount: string; categoryId: string } }
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

const BLANK_CREATE_FORM = {
  createDate: '',
  createDescription: '',
  createValue: '',
  createCategoryId: '',
  createPaymentSource: '',
  createCreditCardId: '',
  createInvoiceDate: '',
  createRoundUpAmount: '',
  createCountsAsTithe: 'true',
  createPaymentMode: 'bank',
} as const

const INITIAL_STATE: ExpenseFormState = {
  isCreateFormOpen: false,
  ...BLANK_CREATE_FORM,
  isCreating: false,
  createError: null,
  editingId: null,
  editDate: '',
  editDescription: '',
  editValue: '',
  editCategoryId: '',
  editPaymentSource: '',
  editCreditCardId: '',
  editCreditCardName: '',
  editInvoiceDate: '',
  editRoundUpAmount: '',
  editCountsAsTithe: 'true',
  editPaymentMode: 'bank',
  editIsSettled: false,
  isSaving: false,
  saveError: null,
}

const BLANK_EDIT_FORM = {
  editingId: null,
  editDate: '',
  editDescription: '',
  editValue: '',
  editCategoryId: '',
  editPaymentSource: '',
  editCreditCardId: '',
  editCreditCardName: '',
  editInvoiceDate: '',
  editRoundUpAmount: '',
  editCountsAsTithe: 'true',
  editPaymentMode: 'bank',
  editIsSettled: false,
} as const

function reducer(state: ExpenseFormState, action: ExpenseFormAction): ExpenseFormState {
  switch (action.type) {
    case 'SHOW_CREATE_FORM':
      return {
        ...state,
        ...BLANK_EDIT_FORM,
        isCreateFormOpen: true,
        saveError: null,
        createPaymentMode: action.payload.mode,
        createPaymentSource: action.payload.paymentSource,
        createCreditCardId: '',
        createRoundUpAmount: action.payload.roundUpAmount,
        createCategoryId: action.payload.categoryId,
      }
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
        editCategoryId: action.payload.categoryId,
        editPaymentSource: action.payload.paymentSourceBankId ?? '',
        editCreditCardId: action.payload.creditCardId ?? '',
        editCreditCardName: action.payload.creditCardName ?? '',
        editInvoiceDate: action.payload.invoiceDate ? action.payload.invoiceDate.slice(0, 7) : '',
        editRoundUpAmount: action.payload.roundUpAmount != null ? String(action.payload.roundUpAmount) : '',
        editCountsAsTithe: String(action.payload.countsAsTithe),
        editPaymentMode: action.payload.paymentStatus === CHARGE_STATUS ? 'card' : 'bank',
        editIsSettled: action.payload.paymentStatus === SETTLED_STATUS,
        saveError: null,
      }
    case 'CANCEL_EDIT':
      return { ...state, ...BLANK_EDIT_FORM, saveError: null }
    case 'SET_EDIT_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null }
    case 'SAVE_SUCCESS':
      return { ...state, ...BLANK_EDIT_FORM, isSaving: false }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload }
    default:
      return state
  }
}

export interface UseExpenseFormResult {
  isCreateFormOpen: boolean
  createDate: string
  createDescription: string
  createValue: string
  createCategoryId: string
  createPaymentSource: string
  createCreditCardId: string
  createInvoiceDate: string
  createRoundUpAmount: string
  createCountsAsTithe: string
  createPaymentMode: PaymentMode
  isCreating: boolean
  createError: string | null
  showCreateForm: (mode: PaymentMode) => void
  cancelCreateForm: () => void
  setCreateField: (field: CreateFormField, value: string) => void
  submitCreate: () => void
  editingId: string | null
  editDate: string
  editDescription: string
  editValue: string
  editCategoryId: string
  editPaymentSource: string
  editCreditCardId: string
  editCreditCardName: string
  editInvoiceDate: string
  editRoundUpAmount: string
  editCountsAsTithe: string
  editPaymentMode: PaymentMode
  editIsSettled: boolean
  isSaving: boolean
  saveError: string | null
  setEditField: (field: EditField, value: string) => void
  showEditForm: (expense: ExpenseDto) => void
  cancelEdit: () => void
  saveEdit: () => void
}

/** Owns an expense's create/edit form state and orchestrates the create/update endpoints. */
export function useExpenseForm(banks: BankDto[], categories: CategoryDto[], onSaved: () => void): UseExpenseFormResult {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  function showCreateForm(mode: PaymentMode) {
    const paymentSource = mode === 'bank' ? (banks[0]?.id ?? '') : ''
    const suggestion = mode === 'bank' ? suggestRoundUpAmount(banks, paymentSource, state.createValue) : null
    const categoryId = state.createCategoryId || (categories.find((c) => c.active)?.id ?? '')
    dispatch({
      type: 'SHOW_CREATE_FORM',
      payload: { mode, paymentSource, roundUpAmount: suggestion ?? '', categoryId },
    })
  }

  const cancelCreateForm = () => dispatch({ type: 'CANCEL_CREATE_FORM' })

  function setCreateField(field: CreateFormField, value: string) {
    dispatch({ type: 'SET_CREATE_FIELD', payload: { field, value } })
    if (field === 'createPaymentSource' && state.createRoundUpAmount.trim() === '') {
      const suggestion = suggestRoundUpAmount(banks, value, state.createValue)
      if (suggestion !== null) {
        dispatch({ type: 'SET_CREATE_FIELD', payload: { field: 'createRoundUpAmount', value: suggestion } })
      }
    }
  }

  function submitCreate() {
    const {
      createDate,
      createDescription,
      createValue,
      createCategoryId,
      createPaymentMode,
      createPaymentSource,
      createCreditCardId,
      createInvoiceDate,
      createRoundUpAmount,
      createCountsAsTithe,
    } = state

    if (!createDate.trim()) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Date is required' })
      return
    }

    if (!createDescription.trim()) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Description is required' })
      return
    }

    const value = parseValidatedNumber(createValue)
    if (value === null || value === 0) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Value must be a non-zero number' })
      return
    }

    if (createPaymentMode === 'card' && createCreditCardId.trim() === '') {
      dispatch({ type: 'CREATE_ERROR', payload: 'Card is required' })
      return
    }

    const selectedBank = banks.find((b) => b.id === createPaymentSource)
    const roundUpEligible = createPaymentMode === 'bank' && selectedBank?.roundUpEnabled === true

    let roundUpAmount: number | null = null
    if (roundUpEligible && createRoundUpAmount.trim() !== '') {
      roundUpAmount = parseValidatedNumber(createRoundUpAmount, { min: MIN_ROUND_UP_AMOUNT, max: MAX_ROUND_UP_AMOUNT })
      if (roundUpAmount === null) {
        dispatch({
          type: 'CREATE_ERROR',
          payload: `Round-up amount must be between £${MIN_ROUND_UP_AMOUNT.toFixed(2)} and £${MAX_ROUND_UP_AMOUNT.toFixed(2)}`,
        })
        return
      }
    }

    dispatch({ type: 'CREATE_START' })

    void apiClient
      .createExpense({
        date: createDate,
        description: createDescription,
        value,
        categoryId: createCategoryId,
        paymentSourceBankId: createPaymentMode === 'bank' ? createPaymentSource : null,
        creditCardId: createPaymentMode === 'card' ? createCreditCardId : null,
        invoiceDate: createPaymentMode === 'card' && createInvoiceDate ? `${createInvoiceDate}-01` : null,
        roundUpAmount,
        countsAsTithe: createCountsAsTithe === 'true',
      })
      .then(() => {
        dispatch({ type: 'CREATE_SUCCESS' })
        onSaved()
      })
      .catch((err: unknown) => {
        dispatch({ type: 'CREATE_ERROR', payload: getErrorMessage(err, 'Failed to create expense') })
      })
  }

  const setEditField = (field: EditField, value: string) => {
    dispatch({ type: 'SET_EDIT_FIELD', payload: { field, value } })
    if (field === 'editPaymentSource' && state.editRoundUpAmount.trim() === '') {
      const suggestion = suggestRoundUpAmount(banks, value, state.editValue)
      if (suggestion !== null) {
        dispatch({ type: 'SET_EDIT_FIELD', payload: { field: 'editRoundUpAmount', value: suggestion } })
      }
    }
  }

  const showEditForm = (expense: ExpenseDto) => dispatch({ type: 'SHOW_EDIT_FORM', payload: expense })

  const cancelEdit = () => dispatch({ type: 'CANCEL_EDIT' })

  function saveEdit() {
    if (!state.editingId) return

    const value = parseValidatedNumber(state.editValue)
    if (value === null || value === 0) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Value must be a non-zero number' })
      return
    }

    if (!state.editIsSettled && state.editPaymentMode === 'card' && state.editCreditCardId.trim() === '') {
      dispatch({ type: 'SAVE_ERROR', payload: 'Card is required' })
      return
    }

    // A settled expense's payment fields are frozen: send them back unchanged so only
    // the editable fields move; the server rejects any payment-field change until the
    // statement is unmarked paid.
    const paymentFields = state.editIsSettled
      ? {
          paymentSourceBankId: state.editPaymentSource.trim() === '' ? null : state.editPaymentSource,
          creditCardId: state.editCreditCardId.trim() === '' ? null : state.editCreditCardId,
        }
      : {
          paymentSourceBankId: state.editPaymentMode === 'bank' ? state.editPaymentSource : null,
          creditCardId: state.editPaymentMode === 'card' ? state.editCreditCardId : null,
        }

    const selectedBank = banks.find((b) => b.id === state.editPaymentSource)
    const roundUpEligible =
      !state.editIsSettled && state.editPaymentMode === 'bank' && selectedBank?.roundUpEnabled === true

    let roundUpAmount: number | null = null
    if (roundUpEligible && state.editRoundUpAmount.trim() !== '') {
      roundUpAmount = parseValidatedNumber(state.editRoundUpAmount, { min: MIN_ROUND_UP_AMOUNT, max: MAX_ROUND_UP_AMOUNT })
      if (roundUpAmount === null) {
        dispatch({
          type: 'SAVE_ERROR',
          payload: `Round-up amount must be between £${MIN_ROUND_UP_AMOUNT.toFixed(2)} and £${MAX_ROUND_UP_AMOUNT.toFixed(2)}`,
        })
        return
      }
    }

    dispatch({ type: 'SAVE_START' })

    void apiClient
      .updateExpense(state.editingId, {
        date: state.editDate,
        description: state.editDescription,
        value,
        categoryId: state.editCategoryId,
        ...paymentFields,
        invoiceDate: state.editPaymentMode === 'card' && state.editInvoiceDate ? `${state.editInvoiceDate}-01` : null,
        roundUpAmount,
        countsAsTithe: state.editCountsAsTithe === 'true',
      })
      .then(() => {
        dispatch({ type: 'SAVE_SUCCESS' })
        onSaved()
      })
      .catch((err: unknown) => {
        dispatch({ type: 'SAVE_ERROR', payload: getErrorMessage(err, 'Failed to update expense') })
      })
  }

  return {
    isCreateFormOpen: state.isCreateFormOpen,
    createDate: state.createDate,
    createDescription: state.createDescription,
    createValue: state.createValue,
    createCategoryId: state.createCategoryId,
    createPaymentSource: state.createPaymentSource,
    createCreditCardId: state.createCreditCardId,
    createInvoiceDate: state.createInvoiceDate,
    createRoundUpAmount: state.createRoundUpAmount,
    createCountsAsTithe: state.createCountsAsTithe,
    createPaymentMode: state.createPaymentMode,
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
    editCategoryId: state.editCategoryId,
    editPaymentSource: state.editPaymentSource,
    editCreditCardId: state.editCreditCardId,
    editCreditCardName: state.editCreditCardName,
    editInvoiceDate: state.editInvoiceDate,
    editRoundUpAmount: state.editRoundUpAmount,
    editCountsAsTithe: state.editCountsAsTithe,
    editPaymentMode: state.editPaymentMode,
    editIsSettled: state.editIsSettled,
    isSaving: state.isSaving,
    saveError: state.saveError,
    setEditField,
    showEditForm,
    cancelEdit,
    saveEdit,
  }
}
