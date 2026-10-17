import { useCallback, useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { CategoryAnnualAverageDto, CategoryAnnualTotalDto, IncomeAnnualSummaryDto, InvestmentAnnualResultDto } from '../api/types'
import { useAsyncResource } from './useAsyncResource'

interface AnnualSummaryFigures {
  categoryTotals: CategoryAnnualTotalDto[]
  incomeSummary: IncomeAnnualSummaryDto | null
  totalDespesasMonthly: number[]
  totalDespesasAnnualTotal: number
  totalDespesasAverage: number
  resultadoMonthly: number[]
  resultadoAnnualTotal: number
  resultadoAverage: number
  investmentAnnualResult: InvestmentAnnualResultDto | null
  historicSummaryAverage: CategoryAnnualAverageDto[]
}

const INITIAL_YEAR = new Date().getFullYear()

const EMPTY_FIGURES: AnnualSummaryFigures = {
  categoryTotals: [],
  incomeSummary: null,
  totalDespesasMonthly: [],
  totalDespesasAnnualTotal: 0,
  totalDespesasAverage: 0,
  resultadoMonthly: [],
  resultadoAnnualTotal: 0,
  resultadoAverage: 0,
  investmentAnnualResult: null,
  historicSummaryAverage: [],
}

function fetchAnnualSummaryFigures(year: number): Promise<AnnualSummaryFigures> {
  return Promise.all([
    apiClient.getCategoryTotalsAnnualForYear(year),
    apiClient.getInvestmentAnnualResultForYear(year),
    apiClient.getHistoricSummaryAverageFromYear(year),
  ]).then(([categoryTotalsAnnual, investmentAnnualResult, historicSummaryAverage]) => ({
    categoryTotals: categoryTotalsAnnual.categoryTotals,
    incomeSummary: categoryTotalsAnnual.incomeSummary,
    totalDespesasMonthly: categoryTotalsAnnual.totalDespesasMonthly,
    totalDespesasAnnualTotal: categoryTotalsAnnual.totalDespesasAnnualTotal,
    totalDespesasAverage: categoryTotalsAnnual.totalDespesasAverage,
    resultadoMonthly: categoryTotalsAnnual.resultadoMonthly,
    resultadoAnnualTotal: categoryTotalsAnnual.resultadoAnnualTotal,
    resultadoAverage: categoryTotalsAnnual.resultadoAverage,
    investmentAnnualResult,
    historicSummaryAverage,
  }))
}

export interface AnnualSummaryData extends AnnualSummaryFigures {
  year: number
  setYear: (year: number) => void
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useAnnualSummary(): AnnualSummaryData {
  const [year, setValidYear] = useState(INITIAL_YEAR)

  const { data, isLoading, error, retry } = useAsyncResource<AnnualSummaryFigures>(
    () => fetchAnnualSummaryFigures(year),
    [year],
    'Unable to load Annual Summary data',
    { initialLoading: true, keepDataWhileLoading: true },
  )

  const setYear = useCallback((nextYear: number) => {
    if (!Number.isFinite(nextYear)) return
    setValidYear(nextYear)
  }, [])

  return { year, setYear, ...(data ?? EMPTY_FIGURES), isLoading, error, retry }
}
