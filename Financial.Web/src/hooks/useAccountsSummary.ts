import { apiClient } from '../api/financialApiClient'
import type { CategoryTotalDto, IncomeSourceTotalDto } from '../api/types'
import { type MonthResult, monthResult } from '../utils/monthResult'
import { useAsyncResource } from './useAsyncResource'

export const SUMMARY_LOAD_ERROR = 'Unable to load the summary.'

export interface AccountsSummaryData {
  categoryTotals: CategoryTotalDto[]
  categoryTotalsSum: number
  incomeTotals: IncomeSourceTotalDto[]
  totalIncoming: number
  result: MonthResult
  isLoading: boolean
  error: string | null
  retry: () => void
}

async function fetchSummary(year: number, month: number) {
  const [categoryTotals, summary] = await Promise.all([apiClient.getCategoryTotalsByMonth(year, month), apiClient.getDefaultGroupSummary(year, month)])
  return { categoryTotals, summary }
}

export function useAccountsSummary(year: number, month: number): AccountsSummaryData {
  const overview = useAsyncResource(() => fetchSummary(year, month), [year, month], SUMMARY_LOAD_ERROR, { initialLoading: true })
  const summary = overview.data?.summary
  const categoryTotalsSum = summary?.categoryTotalsSum ?? 0
  const totalIncoming = summary?.totalIncoming ?? 0

  return {
    categoryTotals: overview.data?.categoryTotals ?? [],
    categoryTotalsSum,
    incomeTotals: summary?.incomeTotals ?? [],
    totalIncoming,
    result: monthResult(totalIncoming, categoryTotalsSum),
    isLoading: overview.isLoading,
    error: overview.error,
    retry: overview.retry,
  }
}
