import { Navigate } from 'react-router-dom'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { ACCOUNTS_LOAD_ERROR } from '../hooks/useAccountTemplateGroups'
import { useAccounts } from '../hooks/useAccounts'

interface MovedPageRedirectProps {
  page: 'reserva' | 'controle-mae'
  tab?: string
}

export default function MovedPageRedirect({ page, tab }: MovedPageRedirectProps) {
  const { accounts, status, retry } = useAccounts()

  if (status === 'failed') return <ErrorState message={ACCOUNTS_LOAD_ERROR} onRetry={retry} />
  if (status === 'loading') return <LoadingState />

  const target = accounts.find((account) => account.replacesPage === page)
  if (!target) return <Navigate to="/cashflow/accounts" state={{ movedNotice: true }} replace />

  return <Navigate to={`/cashflow/accounts/${target.id}${tab ? `?tab=${tab}` : ''}`} replace />
}
