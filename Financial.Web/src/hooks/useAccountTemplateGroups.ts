import { apiClient } from '../api/financialApiClient'
import { useAsyncResource } from './useAsyncResource'

export const ACCOUNTS_LOAD_ERROR = 'Unable to load the accounts'

export function useAccountTemplateGroups(year: number, month: number) {
  return useAsyncResource(() => apiClient.getAccountTemplateGroups(year, month), [year, month], ACCOUNTS_LOAD_ERROR, {
    initialLoading: true,
  })
}
