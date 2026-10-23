import { useCallback, useEffect } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { apiClient } from '../api/financialApiClient'
import { ApiError } from '../api/apiError'
import type { AccountDetailDto, AccountDto, AccountTransactionDto } from '../api/types'
import { currentYearMonth, formatMonthInputValue } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'

const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
const MONTH_PATTERN = /^(\d{4})-(0[1-9]|1[0-2])$/

interface AccountScreenData {
  accounts: AccountDto[]
  account: AccountDetailDto
  transactions: AccountTransactionDto[]
}

type AccountScreenLoad = { found: true; data: AccountScreenData } | { found: false }

export interface AccountScreen {
  year: number
  month: number
  monthInputValue: string
  setMonthInputValue: (value: string) => void
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

function parseMonthParam(value: string | null): { year: number; month: number } | null {
  const match = value ? MONTH_PATTERN.exec(value) : null
  return match ? { year: Number(match[1]), month: Number(match[2]) } : null
}

function loadAccountScreen(accountId: string, year: number, month: number): Promise<AccountScreenLoad> {
  return Promise.all([
    apiClient.getAccounts(),
    apiClient.getAccount(accountId, year, month),
    apiClient.getAccountTransactions(accountId, year, month),
  ])
    .then(([accounts, account, transactions]): AccountScreenLoad => ({ found: true, data: { accounts, account, transactions } }))
    .catch((err: unknown) => {
      if (err instanceof ApiError && err.status === 404) return { found: false }
      throw err
    })
}

export function useAccountScreen(): AccountScreen {
  const { accountId = '' } = useParams<{ accountId: string }>()
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  const monthParam = searchParams.get('month')
  const parsedMonth = parseMonthParam(monthParam)
  const { year, month } = parsedMonth ?? currentYearMonth()
  const monthInputValue = formatMonthInputValue(year, month)
  const isValidId = GUID_PATTERN.test(accountId)
  const hasValidMonth = parsedMonth !== null

  useEffect(() => {
    if (!hasValidMonth) setSearchParams({ month: monthInputValue }, { replace: true })
  }, [hasValidMonth, monthInputValue, setSearchParams])

  const resource = useAsyncResource<AccountScreenLoad>(
    () => (isValidId ? loadAccountScreen(accountId, year, month) : null),
    [accountId, year, month, isValidId],
    'Unable to load this account',
    { initialLoading: isValidId, keepDataWhileLoading: true },
  )

  const setMonthInputValue = useCallback(
    (value: string) => {
      if (parseMonthParam(value)) setSearchParams({ month: value }, { replace: true })
    },
    [setSearchParams],
  )

  const selectAccount = useCallback(
    (nextAccountId: string) => navigate(`/cashflow/accounts/${nextAccountId}?month=${monthInputValue}`),
    [navigate, monthInputValue],
  )

  const loaded = resource.data?.found ? resource.data.data : null

  return {
    year,
    month,
    monthInputValue,
    setMonthInputValue,
    selectAccount,
    accounts: loaded?.accounts ?? [],
    account: loaded?.account ?? null,
    transactions: loaded?.transactions ?? [],
    isLoading: resource.isLoading,
    isNotFound: !isValidId || resource.data?.found === false,
    error: resource.error,
    retry: resource.retry,
    refreshSilently: resource.refresh,
  }
}
