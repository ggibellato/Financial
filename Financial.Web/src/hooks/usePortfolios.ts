import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { InvestmentScope, PortfolioCreateDto, PortfolioDto, PortfolioUpdateDto } from '../api/types'
import { useCrudResource } from './useCrudResource'

/** Uniquely identifies a portfolio row: names are only unique within their own broker. */
export function portfolioKey(brokerName: string, portfolioName: string): string {
  return `${brokerName}/${portfolioName}`
}

export interface PortfoliosData {
  portfolios: PortfolioDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createPortfolio: (request: PortfolioCreateDto) => Promise<PortfolioDto>
  updatePortfolio: (
    brokerName: string,
    currentName: string,
    request: PortfolioUpdateDto,
    brokerScope?: InvestmentScope,
  ) => Promise<PortfolioDto>
  deletingKey: string | null
  deleteError: string | null
  deletePortfolio: (brokerName: string, portfolioName: string, brokerScope: InvestmentScope) => void
}

const NO_PORTFOLIOS: PortfolioDto[] = []

export function usePortfolios(): PortfoliosData {
  const resource = useCrudResource({
    load: () => apiClient.getAdminPortfolios(),
    initialData: NO_PORTFOLIOS,
    loadErrorMessage: 'Unable to load portfolios',
  })
  const { mutateAndRefresh, runPendingMutation } = resource

  const createPortfolio = useCallback(
    (request: PortfolioCreateDto) => mutateAndRefresh(() => apiClient.createPortfolio(request)),
    [mutateAndRefresh],
  )

  const updatePortfolio = useCallback(
    (brokerName: string, currentName: string, request: PortfolioUpdateDto, brokerScope?: InvestmentScope) =>
      mutateAndRefresh(() => apiClient.updatePortfolio(brokerName, currentName, request, brokerScope)),
    [mutateAndRefresh],
  )

  const deletePortfolio = useCallback(
    (brokerName: string, portfolioName: string, brokerScope: InvestmentScope) =>
      runPendingMutation(
        portfolioKey(brokerName, portfolioName),
        () => apiClient.deleteEmptyPortfolio(brokerName, portfolioName, brokerScope),
        'Failed to delete portfolio',
      ),
    [runPendingMutation],
  )

  return {
    portfolios: resource.data,
    isLoading: resource.isLoading,
    error: resource.error,
    retry: resource.retry,
    createPortfolio,
    updatePortfolio,
    deletingKey: resource.pendingId,
    deleteError: resource.pendingError,
    deletePortfolio,
  }
}
