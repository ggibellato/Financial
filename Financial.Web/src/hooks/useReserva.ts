import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { ApiError } from '../api/apiError'
import { apiClient } from '../api/financialApiClient'
import type { IncomeSplitResultDto, ReserveBucketBalanceDto, ReserveBucketDto, ReserveMovementDto } from '../api/types'
import { getErrorMessage, todayIsoDate } from '../utils/formatters'
import { getStoredDefault, setStoredDefault } from '../utils/createFormDefaults'

const SPLIT_PERCENTAGE_MIN = 99.99
const SPLIT_PERCENTAGE_MAX = 100.01
const BUCKET_REQUIRED_ERROR = 'Bucket is required'

const SPLIT_DATE_KEY = 'incomeSplit.date'
const WITHDRAWAL_DATE_KEY = 'withdrawal.date'
const WITHDRAWAL_BUCKET_KEY = 'withdrawal.bucketId'

export type SplitFormField = 'splitDate' | 'splitAmount' | 'splitDescription'

export type WithdrawalFormField = 'withdrawalBucketId' | 'withdrawalAmount' | 'withdrawalDate' | 'withdrawalDescription'

export type EditMovementField = 'editMovementBucketId' | 'editMovementAmount' | 'editMovementDate' | 'editMovementDescription'

/**
 * A movement row for display. `groupTotal` is set only on the last movement of a same
 * date+description group (2+ movements) — how a split's total is found when browsing history.
 * `isPartOfGroup` is set on every movement in such a group (used to warn before a cascading
 * delete, since removing any one line of a split removes all of them). `isLocked` is set for a
 * movement created by an automated income split (F02) — its Edit/Delete controls are disabled.
 */
export interface ReserveMovementRow extends ReserveMovementDto {
  groupTotal: number | null
  isPartOfGroup: boolean
  isLocked: boolean
}

export const LOCKED_MOVEMENT_MESSAGE =
  'This reserve movement is linked to an income and can only be changed by editing that income.'

interface ReservaState {
  balances: ReserveBucketBalanceDto[]
  movements: ReserveMovementDto[]
  buckets: ReserveBucketDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  isSplitFormOpen: boolean
  splitDate: string
  splitAmount: string
  splitDescription: string
  isSubmittingSplit: boolean
  splitError: string | null
  splitErrorFields: Partial<Record<SplitFormField, string>>
  lastSplitResult: IncomeSplitResultDto | null
  isWithdrawalFormOpen: boolean
  withdrawalBucketId: string
  withdrawalAmount: string
  withdrawalDate: string
  withdrawalDescription: string
  isSubmittingWithdrawal: boolean
  withdrawalError: string | null
  withdrawalErrorFields: Partial<Record<WithdrawalFormField, string>>
  editingMovementId: string | null
  editMovementBucketId: string
  editMovementAmount: string
  editMovementDate: string
  editMovementDescription: string
  isSavingMovement: boolean
  saveMovementError: string | null
  saveMovementErrorFields: Partial<Record<EditMovementField, string>>
  deletingMovementId: string | null
  deleteMovementError: string | null
}

