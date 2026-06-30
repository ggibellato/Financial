import { type FormEvent, useCallback, useMemo, useState } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { DividendHistoryItemDto, DividendSummaryDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import TickerCombobox, { type TickerGroup } from '../components/TickerCombobox'
import './DividendCheckPage.css'

const WATCHLIST_GROUPS: TickerGroup[] = [
  { label: 'Ja possuidas', tickers: ['KLBN4', 'TASA4', 'TAEE3'] },
  { label: 'Outras Barse', tickers: ['UNIP6', 'CMIG4', 'TRPL4', 'BBAS3'] },
  { label: 'Outras', tickers: ['CSAN3'] },
]

const DEFAULT_TICKER = 'KLBN4'
const FIXED_EXCHANGE = 'BVMF'

export default function DividendCheckPage() {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [ticker, setTicker] = useState(DEFAULT_TICKER)
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

  const formatDate = useCallback((value: string): string => {
    const d = new Date(value)
    if (Number.isNaN(d.getTime())) return value
    const day = String(d.getUTCDate()).padStart(2, '0')
    const month = String(d.getUTCMonth() + 1).padStart(2, '0')
    const year = d.getUTCFullYear()
    return `${day}/${month}/${year}`
  }, [])

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
      const message = err instanceof Error ? err.message : 'Unable to load dividend data.'
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
        <TickerCombobox groups={WATCHLIST_GROUPS} value={ticker} onChange={setTicker} />
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
            <p>Current price: {formatNumber(summary.currentPrice)}</p>
            <p className="summary-card__avg-dividend">
              Average Dividend: {formatNumber(summary.averageDividendLastFiveYears)} (last 5 years) — Yield: {formatNumber(summary.dividendYieldPercent)}%
            </p>
            <p className={`summary-card__price-max ${priceMaxBuyClass}`}>
              Price max buy: {formatNumber(summary.priceMaxBuy)}&nbsp;&nbsp;&nbsp;Discount{' '}
              {formatNumber(summary.discountPercent)}%
            </p>
          </section>

          <section className="dividend-check__tables">
            <div className="dividend-check__table-column">
              <h3>Dividend History</h3>
              {sortedHistory.length === 0 ? (
                <p>No dividend history found.</p>
              ) : (
                <table>
                  <thead>
                    <tr>
                      <th>Type</th>
                      <th>Date</th>
                      <th className="table-number">Value</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sortedHistory.map((item) => (
                      <tr key={`${item.date}-${item.type}-${item.value}`}>
                        <td>{item.type}</td>
                        <td>{formatDate(item.date)}</td>
                        <td className="table-number">{formatNumber(item.value)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
            <div className="dividend-check__table-column">
              <h3>By Year</h3>
              {sortedYearTotals.length === 0 ? (
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
                    {sortedYearTotals.map((total) => (
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
      ) : null}

      {!summary && !error ? (
        <p className="dividend-check__placeholder">Select a ticker and click Check</p>
      ) : null}
    </section>
  )
}
