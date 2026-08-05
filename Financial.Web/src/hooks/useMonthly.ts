import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type {
  BankBalanceDto,
  BankDto,
  CardStatementDto,
  CategoryTotalDto,
  ExpenseDto,
  IncomeDto,
  TitheSummaryDto,
} from '../api/types'
import { currentYearMonth, formatMonthInputValue, parseMonthInputValue } from '../utils/formatters'

export type PaymentMode = 'bank' | 'card'

export const INCOME_SOURCES_WITH_GROSS_VALUE = ['Gleison', 'Ariana']

const SETTLED_STATUS = 'CreditCardSettled'
const CHARGE_STATUS = 'CreditCardCharge'

const MIN_ROUND_UP_AMOUNT = 0
const MAX_ROUND_UP_AMOUNT = 0.99

function computeRoundUpSuggestion(value: number): number {
  return Math.round((Math.ceil(value) - value) * 100) / 100
}

/** Suggests a round-up amount for the given bank/value, or null if not eligible or no value yet. */
function suggestRoundUpAmount(banks: BankDto[], bankName: string, value: string): string | null {
  const bank = banks.find((b) => b.name === bankName)
  if (!bank?.roundUpEnabled) return null

  const parsedValue = Number(value)
  if (!value.trim() || !isFinite(parsedValue) || parsedValue <= 0) return null

  return computeRoundUpSuggestion(parsedValue).toFixed(2)
}

export interface BankTotal {
  bank: string
  balance: number
  roundUpTotal: number
}

export interface IncomeTotal {
  source: string
  netValue: number
  grossValue: number | null
}

export type CreateFormField =
  | 'createDate'
  | 'createDescription'
  | 'createValue'
  | 'createCategory'
  | 'createPaymentSource'
  | 'createCardTag'
  | 'createRoundUpAmount'
export type EditField =
  | 'editDate'
  | 'editDescription'
  | 'editValue'
  | 'editCategory'
  | 'editPaymentSource'
  | 'editCardTag'
  | 'editRoundUpAmount'

export type CreateIncomeField =
  | 'createIncomeDate'
  | 'createIncomeSource'
  | 'createIncomeGrossValue'
  | 'createIncomeNetValue'
  | 'createIncomeBank'
export type EditIncomeField =
  | 'editIncomeDate'
  | 'editIncomeSource'
  | 'editIncomeGrossValue'
  | 'editIncomeNetValue'
  | 'editIncomeBank'

interface MonthlyState {
  year: number
  month: number
  expenses: ExpenseDto[]
  categoryTotals: CategoryTotalDto[]
  cardStatements: CardStatementDto[]
  banks: BankDto[]
  bankBalances: BankBalanceDto[]
  incomes: IncomeDto[]
  titheSummary: TitheSummaryDto | null
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
  createRoundUpAmount: string
  isCreating: boolean
  createError: string | null
  editingId: string | null
  editDate: string
  editDescription: string
  editValue: string
  editCategory: string
  editPaymentSource: string
  editCardTag: string
  editRoundUpAmount: string
  createPaymentMode: PaymentMode
  editPaymentMode: PaymentMode
  editIsSettled: boolean
  isSaving: boolean
  saveError: string | null
  markPaidSources: Record<string, string>
  isIncomeCreateFormOpen: boolean
  createIncomeDate: string
  createIncomeSource: string
  createIncomeGrossValue: string
  createIncomeNetValue: string
  createIncomeBank: string
  isCreatingIncome: boolean
  createIncomeError: string | null
  editingIncomeId: string | null
  editIncomeDate: string
  editIncomeSource: string
  editIncomeGrossValue: string
  editIncomeNetValue: string
  editIncomeBank: string
  isSavingIncome: boolean
  saveIncomeError: string | null
}

