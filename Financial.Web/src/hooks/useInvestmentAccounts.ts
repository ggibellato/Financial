import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { InvestmentAccountCreateDto, InvestmentAccountDto, InvestmentAccountUpdateDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

interface InvestmentAccountsState {
  investmentAccounts: InvestmentAccountDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  deletingId: string | null
  deleteError: string | null
}

type InvestmentAccountsAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: InvestmentAccountDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }

const INITIAL_STATE: InvestmentAccountsState = {
  investmentAccounts: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  deletingId: null,
  deleteError: null,
}

function reducer(state: InvestmentAccountsState, action: InvestmentAccountsAction): InvestmentAccountsState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, investmentAccounts: action.payload }
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

export interface InvestmentAccountsData {
  investmentAccounts: InvestmentAccountDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createInvestmentAccount: (request: InvestmentAccountCreateDto) => Promise<InvestmentAccountDto>
  updateInvestmentAccount: (id: string, request: InvestmentAccountUpdateDto) => Promise<InvestmentAccountDto>
  deletingId: string | null
  deleteError: string | null
  deleteInvestmentAccount: (id: string) => void
}

export function useInvestmentAccounts(): InvestmentAccountsData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getInvestmentAccounts()
      .then((investmentAccounts) => dispatch({ type: 'FETCH_SUCCESS', payload: investmentAccounts }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load investment accounts') })
      })
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const createInvestmentAccount = useCallback(async (request: InvestmentAccountCreateDto) => {
    const created = await apiClient.createInvestmentAccount(request)
    dispatch({ type: 'RETRY' })
    return created
  }, [])

  const updateInvestmentAccount = useCallback(async (id: string, request: InvestmentAccountUpdateDto) => {
    const updated = await apiClient.updateInvestmentAccount(id, request)
    dispatch({ type: 'RETRY' })
    return updated
  }, [])

  const deleteInvestmentAccount = useCallback((id: string) => {
    dispatch({ type: 'DELETE_START', payload: id })

    void apiClient
      .deleteInvestmentAccount(id)
      .then(() => {
        dispatch({ type: 'DELETE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({ type: 'DELETE_ERROR', payload: getErrorMessage(err, 'Failed to delete investment account') })
      })
  }, [])

  return {
    investmentAccounts: state.investmentAccounts,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    createInvestmentAccount,
    updateInvestmentAccount,
    deletingId: state.deletingId,
    deleteError: state.deleteError,
    deleteInvestmentAccount,
  }
}
