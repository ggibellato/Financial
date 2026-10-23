import { Skeleton, SkeletonItem, Text } from '@fluentui/react-components'
import { Link } from 'react-router-dom'
import AccountHeader from '../components/AccountHeader'
import AccountTransactionsGrid from '../components/AccountTransactionsGrid'
import ErrorState from '../components/ErrorState'
import { ACCOUNT_LOAD_ERROR, useAccountScreen } from '../hooks/useAccountScreen'
import { formatLongMonthYear } from '../utils/formatters'
import './AccountScreenPage.css'

export default function AccountScreenPage() {
  const screen = useAccountScreen()
  const { account } = screen

  if (screen.isNotFound) {
    return (
      <div className="account-screen">
        <p role="alert">This account does not exist.</p>
        <Link to="/cashflow/accounts">Go to Accounts</Link>
      </div>
    )
  }

  if (screen.error && !account) {
    return (
      <div className="account-screen">
        <ErrorState message={ACCOUNT_LOAD_ERROR} onRetry={screen.retry} />
      </div>
    )
  }

  return (
    <div className="account-screen">
      {account ? (
        <AccountHeader
          account={account}
          accounts={screen.accounts}
          selectedAccountId={screen.accountId}
          monthInputValue={screen.monthInputValue}
          isBusy={screen.isLoading}
          onMonthChange={screen.setMonthInputValue}
          onAccountSelect={screen.selectAccount}
        />
      ) : (
        <Skeleton aria-label="Loading account" className="account-screen__header-skeleton">
          <SkeletonItem size={32} />
          <SkeletonItem size={16} />
        </Skeleton>
      )}

      <section className="account-screen__transactions" aria-labelledby="account-screen-transactions">
        <Text as="h2" id="account-screen-transactions" size={400} weight="semibold">
          Transactions
        </Text>
        {screen.error ? (
          <ErrorState message={ACCOUNT_LOAD_ERROR} onRetry={screen.retry} />
        ) : (
          <AccountTransactionsGrid
            transactions={screen.transactions}
            isLoading={screen.isLoading}
            emptyMessage={`No transactions in ${formatLongMonthYear(screen.year, screen.month)}`}
          />
        )}
      </section>
    </div>
  )
}