type MonthlyAction =
  | { type: 'SET_MONTH'; payload: { year: number; month: number } }
  | { type: 'FETCH_START' }
  | {
      type: 'FETCH_SUCCESS'
      payload: {
        expenses: ExpenseDto[]
        categoryTotals: CategoryTotalDto[]
        cardStatements: CardStatementDto[]
        banks: BankDto[]
        bankBalances: BankBalanceDto[]
        incomes: IncomeDto[]
        titheSummary: TitheSummaryDto
      }
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
  | { type: 'SET_MARK_PAID_SOURCE'; payload: { id: string; value: string } }
  | { type: 'SET_CREATE_MODE'; payload: PaymentMode }
  | { type: 'SET_EDIT_MODE'; payload: PaymentMode }
  | { type: 'SHOW_CREATE_INCOME_FORM' }
  | { type: 'CANCEL_CREATE_INCOME_FORM' }
  | { type: 'SET_CREATE_INCOME_FIELD'; payload: { field: CreateIncomeField; value: string } }
  | { type: 'CREATE_INCOME_START' }
  | { type: 'CREATE_INCOME_SUCCESS' }
  | { type: 'CREATE_INCOME_ERROR'; payload: string }
  | { type: 'SHOW_EDIT_INCOME_FORM'; payload: IncomeDto }
  | { type: 'CANCEL_EDIT_INCOME' }
  | { type: 'SET_EDIT_INCOME_FIELD'; payload: { field: EditIncomeField; value: string } }
  | { type: 'SAVE_INCOME_START' }
  | { type: 'SAVE_INCOME_SUCCESS' }
  | { type: 'SAVE_INCOME_ERROR'; payload: string }

const BLANK_CREATE_FORM = {
  createDate: '',
  createDescription: '',
  createValue: '',
  createCategory: 'Mercado',
  createPaymentSource: '',
  createCardTag: '',
  createRoundUpAmount: '',
  createPaymentMode: 'bank',
} as const

const BLANK_CREATE_INCOME_FORM = {
  createIncomeDate: '',
  createIncomeSource: 'Gleison',
  createIncomeGrossValue: '',
  createIncomeNetValue: '',
  createIncomeBank: '',
} as const

const INITIAL_STATE_BASE: Omit<MonthlyState, 'year' | 'month'> = {
  expenses: [],
  categoryTotals: [],
  cardStatements: [],
  banks: [],
  bankBalances: [],
  incomes: [],
  titheSummary: null,
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
  editRoundUpAmount: '',
  editPaymentMode: 'bank',
  editIsSettled: false,
  isSaving: false,
  saveError: null,
  markPaidSources: {},
  isIncomeCreateFormOpen: false,
  ...BLANK_CREATE_INCOME_FORM,
  isCreatingIncome: false,
  createIncomeError: null,
  editingIncomeId: null,
  editIncomeDate: '',
  editIncomeSource: '',
  editIncomeGrossValue: '',
  editIncomeNetValue: '',
  editIncomeBank: '',
  isSavingIncome: false,
  saveIncomeError: null,
}

/** Reads the current year/month fresh at hook-mount time rather than at module-load time, so the
 * default period doesn't go stale in a long-running session and can be pinned via fake timers in tests. */
function buildInitialState(): MonthlyState {
  const { year, month } = currentYearMonth()
  return { ...INITIAL_STATE_BASE, year, month }
}

function reducer(state: MonthlyState, action: MonthlyAction): MonthlyState {
  switch (action.type) {
    case 'SET_MONTH':
      return { ...state, year: action.payload.year, month: action.payload.month }
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS': {
      const defaultBankStillUnset = state.createPaymentMode === 'bank' && state.createPaymentSource === ''
      const defaultIncomeBankStillUnset = state.createIncomeBank === ''
      return {
        ...state,
        isLoading: false,
        expenses: action.payload.expenses,
        categoryTotals: action.payload.categoryTotals,
        cardStatements: action.payload.cardStatements,
        banks: action.payload.banks,
        bankBalances: action.payload.bankBalances,
        incomes: action.payload.incomes,
        titheSummary: action.payload.titheSummary,
        createPaymentSource: defaultBankStillUnset
          ? (action.payload.banks[0]?.name ?? '')
          : state.createPaymentSource,
        createIncomeBank: defaultIncomeBankStillUnset
          ? (action.payload.banks[0]?.name ?? '')
          : state.createIncomeBank,
      }
    }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'SHOW_CREATE_FORM':
      return {
        ...state,
        isCreateFormOpen: true,
        editingId: null,
        saveError: null,
        isIncomeCreateFormOpen: false,
        editingIncomeId: null,
        saveIncomeError: null,
      }
    case 'CANCEL_CREATE_FORM':
      return { ...state, ...BLANK_CREATE_FORM, isCreateFormOpen: false, createError: null }
    case 'SET_CREATE_FIELD': {
      const next = { ...state, [action.payload.field]: action.payload.value }
      if (action.payload.field === 'createPaymentSource' && state.createRoundUpAmount.trim() === '') {
        const suggestion = suggestRoundUpAmount(state.banks, action.payload.value, state.createValue)
        if (suggestion !== null) {
          next.createRoundUpAmount = suggestion
        }
      }
      return next
    }
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
        editPaymentSource: action.payload.paymentSource ?? '',
        editCardTag: action.payload.cardTag ?? '',
        editRoundUpAmount: action.payload.roundUpAmount != null ? String(action.payload.roundUpAmount) : '',
        editPaymentMode: action.payload.paymentStatus === CHARGE_STATUS ? 'card' : 'bank',
        editIsSettled: action.payload.paymentStatus === SETTLED_STATUS,
        saveError: null,
        isIncomeCreateFormOpen: false,
        editingIncomeId: null,
        saveIncomeError: null,
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
        editRoundUpAmount: '',
        editPaymentMode: 'bank',
        editIsSettled: false,
        saveError: null,
      }
    case 'SET_EDIT_FIELD': {
      const next = { ...state, [action.payload.field]: action.payload.value }
      if (action.payload.field === 'editPaymentSource' && state.editRoundUpAmount.trim() === '') {
        const suggestion = suggestRoundUpAmount(state.banks, action.payload.value, state.editValue)
        if (suggestion !== null) {
          next.editRoundUpAmount = suggestion
        }
      }
      return next
    }
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
        editRoundUpAmount: '',
        editPaymentMode: 'bank',
        editIsSettled: false,
      }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload }
    case 'MARK_PAID_ERROR':
      return { ...state, saveError: action.payload }
    case 'SET_MARK_PAID_SOURCE':
      return { ...state, markPaidSources: { ...state.markPaidSources, [action.payload.id]: action.payload.value } }
    case 'SET_CREATE_MODE': {
      const nextPaymentSource = action.payload === 'bank' ? (state.banks[0]?.name ?? '') : ''
      const suggestion =
        action.payload === 'bank' ? suggestRoundUpAmount(state.banks, nextPaymentSource, state.createValue) : null
      return {
        ...state,
        createPaymentMode: action.payload,
        createPaymentSource: nextPaymentSource,
        createCardTag: '',
        createRoundUpAmount: suggestion ?? '',
      }
    }
    case 'SET_EDIT_MODE': {
      const nextPaymentSource = action.payload === 'bank' ? (state.banks[0]?.name ?? '') : ''
      const suggestion =
        action.payload === 'bank' ? suggestRoundUpAmount(state.banks, nextPaymentSource, state.editValue) : null
      return {
        ...state,
        editPaymentMode: action.payload,
        editPaymentSource: nextPaymentSource,
        editCardTag: '',
        editRoundUpAmount: suggestion ?? '',
      }
    }
    case 'SHOW_CREATE_INCOME_FORM':
      return {
        ...state,
        isIncomeCreateFormOpen: true,
        editingIncomeId: null,
        saveIncomeError: null,
        isCreateFormOpen: false,
        editingId: null,
        saveError: null,
      }
    case 'CANCEL_CREATE_INCOME_FORM':
      return { ...state, ...BLANK_CREATE_INCOME_FORM, isIncomeCreateFormOpen: false, createIncomeError: null }
    case 'SET_CREATE_INCOME_FIELD': {
      const next = { ...state, [action.payload.field]: action.payload.value }
      if (
        action.payload.field === 'createIncomeSource' &&
        !INCOME_SOURCES_WITH_GROSS_VALUE.includes(action.payload.value)
      ) {
        next.createIncomeGrossValue = ''
      }
      return next
    }
    case 'CREATE_INCOME_START':
      return { ...state, isCreatingIncome: true, createIncomeError: null }
    case 'CREATE_INCOME_SUCCESS':
      return { ...state, ...BLANK_CREATE_INCOME_FORM, isIncomeCreateFormOpen: false, isCreatingIncome: false }
    case 'CREATE_INCOME_ERROR':
      return { ...state, isCreatingIncome: false, createIncomeError: action.payload }
    case 'SHOW_EDIT_INCOME_FORM':
      return {
        ...state,
        isIncomeCreateFormOpen: false,
        editingIncomeId: action.payload.id,
        editIncomeDate: action.payload.date,
        editIncomeSource: action.payload.incomeSource,
        editIncomeGrossValue: action.payload.grossValue != null ? String(action.payload.grossValue) : '',
        editIncomeNetValue: String(action.payload.netValue),
        editIncomeBank: action.payload.bank,
        saveIncomeError: null,
        isCreateFormOpen: false,
        editingId: null,
        saveError: null,
      }
    case 'CANCEL_EDIT_INCOME':
      return {
        ...state,
        editingIncomeId: null,
        editIncomeDate: '',
        editIncomeSource: '',
        editIncomeGrossValue: '',
        editIncomeNetValue: '',
        editIncomeBank: '',
        saveIncomeError: null,
      }
    case 'SET_EDIT_INCOME_FIELD': {
      const next = { ...state, [action.payload.field]: action.payload.value }
      if (
        action.payload.field === 'editIncomeSource' &&
        !INCOME_SOURCES_WITH_GROSS_VALUE.includes(action.payload.value)
      ) {
        next.editIncomeGrossValue = ''
      }
      return next
    }
    case 'SAVE_INCOME_START':
      return { ...state, isSavingIncome: true, saveIncomeError: null }
    case 'SAVE_INCOME_SUCCESS':
      return {
        ...state,
        isSavingIncome: false,
        editingIncomeId: null,
        editIncomeDate: '',
        editIncomeSource: '',
        editIncomeGrossValue: '',
        editIncomeNetValue: '',
        editIncomeBank: '',
      }
    case 'SAVE_INCOME_ERROR':
      return { ...state, isSavingIncome: false, saveIncomeError: action.payload }
    default:
      return state
  }
}

