import { useCallback, useEffect, useReducer, useRef, useState } from 'react'
import { getErrorMessage } from '../utils/formatters'

interface CrudResourceState<T> {
  data: T
  isLoading: boolean
  error: string | null
  retryCount: number
}

type CrudResourceAction<T> =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: T }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }

function reducer<T>(state: CrudResourceState<T>, action: CrudResourceAction<T>): CrudResourceState<T> {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
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

export type RefreshMode = 'reload' | 'silent'

export interface CrudResourceOptions<T> {
  load: () => Promise<T>
  initialData: T
  loadErrorMessage: string
  refreshMode?: RefreshMode
}

export interface PendingOperation {
  pendingId: string | null
  pendingError: string | null
  start: (id: string) => void
  succeed: () => void
  fail: (message: string) => void
}

export interface CrudResource<T> {
  data: T
  isLoading: boolean
  error: string | null
  retry: () => void
  refresh: () => Promise<void> | void
  mutateAndRefresh: <R>(mutation: () => Promise<R>) => Promise<R>
  pendingId: string | null
  pendingError: string | null
  runPendingMutation: (id: string, mutation: () => Promise<unknown>, errorMessage: string, onSuccess?: () => void) => void
}

interface PendingState {
  pendingId: string | null
  pendingError: string | null
}

const IDLE_PENDING: PendingState = { pendingId: null, pendingError: null }

export function usePendingOperation(): PendingOperation {
  const [state, setState] = useState<PendingState>(IDLE_PENDING)

  const start = useCallback((id: string) => setState({ pendingId: id, pendingError: null }), [])
  const succeed = useCallback(() => setState((current) => ({ ...current, pendingId: null })), [])
  const fail = useCallback((message: string) => setState({ pendingId: null, pendingError: message }), [])

  return { pendingId: state.pendingId, pendingError: state.pendingError, start, succeed, fail }
}

export function useCrudResource<T>(options: CrudResourceOptions<T>): CrudResource<T> {
  const { initialData, refreshMode = 'reload' } = options
  const [state, dispatch] = useReducer(reducer<T>, undefined, () => ({
    data: initialData,
    isLoading: true,
    error: null,
    retryCount: 0,
  }))
  const pending = usePendingOperation()
  const optionsRef = useRef(options)

  useEffect(() => {
    optionsRef.current = options
  })

  const fetchData = useCallback(() => {
    const { load, loadErrorMessage } = optionsRef.current
    return load()
      .then((data) => dispatch({ type: 'FETCH_SUCCESS', payload: data }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, loadErrorMessage) })
      })
  }, [])

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void fetchData()
  }, [state.retryCount, fetchData])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const refresh = useCallback(() => {
    if (refreshMode === 'silent') return fetchData()
    dispatch({ type: 'RETRY' })
  }, [refreshMode, fetchData])

  const mutateAndRefresh = useCallback(
    async <R,>(mutation: () => Promise<R>) => {
      const result = await mutation()
      const refreshing = refresh()
      if (refreshing) await refreshing
      return result
    },
    [refresh],
  )

  const { start, succeed, fail } = pending
  const runPendingMutation = useCallback(
    (id: string, mutation: () => Promise<unknown>, errorMessage: string, onSuccess?: () => void) => {
      start(id)

      void mutation()
        .then(() => {
          succeed()
          onSuccess?.()
          void refresh()
        })
        .catch((err: unknown) => {
          fail(getErrorMessage(err, errorMessage))
        })
    },
    [start, succeed, fail, refresh],
  )

  return {
    data: state.data,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    refresh,
    mutateAndRefresh,
    pendingId: pending.pendingId,
    pendingError: pending.pendingError,
    runPendingMutation,
  }
}