type ReservaAction =
  | { type: 'FETCH_START' }
  | {
      type: 'FETCH_SUCCESS'
      payload: { balances: ReserveBucketBalanceDto[]; movements: ReserveMovementDto[]; buckets?: ReserveBucketDto[] }
    }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SHOW_SPLIT_FORM'; payload: { date: string } }
  | { type: 'CANCEL_SPLIT_FORM' }
  | { type: 'SET_SPLIT_FIELD'; payload: { field: SplitFormField; value: string } }
  | { type: 'SPLIT_START' }
  | { type: 'SPLIT_SUCCESS'; payload: IncomeSplitResultDto }
  | { type: 'SPLIT_ERROR'; payload: { message: string | null; fields: Partial<Record<SplitFormField, string>> } }
  | { type: 'DISMISS_SPLIT_RESULT' }
  | { type: 'SHOW_WITHDRAWAL_FORM'; payload: { date: string } }
  | { type: 'CANCEL_WITHDRAWAL_FORM' }
  | { type: 'SET_WITHDRAWAL_FIELD'; payload: { field: WithdrawalFormField; value: string } }
  | { type: 'WITHDRAWAL_START' }
  | { type: 'WITHDRAWAL_SUCCESS' }
  | { type: 'WITHDRAWAL_ERROR'; payload: { message: string | null; fields: Partial<Record<WithdrawalFormField, string>> } }
  | { type: 'SHOW_EDIT_MOVEMENT_FORM'; payload: ReserveMovementDto }
  | { type: 'CANCEL_EDIT_MOVEMENT' }
  | { type: 'SET_EDIT_MOVEMENT_FIELD'; payload: { field: EditMovementField; value: string } }
  | { type: 'SAVE_MOVEMENT_START' }
  | { type: 'SAVE_MOVEMENT_SUCCESS' }
  | { type: 'SAVE_MOVEMENT_ERROR'; payload: { message: string | null; fields: Partial<Record<EditMovementField, string>> } }
  | { type: 'DELETE_MOVEMENT_START'; payload: string }
  | { type: 'DELETE_MOVEMENT_SUCCESS' }
  | { type: 'DELETE_MOVEMENT_ERROR'; payload: string }

const BLANK_SPLIT_FORM = {
  splitDate: '',
  splitAmount: '',
  splitDescription: '',
} as const

const BLANK_WITHDRAWAL_FORM_FIELDS = {
  withdrawalAmount: '',
  withdrawalDate: '',
  withdrawalDescription: '',
} as const

const INITIAL_STATE: ReservaState = {
  balances: [],
  movements: [],
  buckets: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  isSplitFormOpen: false,
  ...BLANK_SPLIT_FORM,
  isSubmittingSplit: false,
  splitError: null,
  splitErrorFields: {},
  lastSplitResult: null,
  isWithdrawalFormOpen: false,
  withdrawalBucketId: '',
  ...BLANK_WITHDRAWAL_FORM_FIELDS,
  isSubmittingWithdrawal: false,
  withdrawalError: null,
  withdrawalErrorFields: {},
  editingMovementId: null,
  editMovementBucketId: '',
  editMovementAmount: '',
  editMovementDate: '',
  editMovementDescription: '',
  isSavingMovement: false,
  saveMovementError: null,
  saveMovementErrorFields: {},
  deletingMovementId: null,
  deleteMovementError: null,
}

