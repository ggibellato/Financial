import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { TaxRuleCreateDto, TaxRuleDto, TaxRuleUpdateDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

interface TaxRulesState {
  taxRules: TaxRuleDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  deletingId: string | null
  deleteError: string | null
}

type TaxRulesAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: TaxRuleDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }

const INITIAL_STATE: TaxRulesState = {
  taxRules: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  deletingId: null,
  deleteError: null,
}

function reducer(state: TaxRulesState, action: TaxRulesAction): TaxRulesState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, taxRules: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'DELETE_START':
      return { ...state, deletingId: action.payload, deleteError: null }
    case 'DELETE_SUCCESS':
      return { ...state, deletingId: null }
    case 'DELETE_ERROR':
      return { ...state, deletingId: null, deleteError: action.payload }
    default:
      return state
  }
}

export interface TaxRulesData {
  taxRules: TaxRuleDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createTaxRule: (request: TaxRuleCreateDto) => Promise<TaxRuleDto>
  updateTaxRule: (id: string, request: TaxRuleUpdateDto) => Promise<TaxRuleDto>
  deletingId: string | null
  deleteError: string | null
  deleteTaxRule: (taxRule: TaxRuleDto) => void
}

export function useTaxRules(): TaxRulesData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getTaxRules()
      .then((taxRules) => dispatch({ type: 'FETCH_SUCCESS', payload: taxRules }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load tax rules') })
      })
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const createTaxRule = useCallback(async (request: TaxRuleCreateDto) => {
    const created = await apiClient.createTaxRule(request)
    dispatch({ type: 'RETRY' })
    return created
  }, [])

  const updateTaxRule = useCallback(async (id: string, request: TaxRuleUpdateDto) => {
    const updated = await apiClient.updateTaxRule(id, request)
    dispatch({ type: 'RETRY' })
    return updated
  }, [])

  const deleteTaxRule = useCallback((taxRule: TaxRuleDto) => {
    dispatch({ type: 'DELETE_START', payload: taxRule.id })

    void apiClient
      .deleteTaxRule(taxRule.id)
      .then(() => {
        dispatch({ type: 'DELETE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({ type: 'DELETE_ERROR', payload: getErrorMessage(err, 'Failed to delete tax rule') })
      })
  }, [])

  return {
    taxRules: state.taxRules,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    createTaxRule,
    updateTaxRule,
    deletingId: state.deletingId,
    deleteError: state.deleteError,
    deleteTaxRule,
  }
}
