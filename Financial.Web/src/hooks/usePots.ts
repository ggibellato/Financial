import { apiClient } from '../api/financialApiClient'
import { useAsyncResource } from './useAsyncResource'

export const POTS_LOAD_ERROR = 'Unable to load the pots'

export function usePots(potsAccountId: string | null, year: number, month: number) {
  return useAsyncResource(
    () => (potsAccountId ? apiClient.getPots(potsAccountId, year, month) : null),
    [potsAccountId, year, month],
    POTS_LOAD_ERROR,
  )
}
