import { useCallback, useMemo, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { InvestmentSnapshotSuggestionDto, InvestmentSnapshotSuggestionSkippedDto } from '../api/types'
import { getErrorMessage, parseValidatedNumber } from '../utils/formatters'

export type SuggestionRowStatus = 'pending' | 'success' | 'error'

export interface SuggestionRow {
  snapshotId: string
  accountId: string
  accountName: string
  currentValue: number
  suggestedValue: string
  sourceDescription: string
  included: boolean
  status: SuggestionRowStatus
}

export type SuggestedValuesPhase = 'closed' | 'loading' | 'error' | 'ready' | 'applying' | 'completed'

export interface ApplyProgress {
  current: number
  total: number
  accountName: string
}

interface State {
  phase: SuggestedValuesPhase
  fetchError: string | null
  rows: SuggestionRow[]
  notUpdated: InvestmentSnapshotSuggestionSkippedDto[]
  applyProgress: ApplyProgress | null
}

type Action =
  | { type: 'OPEN' }
  | { type: 'CLOSE' }
  | { type: 'FETCH_START' }
  | {
      type: 'FETCH_SUCCESS'
      payload: { suggestions: InvestmentSnapshotSuggestionDto[]; notUpdated: InvestmentSnapshotSuggestionSkippedDto[] }
    }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'TOGGLE_INCLUDED'; payload: string }
  | { type: 'SET_VALUE'; payload: { accountId: string; value: string } }
  | { type: 'APPLY_START' }
  | { type: 'APPLY_PROGRESS'; payload: ApplyProgress }
  | { type: 'APPLY_ROW_DONE'; payload: { accountId: string; status: 'success' | 'error' } }
  | { type: 'APPLY_FINISHED' }

const INITIAL_STATE: State = {
  phase: 'closed',
  fetchError: null,
  rows: [],
  notUpdated: [],
  applyProgress: null,
}

function toRow(dto: InvestmentSnapshotSuggestionDto): SuggestionRow {
  return {
    snapshotId: dto.snapshotId,
    accountId: dto.accountId,
    accountName: dto.accountName,
    currentValue: dto.currentValue,
    suggestedValue: String(dto.suggestedValue),
    sourceDescription: dto.sourceDescription,
    included: dto.currentValue === 0,
    status: 'pending',
  }
}

function reducer(state: State, action: Action): State {
  switch (action.type) {
    case 'OPEN':
      return { ...INITIAL_STATE, phase: 'loading' }
    case 'CLOSE':
      return INITIAL_STATE
    case 'FETCH_START':
      return { ...state, phase: 'loading', fetchError: null }
    case 'FETCH_SUCCESS':
      return {
        ...state,
        phase: 'ready',
        rows: action.payload.suggestions.map(toRow),
        notUpdated: action.payload.notUpdated,
      }
    case 'FETCH_ERROR':
      return { ...state, phase: 'error', fetchError: action.payload }
    case 'TOGGLE_INCLUDED':
      return {
        ...state,
        rows: state.rows.map((r) => (r.accountId === action.payload ? { ...r, included: !r.included } : r)),
      }
    case 'SET_VALUE':
      return {
        ...state,
        rows: state.rows.map((r) =>
          r.accountId === action.payload.accountId ? { ...r, suggestedValue: action.payload.value } : r,
        ),
      }
    case 'APPLY_START':
      return { ...state, phase: 'applying' }
    case 'APPLY_PROGRESS':
      return { ...state, applyProgress: action.payload }
    case 'APPLY_ROW_DONE':
      return {
        ...state,
        rows: state.rows.map((r) =>
          r.accountId === action.payload.accountId ? { ...r, status: action.payload.status } : r,
        ),
      }
    case 'APPLY_FINISHED':
      return { ...state, phase: 'completed', applyProgress: null }
    default:
      return state
  }
}

