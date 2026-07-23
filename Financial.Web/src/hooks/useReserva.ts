import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { ApiError } from '../api/apiError'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { IncomeSplitResultDto, ReserveBucketBalanceDto, ReserveMovementDto } from '../api/types'

export const RESERVE_BUCKETS = ['Investimento', 'HouseTreats', 'Ariana', 'Gleison'] as const

export type SplitFormField = 'splitDate' | 'splitAmount' | 'splitDescription'

export type WithdrawalFormField = 'withdrawalBucket' | 'withdrawalAmount' | 'withdrawalDate' | 'withdrawalDescription'

/**
 * A movement row for display, with `groupTotal` set on the last movement of a same
 * date+description group (2+ movements) — how a split's total is found when browsing history.
 */
export interface ReserveMovementRow extends ReserveMovementDto {
  groupTotal: number | null
}

interface ReservaState {
  balances: ReserveBucketBalanceDto[]
  movements: ReserveMovementDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  isSplitFormOpen: boolean
  splitDate: string
  splitAmount: string
  splitDescription: string
  isSubmittingSplit: boolean
  splitError: string | null
  lastSplitResult: IncomeSplitResultDto | null
  isWithdrawalFormOpen: boolean
  withdrawalBucket: string
  withdrawalAmount: string
  withdrawalDate: string
  withdrawalDescription: string
  isSubmittingWithdrawal: boolean
  withdrawalError: string | null
}

type ReservaAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: { balances: ReserveBucketBalanceDto[]; movements: ReserveMovementDto[] } }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SHOW_SPLIT_FORM' }
  | { type: 'CANCEL_SPLIT_FORM' }
  | { type: 'SET_SPLIT_FIELD'; payload: { field: SplitFormField; value: string } }
  | { type: 'SPLIT_START' }
  | { type: 'SPLIT_SUCCESS'; payload: IncomeSplitResultDto }
  | { type: 'SPLIT_ERROR'; payload: string }
  | { type: 'DISMISS_SPLIT_RESULT' }
  | { type: 'SHOW_WITHDRAWAL_FORM' }
  | { type: 'CANCEL_WITHDRAWAL_FORM' }
  | { type: 'SET_WITHDRAWAL_FIELD'; payload: { field: WithdrawalFormField; value: string } }
  | { type: 'WITHDRAWAL_START' }
  | { type: 'WITHDRAWAL_SUCCESS' }
  | { type: 'WITHDRAWAL_ERROR'; payload: string }

const BLANK_SPLIT_FORM = {
  splitDate: '',
  splitAmount: '',
  splitDescription: '',
} as const

const BLANK_WITHDRAWAL_FORM = {
  withdrawalBucket: RESERVE_BUCKETS[0],
  withdrawalAmount: '',
  withdrawalDate: '',
  withdrawalDescription: '',
} as const

const INITIAL_STATE: ReservaState = {
  balances: [],
  movements: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  isSplitFormOpen: false,
  ...BLANK_SPLIT_FORM,
  isSubmittingSplit: false,
  splitError: null,
  lastSplitResult: null,
  isWithdrawalFormOpen: false,
  ...BLANK_WITHDRAWAL_FORM,
  isSubmittingWithdrawal: false,
  withdrawalError: null,
}

