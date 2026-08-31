import { useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { ExpenseFormField } from '../components/ExpenseForm'
import type { BankDto, CategoryDto, CreditCardDto, ExpenseDto } from '../api/types'
import { getErrorMessage, parseValidatedNumber, todayIsoDate } from '../utils/formatters'
import { getStoredDefault, setStoredDefault } from '../utils/createFormDefaults'
import { computeDefaultInvoiceMonth } from '../utils/expenseDefaults'

export type PaymentMode = 'bank' | 'card'

const SETTLED_STATUS = 'CreditCardSettled'
const CHARGE_STATUS = 'CreditCardCharge'

const MIN_ROUND_UP_AMOUNT = 0
const MAX_ROUND_UP_AMOUNT = 0.99

const DATE_KEY = 'expense.date'
const PAYMENT_SOURCE_KEY = 'expense.paymentSource'
const CREDIT_CARD_KEY = 'expense.creditCardId'
const CATEGORY_KEY = 'expense.categoryId'

function computeRoundUpSuggestion(value: number): number {
  return Math.round((Math.ceil(value) - value) * 100) / 100
}

function suggestRoundUpAmount(banks: BankDto[], bankId: string, value: string): string | null {
  const bank = banks.find((b) => b.id === bankId)
  if (!bank?.roundUpEnabled) return null

  const parsedValue = Number(value)
  if (!value.trim() || !isFinite(parsedValue) || parsedValue <= 0) return null

  return computeRoundUpSuggestion(parsedValue).toFixed(2)
}

interface ExpenseFormState {
  isOpen: boolean
  isEditing: boolean
  editingId: string | null
  date: string
  description: string
  value: string
  categoryId: string
  paymentSource: string
  creditCardId: string
  creditCardName: string
  invoiceDate: string
  roundUpAmount: string
  // True until the user directly edits the round-up field, or an edit form loads a saved
  // amount - both "freeze" it so later Value/PaymentSource edits stop recomputing it.
  roundUpAmountAuto: boolean
  countsAsTithe: string
  paymentMode: PaymentMode
  isSettled: boolean
  isSaving: boolean
  saveError: string | null
  saveErrorField: ExpenseFormField | null
}

type ExpenseFormAction =
  | {
      type: 'SHOW_CREATE_FORM'
      payload: { mode: PaymentMode; date: string; paymentSource: string; creditCardId: string; roundUpAmount: string; categoryId: string }
    }
  | { type: 'SHOW_EDIT_FORM'; payload: ExpenseDto }
  | { type: 'CANCEL_FORM' }
  | { type: 'SET_FIELD'; payload: { field: ExpenseFormField; value: string } }
  | { type: 'SET_ROUND_UP_SUGGESTION'; payload: { value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS' }
  | { type: 'SAVE_ERROR'; payload: { message: string; field: ExpenseFormField | null } }

const BLANK_FORM = {
  date: '',
  description: '',
  value: '',
  categoryId: '',
  paymentSource: '',
  creditCardId: '',
  creditCardName: '',
  invoiceDate: '',
  roundUpAmount: '',
  roundUpAmountAuto: true,
  countsAsTithe: 'true',
  paymentMode: 'bank' as PaymentMode,
  isSettled: false,
} as const

const INITIAL_STATE: ExpenseFormState = {
  isOpen: false,
  isEditing: false,
  editingId: null,
  ...BLANK_FORM,
  isSaving: false,
  saveError: null,
  saveErrorField: null,
}

function reducer(state: ExpenseFormState, action: ExpenseFormAction): ExpenseFormState {
  switch (action.type) {
    case 'SHOW_CREATE_FORM':
      return {
        ...state,
        ...BLANK_FORM,
        isOpen: true,
        isEditing: false,
        editingId: null,
        saveError: null,
        saveErrorField: null,
        date: action.payload.date,
        paymentMode: action.payload.mode,
        paymentSource: action.payload.paymentSource,
        creditCardId: action.payload.creditCardId,
        roundUpAmount: action.payload.roundUpAmount,
        categoryId: action.payload.categoryId,
      }
    case 'SHOW_EDIT_FORM':
      return {
        ...state,
        isOpen: true,
        isEditing: true,
        editingId: action.payload.id,
        date: action.payload.date,
        description: action.payload.description,
        value: String(action.payload.value),
        categoryId: action.payload.categoryId,
        paymentSource: action.payload.paymentSourceBankId ?? '',
        creditCardId: action.payload.creditCardId ?? '',
        creditCardName: action.payload.creditCardName ?? '',
        invoiceDate: action.payload.invoiceDate ? action.payload.invoiceDate.slice(0, 7) : '',
        roundUpAmount: action.payload.roundUpAmount != null ? String(action.payload.roundUpAmount) : '',
        // A saved amount is frozen (not auto-recomputed), same as a user-typed one, so
        // re-editing Value/PaymentSource here doesn't silently change what was saved.
        roundUpAmountAuto: action.payload.roundUpAmount == null,
        countsAsTithe: String(action.payload.countsAsTithe),
        paymentMode: action.payload.paymentStatus === CHARGE_STATUS ? 'card' : 'bank',
        isSettled: action.payload.paymentStatus === SETTLED_STATUS,
        saveError: null,
        saveErrorField: null,
      }
    case 'CANCEL_FORM':
      return { ...state, ...BLANK_FORM, isOpen: false, isEditing: false, editingId: null, saveError: null, saveErrorField: null }
    case 'SET_FIELD':
      return action.payload.field === 'roundUpAmount'
        ? { ...state, roundUpAmount: action.payload.value, roundUpAmountAuto: false }
        : { ...state, [action.payload.field]: action.payload.value }
    case 'SET_ROUND_UP_SUGGESTION':
      return { ...state, roundUpAmount: action.payload.value, roundUpAmountAuto: true }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null, saveErrorField: null }
    case 'SAVE_SUCCESS':
      return { ...state, ...BLANK_FORM, isOpen: false, isEditing: false, editingId: null, isSaving: false }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload.message, saveErrorField: action.payload.field }
    default:
      return state
  }
}

export interface UseExpenseFormResult {
  isOpen: boolean
  isEditing: boolean
  editingId: string | null
  date: string
  description: string
  value: string
  categoryId: string
  paymentSource: string
  creditCardId: string
  creditCardName: string
  invoiceDate: string
  roundUpAmount: string
  countsAsTithe: string
  paymentMode: PaymentMode
  isSettled: boolean
  isSaving: boolean
  saveError: string | null
  saveErrorField: ExpenseFormField | null
  showCreateForm: (mode: PaymentMode) => void
  showEditForm: (expense: ExpenseDto) => void
  cancelForm: () => void
  setField: (field: ExpenseFormField, value: string) => void
  submit: () => void
}

export function useExpenseForm(
  banks: BankDto[],
  categories: CategoryDto[],
  creditCards: CreditCardDto[],
  onSaved: () => void,
): UseExpenseFormResult {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  function showCreateForm(mode: PaymentMode) {
    const date = getStoredDefault(DATE_KEY) ?? todayIsoDate()
    const storedPaymentSource = getStoredDefault(PAYMENT_SOURCE_KEY)
    const paymentSource =
      mode === 'bank' ? (storedPaymentSource && banks.some((b) => b.id === storedPaymentSource) ? storedPaymentSource : (banks[0]?.id ?? '')) : ''
    const creditCardId = mode === 'card' ? (getStoredDefault(CREDIT_CARD_KEY) ?? '') : ''
    const suggestion = mode === 'bank' ? suggestRoundUpAmount(banks, paymentSource, state.value) : null
    const storedCategoryId = getStoredDefault(CATEGORY_KEY)
    const categoryId =
      state.categoryId ||
      (storedCategoryId && categories.some((c) => c.id === storedCategoryId) ? storedCategoryId : (categories.find((c) => c.active)?.id ?? ''))
    dispatch({
      type: 'SHOW_CREATE_FORM',
      payload: { mode, date, paymentSource, creditCardId, roundUpAmount: suggestion ?? '', categoryId },
    })
  }

  const showEditForm = (expense: ExpenseDto) => dispatch({ type: 'SHOW_EDIT_FORM', payload: expense })

  const cancelForm = () => dispatch({ type: 'CANCEL_FORM' })

  function setField(field: ExpenseFormField, value: string) {
    dispatch({ type: 'SET_FIELD', payload: { field, value } })
    if ((field === 'paymentSource' || field === 'value') && state.roundUpAmountAuto) {
      const bankId = field === 'paymentSource' ? value : state.paymentSource
      const expenseValue = field === 'value' ? value : state.value
      const suggestion = suggestRoundUpAmount(banks, bankId, expenseValue)
      dispatch({ type: 'SET_ROUND_UP_SUGGESTION', payload: { value: suggestion ?? '' } })
    }
  }

  function submit() {
    if (!state.isEditing) {
      if (!state.date.trim()) {
        dispatch({ type: 'SAVE_ERROR', payload: { message: 'Date is required', field: 'date' } })
        return
      }

      if (!state.description.trim()) {
        dispatch({ type: 'SAVE_ERROR', payload: { message: 'Description is required', field: 'description' } })
        return
      }
    }

    const value = parseValidatedNumber(state.value)
    if (value === null || value === 0) {
      dispatch({ type: 'SAVE_ERROR', payload: { message: 'Value must be a non-zero number', field: 'value' } })
      return
    }

    if (!state.isSettled && state.paymentMode === 'card' && state.creditCardId.trim() === '') {
      dispatch({ type: 'SAVE_ERROR', payload: { message: 'Card is required', field: 'creditCardId' } })
      return
    }

    // A settled expense's payment fields are frozen: send them back unchanged so only
    // the editable fields move; the server rejects any payment-field change until the
    // statement is unmarked paid. isSettled is always false while creating.
    const paymentFields = state.isSettled
      ? {
          paymentSourceBankId: state.paymentSource.trim() === '' ? null : state.paymentSource,
          creditCardId: state.creditCardId.trim() === '' ? null : state.creditCardId,
        }
      : {
          paymentSourceBankId: state.paymentMode === 'bank' ? state.paymentSource : null,
          creditCardId: state.paymentMode === 'card' ? state.creditCardId : null,
        }

    const selectedBank = banks.find((b) => b.id === state.paymentSource)
    const roundUpEligible = !state.isSettled && state.paymentMode === 'bank' && selectedBank?.roundUpEnabled === true

    let roundUpAmount: number | null = null
    if (roundUpEligible && state.roundUpAmount.trim() !== '') {
      roundUpAmount = parseValidatedNumber(state.roundUpAmount, { min: MIN_ROUND_UP_AMOUNT, max: MAX_ROUND_UP_AMOUNT })
      if (roundUpAmount === null) {
        dispatch({
          type: 'SAVE_ERROR',
          payload: {
            message: `Round-up amount must be between £${MIN_ROUND_UP_AMOUNT.toFixed(2)} and £${MAX_ROUND_UP_AMOUNT.toFixed(2)}`,
            field: 'roundUpAmount',
          },
        })
        return
      }
    }

    dispatch({ type: 'SAVE_START' })

    const payload = {
      date: state.date,
      description: state.description,
      value,
      categoryId: state.categoryId,
      ...paymentFields,
      invoiceDate:
        state.paymentMode === 'card'
          ? `${state.invoiceDate || computeDefaultInvoiceMonth(state.date, state.creditCardId, creditCards)}-01`
          : null,
      roundUpAmount,
      countsAsTithe: state.countsAsTithe === 'true',
    }

    const request =
      state.isEditing && state.editingId ? apiClient.updateExpense(state.editingId, payload) : apiClient.createExpense(payload)

    void request
      .then(() => {
        setStoredDefault(DATE_KEY, state.date)
        if (state.paymentMode === 'bank') {
          setStoredDefault(PAYMENT_SOURCE_KEY, state.paymentSource)
        } else {
          setStoredDefault(CREDIT_CARD_KEY, state.creditCardId)
        }
        setStoredDefault(CATEGORY_KEY, state.categoryId)
        dispatch({ type: 'SAVE_SUCCESS' })
        onSaved()
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: {
            message: getErrorMessage(err, state.isEditing ? 'Failed to update expense' : 'Failed to create expense'),
            field: null,
          },
        })
      })
  }

  return {
    isOpen: state.isOpen,
    isEditing: state.isEditing,
    editingId: state.editingId,
    date: state.date,
    description: state.description,
    value: state.value,
    categoryId: state.categoryId,
    paymentSource: state.paymentSource,
    creditCardId: state.creditCardId,
    creditCardName: state.creditCardName,
    invoiceDate: state.invoiceDate,
    roundUpAmount: state.roundUpAmount,
    countsAsTithe: state.countsAsTithe,
    paymentMode: state.paymentMode,
    isSettled: state.isSettled,
    isSaving: state.isSaving,
    saveError: state.saveError,
    saveErrorField: state.saveErrorField,
    showCreateForm,
    showEditForm,
    cancelForm,
    setField,
    submit,
  }
}
