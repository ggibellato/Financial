import type { ReactNode } from 'react'
import { Badge, Field, Input, MessageBar, MessageBarBody, Tab, Text } from '@fluentui/react-components'
import { Link, useLocation } from 'react-router-dom'
import type { AccountGroupMemberDto, MonthTransactionDto } from '../api/types'
import AccountsSummaryTab from '../components/AccountsSummaryTab'
import ErrorState from '../components/ErrorState'
import IncomeTab from '../components/IncomeTab'
import LoadingState from '../components/LoadingState'
import MonthTransactionsGrid from '../components/MonthTransactionsGrid'
import PageTabList from '../components/PageTabList'
import TitheTab from '../components/TitheTab'
import TotalsGrid from '../components/TotalsGrid'
import { ACCOUNTS_LOAD_ERROR, useAccountTemplateGroups } from '../hooks/useAccountTemplateGroups'
import { useMonthTransactions } from '../hooks/useMonthTransactions'
import { useOverviewAddress } from '../hooks/useOverviewAddress'
import { useTabPanel } from '../hooks/useTabPanel'
import { type AccountGridColumns, accountTemplateGrid } from '../utils/accountTemplateLabel'
import { formatLongMonthYear, formatMonthInputValue, formatMonthYear, formatN2 } from '../utils/formatters'
import { orMissing } from '../utils/orMissing'
import { formatLabelledBalance, formatOwedOrCredit } from '../utils/owedBalance'
import { settlementBalanceText, settlementCounterparty } from '../utils/settlementBalance'
import MissingValue from '../components/MissingValue'
import './AccountsOverview.css'
import './AccountsPage.css'

function nameColumn(renderName: (row: AccountGroupMemberDto) => ReactNode) {
  return {
    key: 'name',
    header: 'Account',
    render: (row: AccountGroupMemberDto) => (
      <span className="accounts-page__name">
        {renderName(row)}
        {!row.isActive && (
          <Badge appearance="outline" color="informative">
            Inactive
          </Badge>
        )}
      </span>
    ),
    sortAccessor: (row: AccountGroupMemberDto) => row.name,
  }
}

const currencyColumn = {
  key: 'currency',
  header: 'Currency',
  render: (row: AccountGroupMemberDto) => row.currency,
  sortAccessor: (row: AccountGroupMemberDto) => row.currency,
}

const balanceColumn = {
  key: 'balance',
  header: 'Balance',
  numeric: true,
  render: (row: AccountGroupMemberDto) => formatLabelledBalance(row.balance, row.balanceIsOwed),
  sortAccessor: (row: AccountGroupMemberDto) => row.balance,
}

const roundUpOf = (row: AccountGroupMemberDto) => row.roundUpTotal ?? 0

const roundUpColumn = {
  key: 'roundUp',
  header: 'Round-up',
  numeric: true,
  render: (row: AccountGroupMemberDto) => formatN2(roundUpOf(row)),
  sortAccessor: roundUpOf,
}

function nextStatementMonth({ nextStatement }: AccountGroupMemberDto): Date | null {
  return nextStatement ? new Date(nextStatement.year, nextStatement.month - 1, 1) : null
}

const nextStatementColumn = {
  key: 'nextStatement',
  header: 'Next statement',
  render: (row: AccountGroupMemberDto) => {
    const month = nextStatementMonth(row)
    return month && row.nextStatement ? `${formatMonthYear(month)} · ${formatN2(row.nextStatement.outstandingTotal)}` : <MissingValue label="None" />
  },
  sortAccessor: nextStatementMonth,
}

const totalOwedColumn = {
  key: 'totalOwed',
  header: 'Total owed',
  numeric: true,
  render: (row: AccountGroupMemberDto) => orMissing(row.totalOwed, formatOwedOrCredit),
  sortAccessor: (row: AccountGroupMemberDto) => row.totalOwed,
}

const settlementBalanceColumn = {
  key: 'balance',
  header: 'Balance',
  render: (row: AccountGroupMemberDto) => settlementBalanceText(settlementCounterparty(row), row.currencyBalances),
  sortAccessor: (row: AccountGroupMemberDto) => row.balance,
}

function gridColumns(templateName: string, columns: AccountGridColumns, renderName: (row: AccountGroupMemberDto) => ReactNode) {
  if (columns === 'settlement') return [nameColumn(renderName), settlementBalanceColumn]
  const leading = [nameColumn(renderName), currencyColumn]
  if (columns === 'card') return [...leading, nextStatementColumn, totalOwedColumn]
  return templateName === 'Bank' ? [...leading, balanceColumn, roundUpColumn] : [...leading, balanceColumn]
}

