import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type {
  AccountGroupSummaryDto,
  BankDto,
  CardStatementDto,
  CategoryDto,
  CategoryTotalDto,
  CurrencyTotalDto,
  ExpenseDto,
  IncomeDto,
  IncomeSourceDto,
  IncomeSourceTotalDto,
  TitheCarryForwardUpdateDto,
  TitheSummaryDto,
} from '../api/types'
import { currentYearMonth, getErrorMessage } from '../utils/formatters'
import { useMonthSelection } from './useMonthSelection'

export interface BankTotal {
  bankId: string
  bank: string
  currency: string
  balance: number
  roundUpTotal: number
}

interface MonthlyState {
  expenses: ExpenseDto[]
  unpaidCardCharges: ExpenseDto[]
  categoryTotals: CategoryTotalDto[]
  cardStatements: CardStatementDto[]
  banks: BankDto[]
  incomeSources: IncomeSourceDto[]
  categories: CategoryDto[]
  summary: AccountGroupSummaryDto | null
  incomes: IncomeDto[]
  titheSummary: TitheSummaryDto | null
  carryForwardUpdating: boolean
  isLoading: boolean
  error: string | null
  retryCount: number
  markPaidSources: Record<string, string>
  listActionError: string | null
  listActionWarning: string | null
}

type MonthlyAction =
  | { type: 'FETCH_START' }
  | {
      type: 'FETCH_SUCCESS'
      payload: {
        expenses: ExpenseDto[]
        unpaidCardCharges: ExpenseDto[]
        categoryTotals: CategoryTotalDto[]
        cardStatements: CardStatementDto[]
        banks: BankDto[]
        incomeSources: IncomeSourceDto[]
        categories: CategoryDto[]
        summary: AccountGroupSummaryDto
        incomes: IncomeDto[]
        titheSummary: TitheSummaryDto
      }
    }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SET_MARK_PAID_SOURCE'; payload: { id: string; value: string } }
  | { type: 'LIST_ACTION_ERROR'; payload: string }
  | { type: 'LIST_ACTION_WARNING'; payload: string | null }
  | { type: 'CARRY_FORWARD_UPDATE_START' }
  | { type: 'CARRY_FORWARD_UPDATE_END' }

const INITIAL_STATE: MonthlyState = {
  expenses: [],
  unpaidCardCharges: [],
  categoryTotals: [],
  cardStatements: [],
  banks: [],
  incomeSources: [],
  categories: [],
  summary: null,
  incomes: [],
  titheSummary: null,
  carryForwardUpdating: false,
  isLoading: true,
  error: null,
  retryCount: 0,
  markPaidSources: {},
  listActionError: null,
  listActionWarning: null,
}

function reducer(state: MonthlyState, action: MonthlyAction): MonthlyState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, ...action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'SET_MARK_PAID_SOURCE':
      return { ...state, markPaidSources: { ...state.markPaidSources, [action.payload.id]: action.payload.value } }
    case 'LIST_ACTION_ERROR':
      return { ...state, listActionError: action.payload, listActionWarning: null }
    case 'LIST_ACTION_WARNING':
      return { ...state, listActionWarning: action.payload, listActionError: null }
    case 'CARRY_FORWARD_UPDATE_START':
      return { ...state, carryForwardUpdating: true }
    case 'CARRY_FORWARD_UPDATE_END':
      return { ...state, carryForwardUpdating: false }
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
  unpaidCardCharges: ExpenseDto[]
  categoryTotals: CategoryTotalDto[]
  categoryTotalsSum: number
  cardStatements: CardStatementDto[]
  banks: BankDto[]
  incomeSources: IncomeSourceDto[]
  categories: CategoryDto[]
  adjustmentTotal: number
  bankTotals: BankTotal[]
  bankCurrencyTotals: CurrencyTotalDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  refreshSilently: () => void
  deleteExpense: (id: string) => void
  markPaidSources: Record<string, string>
  setMarkPaidSource: (id: string, value: string) => void
  markStatementPaid: (id: string, paymentSourceBankId: string) => void
  unmarkStatementPaid: (id: string) => void
  incomes: IncomeDto[]
  incomeTotals: IncomeSourceTotalDto[]
  totalIncoming: number
  titheSummary: TitheSummaryDto | null
  carryForwardUpdating: boolean
  updateCarryForwardInclusion: (included: boolean) => void
  deleteIncome: (id: string) => void
  listActionError: string | null
  listActionWarning: string | null
}

