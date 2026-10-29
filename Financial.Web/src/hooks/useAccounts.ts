import { apiClient } from '../api/financialApiClient'
import type { AccountDto } from '../api/types'
import { useCrudResource } from './useCrudResource'

export interface AccountsData {
  accounts: AccountDto[]
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
  return { accounts: resource.data, error: resource.error, retry: resource.retry }
}
