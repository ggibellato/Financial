import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { BrokerCreateDto, BrokerDto, BrokerUpdateDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

interface BrokersState {
  brokers: BrokerDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  deletingName: string | null
  deleteError: string | null
}

type BrokersAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: BrokerDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }

const INITIAL_STATE: BrokersState = {
  brokers: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  deletingName: null,
  deleteError: null,
}

function reducer(state: BrokersState, action: BrokersAction): BrokersState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, brokers: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'DELETE_START':
      return { ...state, deletingName: action.payload, deleteError: null }
    case 'DELETE_SUCCESS':
      return { ...state, deletingName: null }
    case 'DELETE_ERROR':
      return { ...state, deletingName: null, deleteError: action.payload }
    default:
      return state
  }
}

export interface BrokersData {
  brokers: BrokerDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createBroker: (request: BrokerCreateDto) => Promise<BrokerDto>
  updateBroker: (currentName: string, request: BrokerUpdateDto) => Promise<BrokerDto>
  deletingName: string | null
  deleteError: string | null
  deleteBroker: (name: string) => void
}

export function useBrokers(): BrokersData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getAdminBrokers()
      .then((brokers) => dispatch({ type: 'FETCH_SUCCESS', payload: brokers }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load brokers') })
      })
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const createBroker = useCallback(async (request: BrokerCreateDto) => {
    const created = await apiClient.createBroker(request)
    dispatch({ type: 'RETRY' })
    return created
  }, [])

  const updateBroker = useCallback(async (currentName: string, request: BrokerUpdateDto) => {
    const updated = await apiClient.updateBroker(currentName, request)
    dispatch({ type: 'RETRY' })
    return updated
  }, [])

  const deleteBroker = useCallback((name: string) => {
    dispatch({ type: 'DELETE_START', payload: name })

    void apiClient
      .deleteBroker(name)
      .then(() => {
        dispatch({ type: 'DELETE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({ type: 'DELETE_ERROR', payload: getErrorMessage(err, 'Failed to delete broker') })
      })
  }, [])

  return {
    brokers: state.brokers,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    createBroker,
    updateBroker,
    deletingName: state.deletingName,
    deleteError: state.deleteError,
    deleteBroker,
  }
}
