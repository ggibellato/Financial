import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { deleteFailedMessage, getDeleteErrorMessage } from '../utils/formatters'

export interface PortfolioDeletionData {
  deleteError: string | null
  deletePortfolio: (brokerName: string, portfolioName: string) => void
}

export function usePortfolioDeletion(): PortfolioDeletionData {
  const { setSelectedNode, scope, reload } = useSelectedNode()
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const deletePortfolio = (brokerName: string, portfolioName: string) => {
    setDeleteError(null)
    void apiClient
      .deleteEmptyPortfolio(brokerName, portfolioName, scope)
      .then(() => {
        // The portfolio is gone, so nothing here describes anything any more.
        setSelectedNode(null)
        reload()
      })
      .catch((err: unknown) => {
        setDeleteError(getDeleteErrorMessage(err, deleteFailedMessage('portfolio')))
      })
  }

  return { deleteError, deletePortfolio }
}
