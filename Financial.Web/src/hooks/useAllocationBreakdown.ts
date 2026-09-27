import { apiClient } from '../api/financialApiClient'
import type { AllocationBreakdownDto, BrokerCurrencyFilter, Currency } from '../api/types'
import { useAsyncResource } from './useAsyncResource'

export interface AllocationBreakdownData {
  breakdown: AllocationBreakdownDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useAllocationBreakdown(
  displayCurrency: Currency | null,
  brokerCurrencyFilter: BrokerCurrencyFilter,
): AllocationBreakdownData {
  const { data, isLoading, error, retry } = useAsyncResource<AllocationBreakdownDto>(
    () =>
      displayCurrency
        ? apiClient.getAllocationBreakdown(displayCurrency, brokerCurrencyFilter === 'ALL' ? undefined : brokerCurrencyFilter)
        : null,
    [displayCurrency, brokerCurrencyFilter],
    'Unable to load allocation breakdown',
  )

  return { breakdown: data, isLoading, error, retry }
}
