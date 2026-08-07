import { useEffect, useMemo, useReducer, useState } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { BalanceAdjustmentDto, BankDto, TransferDto } from '../api/types'

/** Sentinel bank-filter value meaning "show every bank's operations, unfiltered". */
export const ALL_BANKS_FILTER = 'All Banks'

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

function matchesBankFilter(entry: BankOperationEntry, bankFilter: string): boolean {
  if (bankFilter === ALL_BANKS_FILTER) return true

  return entry.kind === 'transfer'
    ? entry.sourceBank === bankFilter || entry.destinationBank === bankFilter
    : entry.bank === bankFilter
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
  bankFilter: string
  setBankFilter: (bankFilter: string) => void
  isLoading: boolean
  error: string | null
  retry: () => void
  deleteTransfer: (id: string) => void
  deleteAdjustment: (bankId: string, id: string) => void
}

/**
 * Fetches the month's transfers (all banks) and each bank's adjustments (scoped to the
 * month client-side), combines them into one flat, newest-first list, and exposes a
 * client-side bank filter plus delete actions. Replaces the per-bank grouped useBankHistory.
 */
export function useBankOperations(
  year: number,
  month: number,
  banks: BankDto[],
  onChanged: () => void,
): UseBankOperationsResult {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)
  const [bankFilter, setBankFilter] = useState<string>(ALL_BANKS_FILTER)

  const bankNames = banks.map((bank) => bank.name).join(',')

  useEffect(() => {
    // banks arrives asynchronously from the caller's own fetch (useMonthly) and starts as
    // an empty array; skip fetching until it's actually populated so this hook doesn't run
    // a throwaway zero-bank fetch and then immediately refetch once banks land.
    if (banks.length === 0) return

    dispatch({ type: 'FETCH_START' })

    Promise.all([
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
          payload: err instanceof Error ? err.message : 'Unable to load bank operations',
        })
      })
    // banks is derived every render by the caller; bankNames is a stable key for its contents.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [apiClient, year, month, bankNames, state.retryCount])

  const retry = () => dispatch({ type: 'RETRY' })

  const deleteTransfer = (id: string) => {
    if (!window.confirm('Delete this transfer?')) return

    void apiClient
      .deleteTransfer(id)
      .then(() => {
        retry()
        onChanged()
      })
      .catch((err: unknown) => {
        dispatch({ type: 'ACTION_ERROR', payload: err instanceof Error ? err.message : 'Failed to delete transfer' })
      })
  }

  const deleteAdjustment = (bankId: string, id: string) => {
    if (!window.confirm('Delete this balance adjustment?')) return

    void apiClient
      .deleteBalanceAdjustment(bankId, id)
      .then(() => {
        retry()
        onChanged()
      })
      .catch((err: unknown) => {
        dispatch({
          type: 'ACTION_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to delete balance adjustment',
        })
      })
  }

  const operations = state.operations.filter((entry) => matchesBankFilter(entry, bankFilter))

  return {
    operations,
    bankFilter,
    setBankFilter,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    deleteTransfer,
    deleteAdjustment,
  }
}