function AccountsTab({ year, month }: { year: number; month: number }) {
  const { data: groups, isLoading, error, retry } = useAccountTemplateGroups(year, month)
  const monthInputValue = formatMonthInputValue(year, month)

  return (
    <>
      <Text>Balances at end of {formatLongMonthYear(year, month)}</Text>
      <div className="accounts-page__groups">
        {isLoading ? (
          <LoadingState />
        ) : error || !groups ? (
          <ErrorState message={ACCOUNTS_LOAD_ERROR} onRetry={retry} />
        ) : groups.length === 0 ? (
          <Text>No accounts this month.</Text>
        ) : (
          groups.map((group) => {
            const grid = accountTemplateGrid(group.templateName)
            const formatFooterTotal = grid.columns === 'card' ? formatOwedOrCredit : formatN2
            const tabParam = grid.rowLinkTab ? `&tab=${grid.rowLinkTab}` : ''
            const renderName = grid.linksRows
              ? (row: AccountGroupMemberDto) => <Link to={`/cashflow/accounts/${row.accountId}?month=${monthInputValue}${tabParam}`}>{row.name}</Link>
              : (row: AccountGroupMemberDto) => row.name
            return (
              <div key={group.templateName} className="accounts-page__section">
                <Text as="h2" weight="semibold" size={400}>
                  {grid.heading}
                </Text>
                <TotalsGrid
                  label={grid.heading}
                  columns={gridColumns(group.templateName, grid.columns, renderName)}
                  rows={group.members}
                  rowKey={(row) => row.accountId}
                  emptyMessage="No accounts this month."
                  footerItems={grid.footerLabel ? group.totals.map((total) => ({ label: `${grid.footerLabel} ${total.currency}`, value: formatFooterTotal(total.total) })) : []}
                />
              </div>
            )
          })
        )}
      </div>
    </>
  )
}

const NO_ROWS: MonthTransactionDto[] = []

function AllTransactionsTab({ year, month }: { year: number; month: number }) {
  const { data, isLoading, error, retry, refresh } = useMonthTransactions(year, month)
  return (
    <MonthTransactionsGrid
      year={year}
      month={month}
      rows={data ?? NO_ROWS}
      isLoading={isLoading}
      error={error}
      onRetry={retry}
      onDeleted={() => void refresh()}
    />
  )
}

const OVERVIEW_TABS = [
  { id: 'accounts', label: 'Accounts', isGrid: false },
  { id: 'summary', label: 'Summary', isGrid: false },
  { id: 'income', label: 'Income', isGrid: true },
  { id: 'tithe', label: 'Tithe', isGrid: false },
  { id: 'transactions', label: 'All transactions', isGrid: true },
] as const

type OverviewTabId = (typeof OVERVIEW_TABS)[number]['id']

const OVERVIEW_TAB_IDS = OVERVIEW_TABS.map((tab) => tab.id) as [OverviewTabId, ...OverviewTabId[]]

export default function AccountsPage() {
  const { year, month, monthInputValue, setMonthInputValue, tab, setTab } = useOverviewAddress(OVERVIEW_TAB_IDS)
  const tabPanel = useTabPanel(tab)
  const { state } = useLocation()
  const showMovedNotice = (state as { movedNotice?: boolean } | null)?.movedNotice === true
  const monthKey = `${year}-${month}`
  const isGridTab = OVERVIEW_TABS.some(({ id, isGrid }) => id === tab && isGrid)

  return (
    <div className="accounts-page">
      <Text as="h1" size={600} weight="semibold">
        Accounts
      </Text>
      {showMovedNotice && (
        <MessageBar role="status" intent="info">
          <MessageBarBody>That page moved to the account screen.</MessageBarBody>
        </MessageBar>
      )}
      <div className="monthly-page__header">
        <Field label="Month">
          <Input type="month" value={monthInputValue} onChange={(_event, data) => setMonthInputValue(data.value)} />
        </Field>
      </div>

      <PageTabList aria-label="Accounts views" selectedValue={tab} onTabSelect={(_event, data) => setTab(data.value as OverviewTabId)}>
        {OVERVIEW_TABS.map(({ id, label }) => (
          <Tab key={id} value={id} {...tabPanel.tabProps(id)}>
            {label}
          </Tab>
        ))}
      </PageTabList>

      <div className={isGridTab ? 'monthly-page__panel' : 'accounts-page__groups'} {...tabPanel.panelProps}>
        {tab === 'accounts' && <AccountsTab key={monthKey} year={year} month={month} />}
        {tab === 'summary' && <AccountsSummaryTab key={monthKey} year={year} month={month} />}
        {tab === 'income' && (
          <div className="monthly-page__content">
            <IncomeTab key={monthKey} year={year} month={month} />
          </div>
        )}
        {tab === 'tithe' && <TitheTab key={year} year={year} month={month} onSelectMonth={setMonthInputValue} />}
        {tab === 'transactions' && (
          <div className="monthly-page__content">
            <AllTransactionsTab key={monthKey} year={year} month={month} />
          </div>
        )}
      </div>
    </div>
  )
}
