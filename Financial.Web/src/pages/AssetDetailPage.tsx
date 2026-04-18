import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { AssetDetailsDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'

export default function AssetDetailPage() {
  const { brokerName, portfolioName, assetName } = useParams()
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [asset, setAsset] = useState<AssetDetailsDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadAsset = useCallback(async () => {
    if (!brokerName || !portfolioName || !assetName) {
      setError('Asset route parameters are required.')
      setIsLoading(false)
      return
    }

    setIsLoading(true)
    setError(null)
    try {
      const data = await apiClient.getAssetDetails(brokerName, portfolioName, assetName)
      setAsset(data)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unable to load asset.'
      setError(message)
    } finally {
      setIsLoading(false)
    }
  }, [apiClient, brokerName, portfolioName, assetName])

  useEffect(() => {
    void loadAsset()
  }, [loadAsset])

  if (isLoading) {
    return <LoadingState message="Loading asset..." />
  }

  if (error) {
    return <ErrorState message={error} onRetry={loadAsset} />
  }

  if (!asset) {
    return <p>Asset not found.</p>
  }

  return (
    <section>
      <p>
        <Link to={`/brokers/${encodeURIComponent(asset.brokerName)}`}>← Back to broker</Link>
      </p>
      <h2>{asset.name}</h2>
      <p>
        Ticker: <strong>{asset.ticker}</strong>
      </p>
      <p>
        Quantity: <strong>{asset.quantity}</strong> · Average price: <strong>{asset.averagePrice}</strong>
      </p>
      <p>
        Total bought: <strong>{asset.totalBought}</strong> · Total sold: <strong>{asset.totalSold}</strong> · Total
        credits: <strong>{asset.totalCredits}</strong>
      </p>
      <h3>Operations</h3>
      {asset.operations.length === 0 ? (
        <p>No operations recorded.</p>
      ) : (
        <ul>
          {asset.operations.map((operation) => (
            <li key={operation.id}>
              {operation.type} {operation.quantity} @ {operation.unitPrice} ({operation.date})
            </li>
          ))}
        </ul>
      )}
      <h3>Credits</h3>
      {asset.credits.length === 0 ? (
        <p>No credits recorded.</p>
      ) : (
        <ul>
          {asset.credits.map((credit) => (
            <li key={credit.id}>
              {credit.type} {credit.value} ({credit.date})
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
