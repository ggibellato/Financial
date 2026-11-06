import { apiClient } from '../api/financialApiClient'
import { useAsyncResource } from './useAsyncResource'

export const MONTH_TRANSACTIONS_LOAD_ERROR = 'Unable to load the transactions'

export function useMonthTransactions(year: number, month: number) {
  return useAsyncResource(() => apiClient.getMonthTransactions(year, month), [year, month], MONTH_TRANSACTIONS_LOAD_ERROR, {
    initialLoading: true,
  })
}
