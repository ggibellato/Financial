import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { BrokerNodeDto } from '../api/types'

export default function BrokersPage() {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [brokers, setBrokers] = useState<BrokerNodeDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadBrokers = useCallback(async () => {
    setIsLoading(true)
    setError(null)
    try {
      const data = await apiClient.getBrokers()
      setBrokers(data)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unable to load brokers.'
      setError(message)
    } finally {
      setIsLoading(false)
    }
  }, [apiClient])

  useEffect(() => {
    void loadBrokers()
  }, [loadBrokers])

  if (isLoading) {
    return <LoadingState message="Loading brokers..." />
  }

  if (error) {
    return <ErrorState message={error} onRetry={loadBrokers} />
  }

  return (
    <section>
      <h2>Brokers</h2>
      {brokers.length === 0 ? (
        <p>No brokers found.</p>
      ) : (
        <ul>
          {brokers.map((broker) => (
            <li key={broker.name}>
              <Link to={`/brokers/${encodeURIComponent(broker.name)}`}>
                <strong>{broker.name}</strong> ({broker.currency})
              </Link>{' '}
              — {broker.portfolioCount} portfolios, {broker.totalAssets} assets
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
