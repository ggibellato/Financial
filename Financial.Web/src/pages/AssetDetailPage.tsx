import { type FormEvent, useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { AssetDetailsDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'

export default function AssetDetailPage() {
  const { brokerName, portfolioName, assetName } = useParams()
  const tabs = [
    { id: 'summary', label: 'Summary' },
    { id: 'operations', label: 'Operations' },
    { id: 'credits', label: 'Credits' },
  ] as const
  type AssetTab = (typeof tabs)[number]['id']
  const location = useLocation()
  const isAssetTab = (value: unknown): value is AssetTab => tabs.some((tab) => tab.id === value)
  const resolveDefaultTab = (state: unknown): AssetTab => {
    if (typeof state === 'object' && state !== null) {
      const value = (state as Record<string, unknown>).defaultTab
      if (isAssetTab(value)) {
        return value
      }
    }

    return 'summary'
  }
  const normalizeDateTime = (value: string) => (value.includes('T') ? value : `${value}T00:00:00`)
  const toDateInputValue = (value: string) => value.split('T')[0] ?? value
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [asset, setAsset] = useState<AssetDetailsDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState<AssetTab>(() => resolveDefaultTab(location.state))
  const [operationEditId, setOperationEditId] = useState<string | null>(null)
  const [operationDate, setOperationDate] = useState('')
  const [operationType, setOperationType] = useState('Buy')
  const [quantity, setQuantity] = useState('')
  const [unitPrice, setUnitPrice] = useState('')
  const [fees, setFees] = useState('0')
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [creditEditId, setCreditEditId] = useState<string | null>(null)
  const [creditDate, setCreditDate] = useState('')
  const [creditType, setCreditType] = useState('Dividend')
  const [creditValue, setCreditValue] = useState('')
  const [creditError, setCreditError] = useState<string | null>(null)
  const [isCreditSubmitting, setIsCreditSubmitting] = useState(false)

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

  const resetOperationForm = useCallback(() => {
    setOperationEditId(null)
    setOperationDate('')
    setOperationType('Buy')
    setQuantity('')
    setUnitPrice('')
    setFees('0')
  }, [])

  const startEditOperation = useCallback(
    (operation: AssetDetailsDto['operations'][number]) => {
      setOperationEditId(operation.id)
      setOperationDate(toDateInputValue(operation.date))
      setOperationType(operation.type)
      setQuantity(operation.quantity.toString())
      setUnitPrice(operation.unitPrice.toString())
      setFees(operation.fees.toString())
      setFormError(null)
    },
    [toDateInputValue],
  )

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
      const normalizedDate = normalizeDateTime(operationDate)
      const request = {
        brokerName,
        portfolioName,
        assetName,
        date: normalizedDate,
        type: operationType,
        quantity: parsedQuantity,
        unitPrice: parsedUnitPrice,
        fees: parsedFees,
      }
      const updated = operationEditId
        ? await apiClient.updateOperation({ ...request, id: operationEditId })
        : await apiClient.addOperation(request)
      setAsset(updated)
      resetOperationForm()
    } catch (err) {
      const message =
        err instanceof Error
          ? err.message
          : operationEditId
            ? 'Unable to update operation.'
            : 'Unable to add operation.'
      setFormError(message)
    } finally {
      setIsSubmitting(false)
    }
  }, [
    apiClient,
    assetName,
    brokerName,
    fees,
    operationDate,
    operationEditId,
    operationType,
    portfolioName,
    quantity,
    resetOperationForm,
    unitPrice,
    normalizeDateTime,
  ])

  const deleteOperation = useCallback(
    async (id: string) => {
      if (!brokerName || !portfolioName || !assetName) {
        setFormError('Asset route parameters are required.')
        return
      }

      if (!window.confirm('Delete this operation?')) {
        return
      }

      setIsSubmitting(true)
      setFormError(null)
      try {
        const updated = await apiClient.deleteOperation({
          brokerName,
          portfolioName,
          assetName,
          id,
        })
        setAsset(updated)
        if (operationEditId === id) {
          resetOperationForm()
        }
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Unable to delete operation.'
        setFormError(message)
      } finally {
        setIsSubmitting(false)
      }
    },
    [apiClient, assetName, brokerName, operationEditId, portfolioName, resetOperationForm],
  )

  const handleSubmit = useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault()
      void submitOperation()
    },
    [submitOperation],
  )

  const resetCreditForm = useCallback(() => {
    setCreditEditId(null)
    setCreditDate('')
    setCreditType('Dividend')
    setCreditValue('')
  }, [])

  const startEditCredit = useCallback(
    (credit: AssetDetailsDto['credits'][number]) => {
      setCreditEditId(credit.id)
      setCreditDate(toDateInputValue(credit.date))
      setCreditType(credit.type)
      setCreditValue(credit.value.toString())
      setCreditError(null)
    },
    [toDateInputValue],
  )

  const submitCreditUpdate = useCallback(async () => {
    if (!brokerName || !portfolioName || !assetName) {
      setCreditError('Asset route parameters are required.')
      return
    }

    if (!creditEditId) {
      setCreditError('Select a credit to update.')
      return
    }

    if (!creditDate) {
      setCreditError('Credit date is required.')
      return
    }

    const parsedValue = Number(creditValue)
    if (!Number.isFinite(parsedValue)) {
      setCreditError('Credit value must be a valid number.')
      return
    }

    setIsCreditSubmitting(true)
    setCreditError(null)
    try {
      const normalizedDate = normalizeDateTime(creditDate)
      const updated = await apiClient.updateCredit({
        brokerName,
        portfolioName,
        assetName,
        id: creditEditId,
        date: normalizedDate,
        type: creditType,
        value: parsedValue,
      })
      setAsset(updated)
      resetCreditForm()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unable to update credit.'
      setCreditError(message)
    } finally {
      setIsCreditSubmitting(false)
    }
  }, [
    apiClient,
    assetName,
    brokerName,
    creditDate,
    creditEditId,
    creditType,
    creditValue,
    portfolioName,
    resetCreditForm,
    normalizeDateTime,
  ])

  const deleteCredit = useCallback(
    async (id: string) => {
      if (!brokerName || !portfolioName || !assetName) {
        setCreditError('Asset route parameters are required.')
        return
      }

      if (!window.confirm('Delete this credit?')) {
        return
      }

      setIsCreditSubmitting(true)
      setCreditError(null)
      try {
        const updated = await apiClient.deleteCredit({
          brokerName,
          portfolioName,
          assetName,
          id,
        })
        setAsset(updated)
        if (creditEditId === id) {
          resetCreditForm()
        }
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Unable to delete credit.'
        setCreditError(message)
      } finally {
        setIsCreditSubmitting(false)
      }
    },
    [apiClient, assetName, brokerName, creditEditId, portfolioName, resetCreditForm],
  )

  const handleCreditSubmit = useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault()
      void submitCreditUpdate()
    },
    [submitCreditUpdate],
  )

  useEffect(() => {
    void loadAsset()
  }, [loadAsset])

  useEffect(() => {
    setActiveTab(resolveDefaultTab(location.state))
    resetOperationForm()
    resetCreditForm()
    setFormError(null)
    setCreditError(null)
  }, [assetName, brokerName, portfolioName, location.state, resetOperationForm, resetCreditForm])

  if (isLoading) {
    return <LoadingState message="Loading asset..." />
  }

  if (error) {
    return <ErrorState message={error} onRetry={loadAsset} />
  }

  if (!asset) {
    return <p>Asset not found.</p>
  }

  const isEditingOperation = operationEditId !== null
  const isEditingCredit = creditEditId !== null

  return (
    <section>
      <p>
        <Link to={`/brokers/${encodeURIComponent(asset.brokerName)}`}>← Back to broker</Link>
      </p>
      <h2>{asset.name}</h2>
      <div className="detail-tabs" role="tablist" aria-label="Asset detail tabs">
        {tabs.map((tab) => {
          const isActive = activeTab === tab.id
          return (
            <button
              key={tab.id}
              type="button"
              role="tab"
              aria-selected={isActive}
              aria-controls={`asset-tab-${tab.id}`}
              id={`asset-tab-${tab.id}-button`}
              className="detail-tab"
              onClick={() => setActiveTab(tab.id)}
            >
              {tab.label}
            </button>
          )
        })}
      </div>
      <div
        role="tabpanel"
        id="asset-tab-summary"
        aria-labelledby="asset-tab-summary-button"
        hidden={activeTab !== 'summary'}
        className="detail-panel"
      >
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
      </div>
      <div
        role="tabpanel"
        id="asset-tab-operations"
        aria-labelledby="asset-tab-operations-button"
        hidden={activeTab !== 'operations'}
        className="detail-panel"
      >
        <h3>{isEditingOperation ? 'Edit operation' : 'New operation'}</h3>
        <form onSubmit={handleSubmit} aria-label={isEditingOperation ? 'Edit operation' : 'New operation'}>
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
          <div className="detail-actions">
            <button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Saving...' : isEditingOperation ? 'Update operation' : 'Add operation'}
            </button>
            {isEditingOperation ? (
              <button type="button" disabled={isSubmitting} onClick={resetOperationForm}>
                Cancel
              </button>
            ) : null}
          </div>
        </form>
        {formError ? <p role="alert">{formError}</p> : null}
        <h3>Operations</h3>
        {asset.operations.length === 0 ? (
          <p>No operations recorded.</p>
        ) : (
          <ul className="detail-list">
            {asset.operations.map((operation) => (
              <li key={operation.id}>
                <div className="detail-row">
                  <span>
                    {operation.type} {operation.quantity} @ {operation.unitPrice} ({operation.date})
                  </span>
                  <div className="detail-actions">
                    <button type="button" disabled={isSubmitting} onClick={() => startEditOperation(operation)}>
                      Edit
                    </button>
                    <button type="button" disabled={isSubmitting} onClick={() => deleteOperation(operation.id)}>
                      Delete
                    </button>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
      <div
        role="tabpanel"
        id="asset-tab-credits"
        aria-labelledby="asset-tab-credits-button"
        hidden={activeTab !== 'credits'}
        className="detail-panel"
      >
        <h3>Credits</h3>
        {asset.credits.length === 0 ? (
          <p>No credits recorded.</p>
        ) : (
          <ul className="detail-list">
            {asset.credits.map((credit) => (
              <li key={credit.id}>
                <div className="detail-row">
                  <span>
                    {credit.type} {credit.value} ({credit.date})
                  </span>
                  <div className="detail-actions">
                    <button type="button" disabled={isCreditSubmitting} onClick={() => startEditCredit(credit)}>
                      Edit
                    </button>
                    <button type="button" disabled={isCreditSubmitting} onClick={() => deleteCredit(credit.id)}>
                      Delete
                    </button>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        )}
        {isEditingCredit ? (
          <>
            <h4>Edit credit</h4>
            <form onSubmit={handleCreditSubmit} aria-label="Edit credit">
              <div>
                <label htmlFor="credit-date">Date</label>
                <input
                  id="credit-date"
                  type="date"
                  value={creditDate}
                  onChange={(event) => setCreditDate(event.target.value)}
                  required
                />
              </div>
              <div>
                <label htmlFor="credit-type">Type</label>
                <select
                  id="credit-type"
                  value={creditType}
                  onChange={(event) => setCreditType(event.target.value)}
                >
                  <option value="Dividend">Dividend</option>
                  <option value="Rent">Rent</option>
                </select>
              </div>
              <div>
                <label htmlFor="credit-value">Value</label>
                <input
                  id="credit-value"
                  type="number"
                  value={creditValue}
                  onChange={(event) => setCreditValue(event.target.value)}
                  min="0"
                  step="0.0001"
                  required
                />
              </div>
              <div className="detail-actions">
                <button type="submit" disabled={isCreditSubmitting}>
                  {isCreditSubmitting ? 'Saving...' : 'Update credit'}
                </button>
                <button type="button" disabled={isCreditSubmitting} onClick={resetCreditForm}>
                  Cancel
                </button>
              </div>
            </form>
          </>
        ) : asset.credits.length > 0 ? (
          <p>Select a credit to edit.</p>
        ) : null}
        {creditError ? <p role="alert">{creditError}</p> : null}
      </div>
    </section>
  )
}
