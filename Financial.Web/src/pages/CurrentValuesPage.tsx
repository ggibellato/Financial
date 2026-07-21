import { useCallback, useEffect, useMemo, useState } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { BrokerNodeDto, PortfolioReferenceDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import './CurrentValuesPage.css'

interface PriceResult {
  ticker: string
  exchange: string
  assetName: string
  name: string
  price: number | null
  error?: string
}

export default function CurrentValuesPage() {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [brokers, setBrokers] = useState<BrokerNodeDto[]>([])
  const [scope, setScope] = useState<PortfolioReferenceDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isRunning, setIsRunning] = useState(false)
  const [progressText, setProgressText] = useState('')
  const [progressValue, setProgressValue] = useState(0)
  const [results, setResults] = useState<PriceResult[]>([])
  const [retryCount, setRetryCount] = useState(0)

  const formatter = useMemo(
    () =>
      new Intl.NumberFormat(undefined, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }),
    [],
  )

  useEffect(() => {
    Promise.all([apiClient.getBrokers(), apiClient.getAssetPriceFetchScope()])
      .then(([brokersData, scopeData]) => {
        setBrokers(brokersData)
        setScope(scopeData)
        setError(null)
      })
      .catch((err: unknown) => {
        const message = err instanceof Error ? err.message : 'Unable to load brokers.'
        setError(message)
      })
      .finally(() => setIsLoading(false))
  }, [apiClient, retryCount])

  const handleRetry = useCallback(() => {
    setIsLoading(true)
    setError(null)
    setRetryCount((c) => c + 1)
  }, [])

  const assetsToCheck = useMemo(() => {
    const assets = scope.flatMap(({ brokerName, portfolioName }) => {
      const broker = brokers.find((b) => b.name === brokerName)
      if (!broker) return []
      const portfolio = broker.portfolios.find((p) => p.name === portfolioName)
      if (!portfolio) return []
      return portfolio.assets.map((asset) => ({
        ticker: asset.ticker,
        exchange: asset.exchange,
        assetName: asset.name,
        assetClass: asset.class,
        brokerName: broker.name,
      }))
    })
    return assets.filter(
      (asset) => asset.ticker && (asset.exchange || asset.assetClass === 'Cryptocurrency'),
    )
  }, [brokers, scope])

  const runPriceCheck = useCallback(async () => {
    if (assetsToCheck.length === 0) return

    setIsRunning(true)
    setProgressValue(0)
    setResults([])
    setProgressText(`Fetching 0 of ${assetsToCheck.length}...`)

    let completed = 0
    for (const asset of assetsToCheck) {
      try {
        const price = await apiClient.getCurrentPrice(
          asset.exchange,
          asset.ticker,
          asset.assetClass,
          asset.brokerName,
        )
        setResults((prev) => [
          ...prev,
          {
            ticker: price.ticker,
            exchange: price.exchange,
            assetName: asset.assetName,
            name: price.name || asset.assetName,
            price: price.price,
          },
        ])
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

  if (isLoading) {
    return <LoadingState message="Loading data..." />
  }

  if (error) {
    return <ErrorState message={error} onRetry={handleRetry} />
  }

  return (
    <section className="current-values">
      <header className="current-values__header">
        <h2>Fetch Current Prices</h2>
        <button
          type="button"
          className="current-values__check-btn"
          onClick={() => void runPriceCheck()}
          disabled={isRunning}
        >
          {isRunning ? 'Checking...' : 'Check Prices'}
        </button>
      </header>

      {isRunning ? (
        <div className="current-values__progress">
          <progress max={100} value={progressValue} />
          <p>{progressText}</p>
        </div>
      ) : progressText ? (
        <p>{progressText}</p>
      ) : null}

      {results.length > 0 && (
        <section className="current-values__results">
          <table className="data-table">
            <thead>
              <tr>
                <th>Ticker</th>
                <th>Name</th>
                <th className="current-values__col--price data-table__col--numeric">Price</th>
              </tr>
            </thead>
            <tbody>
              {results.map((result) => (
                <tr key={`${result.exchange}-${result.ticker}-${result.assetName}`}>
                  <td>{result.ticker}</td>
                  <td>{result.name}</td>
                  <td className="current-values__col--price data-table__col--numeric">
                    {result.price === null ? '—' : formatter.format(result.price)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}
    </section>
  )
}
