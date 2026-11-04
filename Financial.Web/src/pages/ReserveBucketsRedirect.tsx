import { Navigate } from 'react-router-dom'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { ACCOUNTS_LOAD_ERROR } from '../hooks/useAccountTemplateGroups'
import { useAccounts } from '../hooks/useAccounts'
import { accountsWith } from '../utils/accountCapabilities'

export default function ReserveBucketsRedirect() {
  const { accounts, status, retry } = useAccounts()

  if (status === 'failed') return <ErrorState message={ACCOUNTS_LOAD_ERROR} onRetry={retry} />
  if (status === 'loading') return <LoadingState />

  const [potsAccount] = accountsWith(accounts, 'HasPots')
  return <Navigate to={potsAccount ? `/cashflow/accounts/${potsAccount.id}?tab=pots` : '/cashflow/accounts'} replace />
}
