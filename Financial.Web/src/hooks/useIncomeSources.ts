import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { IncomeSourceCreateDto, IncomeSourceDto, IncomeSourceUpdateDto } from '../api/types'
import { useCrudResource } from './useCrudResource'

export interface IncomeSourcesData {
  incomeSources: IncomeSourceDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createIncomeSource: (request: IncomeSourceCreateDto) => Promise<IncomeSourceDto>
  updateIncomeSource: (id: string, request: IncomeSourceUpdateDto) => Promise<IncomeSourceDto>
  deletingId: string | null
  deleteError: string | null
  deleteIncomeSource: (id: string) => void
}

const NO_INCOME_SOURCES: IncomeSourceDto[] = []

export function useIncomeSources(): IncomeSourcesData {
  const resource = useCrudResource({
    load: () => apiClient.getIncomeSources(),
    initialData: NO_INCOME_SOURCES,
    loadErrorMessage: 'Unable to load income sources',
  })
  const { mutateAndRefresh, runPendingMutation } = resource

  const createIncomeSource = useCallback(
    (request: IncomeSourceCreateDto) => mutateAndRefresh(() => apiClient.createIncomeSource(request)),
    [mutateAndRefresh],
  )

  const updateIncomeSource = useCallback(
    (id: string, request: IncomeSourceUpdateDto) => mutateAndRefresh(() => apiClient.updateIncomeSource(id, request)),
    [mutateAndRefresh],
  )

  const deleteIncomeSource = useCallback(
    (id: string) => runPendingMutation(id, () => apiClient.deleteIncomeSource(id), 'Failed to delete income source'),
    [runPendingMutation],
  )

  return {
    incomeSources: resource.data,
    isLoading: resource.isLoading,
    error: resource.error,
    retry: resource.retry,
    createIncomeSource,
    updateIncomeSource,
    deletingId: resource.pendingId,
    deleteError: resource.pendingError,
    deleteIncomeSource,
  }
}