function reducer(state: ReservaState, action: ReservaAction): ReservaState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, balances: action.payload.balances, movements: action.payload.movements }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'SHOW_SPLIT_FORM':
      return { ...state, isSplitFormOpen: true, lastSplitResult: null }
    case 'CANCEL_SPLIT_FORM':
      return { ...state, ...BLANK_SPLIT_FORM, isSplitFormOpen: false, splitError: null }
    case 'SET_SPLIT_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SPLIT_START':
      return { ...state, isSubmittingSplit: true, splitError: null }
    case 'SPLIT_SUCCESS':
      return {
        ...state,
        ...BLANK_SPLIT_FORM,
        isSplitFormOpen: false,
        isSubmittingSplit: false,
        lastSplitResult: action.payload,
      }
    case 'SPLIT_ERROR':
      return { ...state, isSubmittingSplit: false, splitError: action.payload }
    case 'DISMISS_SPLIT_RESULT':
      return { ...state, lastSplitResult: null }
    case 'SHOW_WITHDRAWAL_FORM':
      return { ...state, isWithdrawalFormOpen: true }
    case 'CANCEL_WITHDRAWAL_FORM':
      return { ...state, ...BLANK_WITHDRAWAL_FORM, isWithdrawalFormOpen: false, withdrawalError: null }
    case 'SET_WITHDRAWAL_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'WITHDRAWAL_START':
      return { ...state, isSubmittingWithdrawal: true, withdrawalError: null }
    case 'WITHDRAWAL_SUCCESS':
      return { ...state, ...BLANK_WITHDRAWAL_FORM, isWithdrawalFormOpen: false, isSubmittingWithdrawal: false }
    case 'WITHDRAWAL_ERROR':
      return { ...state, isSubmittingWithdrawal: false, withdrawalError: action.payload }
    default:
      return state
  }
}

export interface ReservaData {
  balances: ReserveBucketBalanceDto[]
  totalBalance: number
  movements: ReserveMovementDto[]
  movementRows: ReserveMovementRow[]
  isLoading: boolean
  error: string | null
  retry: () => void
  isSplitFormOpen: boolean
  splitDate: string
  splitAmount: string
  splitDescription: string
  isSubmittingSplit: boolean
  splitError: string | null
  lastSplitResult: IncomeSplitResultDto | null
  showSplitForm: () => void
  cancelSplitForm: () => void
  setSplitField: (field: SplitFormField, value: string) => void
  submitIncomeSplit: () => void
  dismissSplitResult: () => void
  isWithdrawalFormOpen: boolean
  withdrawalBucket: string
  withdrawalAmount: string
  withdrawalDate: string
  withdrawalDescription: string
  isSubmittingWithdrawal: boolean
  withdrawalError: string | null
  showWithdrawalForm: () => void
  cancelWithdrawalForm: () => void
  setWithdrawalField: (field: WithdrawalFormField, value: string) => void
  submitWithdrawal: () => void
}

function buildMovementRows(movements: ReserveMovementDto[]): ReserveMovementRow[] {
  const groups = new Map<string, { total: number; count: number; lastIndex: number }>()
  movements.forEach((m, index) => {
    const key = `${m.date}|${m.description}`
    const group = groups.get(key) ?? { total: 0, count: 0, lastIndex: index }
    group.total += m.amount
    group.count += 1
    group.lastIndex = index
    groups.set(key, group)
  })

  return movements.map((m, index) => {
    const group = groups.get(`${m.date}|${m.description}`)!
    return { ...m, groupTotal: group.count > 1 && group.lastIndex === index ? group.total : null }
  })
}

