import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { BankCreateDto, BankDto, BankUpdateDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

interface BanksState {
  banks: BankDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  deletingId: string | null
  deleteError: string | null
}

type BanksAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: BankDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }

const INITIAL_STATE: BanksState = {
  banks: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  deletingId: null,
  deleteError: null,
}

function reducer(state: BanksState, action: BanksAction): BanksState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, banks: action.payload }
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

export interface BanksData {
  banks: BankDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createBank: (request: BankCreateDto) => Promise<BankDto>
  updateBank: (id: string, request: BankUpdateDto) => Promise<BankDto>
  deletingId: string | null
  deleteError: string | null
  deleteBank: (id: string) => void
}

export function useBanks(): BanksData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getBanks()
      .then((banks) => dispatch({ type: 'FETCH_SUCCESS', payload: banks }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load banks') })
      })
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const createBank = useCallback(async (request: BankCreateDto) => {
    const created = await apiClient.createBank(request)
    dispatch({ type: 'RETRY' })
    return created
  }, [])

  const updateBank = useCallback(async (id: string, request: BankUpdateDto) => {
    const updated = await apiClient.updateBank(id, request)
    dispatch({ type: 'RETRY' })
    return updated
  }, [])

  const deleteBank = useCallback((id: string) => {
    dispatch({ type: 'DELETE_START', payload: id })

    void apiClient
      .deleteBank(id)
      .then(() => {
        dispatch({ type: 'DELETE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({ type: 'DELETE_ERROR', payload: getErrorMessage(err, 'Failed to delete bank') })
      })
  }, [])

  return {
    banks: state.banks,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    createBank,
    updateBank,
    deletingId: state.deletingId,
    deleteError: state.deleteError,
    deleteBank,
  }
}
