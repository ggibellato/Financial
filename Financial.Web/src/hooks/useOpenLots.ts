import { apiClient } from '../api/financialApiClient'
import type { OpenLotDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { useAsyncResource } from './useAsyncResource'

const NO_OPEN_LOTS: OpenLotDto[] = []

export interface OpenLotsData {
  openLots: OpenLotDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
}

/** Fetches open lots only while `enabled` (a SpecificId sale is being entered) - never on every
 * asset-details load, since AverageCost/FIFO brokers never need this data. */
export function useOpenLots(enabled: boolean): OpenLotsData {
  const { selectedNode, scope } = useSelectedNode()

  const { data, isLoading, error, retry } = useAsyncResource<OpenLotDto[]>(
    () => {
      if (!enabled || !selectedNode || selectedNode.nodeType !== 'Asset' || !selectedNode.portfolioName || !selectedNode.assetName) {
        return null
      }
      return apiClient.getOpenLots(selectedNode.brokerName, selectedNode.portfolioName, selectedNode.assetName, scope)
    },
    [enabled, selectedNode, scope],
    'Unable to load open lots',
    { keepDataWhileLoading: true },
  )

  return { openLots: data ?? NO_OPEN_LOTS, isLoading, error, retry }
}