export interface MonthlyData {
  year: number
  month: number
  monthInputValue: string
  setMonthInputValue: (value: string) => void
  expenses: ExpenseDto[]
  categoryTotals: CategoryTotalDto[]
  categoryTotalsSum: number
  cardStatements: CardStatementDto[]
  banks: BankDto[]
  adjustmentTotal: number
  bankTotals: BankTotal[]
  bankTotalsSum: number
  roundUpTotalsSum: number
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
  createRoundUpAmount: string
  createPaymentMode: PaymentMode
  isCreating: boolean
  createError: string | null
  showCreateForm: () => void
  cancelCreateForm: () => void
  setCreateField: (field: CreateFormField, value: string) => void
  setCreatePaymentMode: (mode: PaymentMode) => void
  submitCreate: () => void
  editingId: string | null
  editDate: string
  editDescription: string
  editValue: string
  editCategory: string
  editPaymentSource: string
  editCardTag: string
  editRoundUpAmount: string
  editPaymentMode: PaymentMode
  editIsSettled: boolean
  isSaving: boolean
  saveError: string | null
  setEditField: (field: EditField, value: string) => void
  setEditPaymentMode: (mode: PaymentMode) => void
  showEditForm: (expense: ExpenseDto) => void
  cancelEdit: () => void
  saveEdit: () => void
  deleteExpense: (id: string) => void
  markPaidSources: Record<string, string>
  setMarkPaidSource: (id: string, value: string) => void
  markStatementPaid: (id: string, paymentSource: string) => void
  unmarkStatementPaid: (id: string) => void
  incomes: IncomeDto[]
  incomeTotals: IncomeTotal[]
  totalIncoming: number
  titheSummary: TitheSummaryDto | null
  isIncomeCreateFormOpen: boolean
  createIncomeDate: string
  createIncomeSource: string
  createIncomeGrossValue: string
  createIncomeNetValue: string
  createIncomeBank: string
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
  isSavingIncome: boolean
  saveIncomeError: string | null
  setEditIncomeField: (field: EditIncomeField, value: string) => void
  showEditIncomeForm: (income: IncomeDto) => void
  cancelEditIncome: () => void
  saveEditIncome: () => void
  deleteIncome: (id: string) => void
}

