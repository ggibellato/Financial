import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { CategoryAnnualAverageDto, CategoryAnnualTotalDto, IncomeAnnualSummaryDto, InvestmentAnnualResultDto } from '../api/types'

interface AnnualSummaryState {
  year: number
  categoryTotals: CategoryAnnualTotalDto[]
  incomeSummary: IncomeAnnualSummaryDto | null
  totalDespesasMonthly: number[]
  totalDespesasAnnualTotal: number
  resultadoMonthly: number[]
  resultadoAnnualTotal: number
  investmentAnnualResult: InvestmentAnnualResultDto | null
  historicSummaryAverage: CategoryAnnualAverageDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
}

type AnnualSummaryAction =
  | { type: 'SET_YEAR'; payload: number }
  | { type: 'FETCH_START' }
  | {
      type: 'FETCH_SUCCESS'
      payload: {
        categoryTotals: CategoryAnnualTotalDto[]
        incomeSummary: IncomeAnnualSummaryDto
        totalDespesasMonthly: number[]
        totalDespesasAnnualTotal: number
        resultadoMonthly: number[]
        resultadoAnnualTotal: number
        investmentAnnualResult: InvestmentAnnualResultDto
        historicSummaryAverage: CategoryAnnualAverageDto[]
      }
    }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }

const INITIAL_STATE: AnnualSummaryState = {
  year: new Date().getFullYear(),
  categoryTotals: [],
  incomeSummary: null,
  totalDespesasMonthly: [],
  totalDespesasAnnualTotal: 0,
  resultadoMonthly: [],
  resultadoAnnualTotal: 0,
  investmentAnnualResult: null,
  historicSummaryAverage: [],
  isLoading: true,
  error: null,
  retryCount: 0,
}

function reducer(state: AnnualSummaryState, action: AnnualSummaryAction): AnnualSummaryState {
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
        incomeSummary: action.payload.incomeSummary,
        totalDespesasMonthly: action.payload.totalDespesasMonthly,
        totalDespesasAnnualTotal: action.payload.totalDespesasAnnualTotal,
        resultadoMonthly: action.payload.resultadoMonthly,
        resultadoAnnualTotal: action.payload.resultadoAnnualTotal,
        investmentAnnualResult: action.payload.investmentAnnualResult,
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

export interface AnnualSummaryData {
  year: number
  setYear: (year: number) => void
  categoryTotals: CategoryAnnualTotalDto[]
  incomeSummary: IncomeAnnualSummaryDto | null
  totalDespesasMonthly: number[]
  totalDespesasAnnualTotal: number
  resultadoMonthly: number[]
  resultadoAnnualTotal: number
  investmentAnnualResult: InvestmentAnnualResultDto | null
  historicSummaryAverage: CategoryAnnualAverageDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useAnnualSummary(): AnnualSummaryData {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void Promise.all([
      apiClient.getCategoryTotalsAnnualForYear(state.year),
      apiClient.getInvestmentAnnualResultForYear(state.year),
      apiClient.getHistoricSummaryAverageFromYear(state.year),
    ])
      .then(([categoryTotalsAnnual, investmentAnnualResult, historicSummaryAverage]) =>
        dispatch({
          type: 'FETCH_SUCCESS',
          payload: {
            categoryTotals: categoryTotalsAnnual.categoryTotals,
            incomeSummary: categoryTotalsAnnual.incomeSummary,
            totalDespesasMonthly: categoryTotalsAnnual.totalDespesasMonthly,
            totalDespesasAnnualTotal: categoryTotalsAnnual.totalDespesasAnnualTotal,
            resultadoMonthly: categoryTotalsAnnual.resultadoMonthly,
            resultadoAnnualTotal: categoryTotalsAnnual.resultadoAnnualTotal,
            investmentAnnualResult,
            historicSummaryAverage,
          },
        }),
      )
      .catch((err: unknown) => {
        dispatch({
          type: 'FETCH_ERROR',
          payload: err instanceof Error ? err.message : 'Unable to load Annual Summary data',
        })
      })
  }, [apiClient, state.year, state.retryCount])

  const setYear = useCallback((year: number) => {
    if (!Number.isFinite(year)) return
    dispatch({ type: 'SET_YEAR', payload: year })
  }, [])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  return {
    year: state.year,
    setYear,
    categoryTotals: state.categoryTotals,
    incomeSummary: state.incomeSummary,
    totalDespesasMonthly: state.totalDespesasMonthly,
    totalDespesasAnnualTotal: state.totalDespesasAnnualTotal,
    resultadoMonthly: state.resultadoMonthly,
    resultadoAnnualTotal: state.resultadoAnnualTotal,
    investmentAnnualResult: state.investmentAnnualResult,
    historicSummaryAverage: state.historicSummaryAverage,
    isLoading: state.isLoading,
    error: state.error,
    retry,
  }
}
