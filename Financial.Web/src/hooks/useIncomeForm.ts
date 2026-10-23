import { useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { IncomeFormField } from '../components/IncomeForm'
import type { IncomeDto, IncomeSourceDto } from '../api/types'
import { getSaveErrorMessage, parseValidatedNumber } from '../utils/formatters'
import { getCreateFormDate, getStoredDefault, setStoredDefault } from '../utils/createFormDefaults'
import type { AccountFormSettings } from './formSettings'
import { hasChangedFields, snapshotFields } from './inlineFormSlice'

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

const EDITABLE_FIELDS = ['date', 'incomeSource', 'grossValue', 'netValue', 'bank', 'description', 'splitToReserve'] as const

type EditableValues = Pick<IncomeFormState, (typeof EDITABLE_FIELDS)[number]>

function withOpenedValues(state: IncomeFormState): IncomeFormState {
  return { ...state, openedValues: snapshotFields(state, EDITABLE_FIELDS) }
}

interface IncomeFormState {
  openedValues: EditableValues | null
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
  saveErrorFields: Partial<Record<IncomeFormField, string>>
  splitConfirmationMessage: string | null
}

type IncomeFormAction =
  | { type: 'SHOW_CREATE_FORM'; payload: { date: string; bank: string; source: string; splitToReserve: string } }
  | { type: 'SHOW_EDIT_FORM'; payload: IncomeDto }
  | { type: 'CANCEL_FORM' }
  | { type: 'SET_FIELD'; payload: { field: IncomeFormField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS'; payload: IncomeDto }
  | { type: 'SAVE_SUCCESS_KEEP_OPEN'; payload: { splitConfirmed: boolean; nextSplitToReserve: string } }
  | { type: 'SAVE_ERROR'; payload: { message: string | null; fields: Partial<Record<IncomeFormField, string>> } }
  | { type: 'DISMISS_SPLIT_CONFIRMATION' }

const BLANK_ENTRY = {
  grossValue: '',
  netValue: '',
  description: '',
} as const

const BLANK_FORM = {
  ...BLANK_ENTRY,
  date: '',
  incomeSource: '',
  bank: '',
  splitToReserve: 'false',
} as const

const INITIAL_STATE: IncomeFormState = {
  openedValues: null,
  isOpen: false,
  isEditing: false,
  editingId: null,
  ...BLANK_FORM,
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
  splitConfirmationMessage: null,
}

const SPLIT_CONFIRMATION_MESSAGE = 'Income saved and split to reserve'

function reducer(state: IncomeFormState, action: IncomeFormAction): IncomeFormState {
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
        bank: action.payload.bank,
        incomeSource: action.payload.source,
        splitToReserve: action.payload.splitToReserve,
      })
    case 'SHOW_EDIT_FORM':
      return withOpenedValues({
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
        saveErrorFields: {},
      })
    case 'CANCEL_FORM':
      return { ...state, ...BLANK_FORM, isOpen: false, isEditing: false, editingId: null, saveError: null, saveErrorFields: {} }
    case 'SET_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null, saveErrorFields: {} }
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
    case 'SAVE_SUCCESS_KEEP_OPEN':
      return withOpenedValues({
        ...state,
        ...BLANK_ENTRY,
        splitToReserve: action.payload.nextSplitToReserve,
        isSaving: false,
        splitConfirmationMessage: action.payload.splitConfirmed ? SPLIT_CONFIRMATION_MESSAGE : null,
      })
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload.message, saveErrorFields: action.payload.fields }
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
  isIncomeDirty: boolean
  saveIncomeError: string | null
  saveIncomeErrorFields: Partial<Record<IncomeFormField, string>>
  splitConfirmationMessage: string | null
  showCreateIncomeForm: () => void
  showEditIncomeForm: (income: IncomeDto) => void
  cancelIncomeForm: () => void
  setIncomeField: (field: IncomeFormField, value: string) => void
  submitIncome: () => void
}

export function useIncomeForm(
  incomeSources: IncomeSourceDto[],
  onSaved: (saved: IncomeDto) => void,
  { lockedBank, keepOpenAfterSave = false, viewedMonth }: AccountFormSettings = {},
): UseIncomeFormResult {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    if (!state.splitConfirmationMessage) return
    const timeoutId = setTimeout(() => dispatch({ type: 'DISMISS_SPLIT_CONFIRMATION' }), SPLIT_CONFIRMATION_DELAY_MS)
    return () => clearTimeout(timeoutId)
  }, [state.splitConfirmationMessage])

  function showCreateIncomeForm() {
    const date = getCreateFormDate(DATE_KEY, viewedMonth)
    const bank = lockedBank?.id ?? getStoredDefault(BANK_KEY) ?? ''
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
    const errors: Partial<Record<IncomeFormField, string>> = {}

    if (!state.date.trim()) {
      errors.date = 'Date is required'
    }

    if (!state.incomeSource.trim()) {
      errors.incomeSource = 'Income source is required'
    }

    const netValue = parseValidatedNumber(state.netValue, { min: 0 })
    if (netValue === null) {
      errors.netValue = 'Net value must be a non-negative number'
    }

    let grossValue: number | null = null
    if (state.grossValue.trim() !== '') {
      grossValue = parseValidatedNumber(state.grossValue, { min: netValue ?? 0 })
      if (grossValue === null) {
        errors.grossValue = 'Gross value must be at least the net value'
      }
    }

    if (Object.keys(errors).length > 0) {
      dispatch({ type: 'SAVE_ERROR', payload: { message: Object.values(errors)[0] ?? null, fields: errors } })
      return
    }

    dispatch({ type: 'SAVE_START' })

    const payload = {
      date: state.date,
      incomeSourceId: state.incomeSource,
      grossValue,
      netValue: netValue as number,
      bankId: lockedBank?.id ?? (state.bank || null),
      description: state.description || null,
      splitToReserve: state.splitToReserve === 'true',
    }

    const request =
      state.isEditing && state.editingId ? apiClient.updateIncome(state.editingId, payload) : apiClient.createIncome(payload)

    void request
      .then((savedIncome) => {
        setStoredDefault(DATE_KEY, state.date)
        if (!lockedBank) setStoredDefault(BANK_KEY, state.bank)
        setStoredDefault(SOURCE_KEY, state.incomeSource)
        if (keepOpenAfterSave && !state.isEditing) {
          const nextSplitToReserve = resolveAutoSplitToReserve(incomeSources, state.incomeSource) ? 'true' : 'false'
          dispatch({ type: 'SAVE_SUCCESS_KEEP_OPEN', payload: { splitConfirmed: savedIncome.splitToReserve, nextSplitToReserve } })
        } else {
          dispatch({ type: 'SAVE_SUCCESS', payload: savedIncome })
        }
        onSaved(savedIncome)
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: {
            message: getSaveErrorMessage(err, state.isEditing ? 'Failed to update income' : 'Failed to create income'),
            fields: {},
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
    isIncomeDirty: state.isOpen && hasChangedFields(state, state.openedValues, EDITABLE_FIELDS),
    saveIncomeError: state.saveError,
    saveIncomeErrorFields: state.saveErrorFields,
    splitConfirmationMessage: state.splitConfirmationMessage,
    showCreateIncomeForm,
    showEditIncomeForm,
    cancelIncomeForm,
    setIncomeField,
    submitIncome,
  }
}
