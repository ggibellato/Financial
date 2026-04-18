import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { BrokerNodeDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'

export default function BrokerDetailPage() {
  const { brokerName } = useParams()
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [broker, setBroker] = useState<BrokerNodeDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadBroker = useCallback(async () => {
    if (!brokerName) {
      setError('Broker name is required.')
      setIsLoading(false)
      return
    }

    setIsLoading(true)
    setError(null)
    try {
      const data = await apiClient.getBrokers()
      const match = data.find((item) => item.name === brokerName)
      if (!match) {
        setBroker(null)
        setError('Broker not found.')
      } else {
        setBroker(match)
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unable to load broker.'
      setError(message)
    } finally {
      setIsLoading(false)
    }
  }, [apiClient, brokerName])

  useEffect(() => {
    void loadBroker()
  }, [loadBroker])

  if (isLoading) {
    return <LoadingState message="Loading broker..." />
  }

  if (error) {
    return <ErrorState message={error} onRetry={loadBroker} />
  }

  if (!broker) {
    return <p>Broker not found.</p>
  }

  return (
    <section>
      <p>
        <Link to="/brokers">← Back to brokers</Link>
      </p>
      <h2>{broker.name}</h2>
      <p>
        Currency: <strong>{broker.currency}</strong>
      </p>
      <p>
        Portfolios: <strong>{broker.portfolioCount}</strong> · Total assets:{' '}
        <strong>{broker.totalAssets}</strong>
      </p>
      <h3>Portfolios</h3>
      {broker.portfolios.length === 0 ? (
        <p>No portfolios available.</p>
      ) : (
        <ul>
          {broker.portfolios.map((portfolio) => (
            <li key={portfolio.name}>
              <strong>{portfolio.name}</strong> — {portfolio.assetCount} assets ({portfolio.activeAssetCount} active)
              {portfolio.assets.length > 0 ? (
                <ul>
                  {portfolio.assets.map((asset) => (
                    <li key={asset.name}>
                      <Link
                        to={`/assets/${encodeURIComponent(broker.name)}/${encodeURIComponent(
                          portfolio.name,
                        )}/${encodeURIComponent(asset.name)}`}
                      >
                        {asset.name}
                      </Link>
                    </li>
                  ))}
                </ul>
              ) : null}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
