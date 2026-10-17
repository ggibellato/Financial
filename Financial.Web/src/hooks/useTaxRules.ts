import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { TaxRuleCreateDto, TaxRuleDto, TaxRuleUpdateDto } from '../api/types'
import { useCrudResource } from './useCrudResource'

export interface TaxRulesData {
  taxRules: TaxRuleDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createTaxRule: (request: TaxRuleCreateDto) => Promise<TaxRuleDto>
  updateTaxRule: (id: string, request: TaxRuleUpdateDto) => Promise<TaxRuleDto>
  deletingId: string | null
  deleteError: string | null
  deleteTaxRule: (taxRule: TaxRuleDto) => void
}

const NO_TAX_RULES: TaxRuleDto[] = []

export function useTaxRules(): TaxRulesData {
  const resource = useCrudResource({
    load: () => apiClient.getTaxRules(),
    initialData: NO_TAX_RULES,
    loadErrorMessage: 'Unable to load tax rules',
  })
  const { mutateAndRefresh, runPendingMutation } = resource

  const createTaxRule = useCallback(
    (request: TaxRuleCreateDto) => mutateAndRefresh(() => apiClient.createTaxRule(request)),
    [mutateAndRefresh],
  )

  const updateTaxRule = useCallback(
    (id: string, request: TaxRuleUpdateDto) => mutateAndRefresh(() => apiClient.updateTaxRule(id, request)),
    [mutateAndRefresh],
  )

  const deleteTaxRule = useCallback(
    (taxRule: TaxRuleDto) =>
      runPendingMutation(taxRule.id, () => apiClient.deleteTaxRule(taxRule.id), 'Failed to delete tax rule'),
    [runPendingMutation],
  )

  return {
    taxRules: resource.data,
    isLoading: resource.isLoading,
    error: resource.error,
    retry: resource.retry,
    createTaxRule,
    updateTaxRule,
    deletingId: resource.pendingId,
    deleteError: resource.pendingError,
    deleteTaxRule,
  }
}
