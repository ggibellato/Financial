import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type {
  BankBalanceDto,
  BankDto,
  CardStatementDto,
  CategoryDto,
  CategoryTotalDto,
  ExpenseDto,
  IncomeDto,
  IncomeSourceDto,
  TitheSummaryDto,
} from '../api/types'
import { currentYearMonth, formatMonthInputValue, getErrorMessage, parseMonthInputValue } from '../utils/formatters'

export interface BankTotal {
  bankId: string
  bank: string
  balance: number
  roundUpTotal: number
}

export interface IncomeTotal {
  source: string
  netValue: number
  grossValue: number | null
}

interface MonthlyState {
  year: number
  month: number
  expenses: ExpenseDto[]
  unpaidCardCharges: ExpenseDto[]
  categoryTotals: CategoryTotalDto[]
  cardStatements: CardStatementDto[]
  banks: BankDto[]
  incomeSources: IncomeSourceDto[]
  categories: CategoryDto[]
  bankBalances: BankBalanceDto[]
  incomes: IncomeDto[]
  titheSummary: TitheSummaryDto | null
  isLoading: boolean
  error: string | null
  retryCount: number
  markPaidSources: Record<string, string>
  listActionError: string | null
  listActionWarning: string | null
}

type MonthlyAction =
  | { type: 'SET_MONTH'; payload: { year: number; month: number } }
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
        bankBalances: BankBalanceDto[]
        incomes: IncomeDto[]
        titheSummary: TitheSummaryDto
      }
    }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SET_MARK_PAID_SOURCE'; payload: { id: string; value: string } }
  | { type: 'LIST_ACTION_ERROR'; payload: string }
  | { type: 'LIST_ACTION_WARNING'; payload: string | null }

const INITIAL_STATE_BASE: Omit<MonthlyState, 'year' | 'month'> = {
  expenses: [],
  unpaidCardCharges: [],
  categoryTotals: [],
  cardStatements: [],
  banks: [],
  incomeSources: [],
  categories: [],
  bankBalances: [],
  incomes: [],
  titheSummary: null,
  isLoading: true,
  error: null,
  retryCount: 0,
  markPaidSources: {},
  listActionError: null,
  listActionWarning: null,
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
  bankTotalsSum: number
  roundUpTotalsSum: number
  isLoading: boolean
  error: string | null
  retry: () => void
  deleteExpense: (id: string) => void
  markPaidSources: Record<string, string>
  setMarkPaidSource: (id: string, value: string) => void
  markStatementPaid: (id: string, paymentSourceBankId: string) => void
  unmarkStatementPaid: (id: string) => void
  incomes: IncomeDto[]
  incomeTotals: IncomeTotal[]
  totalIncoming: number
  titheSummary: TitheSummaryDto | null
  deleteIncome: (id: string) => void
  listActionError: string | null
  listActionWarning: string | null
}

/** Fetches a month's cash-flow data and owns list-level mutations (delete, mark paid/unpaid).
 * Create/edit form state lives in {@link useExpenseForm} and {@link useIncomeForm}. */
