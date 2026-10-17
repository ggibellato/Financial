import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AssetAdminCreateDto, AssetAdminDto, AssetAdminUpdateDto, InvestmentScope } from '../api/types'
import { useCrudResource } from './useCrudResource'

/** Uniquely identifies an asset row: names are only unique within their own portfolio. */
export function assetKey(brokerName: string, portfolioName: string, assetName: string): string {
  return `${brokerName}/${portfolioName}/${assetName}`
}

export interface AssetsData {
  assets: AssetAdminDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createAsset: (request: AssetAdminCreateDto) => Promise<AssetAdminDto>
  updateAsset: (
    brokerName: string,
    portfolioName: string,
    currentName: string,
    request: AssetAdminUpdateDto,
    brokerScope?: InvestmentScope,
  ) => Promise<AssetAdminDto>
  deletingKey: string | null
  deleteError: string | null
  /** Archives the asset in place: same portfolio name in Historic Investments. */
  deleteAsset: (brokerName: string, portfolioName: string, assetName: string) => void
}

const NO_ASSETS: AssetAdminDto[] = []

export function useAssets(): AssetsData {
  const resource = useCrudResource({
    load: () => apiClient.getAdminAssets(),
    initialData: NO_ASSETS,
    loadErrorMessage: 'Unable to load assets',
  })
  const { mutateAndRefresh, runPendingMutation } = resource

  const createAsset = useCallback(
    (request: AssetAdminCreateDto) => mutateAndRefresh(() => apiClient.createAsset(request)),
    [mutateAndRefresh],
  )

  const updateAsset = useCallback(
    (
      brokerName: string,
      portfolioName: string,
      currentName: string,
      request: AssetAdminUpdateDto,
      brokerScope?: InvestmentScope,
    ) => mutateAndRefresh(() => apiClient.updateAsset(brokerName, portfolioName, currentName, request, brokerScope)),
    [mutateAndRefresh],
  )

  const deleteAsset = useCallback(
    (brokerName: string, portfolioName: string, assetName: string) =>
      runPendingMutation(
        assetKey(brokerName, portfolioName, assetName),
        () =>
          apiClient.archiveAsset({
            brokerName,
            sourcePortfolioName: portfolioName,
            assetName,
            destinationPortfolioName: portfolioName,
          }),
        'Failed to delete asset',
      ),
    [runPendingMutation],
  )

  return {
    assets: resource.data,
    isLoading: resource.isLoading,
    error: resource.error,
    retry: resource.retry,
    createAsset,
    updateAsset,
    deletingKey: resource.pendingId,
    deleteError: resource.pendingError,
    deleteAsset,
  }
}
