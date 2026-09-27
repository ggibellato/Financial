import { apiClient } from '../api/financialApiClient'
import type { BrokerCurrencyFilter, Currency, PortfolioDashboardDto } from '../api/types'
import { useAsyncResource } from './useAsyncResource'

export interface DashboardSummaryData {
  summary: PortfolioDashboardDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useDashboardSummary(
  displayCurrency: Currency | null,
  brokerCurrencyFilter: BrokerCurrencyFilter,
): DashboardSummaryData {
  const { data, isLoading, error, retry } = useAsyncResource<PortfolioDashboardDto>(
    () =>
      displayCurrency
        ? apiClient.getDashboard(displayCurrency, brokerCurrencyFilter === 'ALL' ? undefined : brokerCurrencyFilter)
        : null,
    [displayCurrency, brokerCurrencyFilter],
    'Unable to load dashboard summary',
  )

  return { summary: data, isLoading, error, retry }
}
