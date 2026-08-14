import { useCallback, useEffect, useReducer } from 'react'
import { getErrorMessage } from '../utils/formatters'

interface AsyncResourceState<T> {
  data: T | null
  isLoading: boolean
  error: string | null
  retryCount: number
}

type AsyncResourceAction<T> =
  | { type: 'RESET' }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: T }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }

function reducer<T>(state: AsyncResourceState<T>, action: AsyncResourceAction<T>): AsyncResourceState<T> {
  switch (action.type) {
    case 'RESET':
      return { data: null, isLoading: false, error: null, retryCount: state.retryCount }
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null, data: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, data: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    default:
      return state
  }
}

function initialState<T>(): AsyncResourceState<T> {
  return { data: null, isLoading: false, error: null, retryCount: 0 }
}

export interface AsyncResource<T> {
  data: T | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

/**
 * Fetches from `fetcher` whenever `deps` changes, tracking the loading/error/retry state so
 * individual hooks don't each hand-roll the same FETCH_START/FETCH_SUCCESS/FETCH_ERROR/RETRY
 * reducer. Return `null` from `fetcher` (e.g. when a required selection is absent) to reset to
 * the idle state instead of fetching.
 */
export function useAsyncResource<T>(
  fetcher: () => Promise<T> | null,
  deps: readonly unknown[],
  errorFallback: string,
): AsyncResource<T> {
  const [state, dispatch] = useReducer(reducer<T>, undefined, initialState<T>)

  useEffect(() => {
    const request = fetcher()
    if (!request) {
      dispatch({ type: 'RESET' })
      return
    }

    dispatch({ type: 'FETCH_START' })
    void request
      .then((result) => dispatch({ type: 'FETCH_SUCCESS', payload: result }))
      .catch((err: unknown) => dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, errorFallback) }))
    // eslint-disable-next-line react-hooks/exhaustive-deps -- deps is caller-supplied; fetcher/errorFallback are intentionally excluded
  }, [...deps, state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  return { data: state.data, isLoading: state.isLoading, error: state.error, retry }
}
