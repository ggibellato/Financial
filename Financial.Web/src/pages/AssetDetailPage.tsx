import { type FormEvent, useCallback, useEffect, useMemo, useState } from 'react'
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
  const [operationDate, setOperationDate] = useState('')
  const [operationType, setOperationType] = useState('Buy')
  const [quantity, setQuantity] = useState('')
  const [unitPrice, setUnitPrice] = useState('')
  const [fees, setFees] = useState('0')
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

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

  const submitOperation = useCallback(async () => {
    if (!brokerName || !portfolioName || !assetName) {
      setFormError('Asset route parameters are required.')
      return
    }

    if (!operationDate) {
      setFormError('Operation date is required.')
      return
    }

    const parsedQuantity = Number(quantity)
    const parsedUnitPrice = Number(unitPrice)
    const parsedFees = Number(fees)
    if (!Number.isFinite(parsedQuantity) || !Number.isFinite(parsedUnitPrice) || !Number.isFinite(parsedFees)) {
      setFormError('Quantity, unit price, and fees must be valid numbers.')
      return
    }

    setIsSubmitting(true)
    setFormError(null)
    try {
      const normalizedDate = operationDate.includes('T') ? operationDate : `${operationDate}T00:00:00`
      const updated = await apiClient.addOperation({
        brokerName,
        portfolioName,
        assetName,
        date: normalizedDate,
        type: operationType,
        quantity: parsedQuantity,
        unitPrice: parsedUnitPrice,
        fees: parsedFees,
      })
      setAsset(updated)
      setQuantity('')
      setUnitPrice('')
      setFees('0')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unable to add operation.'
      setFormError(message)
    } finally {
      setIsSubmitting(false)
    }
  }, [apiClient, assetName, brokerName, fees, operationDate, operationType, portfolioName, quantity, unitPrice])

  const handleSubmit = useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault()
      void submitOperation()
    },
    [submitOperation],
  )

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
      <h3>New operation</h3>
      <form onSubmit={handleSubmit} aria-label="New operation">
        <div>
          <label htmlFor="operation-date">Date</label>
          <input
            id="operation-date"
            type="date"
            value={operationDate}
            onChange={(event) => setOperationDate(event.target.value)}
            required
          />
        </div>
        <div>
          <label htmlFor="operation-type">Type</label>
          <select
            id="operation-type"
            value={operationType}
            onChange={(event) => setOperationType(event.target.value)}
          >
            <option value="Buy">Buy</option>
            <option value="Sell">Sell</option>
          </select>
        </div>
        <div>
          <label htmlFor="operation-quantity">Quantity</label>
          <input
            id="operation-quantity"
            type="number"
            value={quantity}
            onChange={(event) => setQuantity(event.target.value)}
            min="0"
            step="0.0001"
            required
          />
        </div>
        <div>
          <label htmlFor="operation-unit-price">Unit price</label>
          <input
            id="operation-unit-price"
            type="number"
            value={unitPrice}
            onChange={(event) => setUnitPrice(event.target.value)}
            min="0"
            step="0.0001"
            required
          />
        </div>
        <div>
          <label htmlFor="operation-fees">Fees</label>
          <input
            id="operation-fees"
            type="number"
            value={fees}
            onChange={(event) => setFees(event.target.value)}
            min="0"
            step="0.0001"
          />
        </div>
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving...' : 'Add operation'}
        </button>
      </form>
      {formError ? <p role="alert">{formError}</p> : null}
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
