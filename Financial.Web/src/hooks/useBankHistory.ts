import { useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { BalanceAdjustmentDto, BankDto, TransferDto } from '../api/types'

export type BankHistoryEntry =
  | {
      kind: 'transferIn' | 'transferOut'
      id: string
      date: string
      counterpartBank: string
      amount: number
      note: string | null
      transfer: TransferDto
    }
  | {
      kind: 'adjustment'
      id: string
      date: string
      delta: number
      note: string | null
      adjustment: BalanceAdjustmentDto
    }

function isInMonth(dateIso: string, year: number, month: number): boolean {
  const [y, m] = dateIso.split('-').map(Number)
  return y === year && m === month
}

function buildHistoryByBank(
  banks: BankDto[],
  transfers: TransferDto[],
  adjustmentsByBank: Record<string, BalanceAdjustmentDto[]>,
  year: number,
  month: number,
): Record<string, BankHistoryEntry[]> {
  const result: Record<string, BankHistoryEntry[]> = {}

  for (const bank of banks) {
    const entries: BankHistoryEntry[] = []

    for (const transfer of transfers) {
      if (transfer.sourceBank === bank.name) {
        entries.push({
          kind: 'transferOut',
          id: transfer.id,
          date: transfer.date,
          counterpartBank: transfer.destinationBank,
          amount: transfer.amount,
          note: transfer.note,
          transfer,
        })
      } else if (transfer.destinationBank === bank.name) {
        entries.push({
          kind: 'transferIn',
          id: transfer.id,
          date: transfer.date,
          counterpartBank: transfer.sourceBank,
          amount: transfer.amount,
          note: transfer.note,
          transfer,
        })
      }
    }

    for (const adjustment of adjustmentsByBank[bank.name] ?? []) {
      if (isInMonth(adjustment.date, year, month)) {
        entries.push({
          kind: 'adjustment',
          id: adjustment.id,
          date: adjustment.date,
          delta: adjustment.delta,
          note: adjustment.note,
          adjustment,
        })
      }
    }

    entries.sort((a, b) => b.date.localeCompare(a.date))
    result[bank.name] = entries
  }

  return result
}

interface BankHistoryState {
  historyByBank: Record<string, BankHistoryEntry[]>
  isLoading: boolean
  error: string | null
  retryCount: number
}

type BankHistoryAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: Record<string, BankHistoryEntry[]> }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'ACTION_ERROR'; payload: string }

const INITIAL_STATE: BankHistoryState = {
  historyByBank: {},
  isLoading: true,
  error: null,
  retryCount: 0,
}

function reducer(state: BankHistoryState, action: BankHistoryAction): BankHistoryState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, historyByBank: action.payload }
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

export interface UseBankHistoryResult {
  historyByBank: Record<string, BankHistoryEntry[]>
  isLoading: boolean
  error: string | null
  retry: () => void
  deleteTransfer: (id: string) => void
  deleteAdjustment: (bankName: string, id: string) => void
}

/** Fetches, combines, and deletes a month's transfer/adjustment history per bank. */
export function useBankHistory(
  year: number,
  month: number,
  banks: BankDto[],
  onChanged: () => void,
): UseBankHistoryResult {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const bankNames = banks.map((bank) => bank.name).join(',')

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })

    Promise.all([
      apiClient.getTransfersByMonth(year, month),
      Promise.all(banks.map((bank) => apiClient.getAdjustmentsByBank(bank.name))),
    ])
      .then(([transfers, adjustmentsPerBank]) => {
        const adjustmentsByBank: Record<string, BalanceAdjustmentDto[]> = {}
        banks.forEach((bank, index) => {
          adjustmentsByBank[bank.name] = adjustmentsPerBank[index]
        })
        dispatch({
          type: 'FETCH_SUCCESS',
          payload: buildHistoryByBank(banks, transfers, adjustmentsByBank, year, month),
        })
      })
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: err instanceof Error ? err.message : 'Unable to load bank history' })
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

  const deleteAdjustment = (bankName: string, id: string) => {
    if (!window.confirm('Delete this balance adjustment?')) return

    void apiClient
      .deleteBalanceAdjustment(bankName, id)
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

  return {
    historyByBank: state.historyByBank,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    deleteTransfer,
    deleteAdjustment,
  }
}
