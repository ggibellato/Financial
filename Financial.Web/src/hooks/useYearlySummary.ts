import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { CategoryAnnualAverageDto, CategoryYearlyTotalDto, IncomeYearlySummaryDto, InvestmentDiffsYearlyDto } from '../api/types'

interface YearlySummaryState {
  year: number
  categoryTotals: CategoryYearlyTotalDto[]
  investmentDiffs: InvestmentDiffsYearlyDto | null
  incomeSummary: IncomeYearlySummaryDto | null
  historicSummaryAverage: CategoryAnnualAverageDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
}

type YearlySummaryAction =
  | { type: 'SET_YEAR'; payload: number }
  | { type: 'FETCH_START' }
  | {
      type: 'FETCH_SUCCESS'
      payload: {
        categoryTotals: CategoryYearlyTotalDto[]
        investmentDiffs: InvestmentDiffsYearlyDto
        incomeSummary: IncomeYearlySummaryDto
        historicSummaryAverage: CategoryAnnualAverageDto[]
      }
    }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }

const INITIAL_STATE: YearlySummaryState = {
  year: new Date().getFullYear(),
  categoryTotals: [],
  investmentDiffs: null,
  incomeSummary: null,
  historicSummaryAverage: [],
  isLoading: true,
  error: null,
  retryCount: 0,
}

function reducer(state: YearlySummaryState, action: YearlySummaryAction): YearlySummaryState {
  switch (action.type) {
    case 'SET_YEAR':
      return { ...state, year: action.payload }
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return {
        ...state,
        isLoading: false,
        categoryTotals: action.payload.categoryTotals,
        investmentDiffs: action.payload.investmentDiffs,
        incomeSummary: action.payload.incomeSummary,
        historicSummaryAverage: action.payload.historicSummaryAverage,
      }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    default:
      return state
  }
}

const INVESTIMENTO_CATEGORY = 'Investimento'

export interface YearlySummaryData {
  year: number
  setYear: (year: number) => void
  categoryTotals: CategoryYearlyTotalDto[]
  investmentDiffs: InvestmentDiffsYearlyDto | null
  incomeSummary: IncomeYearlySummaryDto | null
  historicSummaryAverage: CategoryAnnualAverageDto[]
  totalDespesasMonthly: number[]
  totalDespesasYearlyTotal: number
  resultadoMonthly: number[]
  resultadoYearlyTotal: number
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useYearlySummary(): YearlySummaryData {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void Promise.all([
      apiClient.getCategoryTotalsForYear(state.year),
      apiClient.getInvestmentDiffsForYear(state.year),
      apiClient.getIncomeSummaryForYear(state.year),
      apiClient.getHistoricSummaryAverageFromYear(state.year),
    ])
      .then(([categoryTotals, investmentDiffs, incomeSummary, historicSummaryAverage]) =>
        dispatch({ type: 'FETCH_SUCCESS', payload: { categoryTotals, investmentDiffs, incomeSummary, historicSummaryAverage } }),
      )
      .catch((err: unknown) => {
        dispatch({
          type: 'FETCH_ERROR',
          payload: err instanceof Error ? err.message : 'Unable to load Yearly Summary data',
        })
      })
  }, [apiClient, state.year, state.retryCount])

  const setYear = useCallback((year: number) => {
    if (!Number.isFinite(year)) return
    dispatch({ type: 'SET_YEAR', payload: year })
  }, [])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const totalDespesasMonthly = useMemo(
    () =>
      state.categoryTotals.length === 0
        ? []
        : Array.from({ length: 12 }, (_, month) =>
            state.categoryTotals.reduce((sum, c) => sum + c.monthlyTotals[month], 0),
          ),
    [state.categoryTotals],
  )

  const totalDespesasYearlyTotal = useMemo(
    () => totalDespesasMonthly.reduce((sum, v) => sum + v, 0),
    [totalDespesasMonthly],
  )

  const resultadoMonthly = useMemo(() => {
    if (!state.incomeSummary || totalDespesasMonthly.length === 0) return []
    const investimento = state.categoryTotals.find((c) => c.category === INVESTIMENTO_CATEGORY)
    return totalDespesasMonthly.map(
      (totalDespesas, month) =>
        state.incomeSummary!.salaryAfterTaxesMonthly[month] +
        state.incomeSummary!.dividendoJurosMonthly[month] -
        totalDespesas +
        (investimento?.monthlyTotals[month] ?? 0),
    )
  }, [state.incomeSummary, state.categoryTotals, totalDespesasMonthly])

  const resultadoYearlyTotal = useMemo(() => resultadoMonthly.reduce((sum, v) => sum + v, 0), [resultadoMonthly])

  return {
    year: state.year,
    setYear,
    categoryTotals: state.categoryTotals,
    investmentDiffs: state.investmentDiffs,
    incomeSummary: state.incomeSummary,
    historicSummaryAverage: state.historicSummaryAverage,
    totalDespesasMonthly,
    totalDespesasYearlyTotal,
    resultadoMonthly,
    resultadoYearlyTotal,
    isLoading: state.isLoading,
    error: state.error,
    retry,
  }
}
