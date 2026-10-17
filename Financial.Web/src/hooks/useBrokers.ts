import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { BrokerCreateDto, BrokerDto, BrokerUpdateDto, CostBasisMethod, InvestmentScope } from '../api/types'
import { useCrudResource } from './useCrudResource'

export interface BrokersData {
  brokers: BrokerDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createBroker: (request: BrokerCreateDto) => Promise<BrokerDto>
  updateBroker: (currentName: string, request: BrokerUpdateDto, scope?: InvestmentScope) => Promise<BrokerDto>
  setCostBasisMethod: (name: string, method: CostBasisMethod, scope?: InvestmentScope) => Promise<BrokerDto>
  deletingName: string | null
  deleteError: string | null
  deleteBroker: (name: string) => void
}

const NO_BROKERS: BrokerDto[] = []

export function useBrokers(): BrokersData {
  const resource = useCrudResource({
    load: () => apiClient.getAdminBrokers(),
    initialData: NO_BROKERS,
    loadErrorMessage: 'Unable to load brokers',
  })
  const { mutateAndRefresh, runPendingMutation } = resource

  const createBroker = useCallback(
    (request: BrokerCreateDto) => mutateAndRefresh(() => apiClient.createBroker(request)),
    [mutateAndRefresh],
  )

  const updateBroker = useCallback(
    (currentName: string, request: BrokerUpdateDto, scope?: InvestmentScope) =>
      mutateAndRefresh(() => apiClient.updateBroker(currentName, request, scope)),
    [mutateAndRefresh],
  )

  const setCostBasisMethod = useCallback(
    (name: string, method: CostBasisMethod, scope?: InvestmentScope) =>
      mutateAndRefresh(() => apiClient.setCostBasisMethod(name, { method }, scope)),
    [mutateAndRefresh],
  )

  const deleteBroker = useCallback(
    (name: string) => runPendingMutation(name, () => apiClient.deleteBroker(name), 'Failed to delete broker'),
    [runPendingMutation],
  )

  return {
    brokers: resource.data,
    isLoading: resource.isLoading,
    error: resource.error,
    retry: resource.retry,
    createBroker,
    updateBroker,
    setCostBasisMethod,
    deletingName: resource.pendingId,
    deleteError: resource.pendingError,
    deleteBroker,
  }
}