function reducer(state: ReservaState, action: ReservaAction): ReservaState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS': {
      const buckets = action.payload.buckets ?? state.buckets
      return {
        ...state,
        isLoading: false,
        balances: action.payload.balances,
        movements: action.payload.movements,
        buckets,
        withdrawalBucketId: state.withdrawalBucketId || defaultBucketId(buckets),
      }
    }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'SHOW_SPLIT_FORM':
      return { ...state, isSplitFormOpen: true, lastSplitResult: null, splitDate: action.payload.date }
    case 'CANCEL_SPLIT_FORM':
      return { ...state, ...BLANK_SPLIT_FORM, isSplitFormOpen: false, splitError: null, splitErrorFields: {} }
    case 'SET_SPLIT_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SPLIT_START':
      return { ...state, isSubmittingSplit: true, splitError: null, splitErrorFields: {} }
    case 'SPLIT_SUCCESS':
      return {
        ...state,
        ...BLANK_SPLIT_FORM,
        isSplitFormOpen: false,
        isSubmittingSplit: false,
        lastSplitResult: action.payload,
      }
    case 'SPLIT_ERROR':
      return { ...state, isSubmittingSplit: false, splitError: action.payload.message, splitErrorFields: action.payload.fields }
    case 'DISMISS_SPLIT_RESULT':
      return { ...state, lastSplitResult: null }
    case 'SHOW_WITHDRAWAL_FORM':
      return { ...state, isWithdrawalFormOpen: true, withdrawalDate: action.payload.date }
    case 'CANCEL_WITHDRAWAL_FORM':
      return {
        ...state,
        ...BLANK_WITHDRAWAL_FORM_FIELDS,
        withdrawalBucketId: defaultBucketId(state.buckets),
        isWithdrawalFormOpen: false,
        withdrawalError: null,
        withdrawalErrorFields: {},
      }
    case 'SET_WITHDRAWAL_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'WITHDRAWAL_START':
      return { ...state, isSubmittingWithdrawal: true, withdrawalError: null, withdrawalErrorFields: {} }
    case 'WITHDRAWAL_SUCCESS':
      return {
        ...state,
        ...BLANK_WITHDRAWAL_FORM_FIELDS,
        withdrawalBucketId: defaultBucketId(state.buckets),
        isWithdrawalFormOpen: false,
        isSubmittingWithdrawal: false,
      }
    case 'WITHDRAWAL_ERROR':
      return {
        ...state,
        isSubmittingWithdrawal: false,
        withdrawalError: action.payload.message,
        withdrawalErrorFields: action.payload.fields,
      }
    case 'SHOW_EDIT_MOVEMENT_FORM':
      return {
        ...state,
        editingMovementId: action.payload.id,
        editMovementBucketId: action.payload.bucketId,
        editMovementAmount: String(action.payload.amount),
        editMovementDate: action.payload.date,
        editMovementDescription: action.payload.description,
        saveMovementError: null,
        saveMovementErrorFields: {},
      }
    case 'CANCEL_EDIT_MOVEMENT':
      return {
        ...state,
        editingMovementId: null,
        editMovementAmount: '',
        editMovementDate: '',
        editMovementDescription: '',
        saveMovementError: null,
        saveMovementErrorFields: {},
      }
    case 'SET_EDIT_MOVEMENT_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_MOVEMENT_START':
      return { ...state, isSavingMovement: true, saveMovementError: null, saveMovementErrorFields: {} }
    case 'SAVE_MOVEMENT_SUCCESS':
      return {
        ...state,
        isSavingMovement: false,
        editingMovementId: null,
        editMovementAmount: '',
        editMovementDate: '',
        editMovementDescription: '',
      }
    case 'SAVE_MOVEMENT_ERROR':
      return {
        ...state,
        isSavingMovement: false,
        saveMovementError: action.payload.message,
        saveMovementErrorFields: action.payload.fields,
      }
    case 'DELETE_MOVEMENT_START':
      return { ...state, deletingMovementId: action.payload, deleteMovementError: null }
    case 'DELETE_MOVEMENT_SUCCESS':
      return { ...state, deletingMovementId: null }
    case 'DELETE_MOVEMENT_ERROR':
      return { ...state, deletingMovementId: null, deleteMovementError: action.payload }
    default:
      return state
  }
}

/**
 * Asked by submitWithdrawal when the server rejects a withdrawal with 409 and a reason -
 * typically that it would overdraw the bucket. Returning true replays the request with
 * confirmed: true; returning false surfaces the server's message as the error.
 *
 * Injected rather than called here so this hook stays free of browser globals: the prompt is a
 * presentation decision, and a test can pass a plain function instead of stubbing window.
 */
export type ConfirmProceed = (serverMessage: string) => boolean

export interface ReservaData {
  balances: ReserveBucketBalanceDto[]
  totalBalance: number
  movements: ReserveMovementDto[]
  movementRows: ReserveMovementRow[]
  buckets: ReserveBucketDto[]
  splitPercentageWarning: string | null
  isLoading: boolean
  error: string | null
  retry: () => void
  isSplitFormOpen: boolean
  splitDate: string
  splitAmount: string
  splitDescription: string
  isSubmittingSplit: boolean
  splitError: string | null
  splitErrorFields: Partial<Record<SplitFormField, string>>
  lastSplitResult: IncomeSplitResultDto | null
  showSplitForm: () => void
  cancelSplitForm: () => void
  setSplitField: (field: SplitFormField, value: string) => void
  submitIncomeSplit: () => void
  dismissSplitResult: () => void
  isWithdrawalFormOpen: boolean
  withdrawalBucketId: string
  withdrawalAmount: string
  withdrawalDate: string
  withdrawalDescription: string
  isSubmittingWithdrawal: boolean
  withdrawalError: string | null
  withdrawalErrorFields: Partial<Record<WithdrawalFormField, string>>
  showWithdrawalForm: () => void
  cancelWithdrawalForm: () => void
  setWithdrawalField: (field: WithdrawalFormField, value: string) => void
  submitWithdrawal: (confirmProceed: ConfirmProceed) => void
  editingMovementId: string | null
  editMovementBucketId: string
  editMovementAmount: string
  editMovementDate: string
  editMovementDescription: string
  isSavingMovement: boolean
  saveMovementError: string | null
  saveMovementErrorFields: Partial<Record<EditMovementField, string>>
  showEditMovementForm: (movement: ReserveMovementDto) => void
  cancelEditMovement: () => void
  setEditMovementField: (field: EditMovementField, value: string) => void
  saveMovementEdit: () => void
  deletingMovementId: string | null
  deleteMovementError: string | null
  deleteMovement: (id: string) => void
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
    return {
      ...m,
      groupTotal: group.count > 1 && group.lastIndex === index ? group.total : null,
      isPartOfGroup: group.count > 1,
      isLocked: m.incomeId != null,
    }
  })
}

