import { apiClient } from '../api/financialApiClient'
import type { BrokerHoldingsDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { useAsyncResource } from './useAsyncResource'

export interface BrokerHoldingsData {
  holdings: BrokerHoldingsDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useBrokerHoldings(): BrokerHoldingsData {
  const { selectedNode } = useSelectedNode()

  const brokerName = selectedNode?.nodeType === 'Broker' ? selectedNode.brokerName : null

  const { data, isLoading, error, retry } = useAsyncResource<BrokerHoldingsDto>(
    () => (brokerName ? apiClient.getBrokerHoldings(brokerName) : null),
    [selectedNode, brokerName, apiClient],
    'Unable to load holdings.',
  )

  return { holdings: data, isLoading, error, retry }
}
