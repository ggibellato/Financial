import { apiClient } from '../api/financialApiClient'
import type { UpcomingIncomeDto } from '../api/types'
import { useAsyncResource } from './useAsyncResource'

export interface UpcomingIncomeData {
  entries: UpcomingIncomeDto[] | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useUpcomingIncome(): UpcomingIncomeData {
  const { data, isLoading, error, retry } = useAsyncResource<UpcomingIncomeDto[]>(
    () => apiClient.getUpcomingIncome(),
    [],
    'Unable to load upcoming income',
  )

  return { entries: data, isLoading, error, retry }
}
