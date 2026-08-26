import { apiClient } from '../api/financialApiClient'
import type { PortfolioBreakdownItemDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { useAsyncResource } from './useAsyncResource'

export interface BrokerBreakdownData {
  breakdown: PortfolioBreakdownItemDto[] | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useBrokerBreakdown(): BrokerBreakdownData {
  const { selectedNode, scope } = useSelectedNode()

  const isBroker = selectedNode?.nodeType === 'Broker'

  const { data, isLoading, error, retry } = useAsyncResource<PortfolioBreakdownItemDto[]>(
    () => (isBroker && selectedNode ? apiClient.getBrokerBreakdown(selectedNode.brokerName, scope) : null),
    [selectedNode, isBroker, apiClient, scope],
    'Unable to load breakdown',
  )

  return { breakdown: data, isLoading, error, retry }
}
