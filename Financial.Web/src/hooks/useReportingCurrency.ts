import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import { getErrorMessage } from '../utils/formatters'

interface ReportingCurrencyState {
  currency: string | null
  isLoading: boolean
  error: string | null
  retryCount: number
  isSaving: boolean
  saveError: string | null
}

type ReportingCurrencyAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: string }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS'; payload: string }
  | { type: 'SAVE_ERROR'; payload: string }

const INITIAL_STATE: ReportingCurrencyState = {
  currency: null,
  isLoading: true,
  error: null,
  retryCount: 0,
  isSaving: false,
  saveError: null,
}

function reducer(state: ReportingCurrencyState, action: ReportingCurrencyAction): ReportingCurrencyState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, currency: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null }
    case 'SAVE_SUCCESS':
      return { ...state, isSaving: false, currency: action.payload }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload }
    default:
      return state
  }
}

export interface ReportingCurrencyData {
  currency: string | null
  isLoading: boolean
  error: string | null
  retry: () => void
  isSaving: boolean
  saveError: string | null
  setCurrency: (currency: string) => Promise<void>
}

export function useReportingCurrency(): ReportingCurrencyData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    apiClient
      .getReportingCurrency()
      .then((setting) => dispatch({ type: 'FETCH_SUCCESS', payload: setting.currency }))
      .catch((err: unknown) => dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load the reporting currency setting') }))
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const setCurrency = useCallback(async (currency: string) => {
    dispatch({ type: 'SAVE_START' })
    try {
      const setting = await apiClient.setReportingCurrency({ currency })
      dispatch({ type: 'SAVE_SUCCESS', payload: setting.currency })
    } catch (err: unknown) {
      dispatch({ type: 'SAVE_ERROR', payload: getErrorMessage(err, 'Unable to save the reporting currency setting') })
    }
  }, [])

  return {
    currency: state.currency,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    isSaving: state.isSaving,
    saveError: state.saveError,
    setCurrency,
  }
}
