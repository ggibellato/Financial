import { type FormEvent, useCallback, useMemo, useState } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { DividendHistoryItemDto, DividendSummaryDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'

const defaultExchange = 'BVMF'

export default function DividendCheckPage() {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [ticker, setTicker] = useState('')
  const [exchange, setExchange] = useState(defaultExchange)
  const [summary, setSummary] = useState<DividendSummaryDto | null>(null)
  const [history, setHistory] = useState<DividendHistoryItemDto[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const formatter = useMemo(
    () =>
      new Intl.NumberFormat(undefined, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }),
    [],
  )

  const formatNumber = useCallback((value: number) => formatter.format(value), [formatter])
  const formatDate = useCallback((value: string) => {
    const parsed = new Date(value)
    if (Number.isNaN(parsed.getTime())) {
      return value
    }
    return parsed.toLocaleDateString()
  }, [])

  const runCheck = useCallback(async () => {
    const trimmedTicker = ticker.trim().toUpperCase()
    const trimmedExchange = exchange.trim().toUpperCase()
    if (!trimmedTicker) {
      setError('Ticker is required.')
      return
    }

    setIsLoading(true)
    setError(null)
    try {
      const [summaryData, historyData] = await Promise.all([
        apiClient.getDividendSummary(trimmedTicker, trimmedExchange || undefined),
        apiClient.getDividendHistory(trimmedTicker, trimmedExchange || undefined),
      ])
      setSummary(summaryData)
      setHistory(historyData)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unable to load dividend data.'
      setError(message)
    } finally {
      setIsLoading(false)
    }
  }, [apiClient, exchange, ticker])

  const handleSubmit = useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault()
      void runCheck()
    },
    [runCheck],
  )

  const summaryName = summary ? `${summary.ticker} - ${summary.name}` : 'Select a ticker and click Check'
  const summaryColor =
    summary && summary.priceMaxBuy > 0 && summary.currentPrice < summary.priceMaxBuy
      ? 'summary-value--positive'
      : 'summary-value--negative'

  return (
    <section className="dividend-check">
      <header className="dividend-check__header">
        <h2>Shares Dividend Check</h2>
        <p>Review dividend history and estimate target entry price.</p>
      </header>
      <form className="dividend-check__form" onSubmit={handleSubmit} aria-label="Dividend check">
        <label className="dividend-check__field">
          <span>Ticker</span>
          <input
            type="text"
            value={ticker}
            onChange={(event) => setTicker(event.target.value)}
            placeholder="e.g. BCIA11"
            required
          />
        </label>
        <label className="dividend-check__field">
          <span>Exchange</span>
          <input
            type="text"
            value={exchange}
            onChange={(event) => setExchange(event.target.value)}
            placeholder={defaultExchange}
          />
        </label>
        <button type="submit" disabled={isLoading}>
          {isLoading ? 'Checking...' : 'Check'}
        </button>
      </form>

      {isLoading ? <LoadingState message="Loading dividend data..." /> : null}
      {error ? <ErrorState message={error} onRetry={runCheck} /> : null}

      {summary ? (
        <>
          <section className="dividend-check__summary">
            <h3>{summaryName}</h3>
            <div className="summary-grid">
              <div>
                <p className="summary-label">Current price</p>
                <p className="summary-value">{formatNumber(summary.currentPrice)}</p>
              </div>
              <div>
                <p className="summary-label">Average Dividend</p>
                <p className="summary-value">{formatNumber(summary.averageDividendLastFiveYears)} (last 5 years)</p>
              </div>
              <div>
                <p className="summary-label">Price max buy</p>
                <p className={`summary-value ${summaryColor}`}>
                  {formatNumber(summary.priceMaxBuy)} · Discount {formatNumber(summary.discountPercent)}%
                </p>
              </div>
            </div>
          </section>

          <section className="dividend-check__tables">
            <div>
              <h3>Dividend History</h3>
              {history.length === 0 ? (
                <p>No dividend history found.</p>
              ) : (
                <table>
                  <thead>
                    <tr>
                      <th>Date</th>
                      <th>Type</th>
                      <th className="table-number">Value</th>
                    </tr>
                  </thead>
                  <tbody>
                    {history.map((item) => (
                      <tr key={`${item.date}-${item.type}-${item.value}`}>
                        <td>{formatDate(item.date)}</td>
                        <td>{item.type}</td>
                        <td className="table-number">{formatNumber(item.value)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
            <div>
              <h3>Dividends by Year</h3>
              {summary.yearTotals.length === 0 ? (
                <p>No yearly totals available.</p>
              ) : (
                <table>
                  <thead>
                    <tr>
                      <th>Year</th>
                      <th className="table-number">Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    {summary.yearTotals.map((total) => (
                      <tr key={total.year}>
                        <td>{total.year}</td>
                        <td className="table-number">{formatNumber(total.total)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </section>
        </>
      ) : (
        <p className="dividend-check__placeholder">{summaryName}</p>
      )}
    </section>
  )
}
