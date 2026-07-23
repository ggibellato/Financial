import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { ApiError } from '../api/apiError'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { ReserveBucketBalanceDto, ReserveMovementDto } from '../api/types'

export const RESERVE_BUCKETS = ['Investimento', 'HouseTreats', 'Ariana', 'Gleison'] as const

export type SplitFormField =
  | 'splitDate'
  | 'gleisonSalaryGross'
  | 'gleisonSalaryNet'
  | 'arianaSalaryGross'
  | 'arianaSalaryNet'
  | 'lottery'
  | 'dividendoJuros'

export type WithdrawalFormField = 'withdrawalBucket' | 'withdrawalAmount' | 'withdrawalDate' | 'withdrawalDescription'

interface ReservaState {
  balances: ReserveBucketBalanceDto[]
  movements: ReserveMovementDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  isSplitFormOpen: boolean
  splitDate: string
  gleisonSalaryGross: string
  gleisonSalaryNet: string
  arianaSalaryGross: string
  arianaSalaryNet: string
  lottery: string
  dividendoJuros: string
  isSubmittingSplit: boolean
  splitError: string | null
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
  | { type: 'SPLIT_SUCCESS' }
  | { type: 'SPLIT_ERROR'; payload: string }
  | { type: 'SHOW_WITHDRAWAL_FORM' }
  | { type: 'CANCEL_WITHDRAWAL_FORM' }
  | { type: 'SET_WITHDRAWAL_FIELD'; payload: { field: WithdrawalFormField; value: string } }
  | { type: 'WITHDRAWAL_START' }
  | { type: 'WITHDRAWAL_SUCCESS' }
  | { type: 'WITHDRAWAL_ERROR'; payload: string }

const BLANK_SPLIT_FORM = {
  splitDate: '',
  gleisonSalaryGross: '',
  gleisonSalaryNet: '',
  arianaSalaryGross: '',
  arianaSalaryNet: '',
  lottery: '',
  dividendoJuros: '',
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
      return { ...state, isSplitFormOpen: true }
    case 'CANCEL_SPLIT_FORM':
      return { ...state, ...BLANK_SPLIT_FORM, isSplitFormOpen: false, splitError: null }
    case 'SET_SPLIT_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SPLIT_START':
      return { ...state, isSubmittingSplit: true, splitError: null }
    case 'SPLIT_SUCCESS':
      return { ...state, ...BLANK_SPLIT_FORM, isSplitFormOpen: false, isSubmittingSplit: false }
    case 'SPLIT_ERROR':
      return { ...state, isSubmittingSplit: false, splitError: action.payload }
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
  isLoading: boolean
  error: string | null
  retry: () => void
  isSplitFormOpen: boolean
  splitDate: string
  gleisonSalaryGross: string
  gleisonSalaryNet: string
  arianaSalaryGross: string
  arianaSalaryNet: string
  lottery: string
  dividendoJuros: string
  isSubmittingSplit: boolean
  splitError: string | null
  showSplitForm: () => void
  cancelSplitForm: () => void
  setSplitField: (field: SplitFormField, value: string) => void
  submitIncomeSplit: () => void
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

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const showSplitForm = useCallback(() => dispatch({ type: 'SHOW_SPLIT_FORM' }), [])

  const cancelSplitForm = useCallback(() => dispatch({ type: 'CANCEL_SPLIT_FORM' }), [])

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
    const {
      splitDate,
      gleisonSalaryGross,
      gleisonSalaryNet,
      arianaSalaryGross,
      arianaSalaryNet,
      lottery,
      dividendoJuros,
    } = state

    if (!splitDate.trim()) {
      dispatch({ type: 'SPLIT_ERROR', payload: 'Date is required' })
      return
    }

    dispatch({ type: 'SPLIT_START' })

    void apiClient
      .postIncomeSplit({
        date: splitDate,
        gleisonSalaryGross: Number(gleisonSalaryGross) || 0,
        gleisonSalaryNet: Number(gleisonSalaryNet) || 0,
        arianaSalaryGross: Number(arianaSalaryGross) || 0,
        arianaSalaryNet: Number(arianaSalaryNet) || 0,
        lottery: Number(lottery) || 0,
        dividendoJuros: Number(dividendoJuros) || 0,
      })
      .then(() => {
        dispatch({ type: 'SPLIT_SUCCESS' })
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
    isLoading: state.isLoading,
    error: state.error,
    retry,
    isSplitFormOpen: state.isSplitFormOpen,
    splitDate: state.splitDate,
    gleisonSalaryGross: state.gleisonSalaryGross,
    gleisonSalaryNet: state.gleisonSalaryNet,
    arianaSalaryGross: state.arianaSalaryGross,
    arianaSalaryNet: state.arianaSalaryNet,
    lottery: state.lottery,
    dividendoJuros: state.dividendoJuros,
    isSubmittingSplit: state.isSubmittingSplit,
    splitError: state.splitError,
    showSplitForm,
    cancelSplitForm,
    setSplitField,
    submitIncomeSplit,
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
