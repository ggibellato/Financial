import { useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { ExpenseFormField } from '../components/ExpenseForm'
import type { BankOption, CategoryDto, ExpenseDto } from '../api/types'
import { getSaveErrorMessage, parseValidatedNumber, positiveAmount, todayIsoDate } from '../utils/formatters'
import { getCreateFormDate, getStoredDefault, setStoredDefault } from '../utils/createFormDefaults'
import { computeDefaultInvoiceMonth } from '../utils/expenseDefaults'
import { selectableBanks } from '../utils/selectableBanks'
import type { AccountFormSettings } from './formSettings'
import { hasChangedFields, snapshotFields } from './inlineFormSlice'
import { isCardChargeStatus, isLockedByPaidStatement } from '../utils/expensePaymentStatus'

export type PaymentMode = 'bank' | 'card'

const MIN_ROUND_UP_AMOUNT = 0
const MAX_ROUND_UP_AMOUNT = 0.99

const DATE_KEY = 'expense.date'
const PAYMENT_SOURCE_KEY = 'expense.paymentSource'
const MISSING_CARD_MESSAGE = "Add charges from the card's account screen."
const CATEGORY_KEY = 'expense.categoryId'

export function computeRoundUpSuggestion(value: number): number {
  return Math.round((Math.ceil(value) - value) * 100) / 100
}

function notAfterToday(date: string): string {
  const today = todayIsoDate()
  return date > today ? today : date
}

function suggestRoundUpAmount(banks: BankOption[], bankId: string, value: string): string | null {
  const bank = banks.find((b) => b.id === bankId)
  if (!bank?.roundUpEnabled) return null

  const parsedValue = Number(value)
  if (!value.trim() || !isFinite(parsedValue) || parsedValue <= 0) return null

  return computeRoundUpSuggestion(parsedValue).toFixed(2)
}

const EDITABLE_FIELDS = [
  'date',
  'description',
  'value',
  'categoryId',
  'paymentSource',
  'creditCardId',
  'invoiceDate',
  'roundUpAmount',
  'countsAsTithe',
  'paymentMode',
] as const

type EditableValues = Pick<ExpenseFormState, (typeof EDITABLE_FIELDS)[number]>

function withOpenedValues(state: ExpenseFormState): ExpenseFormState {
  return { ...state, openedValues: snapshotFields(state, EDITABLE_FIELDS) }
}

interface ExpenseFormState {
  openedValues: EditableValues | null
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
  // False until the user directly edits the invoice field - mirrors ExpenseWorkflowViewModel's
  // _invoiceDateTouchedByUser.
  invoiceDateTouched: boolean
  roundUpAmount: string
  // True until the user directly edits the round-up field, or an edit form loads a saved
  // amount - both "freeze" it so later Value/PaymentSource edits stop recomputing it.
  roundUpAmountAuto: boolean
  countsAsTithe: string
  paymentMode: PaymentMode
  isRefund: boolean
  isSettled: boolean
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<ExpenseFormField, string>>
}

type ExpenseFormAction =
  | {
      type: 'SHOW_CREATE_FORM'
      payload: { mode: PaymentMode; isRefund: boolean; date: string; paymentSource: string; creditCardId: string; roundUpAmount: string; categoryId: string }
    }
  | { type: 'SHOW_EDIT_FORM'; payload: { expense: ExpenseDto; isRefund: boolean } }
  | { type: 'CANCEL_FORM' }
  | { type: 'SET_FIELD'; payload: { field: ExpenseFormField; value: string } }
  | { type: 'SET_ROUND_UP_SUGGESTION'; payload: { value: string } }
  | { type: 'SET_INVOICE_DATE_DEFAULT'; payload: { value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS' }
  | { type: 'SAVE_SUCCESS_KEEP_OPEN' }
  | { type: 'SAVE_ERROR'; payload: { message: string | null; fields: Partial<Record<ExpenseFormField, string>> } }

const BLANK_ENTRY = {
  description: '',
  value: '',
  roundUpAmount: '',
  roundUpAmountAuto: true,
  countsAsTithe: 'true',
} as const

const BLANK_FORM = {
  ...BLANK_ENTRY,
  date: '',
  categoryId: '',
  paymentSource: '',
  creditCardId: '',
  creditCardName: '',
  invoiceDate: '',
  invoiceDateTouched: false,
  paymentMode: 'bank' as PaymentMode,
  isRefund: false,
  isSettled: false,
} as const

const INITIAL_STATE: ExpenseFormState = {
  openedValues: null,
  isOpen: false,
  isEditing: false,
  editingId: null,
  ...BLANK_FORM,
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
}

function reducer(state: ExpenseFormState, action: ExpenseFormAction): ExpenseFormState {
  switch (action.type) {
    case 'SHOW_CREATE_FORM':
      return withOpenedValues({
        ...state,
        ...BLANK_FORM,
        isOpen: true,
        isEditing: false,
        editingId: null,
        saveError: null,
        saveErrorFields: {},
        date: action.payload.date,
        paymentMode: action.payload.mode,
        isRefund: action.payload.isRefund,
        paymentSource: action.payload.paymentSource,
        creditCardId: action.payload.creditCardId,
        roundUpAmount: action.payload.roundUpAmount,
        categoryId: action.payload.categoryId,
      })
    case 'SHOW_EDIT_FORM': {
      const { expense, isRefund } = action.payload
      return withOpenedValues({
        ...state,
        isOpen: true,
        isEditing: true,
        editingId: expense.id,
        date: expense.date,
        description: expense.description,
        value: String(isRefund ? -expense.value : expense.value),
        categoryId: expense.categoryId,
        paymentSource: expense.paymentSourceBankId ?? '',
        creditCardId: expense.creditCardId ?? '',
        creditCardName: expense.creditCardName ?? '',
        invoiceDate: expense.invoiceDate ? expense.invoiceDate.slice(0, 7) : '',
        invoiceDateTouched: false,
        roundUpAmount: expense.roundUpAmount != null ? String(expense.roundUpAmount) : '',
        // A saved amount is frozen (not auto-recomputed), same as a user-typed one, so
        // re-editing Value/PaymentSource here doesn't silently change what was saved.
        roundUpAmountAuto: expense.roundUpAmount == null,
        countsAsTithe: String(expense.countsAsTithe),
        paymentMode: isCardChargeStatus(expense.paymentStatus) ? 'card' : 'bank',
        isRefund,
        isSettled: isLockedByPaidStatement(expense.paymentStatus),
        saveError: null,
        saveErrorFields: {},
      })
    }
    case 'CANCEL_FORM':
      return { ...state, ...BLANK_FORM, isOpen: false, isEditing: false, editingId: null, saveError: null, saveErrorFields: {} }
    case 'SET_FIELD':
      if (action.payload.field === 'roundUpAmount') {
        return { ...state, roundUpAmount: action.payload.value, roundUpAmountAuto: false }
      }
      if (action.payload.field === 'invoiceDate') {
        return { ...state, invoiceDate: action.payload.value, invoiceDateTouched: true }
      }
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SET_ROUND_UP_SUGGESTION':
      return { ...state, roundUpAmount: action.payload.value, roundUpAmountAuto: true }
    case 'SET_INVOICE_DATE_DEFAULT':
      return { ...state, invoiceDate: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null, saveErrorFields: {} }
    case 'SAVE_SUCCESS':
      return { ...state, ...BLANK_FORM, isOpen: false, isEditing: false, editingId: null, isSaving: false }
    case 'SAVE_SUCCESS_KEEP_OPEN':
      return withOpenedValues({ ...state, ...BLANK_ENTRY, isSaving: false })
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload.message, saveErrorFields: action.payload.fields }
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
  isRefund: boolean
  isSettled: boolean
  isSaving: boolean
  isDirty: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<ExpenseFormField, string>>
  showCreateForm: (mode: PaymentMode) => void
  showCreateRefundForm: () => void
  showEditForm: (expense: ExpenseDto) => void
  cancelForm: () => void
  setField: (field: ExpenseFormField, value: string) => void
  submit: () => void
}

export function useExpenseForm(
  banks: BankOption[],
  categories: CategoryDto[],
  onSaved: (isEdit: boolean, isRefund: boolean) => void,
  { lockedBank, lockedCard, keepOpenAfterSave = false, viewedMonth }: AccountFormSettings = {},
): UseExpenseFormResult {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  function defaultPaymentSource(): string {
    if (lockedBank) return lockedBank.id
    const stored = getStoredDefault(PAYMENT_SOURCE_KEY)
    const activeBanks = selectableBanks(banks)
    if (stored && activeBanks.some((b) => b.id === stored)) return stored
    return activeBanks[0]?.id ?? ''
  }

  function openCreateForm(requestedMode: PaymentMode, isRefund: boolean) {
    const mode = lockedBank ? 'bank' : lockedCard ? 'card' : requestedMode
    const date = getCreateFormDate(DATE_KEY, viewedMonth)
    const paymentSource = mode === 'bank' ? defaultPaymentSource() : ''
    const creditCardId = mode === 'card' ? (lockedCard?.id ?? '') : ''
    const suggestion = mode === 'bank' ? suggestRoundUpAmount(banks, paymentSource, state.value) : null
    const storedCategoryId = getStoredDefault(CATEGORY_KEY)
    const categoryId =
      state.categoryId ||
      (storedCategoryId && categories.some((c) => c.id === storedCategoryId) ? storedCategoryId : (categories.find((c) => c.active)?.id ?? ''))
    dispatch({
      type: 'SHOW_CREATE_FORM',
      payload: { mode, isRefund, date: isRefund ? notAfterToday(date) : date, paymentSource, creditCardId, roundUpAmount: suggestion ?? '', categoryId },
    })
  }

  const showCreateForm = (requestedMode: PaymentMode) => openCreateForm(requestedMode, false)

  const showCreateRefundForm = () => openCreateForm('card', true)

  const showEditForm = (expense: ExpenseDto) =>
    dispatch({ type: 'SHOW_EDIT_FORM', payload: { expense, isRefund: lockedCard !== undefined && expense.value < 0 } })

  const cancelForm = () => dispatch({ type: 'CANCEL_FORM' })

  function setField(field: ExpenseFormField, value: string) {
    dispatch({ type: 'SET_FIELD', payload: { field, value } })
    if ((field === 'paymentSource' || field === 'value') && state.roundUpAmountAuto) {
      const bankId = field === 'paymentSource' ? value : state.paymentSource
      const expenseValue = field === 'value' ? value : state.value
      const suggestion = suggestRoundUpAmount(banks, bankId, expenseValue)
      dispatch({ type: 'SET_ROUND_UP_SUGGESTION', payload: { value: suggestion ?? '' } })
    }
    if (field === 'date' && state.paymentMode === 'card' && !state.invoiceDateTouched) {
      dispatch({ type: 'SET_INVOICE_DATE_DEFAULT', payload: { value: computeDefaultInvoiceMonth(value) } })
    }
  }

  function submit() {
    const errors: Partial<Record<ExpenseFormField, string>> = {}

    const requiresDateAndDescription = !state.isEditing || state.isRefund
    if (requiresDateAndDescription && !state.date.trim()) {
      errors.date = 'Date is required.'
    } else if (state.isRefund && state.date > todayIsoDate()) {
      errors.date = 'Date must not be in the future.'
    }

    if (requiresDateAndDescription && !state.description.trim()) {
      errors.description = 'Description is required.'
    }

    const value = state.isRefund ? positiveAmount(state.value) : parseValidatedNumber(state.value)
    if (value === null || value === 0) {
      errors.value = state.isRefund ? 'Amount must be greater than zero.' : 'Value must be a non-zero number.'
    }

    const selectedBank = banks.find((b) => b.id === state.paymentSource)
    const roundUpEligible = !state.isSettled && state.paymentMode === 'bank' && selectedBank?.roundUpEnabled === true

    let roundUpAmount: number | null = null
    if (roundUpEligible && state.roundUpAmount.trim() !== '') {
      roundUpAmount = parseValidatedNumber(state.roundUpAmount, { min: MIN_ROUND_UP_AMOUNT, max: MAX_ROUND_UP_AMOUNT })
      if (roundUpAmount === null) {
        errors.roundUpAmount = `Round-up amount must be between £${MIN_ROUND_UP_AMOUNT.toFixed(2)} and £${MAX_ROUND_UP_AMOUNT.toFixed(2)}.`
      }
    }

    const isMissingCard = !state.isSettled && state.paymentMode === 'card' && state.creditCardId.trim() === ''
    if (Object.keys(errors).length > 0 || isMissingCard) {
      dispatch({ type: 'SAVE_ERROR', payload: { message: Object.values(errors)[0] ?? MISSING_CARD_MESSAGE, fields: errors } })
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
          paymentSourceBankId: state.paymentMode === 'bank' ? (lockedBank?.id ?? state.paymentSource) : null,
          creditCardId: state.paymentMode === 'card' ? state.creditCardId : null,
        }

    dispatch({ type: 'SAVE_START' })

    const payload = {
      date: state.date,
      description: state.description,
      value: state.isRefund ? -(value as number) : (value as number),
      categoryId: state.categoryId,
      ...paymentFields,
      invoiceDate:
        state.paymentMode === 'card'
          ? `${state.invoiceDate || computeDefaultInvoiceMonth(state.date)}-01`
          : null,
      roundUpAmount,
      countsAsTithe: state.countsAsTithe === 'true',
    }

    const request =
      state.isEditing && state.editingId ? apiClient.updateExpense(state.editingId, payload) : apiClient.createExpense(payload)

    void request
      .then(() => {
        setStoredDefault(DATE_KEY, state.date)
        if (state.paymentMode === 'bank' && !lockedBank) {
          setStoredDefault(PAYMENT_SOURCE_KEY, state.paymentSource)
        }
        setStoredDefault(CATEGORY_KEY, state.categoryId)
        dispatch({ type: keepOpenAfterSave && !state.isEditing ? 'SAVE_SUCCESS_KEEP_OPEN' : 'SAVE_SUCCESS' })
        onSaved(state.isEditing, state.isRefund)
      })
      .catch((err: unknown) => {
        const record = state.isRefund ? 'refund' : 'expense'
        dispatch({
          type: 'SAVE_ERROR',
          payload: {
            message: getSaveErrorMessage(err, state.isEditing ? `Failed to update ${record}.` : `Failed to create ${record}.`),
            fields: {},
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
    isRefund: state.isRefund,
    isSettled: state.isSettled,
    isSaving: state.isSaving,
    isDirty: state.isOpen && hasChangedFields(state, state.openedValues, EDITABLE_FIELDS),
    saveError: state.saveError,
    saveErrorFields: state.saveErrorFields,
    showCreateForm,
    showCreateRefundForm,
    showEditForm,
    cancelForm,
    setField,
    submit,
  }
}
