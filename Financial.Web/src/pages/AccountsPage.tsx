import type { ReactNode } from 'react'
import { Badge, Text } from '@fluentui/react-components'
import { Link } from 'react-router-dom'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import TotalsGrid from '../components/TotalsGrid'
import { type DefaultGroupAccount, DEFAULT_GROUP_LOAD_ERROR, useDefaultGroupAccounts } from '../hooks/useDefaultGroupAccounts'
import { accountTemplateLabel } from '../utils/accountTemplateLabel'
import { currentYearMonth, formatLongMonthYear, formatMonthInputValue, formatN2 } from '../utils/formatters'
import { formatLabelledBalance } from '../utils/owedBalance'
import './AccountsPage.css'

function accountColumns(renderName: (row: DefaultGroupAccount) => ReactNode) {
  return [
    {
      key: 'name',
      header: 'Account',
      render: (row: DefaultGroupAccount) => (
        <span className="accounts-page__name">
          {renderName(row)}
          {!row.isActive && (
            <Badge appearance="outline" color="informative">
              Inactive
            </Badge>
          )}
        </span>
      ),
      sortAccessor: (row: DefaultGroupAccount) => row.name,
    },
    {
      key: 'template',
      header: 'Template',
      render: (row: DefaultGroupAccount) => accountTemplateLabel(row.templateName),
      sortAccessor: (row: DefaultGroupAccount) => accountTemplateLabel(row.templateName),
    },
    {
      key: 'currency',
      header: 'Currency',
      render: (row: DefaultGroupAccount) => row.currency,
      sortAccessor: (row: DefaultGroupAccount) => row.currency,
    },
    {
      key: 'balance',
      header: 'Balance',
      numeric: true,
      render: (row: DefaultGroupAccount) => formatLabelledBalance(row.balance, row.balanceIsOwed),
      sortAccessor: (row: DefaultGroupAccount) => row.balance,
    },
  ]
}

export default function AccountsPage() {
  const { year, month } = currentYearMonth()
  const monthParam = formatMonthInputValue(year, month)

  const { data, isLoading, error, retry } = useDefaultGroupAccounts(year, month)

  return (
    <div className="accounts-page">
      <Text>Balances at end of {formatLongMonthYear(year, month)}</Text>

      {isLoading ? (
        <LoadingState />
      ) : error || !data ? (
        <ErrorState message={DEFAULT_GROUP_LOAD_ERROR} onRetry={retry} />
      ) : (
        <>
          <div className="accounts-page__section">
            <Text as="h2" weight="semibold" size={400}>
              Default group
            </Text>
            <TotalsGrid
              label="Default group"
              columns={accountColumns((row) => (
                <Link to={`/cashflow/accounts/${row.accountId}?month=${monthParam}`}>{row.name}</Link>
              ))}
              rows={data.members}
              rowKey={(row) => row.accountId}
              emptyMessage="No accounts in the default group for this month"
              footerItems={data.totals.map((total) => ({ label: `Bank total ${total.currency}`, value: formatN2(total.balance) }))}
            />
          </div>
          <div className="accounts-page__section">
            <Text as="h2" weight="semibold" size={400}>
              Other accounts
            </Text>
            <TotalsGrid
              label="Other accounts"
              columns={accountColumns((row) => row.name)}
              rows={data.others}
              rowKey={(row) => row.accountId}
              emptyMessage="No other accounts this month"
              footerItems={[]}
            />
          </div>
        </>
      )}
    </div>
  )
}
