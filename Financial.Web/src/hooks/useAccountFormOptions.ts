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

export function useAccountFormOptions() {
  const resource = useAsyncResource(loadAccountFormOptions, [], FORM_OPTIONS_LOAD_ERROR, { initialLoading: true })
  return {
    options: resource.data ?? NO_OPTIONS,
    isLoading: resource.isLoading,
    error: resource.error,
    retry: resource.retry,
  }
}
