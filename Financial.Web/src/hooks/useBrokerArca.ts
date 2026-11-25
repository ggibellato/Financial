import { apiClient } from '../api/financialApiClient'
import type { BrokerArcaDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { useAsyncResource } from './useAsyncResource'

export interface BrokerArcaData {
  arca: BrokerArcaDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useBrokerArca(): BrokerArcaData {
  const { selectedNode } = useSelectedNode()

  const brokerName = selectedNode?.nodeType === 'Broker' ? selectedNode.brokerName : null

  const { data, isLoading, error, retry } = useAsyncResource<BrokerArcaDto>(
    () => (brokerName ? apiClient.getBrokerArca(brokerName) : null),
    [selectedNode, brokerName, apiClient],
    'The ARCA allocation could not be loaded.',
  )

  return { arca: data, isLoading, error, retry }
}
