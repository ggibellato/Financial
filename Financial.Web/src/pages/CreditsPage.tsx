import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { CreditDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'

export default function CreditsPage() {
  const { brokerName, portfolioName } = useParams()
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [credits, setCredits] = useState<CreditDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadCredits = useCallback(async () => {
    if (!brokerName) {
      setError('Broker name is required.')
      setIsLoading(false)
      return
    }

    setIsLoading(true)
    setError(null)
    try {
      const data = portfolioName
        ? await apiClient.getCreditsByPortfolio(brokerName, portfolioName)
        : await apiClient.getCreditsByBroker(brokerName)
      setCredits(data)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unable to load credits.'
      setError(message)
    } finally {
      setIsLoading(false)
    }
  }, [apiClient, brokerName, portfolioName])

  useEffect(() => {
    void loadCredits()
  }, [loadCredits])

  if (isLoading) {
    return <LoadingState message="Loading credits..." />
  }

  if (error) {
    return <ErrorState message={error} onRetry={loadCredits} />
  }

  return (
    <section>
      <p>
        <Link to={`/brokers/${encodeURIComponent(brokerName ?? '')}`}>← Back to broker</Link>
      </p>
      <h2>Credits</h2>
      <p>
        Broker: <strong>{brokerName}</strong>
      </p>
      {portfolioName ? (
        <p>
          Portfolio: <strong>{portfolioName}</strong>
        </p>
      ) : null}
      {credits.length === 0 ? (
        <p>No credits recorded.</p>
      ) : (
        <ul>
          {credits.map((credit) => (
            <li key={credit.id}>
              {credit.type} {credit.value} ({credit.date})
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
