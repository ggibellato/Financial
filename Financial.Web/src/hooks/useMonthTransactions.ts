import { apiClient } from '../api/financialApiClient'
import type { MonthTransactionDto } from '../api/types'
import { useAsyncResource } from './useAsyncResource'

export const MONTH_TRANSACTIONS_LOAD_ERROR = 'Unable to load the transactions'

const NO_ROWS: MonthTransactionDto[] = []

export interface MonthTransactions {
  rows: MonthTransactionDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useMonthTransactions(year: number, month: number, isOpen: boolean): MonthTransactions {
  const resource = useAsyncResource(
    () => (isOpen ? apiClient.getMonthTransactions(year, month) : null),
    [year, month, isOpen],
    MONTH_TRANSACTIONS_LOAD_ERROR,
    { initialLoading: isOpen },
  )
  const isAwaitingFirstFetch = isOpen && resource.data === null && resource.error === null
  return {
    rows: resource.data ?? NO_ROWS,
    isLoading: resource.isLoading || isAwaitingFirstFetch,
    error: resource.error,
    retry: resource.retry,
  }
}
