import { Badge, Text } from '@fluentui/react-components'
import { Link } from 'react-router-dom'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import TotalsGrid from '../components/TotalsGrid'
import { DEFAULT_GROUP_LOAD_ERROR, useDefaultGroupAccounts } from '../hooks/useDefaultGroupAccounts'
import { accountTemplateLabel } from '../utils/accountTemplateLabel'
import { currentYearMonth, formatLongMonthYear, formatMonthInputValue, formatN2 } from '../utils/formatters'
import { formatLabelledBalance } from '../utils/owedBalance'
import './AccountsPage.css'

export default function AccountsPage() {
  const { year, month } = currentYearMonth()
  const monthParam = formatMonthInputValue(year, month)

  const { data, isLoading, error, retry } = useDefaultGroupAccounts(year, month)

  return (
    <div className="accounts-page">
      <Text>Default group · balances at end of {formatLongMonthYear(year, month)}</Text>

      {isLoading ? (
        <LoadingState />
      ) : error || !data ? (
        <ErrorState message={DEFAULT_GROUP_LOAD_ERROR} onRetry={retry} />
      ) : (
        <TotalsGrid
          label="Accounts"
          columns={[
            {
              key: 'name',
              header: 'Account',
              render: (row) => (
                <span className="accounts-page__name">
                  <Link to={`/cashflow/accounts/${row.accountId}?month=${monthParam}`}>{row.name}</Link>
                  {!row.isActive && (
                    <Badge appearance="outline" color="informative">
                      Inactive
                    </Badge>
                  )}
                </span>
              ),
              sortAccessor: (row) => row.name,
            },
            {
              key: 'template',
              header: 'Template',
              render: (row) => accountTemplateLabel(row.templateName),
              sortAccessor: (row) => accountTemplateLabel(row.templateName),
            },
            {
              key: 'currency',
              header: 'Currency',
              render: (row) => row.currency,
              sortAccessor: (row) => row.currency,
            },
            {
              key: 'balance',
              header: 'Balance',
              numeric: true,
              render: (row) => formatLabelledBalance(row.balance, row.balanceIsOwed),
              sortAccessor: (row) => row.balance,
            },
          ]}
          rows={data.members}
          rowKey={(row) => row.accountId}
          emptyMessage="No accounts in the default group for this month"
          footerItems={data.totals.map((total) => ({ label: `Bank total ${total.currency}`, value: formatN2(total.balance) }))}
        />
      )}
    </div>
  )
}
