import { apiClient } from '../api/financialApiClient'
import type { PortfolioDashboardDto } from '../api/types'
import { useAsyncResource } from './useAsyncResource'

export interface DashboardSummaryData {
  summary: PortfolioDashboardDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useDashboardSummary(): DashboardSummaryData {
  const { data, isLoading, error, retry } = useAsyncResource<PortfolioDashboardDto>(
    () => apiClient.getDashboard(),
    [],
    'Unable to load dashboard summary',
  )

  return { summary: data, isLoading, error, retry }
}
