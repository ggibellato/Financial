import { Badge, Text } from '@fluentui/react-components'
import { Link } from 'react-router-dom'
import { apiClient } from '../api/financialApiClient'
import type { AccountGroupMemberDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import TotalsGrid from '../components/TotalsGrid'
import { useAsyncResource } from '../hooks/useAsyncResource'
import { currentYearMonth, formatLongMonthYear, formatMonthInputValue, formatN2 } from '../utils/formatters'
import './AccountsPage.css'

interface AccountRow extends AccountGroupMemberDto {
  isActive: boolean
}

export default function AccountsPage() {
  const { year, month } = currentYearMonth()
  const monthParam = formatMonthInputValue(year, month)

  const { data, isLoading, error, retry } = useAsyncResource(
    () =>
      Promise.all([apiClient.getDefaultGroupSummary(year, month), apiClient.getAccounts()]).then(([summary, accounts]) => {
        const inactiveIds = new Set(accounts.filter((account) => !account.isActive).map((account) => account.id))
        return {
          rows: summary.members.map((member): AccountRow => ({ ...member, isActive: !inactiveIds.has(member.accountId) })),
          totals: summary.totals,
        }
      }),
    [year, month],
    'Unable to load the accounts',
    { initialLoading: true },
  )

  return (
    <div className="accounts-page">
      <Text>Default group · balances at end of {formatLongMonthYear(year, month)}</Text>

      {isLoading ? (
        <LoadingState />
      ) : error || !data ? (
        <ErrorState message="Unable to load the accounts" onRetry={retry} />
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
              render: (row) => row.templateName,
              sortAccessor: (row) => row.templateName,
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
              render: (row) => formatN2(row.balance),
              sortAccessor: (row) => row.balance,
            },
          ]}
          rows={data.rows}
          rowKey={(row) => row.accountId}
          emptyMessage="No accounts in the default group for this month"
          footerItems={data.totals.map((total) => ({ label: `Total ${total.currency}`, value: formatN2(total.balance) }))}
        />
      )}
    </div>
  )
}
