import { useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { IncomeDto, IncomeSourceDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

export const INCOME_SOURCES_WITH_GROSS_VALUE = ['Gleison', 'Ariana']

/** Matches the previous hardcoded INCOME_SOURCES array's declaration order. Names outside this
 * list (unexpected but not invalid) sort last rather than being dropped or erroring. */
const INCOME_SOURCE_DISPLAY_ORDER = ['Gleison', 'Ariana', 'Lottery', 'DividendoJuros']

/** Active income sources, ordered to match the picklist's historical display order. */
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

/** Resolves an income source Id back to its name for the gross-value-eligibility check. */
function resolveIncomeSourceName(sources: IncomeSourceDto[], incomeSourceId: string): string {
  return sources.find((s) => s.id === incomeSourceId)?.name ?? ''
}

export type CreateIncomeField =
  | 'createIncomeDate'
  | 'createIncomeSource'
  | 'createIncomeGrossValue'
  | 'createIncomeNetValue'
  | 'createIncomeBank'
  | 'createIncomeDescription'
export type EditIncomeField =
  | 'editIncomeDate'
  | 'editIncomeSource'
  | 'editIncomeGrossValue'
  | 'editIncomeNetValue'
  | 'editIncomeBank'
  | 'editIncomeDescription'

interface IncomeFormState {
  isIncomeCreateFormOpen: boolean
  createIncomeDate: string
  createIncomeSource: string
  createIncomeGrossValue: string
  createIncomeNetValue: string
  createIncomeBank: string
  createIncomeDescription: string
  isCreatingIncome: boolean
  createIncomeError: string | null
  editingIncomeId: string | null
  editIncomeDate: string
  editIncomeSource: string
  editIncomeGrossValue: string
  editIncomeNetValue: string
  editIncomeBank: string
  editIncomeDescription: string
  isSavingIncome: boolean
  saveIncomeError: string | null
}

type IncomeFormAction =
  | { type: 'SHOW_CREATE_FORM'; payload: { source: string } }
  | { type: 'CANCEL_CREATE_FORM' }
  | { type: 'SET_CREATE_FIELD'; payload: { field: CreateIncomeField; value: string } }
  | { type: 'CREATE_START' }
  | { type: 'CREATE_SUCCESS' }
  | { type: 'CREATE_ERROR'; payload: string }
  | { type: 'SHOW_EDIT_FORM'; payload: IncomeDto }
  | { type: 'CANCEL_EDIT' }
  | { type: 'SET_EDIT_FIELD'; payload: { field: EditIncomeField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS' }
  | { type: 'SAVE_ERROR'; payload: string }

const BLANK_CREATE_FORM = {
  createIncomeDate: '',
  createIncomeSource: '',
  createIncomeGrossValue: '',
  createIncomeNetValue: '',
  createIncomeBank: '',
  createIncomeDescription: '',
} as const

const BLANK_EDIT_FORM = {
  editingIncomeId: null,
  editIncomeDate: '',
  editIncomeSource: '',
  editIncomeGrossValue: '',
  editIncomeNetValue: '',
  editIncomeBank: '',
  editIncomeDescription: '',
} as const

const INITIAL_STATE: IncomeFormState = {
  isIncomeCreateFormOpen: false,
  ...BLANK_CREATE_FORM,
  isCreatingIncome: false,
  createIncomeError: null,
  ...BLANK_EDIT_FORM,
  isSavingIncome: false,
  saveIncomeError: null,
}

function reducer(state: IncomeFormState, action: IncomeFormAction): IncomeFormState {
  switch (action.type) {
    case 'SHOW_CREATE_FORM':
      return {
        ...state,
        ...BLANK_EDIT_FORM,
        isIncomeCreateFormOpen: true,
        saveIncomeError: null,
        createIncomeSource: action.payload.source,
      }
    case 'CANCEL_CREATE_FORM':
      return { ...state, ...BLANK_CREATE_FORM, isIncomeCreateFormOpen: false, createIncomeError: null }
    case 'SET_CREATE_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'CREATE_START':
      return { ...state, isCreatingIncome: true, createIncomeError: null }
    case 'CREATE_SUCCESS':
      return { ...state, ...BLANK_CREATE_FORM, isIncomeCreateFormOpen: false, isCreatingIncome: false }
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
        saveIncomeError: null,
      }
    case 'CANCEL_EDIT':
      return { ...state, ...BLANK_EDIT_FORM, saveIncomeError: null }
    case 'SET_EDIT_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSavingIncome: true, saveIncomeError: null }
    case 'SAVE_SUCCESS':
      return { ...state, ...BLANK_EDIT_FORM, isSavingIncome: false }
    case 'SAVE_ERROR':
      return { ...state, isSavingIncome: false, saveIncomeError: action.payload }
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
  isSavingIncome: boolean
  saveIncomeError: string | null
  setEditIncomeField: (field: EditIncomeField, value: string) => void
  showEditIncomeForm: (income: IncomeDto) => void
  cancelEditIncome: () => void
  saveEditIncome: () => void
}

/** Owns an income entry's create/edit form state and orchestrates the create/update endpoints. */
export function useIncomeForm(incomeSources: IncomeSourceDto[], onSaved: () => void): UseIncomeFormResult {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  function showCreateIncomeForm() {
    const source = state.createIncomeSource || (selectActiveIncomeSources(incomeSources)[0]?.id ?? '')
    dispatch({ type: 'SHOW_CREATE_FORM', payload: { source } })
  }

  const cancelCreateIncomeForm = () => dispatch({ type: 'CANCEL_CREATE_FORM' })

  function setCreateIncomeField(field: CreateIncomeField, value: string) {
    dispatch({ type: 'SET_CREATE_FIELD', payload: { field, value } })
    if (
      field === 'createIncomeSource' &&
      !INCOME_SOURCES_WITH_GROSS_VALUE.includes(resolveIncomeSourceName(incomeSources, value))
    ) {
      dispatch({ type: 'SET_CREATE_FIELD', payload: { field: 'createIncomeGrossValue', value: '' } })
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
    } = state

    if (!createIncomeDate.trim()) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Date is required' })
      return
    }

    if (!createIncomeSource.trim()) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Income source is required' })
      return
    }

    const netValue = Number(createIncomeNetValue)
    if (!createIncomeNetValue.trim() || !isFinite(netValue) || netValue < 0) {
      dispatch({ type: 'CREATE_ERROR', payload: 'Net value must be a non-negative number' })
      return
    }

    let grossValue: number | null = null
    if (createIncomeGrossValue.trim() !== '') {
      const parsedGrossValue = Number(createIncomeGrossValue)
      if (!isFinite(parsedGrossValue) || parsedGrossValue < netValue) {
        dispatch({ type: 'CREATE_ERROR', payload: 'Gross value must be at least the net value' })
        return
      }
      grossValue = parsedGrossValue
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
      })
      .then(() => {
        dispatch({ type: 'CREATE_SUCCESS' })
        onSaved()
      })
      .catch((err: unknown) => {
        dispatch({ type: 'CREATE_ERROR', payload: getErrorMessage(err, 'Failed to create income') })
      })
  }

  const setEditIncomeField = (field: EditIncomeField, value: string) => {
    dispatch({ type: 'SET_EDIT_FIELD', payload: { field, value } })
    if (
      field === 'editIncomeSource' &&
      !INCOME_SOURCES_WITH_GROSS_VALUE.includes(resolveIncomeSourceName(incomeSources, value))
    ) {
      dispatch({ type: 'SET_EDIT_FIELD', payload: { field: 'editIncomeGrossValue', value: '' } })
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

    const netValue = Number(state.editIncomeNetValue)
    if (!state.editIncomeNetValue.trim() || !isFinite(netValue) || netValue < 0) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Net value must be a non-negative number' })
      return
    }

    let grossValue: number | null = null
    if (state.editIncomeGrossValue.trim() !== '') {
      const parsedGrossValue = Number(state.editIncomeGrossValue)
      if (!isFinite(parsedGrossValue) || parsedGrossValue < netValue) {
        dispatch({ type: 'SAVE_ERROR', payload: 'Gross value must be at least the net value' })
        return
      }
      grossValue = parsedGrossValue
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
      })
      .then(() => {
        dispatch({ type: 'SAVE_SUCCESS' })
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
    isSavingIncome: state.isSavingIncome,
    saveIncomeError: state.saveIncomeError,
    setEditIncomeField,
    showEditIncomeForm,
    cancelEditIncome,
    saveEditIncome,
  }
}
