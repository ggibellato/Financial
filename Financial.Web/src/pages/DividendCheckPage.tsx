import { type FormEvent, useCallback, useId, useMemo, useRef, useState } from 'react'
import { Button, Table, TableBody, TableHeader, TableRow } from '@fluentui/react-components'
import { apiClient } from '../api/financialApiClient'
import type { DividendHistoryItemDto, DividendSummaryDto, DividendYearTotalDto, WatchlistItemDto } from '../api/types'
import EmptyState from '../components/EmptyState'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import TickerCombobox, { type TickerGroup } from '../components/TickerCombobox'
import DataTableCell from '../components/grid/DataTableCell'
import SortableColumnHeader from '../components/grid/SortableColumnHeader'
import { useAsyncResource } from '../hooks/useAsyncResource'
import { orMissing } from '../utils/orMissing'
import { useFocusReloadedContent } from '../hooks/useFocusReloadedContent'
import { useSortableRows, DATE_DESC_SORT, type SortAccessor } from '../hooks/useSortableRows'
import { formatCurrency, formatPercentN2, formatShortDateUtc, getErrorMessage } from '../utils/formatters'
import './DividendCheckPage.css'

const FIXED_EXCHANGE = 'BVMF'
const formatBrl = (value: number) => formatCurrency(value, 'BRL')

function toTickerGroups(items: WatchlistItemDto[]): TickerGroup[] {
  const map = new Map<string, string[]>()
  for (const item of items) {
    const tickers = map.get(item.group) ?? []
    tickers.push(item.name)
    map.set(item.group, tickers)
  }
  return Array.from(map, ([label, tickers]) => ({ label, tickers }))
}

function describeMaxBuy({ priceMaxBuy, discountPercent, currentPrice }: DividendSummaryDto) {
  if (priceMaxBuy == null) return { className: undefined, text: 'no dividends to set a max buy' }
  if (discountPercent == null) return { className: undefined, text: 'no current price to compare' }
  return currentPrice < priceMaxBuy
    ? { className: 'summary-card__price-max--positive', text: 'current price below max buy' }
    : { className: 'summary-card__price-max--negative', text: 'current price not below max buy' }
}