export function useMonthly(): MonthlyData {
  // Passed as a function so the default period is read at mount, not module load: it stays fresh
  // in a long-running session and can be pinned with fake timers in tests.
  const { year, month, monthInputValue, setMonthInputValue } = useMonthSelection(currentYearMonth)
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const fetchMonthlyData = useCallback((year: number, month: number) => {
    return Promise.all([
      apiClient.getExpensesByMonth(year, month),
      apiClient.getUnpaidCardChargesByMonth(year, month),
      apiClient.getCategoryTotalsByMonth(year, month),
      apiClient.getCardStatementsByMonth(year, month),
      apiClient.getBanks(),
      apiClient.getIncomeSources(),
      apiClient.getCategories(),
      apiClient.getIncomesByMonth(year, month),
      apiClient.getDefaultGroupSummary(year, month),
      apiClient.getTitheSummaryByMonth(year, month),
    ])
      .then(
        ([
          expenses,
          unpaidCardCharges,
          categoryTotals,
          cardStatements,
          banks,
          incomeSources,
          categories,
          incomes,
          summary,
          titheSummary,
        ]) =>
          dispatch({
            type: 'FETCH_SUCCESS',
            payload: {
              expenses,
              unpaidCardCharges,
              categoryTotals,
              cardStatements,
              banks,
              incomeSources,
              categories,
              incomes,
              summary,
              titheSummary,
            },
          }),
      )
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load Monthly data') })
      })
  }, [])

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void fetchMonthlyData(year, month)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [year, month, state.retryCount])

  // Re-fetches after a mutation (add/edit/delete) without flipping isLoading, so the grid stays
  // mounted and its sort/filter state (owned by the section components) survives the refresh -
  // unlike retry(), which is for recovering from a genuine fetch error and should show a reload.
  const refreshSilently = useCallback(
    () => fetchMonthlyData(year, month),
    [fetchMonthlyData, year, month],
  )

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const deleteExpense = useCallback(
    (id: string) => {
      void apiClient
        .deleteExpense(id)
        .then(() => refreshSilently())
        .catch((err: unknown) => {
          dispatch({ type: 'LIST_ACTION_ERROR', payload: getErrorMessage(err, 'Failed to delete expense') })
        })
    },
    [refreshSilently],
  )

  const setMarkPaidSource = useCallback(
    (id: string, value: string) => dispatch({ type: 'SET_MARK_PAID_SOURCE', payload: { id, value } }),
    [],
  )

  const markStatementPaid = useCallback(
    (id: string, paymentSourceBankId: string) => {
      void apiClient
        .markCardStatementPaid(id, { paymentSourceBankId })
        .then((statement) => {
          dispatch({ type: 'LIST_ACTION_WARNING', payload: statement.warning ?? null })
          void refreshSilently()
        })
        .catch((err: unknown) => {
          dispatch({ type: 'LIST_ACTION_ERROR', payload: getErrorMessage(err, 'Failed to mark statement paid') })
        })
    },
    [refreshSilently],
  )

  const unmarkStatementPaid = useCallback(
    (id: string) => {
      void apiClient
        .unmarkCardStatementPaid(id)
        .then((statement) => {
          dispatch({ type: 'LIST_ACTION_WARNING', payload: statement.warning ?? null })
          void refreshSilently()
        })
        .catch((err: unknown) => {
          dispatch({ type: 'LIST_ACTION_ERROR', payload: getErrorMessage(err, 'Failed to unmark statement paid') })
        })
    },
    [refreshSilently],
  )

  const updateCarryForwardInclusion = useCallback(
    (included: boolean) => {
      dispatch({ type: 'CARRY_FORWARD_UPDATE_START' })
      const requestBody: TitheCarryForwardUpdateDto = { included }
      void apiClient
        .updateTitheCarryForward(year, month, requestBody)
        .then(() => {
          dispatch({ type: 'CARRY_FORWARD_UPDATE_END' })
          void refreshSilently()
        })
        .catch((err: unknown) => {
          dispatch({ type: 'CARRY_FORWARD_UPDATE_END' })
          dispatch({ type: 'LIST_ACTION_ERROR', payload: getErrorMessage(err, 'Failed to update carry-forward') })
        })
    },
    [year, month, refreshSilently],
  )

  const deleteIncome = useCallback(
    (id: string) => {
      void apiClient
        .deleteIncome(id)
        .then(() => refreshSilently())
        .catch((err: unknown) => {
          dispatch({ type: 'LIST_ACTION_ERROR', payload: getErrorMessage(err, 'Failed to delete income') })
        })
    },
    [refreshSilently],
  )

  const adjustmentTotal = state.cardStatements.reduce((sum, statement) => sum + statement.outstandingTotal, 0)

  const bankTotals: BankTotal[] = (state.summary?.members ?? []).map((member) => ({
    bankId: member.accountId,
    bank: member.name,
    currency: member.currency,
    balance: member.balance,
    roundUpTotal: member.roundUpTotal ?? 0,
  }))

  return {
    year,
    month,
    monthInputValue,
    setMonthInputValue,
    expenses: state.expenses,
    unpaidCardCharges: state.unpaidCardCharges,
    categoryTotals: state.categoryTotals,
    categoryTotalsSum: state.summary?.categoryTotalsSum ?? 0,
    cardStatements: state.cardStatements,
    banks: state.banks,
    incomeSources: state.incomeSources,
    categories: state.categories,
    adjustmentTotal,
    bankTotals,
    bankCurrencyTotals: state.summary?.totals ?? [],
    isLoading: state.isLoading,
    error: state.error,
    retry,
    refreshSilently,
    deleteExpense,
    markPaidSources: state.markPaidSources,
    setMarkPaidSource,
    markStatementPaid,
    unmarkStatementPaid,
    incomes: state.incomes,
    incomeTotals: state.summary?.incomeTotals ?? [],
    totalIncoming: state.summary?.totalIncoming ?? 0,
    titheSummary: state.titheSummary,
    carryForwardUpdating: state.carryForwardUpdating,
    updateCarryForwardInclusion,
    deleteIncome,
    listActionError: state.listActionError,
    listActionWarning: state.listActionWarning,
  }
}
