import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { RecurringBillCreateDto, RecurringBillDto, RecurringBillUpdateDto } from '../api/types'
import { useCrudResource } from './useCrudResource'

export interface RecurringBillsData {
  recurringBills: RecurringBillDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createRecurringBill: (request: RecurringBillCreateDto) => Promise<RecurringBillDto>
  updateRecurringBill: (id: string, request: RecurringBillUpdateDto) => Promise<RecurringBillDto>
  deletingId: string | null
  deleteError: string | null
  deleteRecurringBill: (id: string) => void
}

const NO_RECURRING_BILLS: RecurringBillDto[] = []

export function useRecurringBills(): RecurringBillsData {
  const resource = useCrudResource({
    load: () => apiClient.getMensaisBills(),
    initialData: NO_RECURRING_BILLS,
    loadErrorMessage: 'Unable to load recurring bills',
  })
  const { mutateAndRefresh, runPendingMutation } = resource

  const createRecurringBill = useCallback(
    (request: RecurringBillCreateDto) => mutateAndRefresh(() => apiClient.createMensaisBill(request)),
    [mutateAndRefresh],
  )

  const updateRecurringBill = useCallback(
    (id: string, request: RecurringBillUpdateDto) => mutateAndRefresh(() => apiClient.updateMensaisBill(id, request)),
    [mutateAndRefresh],
  )

  const deleteRecurringBill = useCallback(
    (id: string) => runPendingMutation(id, () => apiClient.deleteMensaisBill(id), 'Failed to delete recurring bill'),
    [runPendingMutation],
  )

  return {
    recurringBills: resource.data,
    isLoading: resource.isLoading,
    error: resource.error,
    retry: resource.retry,
    createRecurringBill,
    updateRecurringBill,
    deletingId: resource.pendingId,
    deleteError: resource.pendingError,
    deleteRecurringBill,
  }
}
