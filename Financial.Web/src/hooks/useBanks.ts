import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { BankCreateDto, BankDto, BankUpdateDto } from '../api/types'
import { useCrudResource } from './useCrudResource'

export interface BanksData {
  banks: BankDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createBank: (request: BankCreateDto) => Promise<BankDto>
  updateBank: (id: string, request: BankUpdateDto) => Promise<BankDto>
  deletingId: string | null
  deleteError: string | null
  deleteBank: (id: string) => void
}

const NO_BANKS: BankDto[] = []

export function useBanks(): BanksData {
  const resource = useCrudResource({
    load: () => apiClient.getBanks(),
    initialData: NO_BANKS,
    loadErrorMessage: 'Unable to load banks',
  })
  const { mutateAndRefresh, runPendingMutation } = resource

  const createBank = useCallback(
    (request: BankCreateDto) => mutateAndRefresh(() => apiClient.createBank(request)),
    [mutateAndRefresh],
  )

  const updateBank = useCallback(
    (id: string, request: BankUpdateDto) => mutateAndRefresh(() => apiClient.updateBank(id, request)),
    [mutateAndRefresh],
  )

  const deleteBank = useCallback(
    (id: string) => runPendingMutation(id, () => apiClient.deleteBank(id), 'Failed to delete bank'),
    [runPendingMutation],
  )

  return {
    banks: resource.data,
    isLoading: resource.isLoading,
    error: resource.error,
    retry: resource.retry,
    createBank,
    updateBank,
    deletingId: resource.pendingId,
    deleteError: resource.pendingError,
    deleteBank,
  }
}