export default function DividendCheckPage() {
  const historyHeadingId = useId()
  const yearHeadingId = useId()
  const tickerRef = useRef<HTMLInputElement>(null)
  const tickerRegionRef = useRef<HTMLDivElement>(null)
  const watchlist = useAsyncResource(() => apiClient.getWatchlist(), [], 'Unable to load the watchlist.', {
    initialLoading: true,
  })
  useFocusReloadedContent(tickerRegionRef, watchlist.isLoading)
  const groups = useMemo(() => toTickerGroups(watchlist.data ?? []), [watchlist.data])
  const [typedTicker, setTypedTicker] = useState<string | null>(null)
  const ticker = typedTicker ?? watchlist.data?.[0]?.name ?? ''
  const [tickerError, setTickerError] = useState<string>()
  const [summary, setSummary] = useState<DividendSummaryDto | null>(null)
  const [history, setHistory] = useState<DividendHistoryItemDto[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const runCheck = useCallback(async () => {
    const trimmedTicker = ticker.trim().toUpperCase()
    if (!trimmedTicker) {
      setTickerError('Enter a ticker to check.')
      tickerRef.current?.focus()
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
  }, [ticker])

  const handleSubmit = useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault()
      if (!isLoading) void runCheck()
    },
    [isLoading, runCheck],
  )

  const historyAccessors: Record<string, SortAccessor<DividendHistoryItemDto>> = {
    type: (item) => item.type,
    date: (item) => new Date(item.date),
    value: (item) => item.value,
  }
  const { sortedRows: displayedHistory, sortState: historySortState, requestSort: requestHistorySort } =
    useSortableRows(history, historyAccessors, DATE_DESC_SORT)

  const yearTotalsAccessors: Record<string, SortAccessor<DividendYearTotalDto>> = {
    year: (item) => item.year,
    total: (item) => item.total,
  }
  const { sortedRows: displayedYearTotals, sortState: yearSortState, requestSort: requestYearSort } =
    useSortableRows(summary?.yearTotals ?? [], yearTotalsAccessors, { columnKey: 'year', direction: 'descending' })

  const changeTicker = useCallback((value: string) => {
    setTypedTicker(value)
    setTickerError(undefined)
  }, [])

  const isWatchlistEmpty = watchlist.data?.length === 0


  return (
    <section className="dividend-check">
      <header className="dividend-check__header">
        <h2>Shares dividend check</h2>
        <p>Review dividend history and estimate target entry price for B3 (BVMF) shares, in BRL.</p>
      </header>
      <div ref={tickerRegionRef} className="dividend-check__ticker">
        <form className="dividend-check__form" onSubmit={handleSubmit} aria-label="Dividend check" noValidate>
          <TickerCombobox
            ref={tickerRef}
            groups={groups}
            value={ticker}
            onChange={changeTicker}
            placeholder={watchlist.isLoading ? 'Loading watchlist…' : undefined}
            hint={isWatchlistEmpty ? 'Your watchlist is empty. Type a ticker to check it.' : undefined}
            validationMessage={tickerError}
          />
          <Button type="submit" appearance="primary" disabledFocusable={isLoading}>
            {isLoading ? 'Checking…' : 'Check dividends'}
          </Button>
        </form>

        {watchlist.error ? <ErrorState message={watchlist.error} onRetry={watchlist.retry} /> : null}
      </div>

      <div className="dividend-check__results">
        {isLoading ? (
          <LoadingState message="Checking dividends…" />
        ) : error ? (
          <ErrorState message={error} onRetry={runCheck} />
        ) : summary ? (
          <>
            <section className="dividend-check__summary-card">
              <p className="summary-card__title">
                {summary.ticker} - {summary.name}
              </p>
              <p>Current price: <strong>{formatBrl(summary.currentPrice)}</strong></p>
              <p className="summary-card__avg-dividend">
                Average dividend: <strong>{formatBrl(summary.averageDividendLastFiveYears)}</strong> (last 5 years) — Yield: <strong>{orMissing(summary.dividendYieldPercent, formatPercentN2, 'Not available: no current price')}</strong>
              </p>
              <p className={describeMaxBuy(summary).className}>
                Price max buy: <strong>{orMissing(summary.priceMaxBuy, formatBrl, 'Not set: no dividends')}</strong> — Discount <strong>{orMissing(summary.discountPercent, formatPercentN2, 'Not available: no max buy or current price')}</strong> —{' '}
                {describeMaxBuy(summary).text}
              </p>
            </section>

            <section className="dividend-check__tables">
              <div className="dividend-check__table-column">
                <h3 id={historyHeadingId}>Dividend history</h3>
                {displayedHistory.length === 0 ? (
                  <EmptyState>No dividend history found.</EmptyState>
                ) : (
                  <Table className="data-table" aria-labelledby={historyHeadingId}>
                    <TableHeader>
                      <TableRow>
                        <SortableColumnHeader
                          label="Type"
                          columnKey="type"
                          sortDirection={historySortState?.columnKey === 'type' ? historySortState.direction : undefined}
                          onSort={requestHistorySort}
                        />
                        <SortableColumnHeader
                          label="Date"
                          columnKey="date"
                          sortDirection={historySortState?.columnKey === 'date' ? historySortState.direction : undefined}
                          onSort={requestHistorySort}
                        />
                        <SortableColumnHeader
                          label="Value"
                          columnKey="value"
                          numeric
                          sortDirection={historySortState?.columnKey === 'value' ? historySortState.direction : undefined}
                          onSort={requestHistorySort}
                        />
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {displayedHistory.map((item) => (
                        <TableRow key={`${item.date}-${item.type}-${item.value}`}>
                          <DataTableCell label="Type">{item.type}</DataTableCell>
                          <DataTableCell label="Date">{formatShortDateUtc(item.date)}</DataTableCell>
                          <DataTableCell label="Value" className="data-table__col--numeric">
                            {formatBrl(item.value)}
                          </DataTableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </div>
              <div className="dividend-check__table-column">
                <h3 id={yearHeadingId}>By year</h3>
                {displayedYearTotals.length === 0 ? (
                  <EmptyState>No annual totals available.</EmptyState>
                ) : (
                  <Table className="data-table" aria-labelledby={yearHeadingId}>
                    <TableHeader>
                      <TableRow>
                        <SortableColumnHeader
                          label="Year"
                          columnKey="year"
                          sortDirection={yearSortState?.columnKey === 'year' ? yearSortState.direction : undefined}
                          onSort={requestYearSort}
                        />
                        <SortableColumnHeader
                          label="Total"
                          columnKey="total"
                          numeric
                          sortDirection={yearSortState?.columnKey === 'total' ? yearSortState.direction : undefined}
                          onSort={requestYearSort}
                        />
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {displayedYearTotals.map((total) => (
                        <TableRow key={total.year}>
                          <DataTableCell label="Year">{total.year}</DataTableCell>
                          <DataTableCell label="Total" className="data-table__col--numeric">
                            {formatBrl(total.total)}
                          </DataTableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </div>
            </section>
          </>
        ) : (
          <p className="dividend-check__placeholder">Choose a ticker, then Check dividends.</p>
        )}
      </div>
    </section>
  )
}
