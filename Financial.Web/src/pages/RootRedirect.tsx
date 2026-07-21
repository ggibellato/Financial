import { Navigate } from 'react-router-dom'
import { getStoredDomain } from '../utils/domainStorage'

function RootRedirect() {
  const domain = getStoredDomain()

  if (domain === 'cashflow') {
    return <Navigate to="/cashflow/monthly" replace />
  }

  return <Navigate to="/investments/active-investments" replace />
}

export default RootRedirect
