import { apiClient } from '../api/financialApiClient'
import type { BankDto, CategoryDto, IncomeSourceDto } from '../api/types'
import { useAsyncResource } from './useAsyncResource'

export const FORM_OPTIONS_LOAD_ERROR = 'Unable to load the form options'

export interface AccountFormOptions {
  banks: BankDto[]
  categories: CategoryDto[]
  incomeSources: IncomeSourceDto[]
}

const NO_OPTIONS: AccountFormOptions = { banks: [], categories: [], incomeSources: [] }

function loadAccountFormOptions(): Promise<AccountFormOptions> {
  return Promise.all([apiClient.getBanks(), apiClient.getCategories(), apiClient.getIncomeSources()]).then(
    ([banks, categories, incomeSources]) => ({
      banks,
      categories: categories.filter((category) => category.active),
      incomeSources,
    }),
  )
}

export function useAccountFormOptions(isNeeded: boolean) {
  const resource = useAsyncResource(() => (isNeeded ? loadAccountFormOptions() : null), [isNeeded], FORM_OPTIONS_LOAD_ERROR)
  return {
    options: resource.data ?? NO_OPTIONS,
    isReady: resource.data !== null,
    error: resource.error,
    retry: resource.retry,
  }
}
