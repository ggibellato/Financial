import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { CreditCardCreateDto, CreditCardDto, CreditCardUpdateDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'
import { useCrudResource, usePendingOperation } from './useCrudResource'

export interface CreditCardsData {
  creditCards: CreditCardDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  refreshSilently: () => void
  createCreditCard: (request: CreditCardCreateDto) => Promise<CreditCardDto>
  updatingCardId: string | null
  updateError: string | null
  updateCreditCard: (id: string, request: CreditCardUpdateDto) => Promise<CreditCardDto>
  deletingId: string | null
  deleteError: string | null
  deleteCreditCard: (id: string) => void
}

const NO_CREDIT_CARDS: CreditCardDto[] = []

export function useCreditCards(): CreditCardsData {
  const resource = useCrudResource({
    load: () => apiClient.getCreditCards(),
    initialData: NO_CREDIT_CARDS,
    loadErrorMessage: 'Unable to load credit cards',
    // Re-fetches after a mutation without flipping isLoading, so a page rendering this data
    // alongside its own sortable/filterable grid doesn't unmount and lose that grid's state.
    refreshMode: 'silent',
  })
  const { refresh, mutateAndRefresh, runPendingMutation } = resource
  const update = usePendingOperation()
  const { start: startUpdate, succeed: succeedUpdate, fail: failUpdate } = update

  const createCreditCard = useCallback(
    (request: CreditCardCreateDto) => mutateAndRefresh(() => apiClient.createCreditCard(request)),
    [mutateAndRefresh],
  )

  const updateCreditCard = useCallback(
    async (id: string, request: CreditCardUpdateDto) => {
      startUpdate(id)

      try {
        const updated = await apiClient.updateCreditCard(id, request)
        succeedUpdate()
        await refresh()
        return updated
      } catch (err: unknown) {
        failUpdate(getErrorMessage(err, 'Failed to update credit card'))
        throw err
      }
    },
    [startUpdate, succeedUpdate, failUpdate, refresh],
  )

  const deleteCreditCard = useCallback(
    (id: string) => runPendingMutation(id, () => apiClient.deleteCreditCard(id), 'Failed to delete credit card'),
    [runPendingMutation],
  )

  return {
    creditCards: resource.data,
    isLoading: resource.isLoading,
    error: resource.error,
    retry: resource.retry,
    refreshSilently: refresh,
    createCreditCard,
    updatingCardId: update.pendingId,
    updateError: update.pendingError,
    updateCreditCard,
    deletingId: resource.pendingId,
    deleteError: resource.pendingError,
    deleteCreditCard,
  }
}
