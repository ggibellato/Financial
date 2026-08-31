import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { IncomeSourceCreateDto, IncomeSourceDto, IncomeSourceUpdateDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

interface IncomeSourcesState {
  incomeSources: IncomeSourceDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  deletingId: string | null
  deleteError: string | null
}

type IncomeSourcesAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: IncomeSourceDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }

const INITIAL_STATE: IncomeSourcesState = {
  incomeSources: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  deletingId: null,
  deleteError: null,
}

function reducer(state: IncomeSourcesState, action: IncomeSourcesAction): IncomeSourcesState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, incomeSources: action.payload }
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

export interface IncomeSourcesData {
  incomeSources: IncomeSourceDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createIncomeSource: (request: IncomeSourceCreateDto) => Promise<IncomeSourceDto>
  updateIncomeSource: (id: string, request: IncomeSourceUpdateDto) => Promise<IncomeSourceDto>
  deletingId: string | null
  deleteError: string | null
  deleteIncomeSource: (id: string) => void
}

export function useIncomeSources(): IncomeSourcesData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getIncomeSources()
      .then((incomeSources) => dispatch({ type: 'FETCH_SUCCESS', payload: incomeSources }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load income sources') })
      })
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const createIncomeSource = useCallback(async (request: IncomeSourceCreateDto) => {
    const created = await apiClient.createIncomeSource(request)
    dispatch({ type: 'RETRY' })
    return created
  }, [])

  const updateIncomeSource = useCallback(async (id: string, request: IncomeSourceUpdateDto) => {
    const updated = await apiClient.updateIncomeSource(id, request)
    dispatch({ type: 'RETRY' })
    return updated
  }, [])

  const deleteIncomeSource = useCallback((id: string) => {
    dispatch({ type: 'DELETE_START', payload: id })

    void apiClient
      .deleteIncomeSource(id)
      .then(() => {
        dispatch({ type: 'DELETE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({ type: 'DELETE_ERROR', payload: getErrorMessage(err, 'Failed to delete income source') })
      })
  }, [])

  return {
    incomeSources: state.incomeSources,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    createIncomeSource,
    updateIncomeSource,
    deletingId: state.deletingId,
    deleteError: state.deleteError,
    deleteIncomeSource,
  }
}
