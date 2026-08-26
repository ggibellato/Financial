import { apiClient } from '../api/financialApiClient'
import type { AggregatedSummaryDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { useAsyncResource } from './useAsyncResource'

export interface AggregatedSummaryData {
  summary: AggregatedSummaryDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useAggregatedSummary(): AggregatedSummaryData {
  const { selectedNode, scope } = useSelectedNode()

  const isBroker = selectedNode?.nodeType === 'Broker'
  const isPortfolio = selectedNode?.nodeType === 'Portfolio'
  const shouldFetch = isBroker || isPortfolio

  const { data, isLoading, error, retry } = useAsyncResource<AggregatedSummaryDto>(
    () => {
      if (!shouldFetch || !selectedNode) return null
      return isBroker
        ? apiClient.getSummaryByBroker(selectedNode.brokerName, scope)
        : apiClient.getSummaryByPortfolio(selectedNode.brokerName, selectedNode.portfolioName!, scope)
    },
    [selectedNode, shouldFetch, isBroker, apiClient, scope],
    'Unable to load summary',
  )

  return { summary: data, isLoading, error, retry }
}
