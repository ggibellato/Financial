import { useCallback, useEffect } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { apiClient } from '../api/financialApiClient'
import { ApiError } from '../api/apiError'
import type { AccountDetailDto, AccountDto, AccountTransactionDto } from '../api/types'
import { currentYearMonth, formatMonthInputValue, parseMonthInputValue } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'

export const ACCOUNT_LOAD_ERROR = 'Unable to load this account'

export type AccountScreenTab = 'transactions' | 'statements'

function searchFor(month: string, tab: AccountScreenTab): Record<string, string> {
  return tab === 'statements' ? { month, tab } : { month }
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
  const tab: AccountScreenTab = searchParams.get('tab') === 'statements' ? 'statements' : 'transactions'
  const isValidId = GUID_PATTERN.test(accountId)
  const hasValidMonth = parsedMonth !== null

  useEffect(() => {
    if (!hasValidMonth) setSearchParams(searchFor(monthInputValue, tab), { replace: true })
  }, [hasValidMonth, monthInputValue, tab, setSearchParams])

  const resource = useAsyncResource<AccountScreenLoad>(
    () => (isValidId ? loadAccountScreen(accountId, year, month) : null),
    [accountId, year, month, isValidId],
    ACCOUNT_LOAD_ERROR,
    { initialLoading: isValidId, keepDataWhileLoading: true },
  )

  const setMonthInputValue = useCallback(
    (value: string) => {
      if (parseMonthInputValue(value)) setSearchParams(searchFor(value, tab), { replace: true })
    },
    [setSearchParams, tab],
  )

  const setTab = useCallback(
    (nextTab: AccountScreenTab) => setSearchParams(searchFor(monthInputValue, nextTab), { replace: true }),
    [monthInputValue, setSearchParams],
  )

  const selectAccount = useCallback(
    (nextAccountId: string) => navigate(`/cashflow/accounts/${nextAccountId}?month=${monthInputValue}`),
    [navigate, monthInputValue],
  )

  const loaded = resource.data === 'not-found' ? null : resource.data

  return {
    accountId,
    year,
    month,
    monthInputValue,
    setMonthInputValue,
    tab,
    setTab,
    selectAccount,
    accounts: loaded?.accounts ?? NO_ACCOUNTS,
    account: loaded?.account ?? null,
    transactions: loaded?.transactions ?? NO_TRANSACTIONS,
    isLoading: resource.isLoading,
    isNotFound: !isValidId || resource.data === 'not-found',
    error: resource.error,
    retry: resource.retry,
    refreshSilently: resource.refresh,
  }
}
