import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { CategoryTotalDto, IncomeSourceTotalDto, TitheSummaryDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'
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
  titheSummary: TitheSummaryDto | null
  carryForwardUpdating: boolean
  updateCarryForwardInclusion: (included: boolean) => void
  carryForwardError: string | null
}

async function fetchSummary(year: number, month: number) {
  const [categoryTotals, summary, titheSummary] = await Promise.all([
    apiClient.getCategoryTotalsByMonth(year, month),
    apiClient.getDefaultGroupSummary(year, month),
    apiClient.getTitheSummaryByMonth(year, month),
  ])
  return { categoryTotals, summary, titheSummary }
}

export function useAccountsSummary(year: number, month: number): AccountsSummaryData {
  const overview = useAsyncResource(() => fetchSummary(year, month), [year, month], SUMMARY_LOAD_ERROR, { initialLoading: true })
  const [carryForwardUpdating, setCarryForwardUpdating] = useState(false)
  const [carryForwardError, setCarryForwardError] = useState<string | null>(null)

  const retry = () => {
    setCarryForwardError(null)
    overview.retry()
  }

  const updateCarryForwardInclusion = (included: boolean) => {
    setCarryForwardUpdating(true)
    setCarryForwardError(null)
    void apiClient
      .updateTitheCarryForward(year, month, { included })
      .then((titheSummary) => overview.setData((current) => current && { ...current, titheSummary }))
      .catch((err: unknown) => setCarryForwardError(getErrorMessage(err, 'Failed to update carry-forward.')))
      .finally(() => setCarryForwardUpdating(false))
  }

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
    retry,
    titheSummary: overview.data?.titheSummary ?? null,
    carryForwardUpdating,
    updateCarryForwardInclusion,
    carryForwardError,
  }
}
