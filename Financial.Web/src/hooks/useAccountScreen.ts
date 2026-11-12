import { useCallback, useEffect } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { apiClient } from '../api/financialApiClient'
import { isNotFound } from '../api/apiError'
import type { AccountDetailDto, AccountDto, AccountTransactionDto } from '../api/types'
import { currentYearMonth, formatMonthInputValue, parseMonthInputValue } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'

export const ACCOUNT_LOAD_ERROR = 'Unable to load this account.'

export type AccountScreenTab = 'transactions' | 'statements' | 'pots'

export interface ScreenTab {
  value: AccountScreenTab
  label: string
}

const CARD_TABS: ScreenTab[] = [
  { value: 'transactions', label: 'Transactions' },
  { value: 'statements', label: 'Statements' },
]

const POTS_TABS: ScreenTab[] = [
  { value: 'pots', label: 'Pots' },
  { value: 'transactions', label: 'Transactions' },
]

const NO_TABS: ScreenTab[] = []

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

function loadedAccountHas({ accounts, account }: AccountScreenData, capability: string): boolean {
  return accounts.some((candidate) => candidate.id === account.id && candidate.capabilities.includes(capability))
}

function isPotsAccount(data: AccountScreenData, routeAccountId: string): boolean {
  return data.account.id.toLowerCase() === routeAccountId.toLowerCase() && loadedAccountHas(data, 'HasPots')
}

function tabsOf(data: AccountScreenData | null, hasPots: boolean): ScreenTab[] {
  if (hasPots) return POTS_TABS
  return data?.account.hasStatements ? CARD_TABS : NO_TABS
}

export interface AccountScreen {
  accountId: string
  year: number
  month: number
  monthInputValue: string
  setMonthInputValue: (value: string) => void
  tabs: ScreenTab[]
  tab: AccountScreenTab
  setTab: (tab: AccountScreenTab) => void
  operationId: string | null
  clearOperation: () => void
  editedRowKey: string | null
  clearEditedRow: () => void
  selectAccount: (accountId: string) => void
  accounts: AccountDto[]
  account: AccountDetailDto | null
  hasPots: boolean
  hasParticipants: boolean
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
      if (isNotFound(err)) return 'not-found'
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
  const tabs = tabsOf(loaded, hasPots)
  const defaultTab: AccountScreenTab = tabs[0]?.value ?? 'transactions'
  const tabParam = searchParams.get('tab')

  const setTab = (nextTab: AccountScreenTab) =>
    setSearchParams((params) => withParam(params, 'tab', nextTab === defaultTab ? null : nextTab), { replace: true })

  const clearOperation = useCallback(
    () => setSearchParams((params) => withParam(params, 'operation', null), { replace: true }),
    [setSearchParams],
  )

  const clearEditedRow = useCallback(
    () => setSearchParams((params) => withParam(params, 'edit', null), { replace: true }),
    [setSearchParams],
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
    tabs,
    tab: tabs.find((candidate) => candidate.value === tabParam)?.value ?? defaultTab,
    setTab,
    operationId: searchParams.get('operation'),
    clearOperation,
    editedRowKey: searchParams.get('edit'),
    clearEditedRow,
    selectAccount,
    accounts: loaded?.accounts ?? NO_ACCOUNTS,
    account: loaded?.account ?? null,
    hasPots,
    hasParticipants: loaded !== null && loadedAccountHas(loaded, 'HasParticipants'),
    transactions: loaded?.transactions ?? NO_TRANSACTIONS,
    isLoading: resource.isLoading,
    isNotFound: !isValidId || resource.data === 'not-found',
    error: resource.error,
    retry: resource.retry,
    refreshSilently: resource.refresh,
  }
}
