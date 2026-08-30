import { useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { IncomeFormField } from '../components/IncomeForm'
import type { IncomeDto, IncomeSourceDto } from '../api/types'
import { getErrorMessage, parseValidatedNumber, todayIsoDate } from '../utils/formatters'
import { getStoredDefault, setStoredDefault } from '../utils/createFormDefaults'

export const INCOME_SOURCES_WITH_GROSS_VALUE = ['Gleison', 'Ariana']

const DATE_KEY = 'income.date'
const BANK_KEY = 'income.bank'
const SOURCE_KEY = 'income.incomeSource'

/** How long the post-save "split to reserve" confirmation stays visible before clearing itself. */
export const SPLIT_CONFIRMATION_DELAY_MS = 4000

/** Matches the previous hardcoded INCOME_SOURCES array's declaration order. Names outside this
 * list (unexpected but not invalid) sort last rather than being dropped or erroring. */
const INCOME_SOURCE_DISPLAY_ORDER = ['Gleison', 'Ariana', 'Lottery', 'DividendoJuros']

export function selectActiveIncomeSources(sources: IncomeSourceDto[]): IncomeSourceDto[] {
  return sources
    .filter((s) => s.isActive)
    .slice()
    .sort((a, b) => {
      const rank = (name: string) => {
        const index = INCOME_SOURCE_DISPLAY_ORDER.indexOf(name)
        return index === -1 ? INCOME_SOURCE_DISPLAY_ORDER.length : index
      }
      return rank(a.name) - rank(b.name)
    })
}

function resolveIncomeSourceName(sources: IncomeSourceDto[], incomeSourceId: string): string {
  return sources.find((s) => s.id === incomeSourceId)?.name ?? ''
}

function resolveAutoSplitToReserve(sources: IncomeSourceDto[], incomeSourceId: string): boolean {
  return sources.find((s) => s.id === incomeSourceId)?.autoSplitToReserve === true
}

interface IncomeFormState {
  isOpen: boolean
  isEditing: boolean
  editingId: string | null
  date: string
  incomeSource: string
  grossValue: string
  netValue: string
  bank: string
  description: string
  splitToReserve: string
  isSaving: boolean
  saveError: string | null
  saveErrorField: IncomeFormField | null
  splitConfirmationMessage: string | null
}

type IncomeFormAction =
  | { type: 'SHOW_CREATE_FORM'; payload: { date: string; bank: string; source: string; splitToReserve: string } }
  | { type: 'SHOW_EDIT_FORM'; payload: IncomeDto }
  | { type: 'CANCEL_FORM' }
  | { type: 'SET_FIELD'; payload: { field: IncomeFormField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS'; payload: IncomeDto }
  | { type: 'SAVE_ERROR'; payload: { message: string; field: IncomeFormField | null } }
  | { type: 'DISMISS_SPLIT_CONFIRMATION' }

const BLANK_FORM = {
  date: '',
  incomeSource: '',
  grossValue: '',
  netValue: '',
  bank: '',
  description: '',
  splitToReserve: 'false',
} as const

const INITIAL_STATE: IncomeFormState = {
  isOpen: false,
  isEditing: false,
  editingId: null,
  ...BLANK_FORM,
  isSaving: false,
  saveError: null,
  saveErrorField: null,
  splitConfirmationMessage: null,
}

const SPLIT_CONFIRMATION_MESSAGE = 'Income saved and split to reserve'

function reducer(state: IncomeFormState, action: IncomeFormAction): IncomeFormState {
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
        bank: action.payload.bank,
        incomeSource: action.payload.source,
        splitToReserve: action.payload.splitToReserve,
      }
    case 'SHOW_EDIT_FORM':
      return {
        ...state,
        isOpen: true,
        isEditing: true,
        editingId: action.payload.id,
        date: action.payload.date,
        incomeSource: action.payload.incomeSourceId,
        grossValue: action.payload.grossValue != null ? String(action.payload.grossValue) : '',
        netValue: String(action.payload.netValue),
        bank: action.payload.bankId ?? '',
        description: action.payload.description ?? '',
        splitToReserve: String(action.payload.splitToReserve),
        saveError: null,
        saveErrorField: null,
      }
    case 'CANCEL_FORM':
      return { ...state, ...BLANK_FORM, isOpen: false, isEditing: false, editingId: null, saveError: null, saveErrorField: null }
    case 'SET_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null, saveErrorField: null }
    case 'SAVE_SUCCESS':
      return {
        ...state,
        ...BLANK_FORM,
        isOpen: false,
        isEditing: false,
        editingId: null,
        isSaving: false,
        splitConfirmationMessage: action.payload.splitToReserve ? SPLIT_CONFIRMATION_MESSAGE : null,
      }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload.message, saveErrorField: action.payload.field }
    case 'DISMISS_SPLIT_CONFIRMATION':
      return { ...state, splitConfirmationMessage: null }
    default:
      return state
  }
}

