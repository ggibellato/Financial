import { useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { IncomeDto, IncomeSourceDto } from '../api/types'
import { getErrorMessage, parseValidatedNumber } from '../utils/formatters'

export const INCOME_SOURCES_WITH_GROSS_VALUE = ['Gleison', 'Ariana']

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

export type CreateIncomeField =
  | 'createIncomeDate'
  | 'createIncomeSource'
  | 'createIncomeGrossValue'
  | 'createIncomeNetValue'
  | 'createIncomeBank'
  | 'createIncomeDescription'
  | 'createIncomeSplitToReserve'
export type EditIncomeField =
  | 'editIncomeDate'
  | 'editIncomeSource'
  | 'editIncomeGrossValue'
  | 'editIncomeNetValue'
  | 'editIncomeBank'
  | 'editIncomeDescription'
  | 'editIncomeSplitToReserve'

interface IncomeFormState {
  isIncomeCreateFormOpen: boolean
  createIncomeDate: string
  createIncomeSource: string
  createIncomeGrossValue: string
  createIncomeNetValue: string
  createIncomeBank: string
  createIncomeDescription: string
  createIncomeSplitToReserve: string
  isCreatingIncome: boolean
  createIncomeError: string | null
  editingIncomeId: string | null
  editIncomeDate: string
  editIncomeSource: string
  editIncomeGrossValue: string
  editIncomeNetValue: string
  editIncomeBank: string
  editIncomeDescription: string
  editIncomeSplitToReserve: string
  isSavingIncome: boolean
  saveIncomeError: string | null
  splitConfirmationMessage: string | null
}

type IncomeFormAction =
  | { type: 'SHOW_CREATE_FORM'; payload: { source: string; splitToReserve: string } }
  | { type: 'CANCEL_CREATE_FORM' }
  | { type: 'SET_CREATE_FIELD'; payload: { field: CreateIncomeField; value: string } }
  | { type: 'CREATE_START' }
  | { type: 'CREATE_SUCCESS'; payload: IncomeDto }
  | { type: 'CREATE_ERROR'; payload: string }
  | { type: 'SHOW_EDIT_FORM'; payload: IncomeDto }
  | { type: 'CANCEL_EDIT' }
  | { type: 'SET_EDIT_FIELD'; payload: { field: EditIncomeField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS'; payload: IncomeDto }
  | { type: 'SAVE_ERROR'; payload: string }
  | { type: 'DISMISS_SPLIT_CONFIRMATION' }

const BLANK_CREATE_FORM = {
  createIncomeDate: '',
  createIncomeSource: '',
  createIncomeGrossValue: '',
  createIncomeNetValue: '',
  createIncomeBank: '',
  createIncomeDescription: '',
  createIncomeSplitToReserve: 'false',
} as const

const BLANK_EDIT_FORM = {
  editingIncomeId: null,
  editIncomeDate: '',
  editIncomeSource: '',
  editIncomeGrossValue: '',
  editIncomeNetValue: '',
  editIncomeBank: '',
  editIncomeDescription: '',
  editIncomeSplitToReserve: 'false',
} as const

const INITIAL_STATE: IncomeFormState = {
  isIncomeCreateFormOpen: false,
  ...BLANK_CREATE_FORM,
  isCreatingIncome: false,
  createIncomeError: null,
  ...BLANK_EDIT_FORM,
  isSavingIncome: false,
  saveIncomeError: null,
  splitConfirmationMessage: null,
}

const SPLIT_CONFIRMATION_MESSAGE = 'Income saved and split to reserve'

function reducer(state: IncomeFormState, action: IncomeFormAction): IncomeFormState {
  switch (action.type) {
    case 'SHOW_CREATE_FORM':
      return {
        ...state,
        ...BLANK_EDIT_FORM,
        isIncomeCreateFormOpen: true,
        saveIncomeError: null,
        createIncomeSource: action.payload.source,
        createIncomeSplitToReserve: action.payload.splitToReserve,
      }
    case 'CANCEL_CREATE_FORM':
      return { ...state, ...BLANK_CREATE_FORM, isIncomeCreateFormOpen: false, createIncomeError: null }
    case 'SET_CREATE_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'CREATE_START':
      return { ...state, isCreatingIncome: true, createIncomeError: null }
    case 'CREATE_SUCCESS':
      return {
        ...state,
        ...BLANK_CREATE_FORM,
        isIncomeCreateFormOpen: false,
        isCreatingIncome: false,
        splitConfirmationMessage: action.payload.splitToReserve ? SPLIT_CONFIRMATION_MESSAGE : null,
      }
    case 'CREATE_ERROR':
      return { ...state, isCreatingIncome: false, createIncomeError: action.payload }
    case 'SHOW_EDIT_FORM':
      return {
        ...state,
        isIncomeCreateFormOpen: false,
        editingIncomeId: action.payload.id,
        editIncomeDate: action.payload.date,
        editIncomeSource: action.payload.incomeSourceId,
        editIncomeGrossValue: action.payload.grossValue != null ? String(action.payload.grossValue) : '',
        editIncomeNetValue: String(action.payload.netValue),
        editIncomeBank: action.payload.bankId ?? '',
        editIncomeDescription: action.payload.description ?? '',
        editIncomeSplitToReserve: String(action.payload.splitToReserve),
        saveIncomeError: null,
      }
    case 'CANCEL_EDIT':
      return { ...state, ...BLANK_EDIT_FORM, saveIncomeError: null }
    case 'SET_EDIT_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSavingIncome: true, saveIncomeError: null }
    case 'SAVE_SUCCESS':
      return {
        ...state,
        ...BLANK_EDIT_FORM,
        isSavingIncome: false,
        splitConfirmationMessage: action.payload.splitToReserve ? SPLIT_CONFIRMATION_MESSAGE : null,
      }
    case 'SAVE_ERROR':
      return { ...state, isSavingIncome: false, saveIncomeError: action.payload }
    case 'DISMISS_SPLIT_CONFIRMATION':
      return { ...state, splitConfirmationMessage: null }
    default:
      return state
  }
}

export interface UseIncomeFormResult {
  isIncomeCreateFormOpen: boolean
  createIncomeDate: string
  createIncomeSource: string
  createIncomeGrossValue: string
  createIncomeNetValue: string
  createIncomeBank: string
  createIncomeDescription: string
  createIncomeSplitToReserve: string
  isCreatingIncome: boolean
  createIncomeError: string | null
  showCreateIncomeForm: () => void
  cancelCreateIncomeForm: () => void
  setCreateIncomeField: (field: CreateIncomeField, value: string) => void
  submitCreateIncome: () => void
  editingIncomeId: string | null
  editIncomeDate: string
  editIncomeSource: string
  editIncomeGrossValue: string
  editIncomeNetValue: string
  editIncomeBank: string
  editIncomeDescription: string
  editIncomeSplitToReserve: string
  isSavingIncome: boolean
  saveIncomeError: string | null
  setEditIncomeField: (field: EditIncomeField, value: string) => void
  showEditIncomeForm: (income: IncomeDto) => void
  cancelEditIncome: () => void
  saveEditIncome: () => void
  splitConfirmationMessage: string | null
}

export function useIncomeForm(incomeSources: IncomeSourceDto[], onSaved: () => void): UseIncomeFormResult {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    if (!state.splitConfirmationMessage) return
    const timeoutId = setTimeout(() => dispatch({ type: 'DISMISS_SPLIT_CONFIRMATION' }), SPLIT_CONFIRMATION_DELAY_MS)
    return () => clearTimeout(timeoutId)
  }, [state.splitConfirmationMessage])

  function showCreateIncomeForm() {
    const source = state.createIncomeSource || (selectActiveIncomeSources(incomeSources)[0]?.id ?? '')
    const splitToReserve = resolveAutoSplitToReserve(incomeSources, source) ? 'true' : 'false'
    dispatch({ type: 'SHOW_CREATE_FORM', payload: { source, splitToReserve } })
  }

  const cancelCreateIncomeForm = () => dispatch({ type: 'CANCEL_CREATE_FORM' })

  function setCreateIncomeField(field: CreateIncomeField, value: string) {
    dispatch({ type: 'SET_CREATE_FIELD', payload: { field, value } })
    if (field === 'createIncomeSource') {
      if (!INCOME_SOURCES_WITH_GROSS_VALUE.includes(resolveIncomeSourceName(incomeSources, value))) {
        dispatch({ type: 'SET_CREATE_FIELD', payload: { field: 'createIncomeGrossValue', value: '' } })
      }
      const splitToReserve = resolveAutoSplitToReserve(incomeSources, value) ? 'true' : 'false'
      dispatch({ type: 'SET_CREATE_FIELD', payload: { field: 'createIncomeSplitToReserve', value: splitToReserve } })
    }
  }

  function submitCreateIncome() {
    const {
      createIncomeDate,
      createIncomeSource,
      createIncomeGrossValue,
      createIncomeNetValue,
      createIncomeBank,
      createIncomeDescription,
      createIncomeSplitToReserve,
    } = state

    if (!createIncomeDate.trim()) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Date is required' })
      return
    }

    if (!createIncomeSource.trim()) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Income source is required' })
      return
    }

    const netValue = parseValidatedNumber(createIncomeNetValue, { min: 0 })
    if (netValue === null) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Net value must be a non-negative number' })
      return
    }

    let grossValue: number | null = null
    if (createIncomeGrossValue.trim() !== '') {
      grossValue = parseValidatedNumber(createIncomeGrossValue, { min: netValue })
      if (grossValue === null) {
        dispatch({ type: 'CREATE_ERROR', payload: 'Gross value must be at least the net value' })
        return
      }
    }

    dispatch({ type: 'CREATE_START' })

    void apiClient
      .createIncome({
        date: createIncomeDate,
        incomeSourceId: createIncomeSource,
        grossValue,
        netValue,
        bankId: createIncomeBank || null,
        description: createIncomeDescription || null,
        splitToReserve: createIncomeSplitToReserve === 'true',
      })
      .then((createdIncome) => {
        dispatch({ type: 'CREATE_SUCCESS', payload: createdIncome })
        onSaved()
      })
      .catch((err: unknown) => {
        dispatch({ type: 'CREATE_ERROR', payload: getErrorMessage(err, 'Failed to create income') })
      })
  }

  const setEditIncomeField = (field: EditIncomeField, value: string) => {
    dispatch({ type: 'SET_EDIT_FIELD', payload: { field, value } })
    if (field === 'editIncomeSource') {
      if (!INCOME_SOURCES_WITH_GROSS_VALUE.includes(resolveIncomeSourceName(incomeSources, value))) {
        dispatch({ type: 'SET_EDIT_FIELD', payload: { field: 'editIncomeGrossValue', value: '' } })
      }
      const splitToReserve = resolveAutoSplitToReserve(incomeSources, value) ? 'true' : 'false'
      dispatch({ type: 'SET_EDIT_FIELD', payload: { field: 'editIncomeSplitToReserve', value: splitToReserve } })
    }
  }

  const showEditIncomeForm = (income: IncomeDto) => dispatch({ type: 'SHOW_EDIT_FORM', payload: income })

  const cancelEditIncome = () => dispatch({ type: 'CANCEL_EDIT' })

  function saveEditIncome() {
    if (!state.editingIncomeId) return

    if (!state.editIncomeDate.trim()) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Date is required' })
      return
    }

    if (!state.editIncomeSource.trim()) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Income source is required' })
      return
    }

    const netValue = parseValidatedNumber(state.editIncomeNetValue, { min: 0 })
    if (netValue === null) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Net value must be a non-negative number' })
      return
    }

    let grossValue: number | null = null
    if (state.editIncomeGrossValue.trim() !== '') {
      grossValue = parseValidatedNumber(state.editIncomeGrossValue, { min: netValue })
      if (grossValue === null) {
        dispatch({ type: 'SAVE_ERROR', payload: 'Gross value must be at least the net value' })
        return
      }
    }

    dispatch({ type: 'SAVE_START' })

    void apiClient
      .updateIncome(state.editingIncomeId, {
        date: state.editIncomeDate,
        incomeSourceId: state.editIncomeSource,
        grossValue,
        netValue,
        bankId: state.editIncomeBank || null,
        description: state.editIncomeDescription || null,
        splitToReserve: state.editIncomeSplitToReserve === 'true',
      })
      .then((updatedIncome) => {
        dispatch({ type: 'SAVE_SUCCESS', payload: updatedIncome })
        onSaved()
      })
      .catch((err: unknown) => {
        dispatch({ type: 'SAVE_ERROR', payload: getErrorMessage(err, 'Failed to update income') })
      })
  }

  return {
    isIncomeCreateFormOpen: state.isIncomeCreateFormOpen,
    createIncomeDate: state.createIncomeDate,
    createIncomeSource: state.createIncomeSource,
    createIncomeGrossValue: state.createIncomeGrossValue,
    createIncomeNetValue: state.createIncomeNetValue,
    createIncomeBank: state.createIncomeBank,
    createIncomeDescription: state.createIncomeDescription,
    createIncomeSplitToReserve: state.createIncomeSplitToReserve,
    isCreatingIncome: state.isCreatingIncome,
    createIncomeError: state.createIncomeError,
    showCreateIncomeForm,
    cancelCreateIncomeForm,
    setCreateIncomeField,
    submitCreateIncome,
    editingIncomeId: state.editingIncomeId,
    editIncomeDate: state.editIncomeDate,
    editIncomeSource: state.editIncomeSource,
    editIncomeGrossValue: state.editIncomeGrossValue,
    editIncomeNetValue: state.editIncomeNetValue,
    editIncomeBank: state.editIncomeBank,
    editIncomeDescription: state.editIncomeDescription,
    editIncomeSplitToReserve: state.editIncomeSplitToReserve,
    isSavingIncome: state.isSavingIncome,
    saveIncomeError: state.saveIncomeError,
    setEditIncomeField,
    showEditIncomeForm,
    cancelEditIncome,
    saveEditIncome,
    splitConfirmationMessage: state.splitConfirmationMessage,
  }
}
