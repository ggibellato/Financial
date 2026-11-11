import { useCallback, useEffect, useReducer, useRef, type Dispatch } from 'react'
import { getErrorMessage } from '../utils/formatters'

interface AsyncResourceState<T> {
  data: T | null
  isLoading: boolean
  error: string | null
  retryCount: number
}

type AsyncResourceAction<T> =
  | { type: 'RESET' }
  | { type: 'FETCH_START'; keepData: boolean }
  | { type: 'FETCH_SUCCESS'; payload: T }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SET_DATA'; update: (current: T | null) => T | null }

function reducer<T>(state: AsyncResourceState<T>, action: AsyncResourceAction<T>): AsyncResourceState<T> {
  switch (action.type) {
    case 'RESET':
      return { data: null, isLoading: false, error: null, retryCount: state.retryCount }
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null, data: action.keepData ? state.data : null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, error: null, data: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'SET_DATA':
      return { ...state, data: action.update(state.data) }
    default:
      return state
  }
}

function initialState<T>(initialLoading: boolean): AsyncResourceState<T> {
  return { data: null, isLoading: initialLoading, error: null, retryCount: 0 }
}

export function useLatestRequestGuard(): () => () => boolean {
  const latestRequestId = useRef(0)
  return useCallback(() => {
    const requestId = ++latestRequestId.current
    return () => requestId === latestRequestId.current
  }, [])
}

export interface SelectionGuard<A> {
  beginSelection: () => void
  dispatchForCurrentSelection: () => (action: A) => void
}

export function useSelectionGuard<A>(dispatch: Dispatch<A>): SelectionGuard<A> {
  const beginRequest = useLatestRequestGuard()
  const isCurrentSelection = useRef<() => boolean>(() => true)

  const beginSelection = useCallback(() => {
    isCurrentSelection.current = beginRequest()
  }, [beginRequest])

  const dispatchForCurrentSelection = useCallback(() => {
    const isCurrent = isCurrentSelection.current
    return (action: A) => {
      if (isCurrent()) dispatch(action)
    }
  }, [dispatch])

  return { beginSelection, dispatchForCurrentSelection }
}

export interface AsyncResourceOptions {
  initialLoading?: boolean
  keepDataWhileLoading?: boolean
}

export interface AsyncResource<T> {
  data: T | null
  isLoading: boolean
  error: string | null
  retry: () => void
  refresh: () => Promise<void>
  setData: (update: (current: T | null) => T | null) => void
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
  { initialLoading = false, keepDataWhileLoading = false }: AsyncResourceOptions = {},
): AsyncResource<T> {
  const [state, dispatch] = useReducer(reducer<T>, initialLoading, initialState<T>)
  const latest = useRef({ fetcher, errorFallback })
  const beginRequest = useLatestRequestGuard()

  useEffect(() => {
    latest.current = { fetcher, errorFallback }
  })

  const settleLatest = useCallback((request: Promise<T>, fallback: string) => {
    const isLatest = beginRequest()
    return request
      .then((result) => {
        if (isLatest()) dispatch({ type: 'FETCH_SUCCESS', payload: result })
      })
      .catch((err: unknown) => {
        if (isLatest()) dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, fallback) })
      })
  }, [beginRequest])

  useEffect(() => {
    const request = fetcher()
    if (!request) {
      beginRequest()
      dispatch({ type: 'RESET' })
      return
    }

    dispatch({ type: 'FETCH_START', keepData: keepDataWhileLoading })
    void settleLatest(request, errorFallback)
    // eslint-disable-next-line react-hooks/exhaustive-deps -- deps is caller-supplied; fetcher/errorFallback are intentionally excluded
  }, [...deps, state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const refresh = useCallback(() => {
    const { fetcher: currentFetcher, errorFallback: currentFallback } = latest.current
    const request = currentFetcher()
    if (!request) return Promise.resolve()

    return settleLatest(request, currentFallback)
  }, [settleLatest])

  const setData = useCallback(
    (update: (current: T | null) => T | null) => dispatch({ type: 'SET_DATA', update }),
    [],
  )

  return { data: state.data, isLoading: state.isLoading, error: state.error, retry, refresh, setData }
}
