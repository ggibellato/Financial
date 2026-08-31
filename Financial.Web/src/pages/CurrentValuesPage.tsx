import { useCallback, useEffect, useMemo, useState } from 'react'
import { Button } from '@fluentui/react-components'
import { SearchRegular } from '@fluentui/react-icons'
import { apiClient } from '../api/financialApiClient'
import type { BrokerNodeDto, PortfolioReferenceDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import SortableColumnHeader from '../components/grid/SortableColumnHeader'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { formatN2, getErrorMessage } from '../utils/formatters'
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
  const [brokers, setBrokers] = useState<BrokerNodeDto[]>([])
  const [scope, setScope] = useState<PortfolioReferenceDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isRunning, setIsRunning] = useState(false)
  const [progressText, setProgressText] = useState('')
  const [progressValue, setProgressValue] = useState(0)
  const [results, setResults] = useState<PriceResult[]>([])
  const [retryCount, setRetryCount] = useState(0)

  useEffect(() => {
    Promise.all([apiClient.getBrokers(), apiClient.getAssetPriceFetchScope()])
      .then(([brokersData, scopeData]) => {
        setBrokers(brokersData)
        setScope(scopeData)
        setError(null)
      })
      .catch((err: unknown) => {
        const message = getErrorMessage(err, 'Unable to load brokers.')
        setError(message)
      })
      .finally(() => setIsLoading(false))
  }, [retryCount])

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
        portfolioName: portfolio.name,
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
        // Supplying the portfolio and asset name is what makes the API record the fetched
        // price into Price History, the same as the per-asset Refresh button and the portfolio
        // grid. Without them this screen took the lookup-only path and built no history at all.
        const price = await apiClient.getCurrentPrice(
          asset.exchange,
          asset.ticker,
          asset.assetClass,
          asset.brokerName,
          asset.assetName,
          asset.portfolioName,
          asset.assetName,
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
        const message = getErrorMessage(err, 'Unable to fetch price.')
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
  }, [assetsToCheck])

  const priceAccessors: Record<string, SortAccessor<PriceResult>> = {
    ticker: (r) => r.ticker,
    name: (r) => r.name,
    price: (r) => r.price,
  }
  const { sortedRows, sortState, requestSort } = useSortableRows(results, priceAccessors)

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
        <Button
          type="button"
          appearance="primary"
          icon={<SearchRegular />}
          onClick={() => void runPriceCheck()}
          disabled={isRunning}
        >
          {isRunning ? 'Checking...' : 'Check Prices'}
        </Button>
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
                <SortableColumnHeader
                  label="Ticker"
                  columnKey="ticker"
                  sortDirection={sortState?.columnKey === 'ticker' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <SortableColumnHeader
                  label="Name"
                  columnKey="name"
                  sortDirection={sortState?.columnKey === 'name' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <SortableColumnHeader
                  label="Price"
                  columnKey="price"
                  numeric
                  sortDirection={sortState?.columnKey === 'price' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
              </tr>
            </thead>
            <tbody>
              {sortedRows.map((result) => (
                <tr key={`${result.exchange}-${result.ticker}-${result.assetName}`}>
                  <td>{result.ticker}</td>
                  <td>{result.name}</td>
                  <td className="current-values__col--price data-table__col--numeric">
                    {result.price === null ? '—' : formatN2(result.price)}
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
