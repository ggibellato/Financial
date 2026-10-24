import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountCreateDto, AccountDto, AccountTemplateDto, AccountUpdateDto } from '../api/types'
import { useCrudResource } from './useCrudResource'

export interface AdminAccountsData {
  accounts: AccountDto[]
  templates: AccountTemplateDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createAccount: (request: AccountCreateDto) => Promise<AccountDto>
  updateAccount: (id: string, request: AccountUpdateDto) => Promise<AccountDto>
  deactivateAccount: (account: AccountDto) => Promise<AccountDto>
  deletingId: string | null
  deleteError: string | null
  deleteAccount: (id: string, onDeleted: () => void) => void
}

interface AccountsAndTemplates {
  accounts: AccountDto[]
  templates: AccountTemplateDto[]
}

const NOTHING_LOADED: AccountsAndTemplates = { accounts: [], templates: [] }

async function loadAccountsAndTemplates(): Promise<AccountsAndTemplates> {
  const [accounts, templates] = await Promise.all([apiClient.getAccounts(), apiClient.getAccountTemplates()])
  return { accounts, templates }
}

function toUpdateRequest(account: AccountDto): AccountUpdateDto {
  return {
    templateName: account.templateName,
    name: account.name,
    currency: account.currency,
    openingBalance: account.openingBalance,
    balanceStartDate: account.balanceStartDate,
    roundUpEnabled: account.roundUpEnabled,
    isActive: account.isActive,
  }
}

export function useAdminAccounts(): AdminAccountsData {
  const resource = useCrudResource({
    load: loadAccountsAndTemplates,
    initialData: NOTHING_LOADED,
    loadErrorMessage: 'Unable to load accounts',
    refreshMode: 'silent',
  })
  const { mutateAndRefresh, runPendingMutation } = resource

  const createAccount = useCallback(
    (request: AccountCreateDto) => mutateAndRefresh(() => apiClient.createAccount(request)),
    [mutateAndRefresh],
  )

  const updateAccount = useCallback(
    (id: string, request: AccountUpdateDto) => mutateAndRefresh(() => apiClient.updateAccount(id, request)),
    [mutateAndRefresh],
  )

  const deactivateAccount = useCallback(
    (account: AccountDto) => updateAccount(account.id, { ...toUpdateRequest(account), isActive: false }),
    [updateAccount],
  )

  const deleteAccount = useCallback(
    (id: string, onDeleted: () => void) =>
      runPendingMutation(id, () => apiClient.deleteAccount(id), 'The account could not be deleted.', onDeleted),
    [runPendingMutation],
  )

  return {
    accounts: resource.data.accounts,
    templates: resource.data.templates,
    isLoading: resource.isLoading,
    error: resource.error,
    retry: resource.retry,
    createAccount,
    updateAccount,
    deactivateAccount,
    deletingId: resource.pendingId,
    deleteError: resource.pendingError,
    deleteAccount,
  }
}