export function useMonthly(): MonthlyData {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, undefined, buildInitialState)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void Promise.all([
      apiClient.getExpensesByMonth(state.year, state.month),
      apiClient.getCategoryTotalsByMonth(state.year, state.month),
      apiClient.getCardStatementsByMonth(state.year, state.month),
      apiClient.getBanks(),
      apiClient.getIncomesByMonth(state.year, state.month),
      apiClient.getBankBalancesByMonth(state.year, state.month),
      apiClient.getTitheSummaryByMonth(state.year, state.month),
    ])
      .then(([expenses, categoryTotals, cardStatements, banks, incomes, bankBalances, titheSummary]) =>
        dispatch({
          type: 'FETCH_SUCCESS',
          payload: { expenses, categoryTotals, cardStatements, banks, incomes, bankBalances, titheSummary },
        }),
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

  const setCreatePaymentMode = useCallback(
    (mode: PaymentMode) => dispatch({ type: 'SET_CREATE_MODE', payload: mode }),
    [],
  )

  const setEditPaymentMode = useCallback(
    (mode: PaymentMode) => dispatch({ type: 'SET_EDIT_MODE', payload: mode }),
    [],
  )

  function submitCreate() {
    const {
      createDate,
      createDescription,
      createValue,
      createCategory,
      createPaymentMode,
      createPaymentSource,
      createCardTag,
      createRoundUpAmount,
      banks,
    } = state

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

    if (createPaymentMode === 'card' && createCardTag.trim() === '') {
      dispatch({ type: 'CREATE_ERROR', payload: 'Card is required' })
      return
    }

    const selectedBank = banks.find((b) => b.name === createPaymentSource)
    const roundUpEligible = createPaymentMode === 'bank' && selectedBank?.roundUpEnabled === true

    let roundUpAmount: number | null = null
    if (roundUpEligible && createRoundUpAmount.trim() !== '') {
      const parsedRoundUp = Number(createRoundUpAmount)
      if (!isFinite(parsedRoundUp) || parsedRoundUp < MIN_ROUND_UP_AMOUNT || parsedRoundUp > MAX_ROUND_UP_AMOUNT) {
        dispatch({
          type: 'CREATE_ERROR',
          payload: `Round-up amount must be between £${MIN_ROUND_UP_AMOUNT.toFixed(2)} and £${MAX_ROUND_UP_AMOUNT.toFixed(2)}`,
        })
        return
      }
      roundUpAmount = parsedRoundUp
    }

    dispatch({ type: 'CREATE_START' })

    void apiClient
      .createExpense({
        date: createDate,
        description: createDescription,
        value,
        category: createCategory,
        paymentSource: createPaymentMode === 'bank' ? createPaymentSource : null,
        cardTag: createPaymentMode === 'card' ? createCardTag : null,
        roundUpAmount,
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

    if (!state.editIsSettled && state.editPaymentMode === 'card' && state.editCardTag.trim() === '') {
      dispatch({ type: 'SAVE_ERROR', payload: 'Card is required' })
      return
    }

    // A settled expense's payment fields are frozen: send them back unchanged so only
    // the editable fields move; the server rejects any payment-field change until the
    // statement is unmarked paid.
    const paymentFields = state.editIsSettled
      ? {
          paymentSource: state.editPaymentSource.trim() === '' ? null : state.editPaymentSource,
          cardTag: state.editCardTag.trim() === '' ? null : state.editCardTag,
        }
      : {
          paymentSource: state.editPaymentMode === 'bank' ? state.editPaymentSource : null,
          cardTag: state.editPaymentMode === 'card' ? state.editCardTag : null,
        }

    const selectedBank = state.banks.find((b) => b.name === state.editPaymentSource)
    const roundUpEligible =
      !state.editIsSettled && state.editPaymentMode === 'bank' && selectedBank?.roundUpEnabled === true

    let roundUpAmount: number | null = null
    if (roundUpEligible && state.editRoundUpAmount.trim() !== '') {
      const parsedRoundUp = Number(state.editRoundUpAmount)
      if (!isFinite(parsedRoundUp) || parsedRoundUp < MIN_ROUND_UP_AMOUNT || parsedRoundUp > MAX_ROUND_UP_AMOUNT) {
        dispatch({
          type: 'SAVE_ERROR',
          payload: `Round-up amount must be between £${MIN_ROUND_UP_AMOUNT.toFixed(2)} and £${MAX_ROUND_UP_AMOUNT.toFixed(2)}`,
        })
        return
      }
      roundUpAmount = parsedRoundUp
    }

    dispatch({ type: 'SAVE_START' })

    void apiClient
      .updateExpense(state.editingId, {
        date: state.editDate,
        description: state.editDescription,
        value,
        category: state.editCategory,
        ...paymentFields,
        roundUpAmount,
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

  const setMarkPaidSource = useCallback(
    (id: string, value: string) => dispatch({ type: 'SET_MARK_PAID_SOURCE', payload: { id, value } }),
    [],
  )

  const markStatementPaid = useCallback(
    (id: string, paymentSource: string) => {
      void apiClient
        .markCardStatementPaid(id, { paymentSource })
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

  const unmarkStatementPaid = useCallback(
    (id: string) => {
      if (!window.confirm('Unmark this statement as paid? Its settled charges revert to unsettled.')) return

      void apiClient
        .unmarkCardStatementPaid(id)
        .then(() => dispatch({ type: 'RETRY' }))
        .catch((err: unknown) => {
          dispatch({
            type: 'MARK_PAID_ERROR',
            payload: err instanceof Error ? err.message : 'Failed to unmark statement paid',
          })
        })
    },
    [apiClient],
  )

  const showCreateIncomeForm = useCallback(() => dispatch({ type: 'SHOW_CREATE_INCOME_FORM' }), [])

  const cancelCreateIncomeForm = useCallback(() => dispatch({ type: 'CANCEL_CREATE_INCOME_FORM' }), [])

  const setCreateIncomeField = useCallback(
    (field: CreateIncomeField, value: string) =>
      dispatch({ type: 'SET_CREATE_INCOME_FIELD', payload: { field, value } }),
    [],
  )

  function submitCreateIncome() {
    const { createIncomeDate, createIncomeSource, createIncomeGrossValue, createIncomeNetValue, createIncomeBank } =
      state

    if (!createIncomeDate.trim()) {
      dispatch({ type: 'CREATE_INCOME_ERROR', payload: 'Date is required' })
      return
    }

    if (!createIncomeSource.trim()) {
      dispatch({ type: 'CREATE_INCOME_ERROR', payload: 'Income source is required' })
      return
    }

    if (!createIncomeBank.trim()) {
      dispatch({ type: 'CREATE_INCOME_ERROR', payload: 'Bank is required' })
      return
    }

    const netValue = Number(createIncomeNetValue)
    if (!createIncomeNetValue.trim() || !isFinite(netValue) || netValue < 0) {
      dispatch({ type: 'CREATE_INCOME_ERROR', payload: 'Net value must be a non-negative number' })
      return
    }

    let grossValue: number | null = null
    if (createIncomeGrossValue.trim() !== '') {
      const parsedGrossValue = Number(createIncomeGrossValue)
      if (!isFinite(parsedGrossValue) || parsedGrossValue < netValue) {
        dispatch({ type: 'CREATE_INCOME_ERROR', payload: 'Gross value must be at least the net value' })
        return
      }
      grossValue = parsedGrossValue
    }

    dispatch({ type: 'CREATE_INCOME_START' })

    void apiClient
      .createIncome({
        date: createIncomeDate,
        incomeSource: createIncomeSource,
        grossValue,
        netValue,
        bank: createIncomeBank,
      })
      .then(() => {
        dispatch({ type: 'CREATE_INCOME_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'CREATE_INCOME_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to create income',
        })
      })
  }

  const setEditIncomeField = useCallback(
    (field: EditIncomeField, value: string) => dispatch({ type: 'SET_EDIT_INCOME_FIELD', payload: { field, value } }),
    [],
  )

  const showEditIncomeForm = useCallback(
    (income: IncomeDto) => dispatch({ type: 'SHOW_EDIT_INCOME_FORM', payload: income }),
    [],
  )

  const cancelEditIncome = useCallback(() => dispatch({ type: 'CANCEL_EDIT_INCOME' }), [])

  function saveEditIncome() {
    if (!state.editingIncomeId) return

    if (!state.editIncomeDate.trim()) {
      dispatch({ type: 'SAVE_INCOME_ERROR', payload: 'Date is required' })
      return
    }

    if (!state.editIncomeSource.trim()) {
      dispatch({ type: 'SAVE_INCOME_ERROR', payload: 'Income source is required' })
      return
    }

    if (!state.editIncomeBank.trim()) {
      dispatch({ type: 'SAVE_INCOME_ERROR', payload: 'Bank is required' })
      return
    }

    const netValue = Number(state.editIncomeNetValue)
    if (!state.editIncomeNetValue.trim() || !isFinite(netValue) || netValue < 0) {
      dispatch({ type: 'SAVE_INCOME_ERROR', payload: 'Net value must be a non-negative number' })
      return
    }

    let grossValue: number | null = null
    if (state.editIncomeGrossValue.trim() !== '') {
      const parsedGrossValue = Number(state.editIncomeGrossValue)
      if (!isFinite(parsedGrossValue) || parsedGrossValue < netValue) {
        dispatch({ type: 'SAVE_INCOME_ERROR', payload: 'Gross value must be at least the net value' })
        return
      }
      grossValue = parsedGrossValue
    }

    dispatch({ type: 'SAVE_INCOME_START' })

    void apiClient
      .updateIncome(state.editingIncomeId, {
        date: state.editIncomeDate,
        incomeSource: state.editIncomeSource,
        grossValue,
        netValue,
        bank: state.editIncomeBank,
      })
      .then(() => {
        dispatch({ type: 'SAVE_INCOME_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_INCOME_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to update income',
        })
      })
  }

  const deleteIncome = useCallback(
    (id: string) => {
      if (!window.confirm('Delete this income entry?')) return

      void apiClient
        .deleteIncome(id)
        .then(() => dispatch({ type: 'RETRY' }))
        .catch((err: unknown) => {
          dispatch({
            type: 'SAVE_INCOME_ERROR',
            payload: err instanceof Error ? err.message : 'Failed to delete income',
          })
        })
    },
    [apiClient],
  )

  const adjustmentTotal = state.cardStatements.reduce((sum, statement) => sum + statement.outstandingTotal, 0)

  const categoryTotalsSum = state.categoryTotals.reduce((sum, c) => sum + c.totalValue, 0)

  const bankTotals: BankTotal[] = state.banks.map((bank) => {
    const bankExpenses = state.expenses.filter((expense) => expense.paymentSource === bank.name)
    const roundUpTotal = bankExpenses.reduce((sum, expense) => sum + (expense.roundUpAmount ?? 0), 0)
    const balance = state.bankBalances.find((b) => b.bank === bank.name)?.balance ?? 0
    return { bank: bank.name, balance, roundUpTotal }
  })
  const bankTotalsSum = bankTotals.reduce((sum, b) => sum + b.balance, 0)
  const roundUpTotalsSum = bankTotals.reduce((sum, b) => sum + b.roundUpTotal, 0)

  const incomeTotals: IncomeTotal[] = Array.from(
    state.incomes
      .reduce((bySource, income) => {
        const entry = bySource.get(income.incomeSource) ?? { netValue: 0, grossValue: 0, hasGross: false }
        entry.netValue += income.netValue
        if (income.grossValue != null) {
          entry.grossValue += income.grossValue
          entry.hasGross = true
        }
        bySource.set(income.incomeSource, entry)
        return bySource
      }, new Map<string, { netValue: number; grossValue: number; hasGross: boolean }>())
      .entries(),
  ).map(([source, v]) => ({
    source,
    netValue: v.netValue,
    grossValue: v.hasGross ? v.grossValue : null,
  }))
  const totalIncoming = incomeTotals.reduce((sum, i) => sum + i.netValue, 0)

  return {
    year: state.year,
    month: state.month,
    monthInputValue,
    setMonthInputValue,
    expenses: state.expenses,
    categoryTotals: state.categoryTotals,
    categoryTotalsSum,
    cardStatements: state.cardStatements,
    banks: state.banks,
    adjustmentTotal,
    bankTotals,
    bankTotalsSum,
    roundUpTotalsSum,
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
    createRoundUpAmount: state.createRoundUpAmount,
    createPaymentMode: state.createPaymentMode,
    setCreatePaymentMode,
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
    editRoundUpAmount: state.editRoundUpAmount,
    editPaymentMode: state.editPaymentMode,
    editIsSettled: state.editIsSettled,
    setEditPaymentMode,
    isSaving: state.isSaving,
    saveError: state.saveError,
    setEditField,
    showEditForm,
    cancelEdit,
    saveEdit,
    deleteExpense,
    markPaidSources: state.markPaidSources,
    setMarkPaidSource,
    markStatementPaid,
    unmarkStatementPaid,
    incomes: state.incomes,
    incomeTotals,
    totalIncoming,
    titheSummary: state.titheSummary,
    isIncomeCreateFormOpen: state.isIncomeCreateFormOpen,
    createIncomeDate: state.createIncomeDate,
    createIncomeSource: state.createIncomeSource,
    createIncomeGrossValue: state.createIncomeGrossValue,
    createIncomeNetValue: state.createIncomeNetValue,
    createIncomeBank: state.createIncomeBank,
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
    isSavingIncome: state.isSavingIncome,
    saveIncomeError: state.saveIncomeError,
    setEditIncomeField,
    showEditIncomeForm,
    cancelEditIncome,
    saveEditIncome,
    deleteIncome,
  }
}
