import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type {
  AccountGroupMemberDto,
  AccountGroupSummaryDto,
  CategoryTotalDto,
  CurrencyTotalDto,
  IncomeSourceTotalDto,
  TitheCarryForwardUpdateDto,
  TitheSummaryDto,
} from '../api/types'
import { currentYearMonth, getErrorMessage } from '../utils/formatters'
import { useMonthSelection } from './useMonthSelection'

export interface BankTotal {
  bankId: string
  bank: string
  currency: string
  balance: number
  roundUpTotal: number
}

interface MonthlyState {
  categoryTotals: CategoryTotalDto[]
  summary: AccountGroupSummaryDto | null
  titheSummary: TitheSummaryDto | null
  carryForwardUpdating: boolean
  carryForwardError: string | null
  isLoading: boolean
  error: string | null
  retryCount: number
}

type MonthlyAction =
  | { type: 'FETCH_START' }
  | {
      type: 'FETCH_SUCCESS'
      payload: {
        categoryTotals: CategoryTotalDto[]
        summary: AccountGroupSummaryDto
        titheSummary: TitheSummaryDto
      }
    }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'CARRY_FORWARD_UPDATE_START' }
  | { type: 'CARRY_FORWARD_UPDATE_END' }
  | { type: 'CARRY_FORWARD_UPDATE_ERROR'; payload: string }

const INITIAL_STATE: MonthlyState = {
  categoryTotals: [],
  summary: null,
  titheSummary: null,
  carryForwardUpdating: false,
  carryForwardError: null,
  isLoading: true,
  error: null,
  retryCount: 0,
}

function reducer(state: MonthlyState, action: MonthlyAction): MonthlyState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null, carryForwardError: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, ...action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'CARRY_FORWARD_UPDATE_START':
      return { ...state, carryForwardUpdating: true, carryForwardError: null }
    case 'CARRY_FORWARD_UPDATE_END':
      return { ...state, carryForwardUpdating: false }
    case 'CARRY_FORWARD_UPDATE_ERROR':
      return { ...state, carryForwardUpdating: false, carryForwardError: action.payload }
    default:
      return state
  }
}

export interface MonthlyData {
  year: number
  month: number
  monthInputValue: string
  setMonthInputValue: (value: string) => void
  categoryTotals: CategoryTotalDto[]
  categoryTotalsSum: number
  bankTotals: BankTotal[]
  cardMembers: AccountGroupMemberDto[]
  currencyTotals: CurrencyTotalDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  incomeTotals: IncomeSourceTotalDto[]
  totalIncoming: number
  titheSummary: TitheSummaryDto | null
  carryForwardUpdating: boolean
  updateCarryForwardInclusion: (included: boolean) => void
  carryForwardError: string | null
}

export function useMonthly(): MonthlyData {
  const { year, month, monthInputValue, setMonthInputValue } = useMonthSelection(currentYearMonth)
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const fetchMonthlyData = useCallback((year: number, month: number) => {
    return Promise.all([
      apiClient.getCategoryTotalsByMonth(year, month),
      apiClient.getDefaultGroupSummary(year, month),
      apiClient.getTitheSummaryByMonth(year, month),
    ])
      .then(([categoryTotals, summary, titheSummary]) =>
        dispatch({ type: 'FETCH_SUCCESS', payload: { categoryTotals, summary, titheSummary } }),
      )
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load Monthly data') })
      })
  }, [])

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void fetchMonthlyData(year, month)
  }, [fetchMonthlyData, year, month, state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const updateCarryForwardInclusion = useCallback(
    (included: boolean) => {
      dispatch({ type: 'CARRY_FORWARD_UPDATE_START' })
      const requestBody: TitheCarryForwardUpdateDto = { included }
      void apiClient
        .updateTitheCarryForward(year, month, requestBody)
        .then(() => {
          dispatch({ type: 'CARRY_FORWARD_UPDATE_END' })
          void fetchMonthlyData(year, month)
        })
        .catch((err: unknown) => {
          dispatch({ type: 'CARRY_FORWARD_UPDATE_ERROR', payload: getErrorMessage(err, 'Failed to update carry-forward') })
        })
    },
    [year, month, fetchMonthlyData],
  )

  const members = state.summary?.members ?? []

  const bankTotals: BankTotal[] = members
    .filter((member) => !member.statement)
    .map((member) => ({
      bankId: member.accountId,
      bank: member.name,
      currency: member.currency,
      balance: member.balance,
      roundUpTotal: member.roundUpTotal ?? 0,
    }))

  return {
    year,
    month,
    monthInputValue,
    setMonthInputValue,
    categoryTotals: state.categoryTotals,
    categoryTotalsSum: state.summary?.categoryTotalsSum ?? 0,
    bankTotals,
    cardMembers: members.filter((member) => member.statement),
    currencyTotals: state.summary?.totals ?? [],
    isLoading: state.isLoading,
    error: state.error,
    retry,
    incomeTotals: state.summary?.incomeTotals ?? [],
    totalIncoming: state.summary?.totalIncoming ?? 0,
    titheSummary: state.titheSummary,
    carryForwardUpdating: state.carryForwardUpdating,
    updateCarryForwardInclusion,
    carryForwardError: state.carryForwardError,
  }
}
