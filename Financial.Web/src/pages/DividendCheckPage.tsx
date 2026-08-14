import { type FormEvent, useCallback, useEffect, useMemo, useState } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { DividendHistoryItemDto, DividendSummaryDto, WatchlistItemDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import TickerCombobox, { type TickerGroup } from '../components/TickerCombobox'
import { formatN2, formatShortDateUtc, getErrorMessage } from '../utils/formatters'
import './DividendCheckPage.css'

const FIXED_EXCHANGE = 'BVMF'

function toTickerGroups(items: WatchlistItemDto[]): TickerGroup[] {
  const map = new Map<string, string[]>()
  for (const item of items) {
    const tickers = map.get(item.group) ?? []
    tickers.push(item.name)
    map.set(item.group, tickers)
  }
  return Array.from(map, ([label, tickers]) => ({ label, tickers }))
}

export default function DividendCheckPage() {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [groups, setGroups] = useState<TickerGroup[]>([])
  const [ticker, setTicker] = useState('')

  useEffect(() => {
    void apiClient.getWatchlist().then((items) => {
      setGroups(toTickerGroups(items))
      setTicker((prev) => (prev === '' && items.length > 0 ? items[0].name : prev))
    })
  }, [apiClient])
  const [summary, setSummary] = useState<DividendSummaryDto | null>(null)
  const [history, setHistory] = useState<DividendHistoryItemDto[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const runCheck = useCallback(async () => {
    const trimmedTicker = ticker.trim().toUpperCase()
    if (!trimmedTicker) {
      setError('Ticker is required.')
      return
    }

    setIsLoading(true)
    setError(null)
    try {
      const [summaryData, historyData] = await Promise.all([
        apiClient.getDividendSummary(trimmedTicker, FIXED_EXCHANGE),
        apiClient.getDividendHistory(trimmedTicker, FIXED_EXCHANGE),
      ])
      setSummary(summaryData)
      setHistory(historyData)
    } catch (err) {
      const message = getErrorMessage(err, 'Unable to load dividend data.')
      setError(message)
      setSummary(null)
      setHistory([])
    } finally {
      setIsLoading(false)
    }
  }, [apiClient, ticker])

  const handleSubmit = useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault()
      void runCheck()
    },
    [runCheck],
  )

  const sortedHistory = useMemo(
    () => [...history].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()),
    [history],
  )

  const sortedYearTotals = useMemo(
    () => [...(summary?.yearTotals ?? [])].sort((a, b) => b.year - a.year),
    [summary],
  )

  const priceMaxBuyClass =
    summary && summary.priceMaxBuy > 0 && summary.currentPrice < summary.priceMaxBuy
      ? 'summary-card__price-max--positive'
      : 'summary-card__price-max--negative'

  return (
    <section className="dividend-check">
      <header className="dividend-check__header">
        <h2>Shares Dividend Check</h2>
        <p>Review dividend history and estimate target entry price.</p>
      </header>
      <form className="dividend-check__form" onSubmit={handleSubmit} aria-label="Dividend check">
        <TickerCombobox groups={groups} value={ticker} onChange={setTicker} />
        <button type="submit" disabled={isLoading}>
          {isLoading ? 'Checking...' : 'Check'}
        </button>
      </form>

      {error ? <ErrorState message={error} onRetry={runCheck} /> : null}

      {summary ? (
        <>
          <section className="dividend-check__summary-card">
            <p className="summary-card__title">
              {summary.ticker} - {summary.name}
            </p>
            <p>Current price: {formatN2(summary.currentPrice)}</p>
            <p className="summary-card__avg-dividend">
              Average Dividend: {formatN2(summary.averageDividendLastFiveYears)} (last 5 years) — Yield: {formatN2(summary.dividendYieldPercent)}%
            </p>
            <p className={`summary-card__price-max ${priceMaxBuyClass}`}>
              Price max buy: {formatN2(summary.priceMaxBuy)}&nbsp;&nbsp;&nbsp;Discount{' '}
              {formatN2(summary.discountPercent)}%
            </p>
          </section>

          <section className="dividend-check__tables">
            <div className="dividend-check__table-column">
              <h3>Dividend History</h3>
              {sortedHistory.length === 0 ? (
                <p>No dividend history found.</p>
              ) : (
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Type</th>
                      <th>Date</th>
                      <th className="data-table__col--numeric">Value</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sortedHistory.map((item) => (
                      <tr key={`${item.date}-${item.type}-${item.value}`}>
                        <td>{item.type}</td>
                        <td>{formatShortDateUtc(item.date)}</td>
                        <td className="data-table__col--numeric">{formatN2(item.value)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
            <div className="dividend-check__table-column">
              <h3>By Year</h3>
              {sortedYearTotals.length === 0 ? (
                <p>No annual totals available.</p>
              ) : (
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Year</th>
                      <th className="data-table__col--numeric">Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sortedYearTotals.map((total) => (
                      <tr key={total.year}>
                        <td>{total.year}</td>
                        <td className="data-table__col--numeric">{formatN2(total.total)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </section>
        </>
      ) : null}

      {!summary && !error ? (
        <p className="dividend-check__placeholder">Select a ticker and click Check</p>
      ) : null}
    </section>
  )
}