function computeSplitPercentageWarning(buckets: ReserveBucketDto[]): string | null {
  if (buckets.length === 0) return null

  const activeSum = buckets.reduce((sum, b) => (b.isActive ? sum + b.splitPercentage : sum), 0)

  if (activeSum >= SPLIT_PERCENTAGE_MIN && activeSum <= SPLIT_PERCENTAGE_MAX) return null

  return `Active bucket percentages sum to ${activeSum.toFixed(2)}%, not 100%`
}

function defaultBucketId(buckets: ReserveBucketDto[]): string {
  const stored = getStoredDefault(WITHDRAWAL_BUCKET_KEY)
  if (stored && buckets.some((b) => b.id === stored)) {
    return stored
  }
  return (buckets.find((b) => b.isActive) ?? buckets[0])?.id ?? ''
}

export function useReserva(): ReservaData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const fetchReservaData = useCallback((options?: { includeBuckets?: boolean }) => {
    const includeBuckets = options?.includeBuckets ?? true
    dispatch({ type: 'FETCH_START' })
    void Promise.all([
      apiClient.getReserveBalances(),
      apiClient.getReserveMovements(),
      includeBuckets ? apiClient.getReserveBuckets().catch(() => []) : Promise.resolve(undefined),
    ])
      .then(([balances, movements, buckets]) => dispatch({ type: 'FETCH_SUCCESS', payload: { balances, movements, buckets } }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load Reserva data') })
      })
  }, [])

  useEffect(() => {
    fetchReservaData()
  }, [fetchReservaData, state.retryCount])

  const totalBalance = useMemo(
    () => state.balances.reduce((sum, b) => sum + b.balance, 0),
    [state.balances],
  )

  const movementRows = useMemo(() => buildMovementRows(state.movements), [state.movements])

  const splitPercentageWarning = useMemo(() => computeSplitPercentageWarning(state.buckets), [state.buckets])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const showSplitForm = useCallback(
    () => dispatch({ type: 'SHOW_SPLIT_FORM', payload: { date: getStoredDefault(SPLIT_DATE_KEY) ?? todayIsoDate() } }),
    [],
  )

  const cancelSplitForm = useCallback(() => dispatch({ type: 'CANCEL_SPLIT_FORM' }), [])

  const dismissSplitResult = useCallback(() => dispatch({ type: 'DISMISS_SPLIT_RESULT' }), [])

  const showWithdrawalForm = useCallback(
    () => dispatch({ type: 'SHOW_WITHDRAWAL_FORM', payload: { date: getStoredDefault(WITHDRAWAL_DATE_KEY) ?? todayIsoDate() } }),
    [],
  )

  const cancelWithdrawalForm = useCallback(() => dispatch({ type: 'CANCEL_WITHDRAWAL_FORM' }), [])

  const setSplitField = useCallback(
    (field: SplitFormField, value: string) => dispatch({ type: 'SET_SPLIT_FIELD', payload: { field, value } }),
    [],
  )

  const setWithdrawalField = useCallback(
    (field: WithdrawalFormField, value: string) => dispatch({ type: 'SET_WITHDRAWAL_FIELD', payload: { field, value } }),
    [],
  )

  const showEditMovementForm = useCallback(
    (movement: ReserveMovementDto) => dispatch({ type: 'SHOW_EDIT_MOVEMENT_FORM', payload: movement }),
    [],
  )

  const cancelEditMovement = useCallback(() => dispatch({ type: 'CANCEL_EDIT_MOVEMENT' }), [])

  const setEditMovementField = useCallback(
    (field: EditMovementField, value: string) => dispatch({ type: 'SET_EDIT_MOVEMENT_FIELD', payload: { field, value } }),
    [],
  )

  function submitIncomeSplit() {
    const { splitDate, splitAmount, splitDescription } = state
    const errors: Partial<Record<SplitFormField, string>> = {}

    if (!splitDate.trim()) {
      errors.splitDate = 'Date is required'
    }

    const amount = Number(splitAmount)
    if (!splitAmount.trim() || !isFinite(amount) || amount <= 0) {
      errors.splitAmount = 'Amount must be a positive number'
    }

    if (!splitDescription.trim()) {
      errors.splitDescription = 'Description is required'
    }

    if (Object.keys(errors).length > 0) {
      dispatch({ type: 'SPLIT_ERROR', payload: { message: Object.values(errors)[0] ?? null, fields: errors } })
      return
    }

    dispatch({ type: 'SPLIT_START' })

    void apiClient
      .postIncomeSplit({ date: splitDate, amount, description: splitDescription })
      .then((result) => {
        setStoredDefault(SPLIT_DATE_KEY, splitDate)
        dispatch({ type: 'SPLIT_SUCCESS', payload: result })
        fetchReservaData({ includeBuckets: false })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SPLIT_ERROR',
          payload: { message: getErrorMessage(err, 'Failed to post income split'), fields: {} },
        })
      })
  }

  function performWithdrawal(confirmed: boolean, confirmProceed: ConfirmProceed) {
    const { withdrawalBucketId, withdrawalAmount, withdrawalDate, withdrawalDescription } = state

    void apiClient
      .postWithdrawal({
        bucketId: withdrawalBucketId,
        amount: Number(withdrawalAmount) || 0,
        date: withdrawalDate,
        description: withdrawalDescription,
        confirmed,
      })
      .then(() => {
        setStoredDefault(WITHDRAWAL_DATE_KEY, withdrawalDate)
        setStoredDefault(WITHDRAWAL_BUCKET_KEY, withdrawalBucketId)
        dispatch({ type: 'WITHDRAWAL_SUCCESS' })
        fetchReservaData({ includeBuckets: false })
      })
      .catch((err: unknown) => {
        if (err instanceof ApiError && err.status === 409 && !confirmed) {
          if (confirmProceed(err.message)) {
            performWithdrawal(true, confirmProceed)
            return
          }
          dispatch({ type: 'WITHDRAWAL_ERROR', payload: { message: err.message, fields: {} } })
          return
        }

        dispatch({
          type: 'WITHDRAWAL_ERROR',
          payload: { message: getErrorMessage(err, 'Failed to post withdrawal'), fields: {} },
        })
      })
  }

  function submitWithdrawal(confirmProceed: ConfirmProceed) {
    const { withdrawalBucketId, withdrawalAmount, withdrawalDate, withdrawalDescription } = state
    const errors: Partial<Record<WithdrawalFormField, string>> = {}

    if (!withdrawalDate.trim()) {
      errors.withdrawalDate = 'Date is required'
    }

    if (!withdrawalBucketId.trim()) {
      errors.withdrawalBucketId = BUCKET_REQUIRED_ERROR
    }

    if (!withdrawalDescription.trim()) {
      errors.withdrawalDescription = 'Description is required'
    }

    const amount = Number(withdrawalAmount)
    if (!withdrawalAmount.trim() || !isFinite(amount) || amount <= 0) {
      errors.withdrawalAmount = 'Amount must be a positive number'
    }

    if (Object.keys(errors).length > 0) {
      dispatch({ type: 'WITHDRAWAL_ERROR', payload: { message: Object.values(errors)[0] ?? null, fields: errors } })
      return
    }

    dispatch({ type: 'WITHDRAWAL_START' })
    performWithdrawal(false, confirmProceed)
  }

  function saveMovementEdit() {
    const { editingMovementId, editMovementBucketId, editMovementAmount, editMovementDate, editMovementDescription } = state
    if (!editingMovementId) return

    const errors: Partial<Record<EditMovementField, string>> = {}

    if (!editMovementDate.trim()) {
      errors.editMovementDate = 'Date is required'
    }

    if (!editMovementBucketId.trim()) {
      errors.editMovementBucketId = BUCKET_REQUIRED_ERROR
    }

    if (!editMovementDescription.trim()) {
      errors.editMovementDescription = 'Description is required'
    }

    const amount = Number(editMovementAmount)
    if (!editMovementAmount.trim() || !isFinite(amount)) {
      errors.editMovementAmount = 'Amount must be a number'
    }

    if (Object.keys(errors).length > 0) {
      dispatch({ type: 'SAVE_MOVEMENT_ERROR', payload: { message: Object.values(errors)[0] ?? null, fields: errors } })
      return
    }

    dispatch({ type: 'SAVE_MOVEMENT_START' })

    void apiClient
      .updateReserveMovement(editingMovementId, {
        bucketId: editMovementBucketId,
        amount,
        date: editMovementDate,
        description: editMovementDescription,
      })
      .then(() => {
        dispatch({ type: 'SAVE_MOVEMENT_SUCCESS' })
        fetchReservaData({ includeBuckets: false })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_MOVEMENT_ERROR',
          payload: { message: getErrorMessage(err, 'Failed to update movement'), fields: {} },
        })
      })
  }

  function deleteMovement(id: string) {
    dispatch({ type: 'DELETE_MOVEMENT_START', payload: id })

    void apiClient
      .deleteReserveMovement(id)
      .then(() => {
        dispatch({ type: 'DELETE_MOVEMENT_SUCCESS' })
        fetchReservaData({ includeBuckets: false })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'DELETE_MOVEMENT_ERROR',
          payload: getErrorMessage(err, 'Failed to delete movement'),
        })
      })
  }

  return {
    balances: state.balances,
    totalBalance,
    movements: state.movements,
    movementRows,
    buckets: state.buckets,
    splitPercentageWarning,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    isSplitFormOpen: state.isSplitFormOpen,
    splitDate: state.splitDate,
    splitAmount: state.splitAmount,
    splitDescription: state.splitDescription,
    isSubmittingSplit: state.isSubmittingSplit,
    splitError: state.splitError,
    splitErrorFields: state.splitErrorFields,
    lastSplitResult: state.lastSplitResult,
    showSplitForm,
    cancelSplitForm,
    setSplitField,
    submitIncomeSplit,
    dismissSplitResult,
    isWithdrawalFormOpen: state.isWithdrawalFormOpen,
    withdrawalBucketId: state.withdrawalBucketId,
    withdrawalAmount: state.withdrawalAmount,
    withdrawalDate: state.withdrawalDate,
    withdrawalDescription: state.withdrawalDescription,
    isSubmittingWithdrawal: state.isSubmittingWithdrawal,
    withdrawalError: state.withdrawalError,
    withdrawalErrorFields: state.withdrawalErrorFields,
    showWithdrawalForm,
    cancelWithdrawalForm,
    setWithdrawalField,
    submitWithdrawal,
    editingMovementId: state.editingMovementId,
    editMovementBucketId: state.editMovementBucketId,
    editMovementAmount: state.editMovementAmount,
    editMovementDate: state.editMovementDate,
    editMovementDescription: state.editMovementDescription,
    isSavingMovement: state.isSavingMovement,
    saveMovementError: state.saveMovementError,
    saveMovementErrorFields: state.saveMovementErrorFields,
    showEditMovementForm,
    cancelEditMovement,
    setEditMovementField,
    saveMovementEdit,
    deletingMovementId: state.deletingMovementId,
    deleteMovementError: state.deleteMovementError,
    deleteMovement,
  }
}
