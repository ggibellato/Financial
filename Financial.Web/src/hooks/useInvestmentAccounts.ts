import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { InvestmentAccountCreateDto, InvestmentAccountDto, InvestmentAccountUpdateDto } from '../api/types'
import { useCrudResource } from './useCrudResource'

export interface InvestmentAccountsData {
  investmentAccounts: InvestmentAccountDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createInvestmentAccount: (request: InvestmentAccountCreateDto) => Promise<InvestmentAccountDto>
  updateInvestmentAccount: (id: string, request: InvestmentAccountUpdateDto) => Promise<InvestmentAccountDto>
  deletingId: string | null
  deleteError: string | null
  deleteInvestmentAccount: (id: string) => void
}

const NO_INVESTMENT_ACCOUNTS: InvestmentAccountDto[] = []

export function useInvestmentAccounts(): InvestmentAccountsData {
  const resource = useCrudResource({
    load: () => apiClient.getInvestmentAccounts(),
    initialData: NO_INVESTMENT_ACCOUNTS,
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
    investmentAccounts: resource.data,
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
