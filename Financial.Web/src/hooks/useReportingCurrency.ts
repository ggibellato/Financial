import { useCallback, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { ReportingCurrencySettingDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'

interface SaveState {
  isSaving: boolean
  saveError: string | null
}

type SaveAction =
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS' }
  | { type: 'SAVE_ERROR'; payload: string }

const INITIAL_SAVE_STATE: SaveState = {
  isSaving: false,
  saveError: null,
}

function saveReducer(state: SaveState, action: SaveAction): SaveState {
  switch (action.type) {
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null }
    case 'SAVE_SUCCESS':
      return { ...state, isSaving: false }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload }
    default:
      return state
  }
}

export interface ReportingCurrencyData {
  currency: string | null
  enabled: boolean | null
  isLoading: boolean
  error: string | null
  retry: () => void
  isSaving: boolean
  saveError: string | null
  setCurrency: (currency: string) => Promise<void>
  setEnabled: (enabled: boolean) => Promise<void>
}

export function useReportingCurrency(): ReportingCurrencyData {
  const [saveState, dispatch] = useReducer(saveReducer, INITIAL_SAVE_STATE)

  const { data: setting, isLoading, error, retry, setData } = useAsyncResource<ReportingCurrencySettingDto>(
    () => apiClient.getReportingCurrency(),
    [],
    'Unable to load the reporting currency setting',
    { initialLoading: true, keepDataWhileLoading: true },
  )

  const enabled = setting?.enabled ?? null

  const save = useCallback(
    async (request: () => Promise<ReportingCurrencySettingDto>) => {
      dispatch({ type: 'SAVE_START' })
      try {
        const saved = await request()
        setData(() => saved)
        dispatch({ type: 'SAVE_SUCCESS' })
      } catch (err: unknown) {
        dispatch({ type: 'SAVE_ERROR', payload: getErrorMessage(err, 'Unable to save the reporting currency setting') })
      }
    },
    [setData],
  )

  const setCurrency = useCallback(
    (currency: string) => save(() => apiClient.setReportingCurrency({ currency, enabled: enabled ?? true })),
    [save, enabled],
  )

  const setEnabled = useCallback(
    (nextEnabled: boolean) => save(() => apiClient.setReportingCurrencyEnabled({ enabled: nextEnabled })),
    [save],
  )

  return {
    currency: setting?.currency ?? null,
    enabled,
    isLoading,
    error,
    retry,
    isSaving: saveState.isSaving,
    saveError: saveState.saveError,
    setCurrency,
    setEnabled,
  }
}
