import { useEffect, useRef } from 'react'
import { isNotFound } from '../api/apiError'
import { apiClient } from '../api/financialApiClient'
import type { IncomeDto, PotOperationDto } from '../api/types'
import { useAsyncResource } from './useAsyncResource'

export const OPERATION_LOAD_ERROR = 'Unable to load this deposit or withdrawal'
export const OPERATION_NOT_FOUND = 'This deposit or withdrawal no longer exists.'

export interface OperationToOpen {
  operation: PotOperationDto
  income: IncomeDto | null
}

type OperationLoad = OperationToOpen | 'not-found'

async function loadOperation(operationId: string, potsAccountId: string): Promise<OperationLoad> {
  try {
    const operation = await apiClient.getPotOperation(operationId)
    if (operation.potsAccountId.toLowerCase() !== potsAccountId.toLowerCase()) return 'not-found'
    if (!operation.incomeId) return { operation, income: null }

    const [year, month] = operation.date.split('-').map(Number)
    const incomes = await apiClient.getIncomesByMonth(year, month)
    const income = incomes.find((candidate) => candidate.id === operation.incomeId)
    return income ? { operation, income } : 'not-found'
  } catch (err) {
    if (isNotFound(err)) return 'not-found'
    throw err
  }
}

export function useOperationRoute(
  operationId: string | null,
  potsAccountId: string,
  isReady: boolean,
  isFormOpen: boolean,
  openForm: (toOpen: OperationToOpen) => void,
  clearOperation: () => void,
) {
  const load = useAsyncResource<OperationLoad>(
    () => (operationId && isReady ? loadOperation(operationId, potsAccountId) : null),
    [operationId, potsAccountId, isReady],
    OPERATION_LOAD_ERROR,
  )
  const openedOperationId = useRef<string | null>(null)
  const loaded = load.data

  useEffect(() => {
    if (operationId === null || openedOperationId.current !== operationId || isFormOpen) return
    openedOperationId.current = null
    clearOperation()
  })

  useEffect(() => {
    if (!loaded || loaded === 'not-found' || loaded.operation.operationId === openedOperationId.current) return
    openedOperationId.current = loaded.operation.operationId
    openForm(loaded)
    // eslint-disable-next-line react-hooks/exhaustive-deps -- opening is driven only by a newly loaded operation
  }, [loaded])

  return {
    isLoading: load.isLoading,
    isNotFound: loaded === 'not-found',
    error: load.error,
    retry: load.retry,
  }
}
