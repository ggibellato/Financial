import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { ReserveBucketCreateDto, ReserveBucketDto, ReserveBucketUpdateDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

const SPLIT_PERCENTAGE_TOLERANCE = 0.01

interface ReserveBucketsState {
  reserveBuckets: ReserveBucketDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  savingId: string | null
  saveError: string | null
}

type ReserveBucketsAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: ReserveBucketDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SAVE_START'; payload: string }
  | { type: 'SAVE_SUCCESS' }
  | { type: 'SAVE_ERROR'; payload: string }

const INITIAL_STATE: ReserveBucketsState = {
  reserveBuckets: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  savingId: null,
  saveError: null,
}

function reducer(state: ReserveBucketsState, action: ReserveBucketsAction): ReserveBucketsState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, reserveBuckets: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'SAVE_START':
      return { ...state, savingId: action.payload, saveError: null }
    case 'SAVE_SUCCESS':
      return { ...state, savingId: null }
    case 'SAVE_ERROR':
      return { ...state, savingId: null, saveError: action.payload }
    default:
      return state
  }
}

export interface ReserveBucketsData {
  reserveBuckets: ReserveBucketDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createReserveBucket: (request: ReserveBucketCreateDto) => Promise<ReserveBucketDto>
  updateReserveBucket: (id: string, request: ReserveBucketUpdateDto) => Promise<ReserveBucketDto>
  savingId: string | null
  saveError: string | null
  deactivateReserveBucket: (bucket: ReserveBucketDto) => void
  activeSplitWarning: string | null
}

export function useReserveBuckets(): ReserveBucketsData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getReserveBuckets()
      .then((reserveBuckets) => dispatch({ type: 'FETCH_SUCCESS', payload: reserveBuckets }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load reserve buckets') })
      })
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const createReserveBucket = useCallback(async (request: ReserveBucketCreateDto) => {
    const created = await apiClient.createReserveBucket(request)
    dispatch({ type: 'RETRY' })
    return created
  }, [])

  const updateReserveBucket = useCallback(async (id: string, request: ReserveBucketUpdateDto) => {
    const updated = await apiClient.updateReserveBucket(id, request)
    dispatch({ type: 'RETRY' })
    return updated
  }, [])

  const deactivateReserveBucket = useCallback((bucket: ReserveBucketDto) => {
    dispatch({ type: 'SAVE_START', payload: bucket.id })

    void apiClient
      .updateReserveBucket(bucket.id, { name: bucket.name, splitPercentage: bucket.splitPercentage, isActive: false })
      .then(() => {
        dispatch({ type: 'SAVE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({ type: 'SAVE_ERROR', payload: getErrorMessage(err, 'Failed to deactivate reserve bucket') })
      })
  }, [])

  const activeSplitTotal = state.reserveBuckets
    .filter((bucket) => bucket.isActive)
    .reduce((sum, bucket) => sum + bucket.splitPercentage, 0)
  const activeSplitWarning =
    state.reserveBuckets.length === 0 || Math.abs(activeSplitTotal - 100) <= SPLIT_PERCENTAGE_TOLERANCE
      ? null
      : `Active buckets currently sum to ${activeSplitTotal.toFixed(2)}% — review your split percentages`

  return {
    reserveBuckets: state.reserveBuckets,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    createReserveBucket,
    updateReserveBucket,
    savingId: state.savingId,
    saveError: state.saveError,
    deactivateReserveBucket,
    activeSplitWarning,
  }
}