export interface UseSuggestedValuesResult {
  isOpen: boolean
  phase: SuggestedValuesPhase
  fetchError: string | null
  rows: SuggestionRow[]
  notUpdated: InvestmentSnapshotSuggestionSkippedDto[]
  checkedCount: number
  applyProgress: ApplyProgress | null
  succeededCount: number
  failedRows: SuggestionRow[]
  open: () => void
  close: () => void
  retryFetch: () => void
  toggleIncluded: (accountId: string) => void
  setValue: (accountId: string, value: string) => void
  apply: () => Promise<void>
  retryFailed: () => Promise<void>
}

/** Manages the "Suggest Values" review panel: fetch, per-row include/edit state, sequential apply
 * with progress, and a Retry Failed that resubmits currently-shown values without refetching. */
export function useSuggestedValues(year: number, month: number, onApplied: () => void): UseSuggestedValuesResult {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const fetchSuggestions = useCallback(() => {
    dispatch({ type: 'FETCH_START' })
    return apiClient
      .getInvestmentSnapshotSuggestions(year, month)
      .then((result) => dispatch({ type: 'FETCH_SUCCESS', payload: result }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, "Couldn't load suggestions") })
      })
  }, [year, month])

  const open = useCallback(() => {
    dispatch({ type: 'OPEN' })
    void fetchSuggestions()
  }, [fetchSuggestions])

  const close = useCallback(() => dispatch({ type: 'CLOSE' }), [])
  const retryFetch = useCallback(() => void fetchSuggestions(), [fetchSuggestions])
  const toggleIncluded = useCallback(
    (accountId: string) => dispatch({ type: 'TOGGLE_INCLUDED', payload: accountId }),
    [],
  )
  const setValue = useCallback(
    (accountId: string, value: string) => dispatch({ type: 'SET_VALUE', payload: { accountId, value } }),
    [],
  )

  const applyRows = useCallback(
    async (rowsToApply: SuggestionRow[]) => {
      dispatch({ type: 'APPLY_START' })
      let anySucceeded = false
      let anyFailed = false

      for (let i = 0; i < rowsToApply.length; i++) {
        const row = rowsToApply[i]
        dispatch({
          type: 'APPLY_PROGRESS',
          payload: { current: i + 1, total: rowsToApply.length, accountName: row.accountName },
        })

        const value = parseValidatedNumber(row.suggestedValue, { min: 0 })
        let status: 'success' | 'error' = 'error'

        if (value !== null) {
          try {
            await apiClient.updateInvestmentSnapshotValue(row.snapshotId, { value })
            status = 'success'
          } catch {
            status = 'error'
          }
        }

        dispatch({ type: 'APPLY_ROW_DONE', payload: { accountId: row.accountId, status } })
        if (status === 'success') anySucceeded = true
        else anyFailed = true
      }

      if (anySucceeded) {
        onApplied()
      }

      if (anyFailed) {
        dispatch({ type: 'APPLY_FINISHED' })
      } else {
        dispatch({ type: 'CLOSE' })
      }
    },
    [onApplied],
  )

  const apply = useCallback(() => applyRows(state.rows.filter((r) => r.included)), [state.rows, applyRows])

  const retryFailed = useCallback(
    () => applyRows(state.rows.filter((r) => r.status === 'error')),
    [state.rows, applyRows],
  )

  const checkedCount = useMemo(() => state.rows.filter((r) => r.included).length, [state.rows])
  const succeededCount = useMemo(() => state.rows.filter((r) => r.status === 'success').length, [state.rows])
  const failedRows = useMemo(() => state.rows.filter((r) => r.status === 'error'), [state.rows])

  return {
    isOpen: state.phase !== 'closed',
    phase: state.phase,
    fetchError: state.fetchError,
    rows: state.rows,
    notUpdated: state.notUpdated,
    checkedCount,
    applyProgress: state.applyProgress,
    succeededCount,
    failedRows,
    open,
    close,
    retryFetch,
    toggleIncluded,
    setValue,
    apply,
    retryFailed,
  }
}