export function useReserva(): ReservaData {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const fetchReservaData = useCallback(() => {
    dispatch({ type: 'FETCH_START' })
    void Promise.all([apiClient.getReserveBalances(), apiClient.getReserveMovements()])
      .then(([balances, movements]) => dispatch({ type: 'FETCH_SUCCESS', payload: { balances, movements } }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Unable to load Reserva data' })
      })
  }, [apiClient])

  useEffect(() => {
    fetchReservaData()
  }, [fetchReservaData, state.retryCount])

  const totalBalance = useMemo(
    () => state.balances.reduce((sum, b) => sum + b.balance, 0),
    [state.balances],
  )

  const movementRows = useMemo(() => buildMovementRows(state.movements), [state.movements])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const showSplitForm = useCallback(() => dispatch({ type: 'SHOW_SPLIT_FORM' }), [])

  const cancelSplitForm = useCallback(() => dispatch({ type: 'CANCEL_SPLIT_FORM' }), [])

  const dismissSplitResult = useCallback(() => dispatch({ type: 'DISMISS_SPLIT_RESULT' }), [])

  const showWithdrawalForm = useCallback(() => dispatch({ type: 'SHOW_WITHDRAWAL_FORM' }), [])

  const cancelWithdrawalForm = useCallback(() => dispatch({ type: 'CANCEL_WITHDRAWAL_FORM' }), [])

  const setSplitField = useCallback(
    (field: SplitFormField, value: string) => dispatch({ type: 'SET_SPLIT_FIELD', payload: { field, value } }),
    [],
  )

  const setWithdrawalField = useCallback(
    (field: WithdrawalFormField, value: string) => dispatch({ type: 'SET_WITHDRAWAL_FIELD', payload: { field, value } }),
    [],
  )

  function submitIncomeSplit() {
    const { splitDate, splitAmount, splitDescription } = state

    if (!splitDate.trim()) {
      dispatch({ type: 'SPLIT_ERROR', payload: 'Date is required' })
      return
    }

    const amount = Number(splitAmount)
    if (!splitAmount.trim() || !isFinite(amount) || amount <= 0) {
      dispatch({ type: 'SPLIT_ERROR', payload: 'Amount must be a positive number' })
      return
    }

    if (!splitDescription.trim()) {
      dispatch({ type: 'SPLIT_ERROR', payload: 'Description is required' })
      return
    }

    dispatch({ type: 'SPLIT_START' })

    void apiClient
      .postIncomeSplit({ date: splitDate, amount, description: splitDescription })
      .then((result) => {
        dispatch({ type: 'SPLIT_SUCCESS', payload: result })
        fetchReservaData()
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SPLIT_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to post income split',
        })
      })
  }

  function performWithdrawal(confirmed: boolean) {
    const { withdrawalBucket, withdrawalAmount, withdrawalDate, withdrawalDescription } = state

    void apiClient
      .postWithdrawal({
        bucket: withdrawalBucket,
        amount: Number(withdrawalAmount) || 0,
        date: withdrawalDate,
        description: withdrawalDescription,
        confirmed,
      })
      .then(() => {
        dispatch({ type: 'WITHDRAWAL_SUCCESS' })
        fetchReservaData()
      })
      .catch((err: unknown) => {
        if (err instanceof ApiError && err.status === 409 && !confirmed) {
          if (window.confirm(`${err.message}\n\nProceed anyway?`)) {
            performWithdrawal(true)
            return
          }
          dispatch({ type: 'WITHDRAWAL_ERROR', payload: err.message })
          return
        }

        dispatch({
          type: 'WITHDRAWAL_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to post withdrawal',
        })
      })
  }

  function submitWithdrawal() {
    const { withdrawalAmount, withdrawalDate, withdrawalDescription } = state

    const amount = Number(withdrawalAmount)
    if (!withdrawalAmount.trim() || !isFinite(amount) || amount <= 0) {
      dispatch({ type: 'WITHDRAWAL_ERROR', payload: 'Amount must be a positive number' })
      return
    }

    if (!withdrawalDate.trim()) {
      dispatch({ type: 'WITHDRAWAL_ERROR', payload: 'Date is required' })
      return
    }

    if (!withdrawalDescription.trim()) {
      dispatch({ type: 'WITHDRAWAL_ERROR', payload: 'Description is required' })
      return
    }

    dispatch({ type: 'WITHDRAWAL_START' })
    performWithdrawal(false)
  }

  return {
    balances: state.balances,
    totalBalance,
    movements: state.movements,
    movementRows,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    isSplitFormOpen: state.isSplitFormOpen,
    splitDate: state.splitDate,
    splitAmount: state.splitAmount,
    splitDescription: state.splitDescription,
    isSubmittingSplit: state.isSubmittingSplit,
    splitError: state.splitError,
    lastSplitResult: state.lastSplitResult,
    showSplitForm,
    cancelSplitForm,
    setSplitField,
    submitIncomeSplit,
    dismissSplitResult,
    isWithdrawalFormOpen: state.isWithdrawalFormOpen,
    withdrawalBucket: state.withdrawalBucket,
    withdrawalAmount: state.withdrawalAmount,
    withdrawalDate: state.withdrawalDate,
    withdrawalDescription: state.withdrawalDescription,
    isSubmittingWithdrawal: state.isSubmittingWithdrawal,
    withdrawalError: state.withdrawalError,
    showWithdrawalForm,
    cancelWithdrawalForm,
    setWithdrawalField,
    submitWithdrawal,
  }
}
