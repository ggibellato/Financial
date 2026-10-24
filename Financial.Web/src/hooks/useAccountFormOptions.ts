import { apiClient } from '../api/financialApiClient'
import type { BankDto, CategoryDto, IncomeSourceDto, TransferAccountOptionDto } from '../api/types'
import { useAsyncResource } from './useAsyncResource'

export const FORM_OPTIONS_LOAD_ERROR = 'Unable to load the form options'

export interface TransferCounterparts {
  destinations: TransferAccountOptionDto[]
  sources: TransferAccountOptionDto[]
}

export interface AccountFormOptions extends TransferCounterparts {
  banks: BankDto[]
  categories: CategoryDto[]
  incomeSources: IncomeSourceDto[]
}

type SharedFormOptions = Omit<AccountFormOptions, keyof TransferCounterparts>

const NO_COUNTERPARTS: TransferCounterparts = { destinations: [], sources: [] }
const NO_SHARED_OPTIONS: SharedFormOptions = { banks: [], categories: [], incomeSources: [] }

function loadSharedFormOptions(): Promise<SharedFormOptions> {
  return Promise.all([apiClient.getBanks(), apiClient.getCategories(), apiClient.getIncomeSources()]).then(
    ([banks, categories, incomeSources]) => ({
      banks,
      categories: categories.filter((category) => category.active),
      incomeSources,
    }),
  )
}

function loadTransferCounterparts(accountId: string): Promise<TransferCounterparts> {
  return Promise.all([apiClient.getTransferDestinations(accountId), apiClient.getTransferSources(accountId)]).then(
    ([destinations, sources]) => ({ destinations, sources }),
  )
}

export function useAccountFormOptions(isNeeded: boolean, transferAccountId: string | null) {
  const shared = useAsyncResource(() => (isNeeded ? loadSharedFormOptions() : null), [isNeeded], FORM_OPTIONS_LOAD_ERROR)
  const counterparts = useAsyncResource(
    () => (transferAccountId ? loadTransferCounterparts(transferAccountId) : null),
    [transferAccountId],
    FORM_OPTIONS_LOAD_ERROR,
  )

  return {
    options: { ...(shared.data ?? NO_SHARED_OPTIONS), ...(counterparts.data ?? NO_COUNTERPARTS) },
    isReady: shared.data !== null,
    areCounterpartsReady: counterparts.data !== null,
    error: shared.error ?? counterparts.error,
    retry: () => {
      if (shared.error) shared.retry()
      if (counterparts.error) counterparts.retry()
    },
  }
}
