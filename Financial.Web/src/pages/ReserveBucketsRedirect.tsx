import { Navigate } from 'react-router-dom'
import { apiClient } from '../api/financialApiClient'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useAsyncResource } from '../hooks/useAsyncResource'

const LOAD_ERROR = 'Unable to load the accounts'

export default function ReserveBucketsRedirect() {
  const accounts = useAsyncResource(() => apiClient.getAccounts(), [], LOAD_ERROR, { initialLoading: true })

  if (accounts.error) return <ErrorState message={LOAD_ERROR} onRetry={accounts.retry} />
  if (!accounts.data) return <LoadingState />

  const potsAccount = accounts.data.find((account) => account.capabilities.includes('HasPots'))
  return <Navigate to={potsAccount ? `/cashflow/accounts/${potsAccount.id}?tab=pots` : '/cashflow/accounts'} replace />
}
