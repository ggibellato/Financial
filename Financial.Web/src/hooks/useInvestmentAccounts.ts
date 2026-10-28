import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountDto, InvestmentAccountCreateDto, InvestmentAccountDto, InvestmentAccountUpdateDto } from '../api/types'
import { useCrudResource } from './useCrudResource'

export interface InvestmentAccountsData {
  investmentAccounts: InvestmentAccountDto[]
  accounts: AccountDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createInvestmentAccount: (request: InvestmentAccountCreateDto) => Promise<InvestmentAccountDto>
  updateInvestmentAccount: (id: string, request: InvestmentAccountUpdateDto) => Promise<InvestmentAccountDto>
  deletingId: string | null
  deleteError: string | null
  deleteInvestmentAccount: (id: string) => void
}

interface InvestmentAccountsWithAccounts {
  investmentAccounts: InvestmentAccountDto[]
  accounts: AccountDto[]
}

const NOTHING_LOADED: InvestmentAccountsWithAccounts = { investmentAccounts: [], accounts: [] }

async function loadInvestmentAccountsWithAccounts(): Promise<InvestmentAccountsWithAccounts> {
  const [investmentAccounts, accounts] = await Promise.all([apiClient.getInvestmentAccounts(), apiClient.getAccounts()])
  return { investmentAccounts, accounts }
}

export function useInvestmentAccounts(): InvestmentAccountsData {
  const resource = useCrudResource({
    load: loadInvestmentAccountsWithAccounts,
    initialData: NOTHING_LOADED,
    loadErrorMessage: 'Unable to load investment accounts',
  })
  const { mutateAndRefresh, runPendingMutation } = resource

  const createInvestmentAccount = useCallback(
    (request: InvestmentAccountCreateDto) => mutateAndRefresh(() => apiClient.createInvestmentAccount(request)),
    [mutateAndRefresh],
  )

  const updateInvestmentAccount = useCallback(
    (id: string, request: InvestmentAccountUpdateDto) =>
      mutateAndRefresh(() => apiClient.updateInvestmentAccount(id, request)),
    [mutateAndRefresh],
  )

  const deleteInvestmentAccount = useCallback(
    (id: string) =>
      runPendingMutation(id, () => apiClient.deleteInvestmentAccount(id), 'Failed to delete investment account'),
    [runPendingMutation],
  )

  return {
    investmentAccounts: resource.data.investmentAccounts,
    accounts: resource.data.accounts,
    isLoading: resource.isLoading,
    error: resource.error,
    retry: resource.retry,
    createInvestmentAccount,
    updateInvestmentAccount,
    deletingId: resource.pendingId,
    deleteError: resource.pendingError,
    deleteInvestmentAccount,
  }
}
