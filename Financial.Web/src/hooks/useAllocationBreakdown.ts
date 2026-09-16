import { apiClient } from '../api/financialApiClient'
import type { AllocationBreakdownDto } from '../api/types'
import { useAsyncResource } from './useAsyncResource'

export interface AllocationBreakdownData {
  breakdown: AllocationBreakdownDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useAllocationBreakdown(): AllocationBreakdownData {
  const { data, isLoading, error, retry } = useAsyncResource<AllocationBreakdownDto>(
    () => apiClient.getAllocationBreakdown(),
    [],
    'Unable to load allocation breakdown',
  )

  return { breakdown: data, isLoading, error, retry }
}