export interface UseIncomeFormResult {
  isIncomeFormOpen: boolean
  isIncomeEditing: boolean
  editingIncomeId: string | null
  incomeDate: string
  incomeSource: string
  incomeGrossValue: string
  incomeNetValue: string
  incomeBank: string
  incomeDescription: string
  incomeSplitToReserve: string
  isSavingIncome: boolean
  saveIncomeError: string | null
  saveIncomeErrorField: IncomeFormField | null
  splitConfirmationMessage: string | null
  showCreateIncomeForm: () => void
  showEditIncomeForm: (income: IncomeDto) => void
  cancelIncomeForm: () => void
  setIncomeField: (field: IncomeFormField, value: string) => void
  submitIncome: () => void
}

export function useIncomeForm(incomeSources: IncomeSourceDto[], onSaved: () => void): UseIncomeFormResult {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    if (!state.splitConfirmationMessage) return
    const timeoutId = setTimeout(() => dispatch({ type: 'DISMISS_SPLIT_CONFIRMATION' }), SPLIT_CONFIRMATION_DELAY_MS)
    return () => clearTimeout(timeoutId)
  }, [state.splitConfirmationMessage])

  function showCreateIncomeForm() {
    const date = getStoredDefault(DATE_KEY) ?? todayIsoDate()
    const bank = getStoredDefault(BANK_KEY) ?? ''
    const storedSource = getStoredDefault(SOURCE_KEY)
    const source =
      state.incomeSource ||
      (storedSource && incomeSources.some((s) => s.id === storedSource) ? storedSource : (selectActiveIncomeSources(incomeSources)[0]?.id ?? ''))
    const splitToReserve = resolveAutoSplitToReserve(incomeSources, source) ? 'true' : 'false'
    dispatch({ type: 'SHOW_CREATE_FORM', payload: { date, bank, source, splitToReserve } })
  }

  const showEditIncomeForm = (income: IncomeDto) => dispatch({ type: 'SHOW_EDIT_FORM', payload: income })

  const cancelIncomeForm = () => dispatch({ type: 'CANCEL_FORM' })

  function setIncomeField(field: IncomeFormField, value: string) {
    dispatch({ type: 'SET_FIELD', payload: { field, value } })
    if (field === 'incomeSource') {
      if (!INCOME_SOURCES_WITH_GROSS_VALUE.includes(resolveIncomeSourceName(incomeSources, value))) {
        dispatch({ type: 'SET_FIELD', payload: { field: 'grossValue', value: '' } })
      }
      const splitToReserve = resolveAutoSplitToReserve(incomeSources, value) ? 'true' : 'false'
      dispatch({ type: 'SET_FIELD', payload: { field: 'splitToReserve', value: splitToReserve } })
    }
  }

  function submitIncome() {
    if (!state.date.trim()) {
      dispatch({ type: 'SAVE_ERROR', payload: { message: 'Date is required', field: 'date' } })
      return
    }

    if (!state.incomeSource.trim()) {
      dispatch({ type: 'SAVE_ERROR', payload: { message: 'Income source is required', field: 'incomeSource' } })
      return
    }

    const netValue = parseValidatedNumber(state.netValue, { min: 0 })
    if (netValue === null) {
      dispatch({ type: 'SAVE_ERROR', payload: { message: 'Net value must be a non-negative number', field: 'netValue' } })
      return
    }

    let grossValue: number | null = null
    if (state.grossValue.trim() !== '') {
      grossValue = parseValidatedNumber(state.grossValue, { min: netValue })
      if (grossValue === null) {
        dispatch({
          type: 'SAVE_ERROR',
          payload: { message: 'Gross value must be at least the net value', field: 'grossValue' },
        })
        return
      }
    }

    dispatch({ type: 'SAVE_START' })

    const payload = {
      date: state.date,
      incomeSourceId: state.incomeSource,
      grossValue,
      netValue,
      bankId: state.bank || null,
      description: state.description || null,
      splitToReserve: state.splitToReserve === 'true',
    }

    const request =
      state.isEditing && state.editingId ? apiClient.updateIncome(state.editingId, payload) : apiClient.createIncome(payload)

    void request
      .then((savedIncome) => {
        setStoredDefault(DATE_KEY, state.date)
        setStoredDefault(BANK_KEY, state.bank)
        setStoredDefault(SOURCE_KEY, state.incomeSource)
        dispatch({ type: 'SAVE_SUCCESS', payload: savedIncome })
        onSaved()
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: {
            message: getErrorMessage(err, state.isEditing ? 'Failed to update income' : 'Failed to create income'),
            field: null,
          },
        })
      })
  }

  return {
    isIncomeFormOpen: state.isOpen,
    isIncomeEditing: state.isEditing,
    editingIncomeId: state.editingId,
    incomeDate: state.date,
    incomeSource: state.incomeSource,
    incomeGrossValue: state.grossValue,
    incomeNetValue: state.netValue,
    incomeBank: state.bank,
    incomeDescription: state.description,
    incomeSplitToReserve: state.splitToReserve,
    isSavingIncome: state.isSaving,
    saveIncomeError: state.saveError,
    saveIncomeErrorField: state.saveErrorField,
    splitConfirmationMessage: state.splitConfirmationMessage,
    showCreateIncomeForm,
    showEditIncomeForm,
    cancelIncomeForm,
    setIncomeField,
    submitIncome,
  }
}
