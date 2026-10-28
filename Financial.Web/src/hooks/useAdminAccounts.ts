import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountCreateDto, AccountDto, AccountTemplateDto, AccountUpdateDto, CategoryDto } from '../api/types'
import { useCrudResource } from './useCrudResource'

export interface AdminAccountsData {
  accounts: AccountDto[]
  templates: AccountTemplateDto[]
  categories: CategoryDto[]
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

interface AccountsReferenceData {
  accounts: AccountDto[]
  templates: AccountTemplateDto[]
  categories: CategoryDto[]
}

const NOTHING_LOADED: AccountsReferenceData = { accounts: [], templates: [], categories: [] }

async function loadAccountsReferenceData(): Promise<AccountsReferenceData> {
  const [accounts, templates, categories] = await Promise.all([
    apiClient.getAccounts(),
    apiClient.getAccountTemplates(),
    apiClient.getCategories(),
  ])
  return { accounts, templates, categories }
}

function toUpdateRequest(account: AccountDto): AccountUpdateDto {
  return {
    templateName: account.templateName,
    name: account.name,
    currency: account.currency,
    openingBalance: account.openingBalance,
    balanceStartDate: account.balanceStartDate,
    roundUpEnabled: account.roundUpEnabled,
    categoryId: account.categoryId,
    isActive: account.isActive,
  }
}

export function useAdminAccounts(): AdminAccountsData {
  const resource = useCrudResource({
    load: loadAccountsReferenceData,
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
    categories: resource.data.categories,
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
