import { useCallback, useEffect } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { apiClient } from '../api/financialApiClient'
import { ApiError } from '../api/apiError'
import type { AccountDetailDto, AccountDto, AccountTransactionDto } from '../api/types'
import { currentYearMonth, formatMonthInputValue, parseMonthInputValue } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'

export const ACCOUNT_LOAD_ERROR = 'Unable to load this account'

export type AccountScreenTab = 'transactions' | 'statements' | 'pots'

function withParam(params: URLSearchParams, key: string, value: string | null): URLSearchParams {
  const next = new URLSearchParams(params)
  if (value === null) next.delete(key)
  else next.set(key, value)
  return next
}

const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

interface AccountScreenData {
  accounts: AccountDto[]
  account: AccountDetailDto
  transactions: AccountTransactionDto[]
}

type AccountScreenLoad = AccountScreenData | 'not-found'

const NO_ACCOUNTS: AccountDto[] = []
const NO_TRANSACTIONS: AccountTransactionDto[] = []

function isPotsAccount({ accounts, account }: AccountScreenData, routeAccountId: string): boolean {
  return (
    account.id.toLowerCase() === routeAccountId.toLowerCase() &&
    accounts.some((candidate) => candidate.id === account.id && candidate.capabilities.includes('HasPots'))
  )
}

function tabFromParam(param: string | null, hasPots: boolean): AccountScreenTab {
  if (hasPots) return param === 'transactions' ? 'transactions' : 'pots'
  return param === 'statements' ? 'statements' : 'transactions'
}

export interface AccountScreen {
  accountId: string
  year: number
  month: number
  monthInputValue: string
  setMonthInputValue: (value: string) => void
  tab: AccountScreenTab
  setTab: (tab: AccountScreenTab) => void
  selectAccount: (accountId: string) => void
  accounts: AccountDto[]
  account: AccountDetailDto | null
  hasPots: boolean
  transactions: AccountTransactionDto[]
  isLoading: boolean
  isNotFound: boolean
  error: string | null
  retry: () => void
  refreshSilently: () => Promise<void>
}

function loadAccountScreen(accountId: string, year: number, month: number): Promise<AccountScreenLoad> {
  return Promise.all([
    apiClient.getAccounts(),
    apiClient.getAccount(accountId, year, month),
    apiClient.getAccountTransactions(accountId, year, month),
  ])
    .then(([accounts, account, transactions]): AccountScreenLoad => ({ accounts, account, transactions }))
    .catch((err: unknown) => {
      if (err instanceof ApiError && err.status === 404) return 'not-found'
      throw err
    })
}

export function useAccountScreen(): AccountScreen {
  const { accountId = '' } = useParams<{ accountId: string }>()
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  const parsedMonth = parseMonthInputValue(searchParams.get('month') ?? '')
  const { year, month } = parsedMonth ?? currentYearMonth()
  const monthInputValue = formatMonthInputValue(year, month)
  const isValidId = GUID_PATTERN.test(accountId)
  const hasValidMonth = parsedMonth !== null

  useEffect(() => {
    if (!hasValidMonth) setSearchParams((params) => withParam(params, 'month', monthInputValue), { replace: true })
  }, [hasValidMonth, monthInputValue, setSearchParams])

  const resource = useAsyncResource<AccountScreenLoad>(
    () => (isValidId ? loadAccountScreen(accountId, year, month) : null),
    [accountId, year, month, isValidId],
    ACCOUNT_LOAD_ERROR,
    { initialLoading: isValidId, keepDataWhileLoading: true },
  )

  const setMonthInputValue = useCallback(
    (value: string) => {
      if (parseMonthInputValue(value)) setSearchParams((params) => withParam(params, 'month', value), { replace: true })
    },
    [setSearchParams],
  )

  const loaded = resource.data === 'not-found' ? null : resource.data
  const hasPots = loaded !== null && isPotsAccount(loaded, accountId)
  const defaultTab: AccountScreenTab = hasPots ? 'pots' : 'transactions'

  const setTab = useCallback(
    (nextTab: AccountScreenTab) =>
      setSearchParams((params) => withParam(params, 'tab', nextTab === defaultTab ? null : nextTab), { replace: true }),
    [setSearchParams, defaultTab],
  )

  const selectAccount = useCallback(
    (nextAccountId: string) => navigate(`/cashflow/accounts/${nextAccountId}?month=${monthInputValue}`),
    [navigate, monthInputValue],
  )

  return {
    accountId,
    year,
    month,
    monthInputValue,
    setMonthInputValue,
    tab: tabFromParam(searchParams.get('tab'), hasPots),
    setTab,
    selectAccount,
    accounts: loaded?.accounts ?? NO_ACCOUNTS,
    account: loaded?.account ?? null,
    hasPots,
    transactions: loaded?.transactions ?? NO_TRANSACTIONS,
    isLoading: resource.isLoading,
    isNotFound: !isValidId || resource.data === 'not-found',
    error: resource.error,
    retry: resource.retry,
    refreshSilently: resource.refresh,
  }
}
