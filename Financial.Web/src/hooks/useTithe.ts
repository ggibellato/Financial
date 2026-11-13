import { useState } from 'react'
import { isBadRequest } from '../api/apiError'
import { apiClient } from '../api/financialApiClient'
import type { TitheMonthDto, TitheSummaryDto } from '../api/types'
import { type AsyncResource, useAsyncResource } from './useAsyncResource'

export const TITHE_DETAIL_LOAD_ERROR = 'Unable to load the tithe for this month.'
export const TITHE_HISTORY_LOAD_ERROR = 'Unable to load the tithe history.'
export const NO_CARRY_FORWARD_MESSAGE = 'There is no balance to carry into this month.'
export const CARRY_FORWARD_UPDATE_ERROR = "Couldn't update the carry-forward. Try again."

interface TitheData {
  detail: AsyncResource<TitheSummaryDto>
  history: AsyncResource<TitheMonthDto[]>
  carryForwardUpdating: boolean
  carryForwardError: string | null
  updateCarryForwardInclusion: (included: boolean) => void
}

export function useTithe(year: number, month: number): TitheData {
  const detail = useAsyncResource(() => apiClient.getTitheSummaryByMonth(year, month), [year, month], TITHE_DETAIL_LOAD_ERROR, { initialLoading: true })
  const history = useAsyncResource(() => apiClient.getTitheYear(year), [year], TITHE_HISTORY_LOAD_ERROR, { initialLoading: true })
  const [updatingMonth, setUpdatingMonth] = useState<number | null>(null)
  const [carryForwardFailure, setCarryForwardFailure] = useState<{ month: number; message: string } | null>(null)

  const refreshBoth = () => Promise.all([detail.refresh(), history.refresh()])

  const updateCarryForwardInclusion = (included: boolean) => {
    setUpdatingMonth(month)
    setCarryForwardFailure(null)
    void apiClient
      .updateTitheCarryForward(year, month, { included })
      .then(refreshBoth)
      .catch((err: unknown) => {
        if (isBadRequest(err)) {
          setCarryForwardFailure({ month, message: NO_CARRY_FORWARD_MESSAGE })
          return refreshBoth()
        }
        setCarryForwardFailure({ month, message: CARRY_FORWARD_UPDATE_ERROR })
      })
      .finally(() => setUpdatingMonth((current) => (current === month ? null : current)))
  }

  if (carryForwardFailure && carryForwardFailure.month !== month) setCarryForwardFailure(null)

  const carryForwardError = carryForwardFailure?.message ?? null
  const carryForwardUpdating = updatingMonth === month

  return { detail, history, carryForwardUpdating, carryForwardError, updateCarryForwardInclusion }
}
