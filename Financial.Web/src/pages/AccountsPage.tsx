import type { ReactNode } from 'react'
import { Badge, Field, Input, MessageBar, MessageBarBody, Text } from '@fluentui/react-components'
import { Link, useLocation } from 'react-router-dom'
import type { AccountGroupMemberDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import TotalsGrid from '../components/TotalsGrid'
import { ACCOUNTS_LOAD_ERROR, useAccountTemplateGroups } from '../hooks/useAccountTemplateGroups'
import { useOverviewAddress } from '../hooks/useOverviewAddress'
import { type AccountGridColumns, accountTemplateGrid } from '../utils/accountTemplateLabel'
import { formatLongMonthYear, formatMonthYear, formatN2 } from '../utils/formatters'
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

function gridColumns(columns: AccountGridColumns, renderName: (row: AccountGroupMemberDto) => ReactNode) {
  if (columns === 'settlement') return [nameColumn(renderName), settlementBalanceColumn]
  const leading = [nameColumn(renderName), currencyColumn]
  return columns === 'card' ? [...leading, nextStatementColumn, totalOwedColumn] : [...leading, balanceColumn]
}

export default function AccountsPage() {
  const { year, month, monthInputValue: monthParam, setMonthInputValue } = useOverviewAddress()
  const { data: groups, isLoading, error, retry } = useAccountTemplateGroups(year, month)
  const { state } = useLocation()
  const showMovedNotice = (state as { movedNotice?: boolean } | null)?.movedNotice === true

  return (
    <div className="accounts-page">
      {showMovedNotice && (
        <MessageBar role="status" intent="info">
          <MessageBarBody>That page moved to the account screen.</MessageBarBody>
        </MessageBar>
      )}
      <div className="monthly-page__header">
        <Field label="Month">
          <Input type="month" value={monthParam} onChange={(_event, data) => setMonthInputValue(data.value)} />
        </Field>
      </div>
      <Text>Balances at end of {formatLongMonthYear(year, month)}</Text>

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
            ? (row: AccountGroupMemberDto) => <Link to={`/cashflow/accounts/${row.accountId}?month=${monthParam}${tabParam}`}>{row.name}</Link>
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
                emptyMessage="No accounts this month."
                footerItems={grid.footerLabel ? group.totals.map((total) => ({ label: `${grid.footerLabel} ${total.currency}`, value: formatFooterTotal(total.total) })) : []}
              />
            </div>
          )
        })
      )}
    </div>
  )
}