export function useMonthly(): MonthlyData {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, undefined, buildInitialState)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void Promise.all([
      apiClient.getExpensesByMonth(state.year, state.month),
      apiClient.getUnpaidCardChargesByMonth(state.year, state.month),
      apiClient.getCategoryTotalsByMonth(state.year, state.month),
      apiClient.getCardStatementsByMonth(state.year, state.month),
      apiClient.getBanks(),
      apiClient.getIncomeSources(),
      apiClient.getCategories(),
      apiClient.getIncomesByMonth(state.year, state.month),
      apiClient.getBankBalancesByMonth(state.year, state.month),
      apiClient.getTitheSummaryByMonth(state.year, state.month),
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
          bankBalances,
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
              bankBalances,
              titheSummary,
            },
          }),
      )
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load Monthly data') })
      })
  }, [apiClient, state.year, state.month, state.retryCount])

  const monthInputValue = formatMonthInputValue(state.year, state.month)

  const setMonthInputValue = useCallback((value: string) => {
    const parsed = parseMonthInputValue(value)
    if (!parsed) return
    dispatch({ type: 'SET_MONTH', payload: parsed })
  }, [])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const deleteExpense = useCallback(
    (id: string) => {
      if (!window.confirm('Delete this expense?')) return

      void apiClient
        .deleteExpense(id)
        .then(() => dispatch({ type: 'RETRY' }))
        .catch((err: unknown) => {
          dispatch({ type: 'LIST_ACTION_ERROR', payload: getErrorMessage(err, 'Failed to delete expense') })
        })
    },
    [apiClient],
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
          dispatch({ type: 'RETRY' })
        })
        .catch((err: unknown) => {
          dispatch({ type: 'LIST_ACTION_ERROR', payload: getErrorMessage(err, 'Failed to mark statement paid') })
        })
    },
    [apiClient],
  )

  const unmarkStatementPaid = useCallback(
    (id: string) => {
      if (!window.confirm('Unmark this statement as paid? Its settled charges revert to unsettled.')) return

      void apiClient
        .unmarkCardStatementPaid(id)
        .then((statement) => {
          dispatch({ type: 'LIST_ACTION_WARNING', payload: statement.warning ?? null })
          dispatch({ type: 'RETRY' })
        })
        .catch((err: unknown) => {
          dispatch({ type: 'LIST_ACTION_ERROR', payload: getErrorMessage(err, 'Failed to unmark statement paid') })
        })
    },
    [apiClient],
  )

  const deleteIncome = useCallback(
    (id: string) => {
      if (!window.confirm('Delete this income entry?')) return

      void apiClient
        .deleteIncome(id)
        .then(() => dispatch({ type: 'RETRY' }))
        .catch((err: unknown) => {
          dispatch({ type: 'LIST_ACTION_ERROR', payload: getErrorMessage(err, 'Failed to delete income') })
        })
    },
    [apiClient],
  )

  const adjustmentTotal = state.cardStatements.reduce((sum, statement) => sum + statement.outstandingTotal, 0)

  const categoryTotalsSum = state.categoryTotals.reduce((sum, c) => sum + c.totalValue, 0)

  const bankTotals: BankTotal[] = state.banks.map((bank) => {
    const bankExpenses = state.expenses.filter((expense) => expense.paymentSourceBankId === bank.id)
    const roundUpTotal = bankExpenses.reduce((sum, expense) => sum + (expense.roundUpAmount ?? 0), 0)
    const balance = state.bankBalances.find((b) => b.bank === bank.name)?.balance ?? 0
    return { bankId: bank.id, bank: bank.name, balance, roundUpTotal }
  })
  const bankTotalsSum = bankTotals.reduce((sum, b) => sum + b.balance, 0)
  const roundUpTotalsSum = bankTotals.reduce((sum, b) => sum + b.roundUpTotal, 0)

  const incomeTotals: IncomeTotal[] = Array.from(
    state.incomes
      .reduce((bySource, income) => {
        const entry = bySource.get(income.incomeSourceName) ?? { netValue: 0, grossValue: 0, hasGross: false }
        entry.netValue += income.netValue
        if (income.grossValue != null) {
          entry.grossValue += income.grossValue
          entry.hasGross = true
        }
        bySource.set(income.incomeSourceName, entry)
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
    unpaidCardCharges: state.unpaidCardCharges,
    categoryTotals: state.categoryTotals,
    categoryTotalsSum,
    cardStatements: state.cardStatements,
    banks: state.banks,
    incomeSources: state.incomeSources,
    categories: state.categories,
    adjustmentTotal,
    bankTotals,
    bankTotalsSum,
    roundUpTotalsSum,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    deleteExpense,
    markPaidSources: state.markPaidSources,
    setMarkPaidSource,
    markStatementPaid,
    unmarkStatementPaid,
    incomes: state.incomes,
    incomeTotals,
    totalIncoming,
    titheSummary: state.titheSummary,
    deleteIncome,
    listActionError: state.listActionError,
    listActionWarning: state.listActionWarning,
  }
}
