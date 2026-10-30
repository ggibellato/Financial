import type { ReactNode } from 'react'
import { Badge, Text } from '@fluentui/react-components'
import { Link } from 'react-router-dom'
import type { AccountGroupMemberDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import TotalsGrid from '../components/TotalsGrid'
import { ACCOUNTS_LOAD_ERROR, useAccountTemplateGroups } from '../hooks/useAccountTemplateGroups'
import { type AccountGridColumns, accountTemplateGrid } from '../utils/accountTemplateLabel'
import { currentYearMonth, formatLongMonthYear, formatMonthInputValue, formatMonthYear, formatN2, formatN2OrDash } from '../utils/formatters'
import { formatLabelledBalance } from '../utils/owedBalance'
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

function nextStatementMonth({ nextStatement }: AccountGroupMemberDto): Date | null {
  return nextStatement ? new Date(nextStatement.year, nextStatement.month - 1, 1) : null
}

const nextStatementColumn = {
  key: 'nextStatement',
  header: 'Next statement',
  render: (row: AccountGroupMemberDto) => {
    const month = nextStatementMonth(row)
    return month && row.nextStatement ? `${formatMonthYear(month)} · ${formatN2(row.nextStatement.outstandingTotal)}` : '—'
  },
  sortAccessor: nextStatementMonth,
}

const totalOwedColumn = {
  key: 'totalOwed',
  header: 'Total owed',
  numeric: true,
  render: (row: AccountGroupMemberDto) => formatN2OrDash(row.totalOwed),
  sortAccessor: (row: AccountGroupMemberDto) => row.totalOwed,
}

function gridColumns(columns: AccountGridColumns, renderName: (row: AccountGroupMemberDto) => ReactNode) {
  const leading = [nameColumn(renderName), currencyColumn]
  return columns === 'card' ? [...leading, nextStatementColumn, totalOwedColumn] : [...leading, balanceColumn]
}

export default function AccountsPage() {
  const { year, month } = currentYearMonth()
  const monthParam = formatMonthInputValue(year, month)
  const { data: groups, isLoading, error, retry } = useAccountTemplateGroups(year, month)

  return (
    <div className="accounts-page">
      <Text>Balances at end of {formatLongMonthYear(year, month)}</Text>

      {isLoading ? (
        <LoadingState />
      ) : error || !groups ? (
        <ErrorState message={ACCOUNTS_LOAD_ERROR} onRetry={retry} />
      ) : groups.length === 0 ? (
        <Text>No accounts this month</Text>
      ) : (
        groups.map((group) => {
          const grid = accountTemplateGrid(group.templateName)
          const renderName = grid.linksRows
            ? (row: AccountGroupMemberDto) => <Link to={`/cashflow/accounts/${row.accountId}?month=${monthParam}`}>{row.name}</Link>
            : (row: AccountGroupMemberDto) => row.name
          return (
            <div key={group.templateName} className="accounts-page__section">
              <Text as="h2" weight="semibold" size={400}>
                {grid.heading}
              </Text>
              <TotalsGrid
                label={grid.heading}
                columns={gridColumns(grid.columns, renderName)}
                rows={group.members}
                rowKey={(row) => row.accountId}
                emptyMessage="No accounts this month"
                footerItems={group.totals.map((total) => ({ label: `${grid.footerLabel} ${total.currency}`, value: formatN2(total.total) }))}
              />
            </div>
          )
        })
      )}
    </div>
  )
}
