import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type {
  AccountGroupMemberDto,
  CategoryTotalDto,
  CurrencyTotalDto,
  IncomeSourceTotalDto,
  TitheSummaryDto,
} from '../api/types'
import { currentYearMonth, getErrorMessage } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'
import { useMonthSelection } from './useMonthSelection'

export interface BankTotal {
  bankId: string
  bank: string
  currency: string
  balance: number
  roundUpTotal: number
}

export interface MonthlyData {
  year: number
  month: number
  monthInputValue: string
  setMonthInputValue: (value: string) => void
  categoryTotals: CategoryTotalDto[]
  categoryTotalsSum: number
  bankTotals: BankTotal[]
  cardMembers: AccountGroupMemberDto[]
  currencyTotals: CurrencyTotalDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  incomeTotals: IncomeSourceTotalDto[]
  totalIncoming: number
  titheSummary: TitheSummaryDto | null
  carryForwardUpdating: boolean
  updateCarryForwardInclusion: (included: boolean) => void
  carryForwardError: string | null
}

async function fetchOverview(year: number, month: number) {
  const [categoryTotals, summary, titheSummary] = await Promise.all([
    apiClient.getCategoryTotalsByMonth(year, month),
    apiClient.getDefaultGroupSummary(year, month),
    apiClient.getTitheSummaryByMonth(year, month),
  ])
  return { categoryTotals, summary, titheSummary }
}

export function useMonthly(): MonthlyData {
  const { year, month, monthInputValue, setMonthInputValue } = useMonthSelection(currentYearMonth)
  const overview = useAsyncResource(() => fetchOverview(year, month), [year, month], 'Unable to load monthly data.', {
    initialLoading: true,
  })
  const [carryForwardUpdating, setCarryForwardUpdating] = useState(false)
  const [carryForwardFailure, setCarryForwardFailure] = useState<{ monthInputValue: string; message: string } | null>(null)

  const retry = () => {
    setCarryForwardFailure(null)
    overview.retry()
  }

  const updateCarryForwardInclusion = (included: boolean) => {
    setCarryForwardUpdating(true)
    setCarryForwardFailure(null)
    void apiClient
      .updateTitheCarryForward(year, month, { included })
      .then(() => overview.refresh())
      .catch((err: unknown) =>
        setCarryForwardFailure({ monthInputValue, message: getErrorMessage(err, 'Failed to update carry-forward.') }),
      )
      .finally(() => setCarryForwardUpdating(false))
  }

  const summary = overview.data?.summary
  const members = summary?.members ?? []

  const bankTotals: BankTotal[] = members
    .filter((member) => !member.statement)
    .map((member) => ({
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
    categoryTotals: overview.data?.categoryTotals ?? [],
    categoryTotalsSum: summary?.categoryTotalsSum ?? 0,
    bankTotals,
    cardMembers: members.filter((member) => member.statement),
    currencyTotals: summary?.totals ?? [],
    isLoading: overview.isLoading,
    error: overview.error,
    retry,
    incomeTotals: summary?.incomeTotals ?? [],
    totalIncoming: summary?.totalIncoming ?? 0,
    titheSummary: overview.data?.titheSummary ?? null,
    carryForwardUpdating,
    updateCarryForwardInclusion,
    carryForwardError: carryForwardFailure?.monthInputValue === monthInputValue ? carryForwardFailure.message : null,
  }
}
