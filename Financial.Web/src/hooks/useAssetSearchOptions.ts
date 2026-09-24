import { useMemo } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AssetAdminDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { useAsyncResource } from './useAsyncResource'

export interface AssetSearchOptionsData {
  options: AssetAdminDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useAssetSearchOptions(): AssetSearchOptionsData {
  const { selectedNode } = useSelectedNode()
  const hasHoldingScope = Boolean(selectedNode?.portfolioName && selectedNode.assetName)

  const { data, isLoading, error, retry } = useAsyncResource<AssetAdminDto[]>(
    () => (hasHoldingScope ? apiClient.getAdminAssets() : null),
    [selectedNode?.brokerName, selectedNode?.portfolioName, selectedNode?.assetName],
    'Unable to load assets',
  )

  const options = useMemo(() => {
    if (!data || !hasHoldingScope || !selectedNode) return []
    return data.filter(
      (asset) =>
        asset.brokerName === selectedNode.brokerName &&
        asset.portfolioName === selectedNode.portfolioName &&
        asset.name !== selectedNode.assetName,
    )
  }, [data, hasHoldingScope, selectedNode])

  return { options, isLoading, error, retry }
}
