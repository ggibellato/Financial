import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import { getDeleteErrorMessage } from '../utils/formatters'

export type PotOperationNoun = 'deposit' | 'withdrawal'

interface OperationDeleteTarget {
  operationId: string
  noun: PotOperationNoun
}

export function usePotOperationDelete(onDeleted: (noun: PotOperationNoun) => void) {
  const [target, setTarget] = useState<OperationDeleteTarget | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const request = (operationId: string, noun: PotOperationNoun) => {
    setTarget({ operationId, noun })
    setError(null)
  }

  const cancel = () => {
    if (!isDeleting) setTarget(null)
  }

  const confirm = () => {
    if (!target || isDeleting) return
    setIsDeleting(true)
    setError(null)
    void apiClient
      .deletePotOperation(target.operationId)
      .then(() => {
        setTarget(null)
        onDeleted(target.noun)
      })
      .catch((err: unknown) => setError(getDeleteErrorMessage(err, `Failed to delete the ${target.noun}`)))
      .finally(() => setIsDeleting(false))
  }

  return { isOpen: target !== null, noun: target?.noun ?? 'deposit', isDeleting, error, request, cancel, confirm }
}
