import { useCallback, useEffect, useMemo, useState } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { BrokerNodeDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'

interface PriceResult {
  ticker: string
  exchange: string
  assetName: string
  name: string
  price: number | null
  asOf: string | null
  error?: string
}

export default function CurrentValuesPage() {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [brokers, setBrokers] = useState<BrokerNodeDto[]>([])
  const [selectedBroker, setSelectedBroker] = useState('')
  const [selectedPortfolio, setSelectedPortfolio] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isRunning, setIsRunning] = useState(false)
  const [progressText, setProgressText] = useState('')
  const [progressValue, setProgressValue] = useState(0)
  const [results, setResults] = useState<PriceResult[]>([])
  const formatter = useMemo(
    () =>
      new Intl.NumberFormat(undefined, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }),
    [],
  )

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

  const portfolios = useMemo(() => {
    const broker = brokers.find((item) => item.name === selectedBroker)
    return broker ? broker.portfolios : []
  }, [brokers, selectedBroker])

  const assetsToCheck = useMemo(() => {
    const filteredBrokers = selectedBroker
      ? brokers.filter((broker) => broker.name === selectedBroker)
      : brokers
    const assets = filteredBrokers.flatMap((broker) =>
      broker.portfolios.flatMap((portfolio) => {
        if (selectedPortfolio && portfolio.name !== selectedPortfolio) {
          return []
        }
        return portfolio.assets.map((asset) => ({
          ticker: asset.ticker,
          exchange: asset.exchange,
          assetName: asset.name,
          isActive: asset.isActive,
        }))
      }),
    )
    return assets.filter((asset) => asset.isActive && asset.ticker && asset.exchange)
  }, [brokers, selectedBroker, selectedPortfolio])

  const runPriceCheck = useCallback(async () => {
    if (assetsToCheck.length === 0) {
      setError('No assets available for the selected scope.')
      return
    }

    setIsRunning(true)
    setProgressValue(0)
    setResults([])
    setError(null)
    setProgressText(`Fetching 0 of ${assetsToCheck.length}...`)

    let completed = 0
    for (const asset of assetsToCheck) {
      try {
        const price = await apiClient.getCurrentPrice(asset.exchange, asset.ticker)
        const result: PriceResult = {
          ticker: price.ticker,
          exchange: price.exchange,
          assetName: asset.assetName,
          name: price.name || asset.assetName,
          price: price.price,
          asOf: price.asOf,
        }
        setResults((prev) => [...prev, result])
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Unable to fetch price.'
        setResults((prev) => [
          ...prev,
          {
            ticker: asset.ticker,
            exchange: asset.exchange,
            assetName: asset.assetName,
            name: asset.assetName,
            price: null,
            asOf: null,
            error: message,
          },
        ])
      }

      completed += 1
      setProgressValue(Math.round((completed / assetsToCheck.length) * 100))
      setProgressText(`Fetching ${completed} of ${assetsToCheck.length}: ${asset.ticker}...`)
    }

    setProgressText(`Completed! Loaded ${assetsToCheck.length} assets.`)
    setIsRunning(false)
  }, [apiClient, assetsToCheck])

  const formatDateTime = useCallback((value: string | null) => {
    if (!value) {
      return '--'
    }
    const parsed = new Date(value)
    if (Number.isNaN(parsed.getTime())) {
      return value
    }
    return parsed.toLocaleString()
  }, [])

  if (isLoading) {
    return <LoadingState message="Loading brokers..." />
  }

  if (error) {
    return <ErrorState message={error} onRetry={loadBrokers} />
  }

  return (
    <section className="current-values">
      <header className="current-values__header">
        <h2>Read Assets Current Values</h2>
        <p>Fetch current prices for active assets across your portfolios.</p>
      </header>

      <div className="current-values__controls">
        <label className="current-values__field">
          <span>Broker</span>
          <select
            value={selectedBroker}
            onChange={(event) => {
              setSelectedBroker(event.target.value)
              setSelectedPortfolio('')
            }}
          >
            <option value="">All brokers</option>
            {brokers.map((broker) => (
              <option key={broker.name} value={broker.name}>
                {broker.name}
              </option>
            ))}
          </select>
        </label>
        <label className="current-values__field">
          <span>Portfolio</span>
          <select
            value={selectedPortfolio}
            onChange={(event) => setSelectedPortfolio(event.target.value)}
            disabled={!selectedBroker}
          >
            <option value="">All portfolios</option>
            {portfolios.map((portfolio) => (
              <option key={portfolio.name} value={portfolio.name}>
                {portfolio.name}
              </option>
            ))}
          </select>
        </label>
        <button type="button" onClick={runPriceCheck} disabled={isRunning}>
          {isRunning ? 'Checking...' : 'Check Prices'}
        </button>
      </div>

      {isRunning ? (
        <div className="current-values__progress">
          <progress max={100} value={progressValue} />
          <p>{progressText}</p>
        </div>
      ) : progressText ? (
        <p>{progressText}</p>
      ) : null}

      <section className="current-values__results">
        <h3>Assets Current Prices</h3>
        {results.length === 0 ? (
          <p>No prices loaded yet.</p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Ticker</th>
                <th>Name</th>
                <th className="table-number">Price</th>
                <th>As of</th>
              </tr>
            </thead>
            <tbody>
              {results.map((result) => (
                <tr key={`${result.exchange}-${result.ticker}-${result.assetName}`}>
                  <td>{result.ticker}</td>
                  <td>{result.name}</td>
                  <td className="table-number">
                    {result.price === null ? '—' : formatter.format(result.price)}
                  </td>
                  <td>{result.error ? result.error : formatDateTime(result.asOf)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </section>
  )
}
