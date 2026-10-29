import { apiClient } from '../api/financialApiClient'
import type { AccountDto } from '../api/types'
import { useCrudResource } from './useCrudResource'

export type AccountsStatus = 'loading' | 'failed' | 'ready'

export interface AccountsData {
  accounts: AccountDto[]
  status: AccountsStatus
  error: string | null
  retry: () => void
}

const NO_ACCOUNTS: AccountDto[] = []

export function useAccounts(): AccountsData {
  const resource = useCrudResource({
    load: () => apiClient.getAccounts(),
    initialData: NO_ACCOUNTS,
    loadErrorMessage: 'Unable to load accounts',
  })
  const status: AccountsStatus = resource.error ? 'failed' : resource.isLoading ? 'loading' : 'ready'
  return { accounts: resource.data, status, error: resource.error, retry: resource.retry }
}
