import { useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { BalanceAdjustmentDto, BankDto, TransferDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

export type BankOperationEntry =
  | {
      kind: 'transfer'
      id: string
      date: string
      sourceBank: string
      destinationBank: string
      amount: number
      note: string | null
      transfer: TransferDto
    }
  | {
      kind: 'adjustment'
      id: string
      date: string
      bank: string
      bankId: string
      delta: number
      note: string | null
      adjustment: BalanceAdjustmentDto
    }

function isInMonth(dateIso: string, year: number, month: number): boolean {
  const [y, m] = dateIso.split('-').map(Number)
  return y === year && m === month
}

function buildOperations(
  transfers: TransferDto[],
  adjustments: BalanceAdjustmentDto[],
): BankOperationEntry[] {
  const transferEntries: BankOperationEntry[] = transfers.map((transfer) => ({
    kind: 'transfer',
    id: transfer.id,
    date: transfer.date,
    sourceBank: transfer.sourceBankName,
    destinationBank: transfer.destinationBankName,
    amount: transfer.amount,
    note: transfer.note,
    transfer,
  }))

  const adjustmentEntries: BankOperationEntry[] = adjustments.map((adjustment) => ({
    kind: 'adjustment',
    id: adjustment.id,
    date: adjustment.date,
    bank: adjustment.bankName,
    bankId: adjustment.bankId,
    delta: adjustment.delta,
    note: adjustment.note,
    adjustment,
  }))

  return [...transferEntries, ...adjustmentEntries].sort((a, b) => b.date.localeCompare(a.date))
}

interface BankOperationsState {
  operations: BankOperationEntry[]
  isLoading: boolean
  error: string | null
  retryCount: number
}

type BankOperationsAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: BankOperationEntry[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'ACTION_ERROR'; payload: string }

const INITIAL_STATE: BankOperationsState = {
  operations: [],
  isLoading: true,
  error: null,
  retryCount: 0,
}

function reducer(state: BankOperationsState, action: BankOperationsAction): BankOperationsState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, operations: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'ACTION_ERROR':
      return { ...state, error: action.payload }
    default:
      return state
  }
}

export interface UseBankOperationsResult {
  operations: BankOperationEntry[]
  isLoading: boolean
  error: string | null
  retry: () => void
  refreshSilently: () => void
  deleteTransfer: (id: string) => void
  deleteAdjustment: (bankId: string, id: string) => void
}

export function useBankOperations(
  year: number,
  month: number,
  banks: BankDto[],
  onChanged: () => void,
): UseBankOperationsResult {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const bankNames = banks.map((bank) => bank.name).join(',')

  const fetchOperations = () => {
    if (banks.length === 0) return Promise.resolve()

    return Promise.all([
      apiClient.getTransfersByMonth(year, month),
      Promise.all(banks.map((bank) => apiClient.getAdjustmentsByBank(bank.id))),
    ])
      .then(([transfers, adjustmentsPerBank]) => {
        const adjustments = adjustmentsPerBank
          .flat()
          .filter((adjustment) => isInMonth(adjustment.date, year, month))
        dispatch({ type: 'FETCH_SUCCESS', payload: buildOperations(transfers, adjustments) })
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'FETCH_ERROR',
          payload: getErrorMessage(err, 'Unable to load bank operations'),
        })
      })
  }

  useEffect(() => {
    // banks arrives asynchronously from the caller's own fetch (useMonthly) and starts as
    // an empty array; skip fetching until it's actually populated so this hook doesn't run
    // a throwaway zero-bank fetch and then immediately refetch once banks land.
    if (banks.length === 0) return

    dispatch({ type: 'FETCH_START' })
    void fetchOperations()
    // banks is derived every render by the caller; bankNames is a stable key for its contents.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [apiClient, year, month, bankNames, state.retryCount])

  const retry = () => dispatch({ type: 'RETRY' })

  // Re-fetches after a mutation without flipping isLoading, so BankOperationsSection's own
  // sortable/filterable grid state (owned by that component) survives the refresh.
  const refreshSilently = () => fetchOperations()

  const deleteTransfer = (id: string) => {
    void apiClient
      .deleteTransfer(id)
      .then(() => {
        void refreshSilently()
        onChanged()
      })
      .catch((err: unknown) => {
        dispatch({ type: 'ACTION_ERROR', payload: getErrorMessage(err, 'Failed to delete transfer') })
      })
  }

  const deleteAdjustment = (bankId: string, id: string) => {
    void apiClient
      .deleteBalanceAdjustment(bankId, id)
      .then(() => {
        void refreshSilently()
        onChanged()
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'ACTION_ERROR',
          payload: getErrorMessage(err, 'Failed to delete balance adjustment'),
        })
      })
  }

  return {
    operations: state.operations,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    refreshSilently,
    deleteTransfer,
    deleteAdjustment,
  }
}
