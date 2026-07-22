import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { CategoryYearlyTotalDto, InvestmentDiffsYearlyDto } from '../api/types'

interface YearlySummaryState {
  year: number
  categoryTotals: CategoryYearlyTotalDto[]
  investmentDiffs: InvestmentDiffsYearlyDto | null
  isLoading: boolean
  error: string | null
  retryCount: number
}

type YearlySummaryAction =
  | { type: 'SET_YEAR'; payload: number }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: { categoryTotals: CategoryYearlyTotalDto[]; investmentDiffs: InvestmentDiffsYearlyDto } }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }

const INITIAL_STATE: YearlySummaryState = {
  year: new Date().getFullYear(),
  categoryTotals: [],
  investmentDiffs: null,
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
      }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    default:
      return state
  }
}

export interface YearlySummaryData {
  year: number
  setYear: (year: number) => void
  categoryTotals: CategoryYearlyTotalDto[]
  investmentDiffs: InvestmentDiffsYearlyDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useYearlySummary(): YearlySummaryData {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void Promise.all([apiClient.getCategoryTotalsForYear(state.year), apiClient.getInvestmentDiffsForYear(state.year)])
      .then(([categoryTotals, investmentDiffs]) =>
        dispatch({ type: 'FETCH_SUCCESS', payload: { categoryTotals, investmentDiffs } }),
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

  return {
    year: state.year,
    setYear,
    categoryTotals: state.categoryTotals,
    investmentDiffs: state.investmentDiffs,
    isLoading: state.isLoading,
    error: state.error,
    retry,
  }
}
