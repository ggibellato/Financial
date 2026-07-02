import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import { usePortfolioAssetSummary } from '../hooks/usePortfolioAssetSummary'
import type { RowPriceState } from '../hooks/usePortfolioAssetSummary'
import type { PortfolioAssetSummaryItemDto } from '../api/types'
import { xirr } from '../utils/xirr'
import AggregatedSummaryTab from './AggregatedSummaryTab'
import './PortfolioSummaryTab.css'

function formatN2(value: number): string {
  return new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

function formatN8(value: number): string {
  return new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 8,
    maximumFractionDigits: 8,
  }).format(value)
}

function formatPortfolioWeight(value: number): string {
  return `${new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  }).format(value)}%`
}

function formatShortDate(isoString: string | null): string {
  if (!isoString) return ''
  const d = new Date(isoString)
  if (Number.isNaN(d.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`
}

function getProfitClass(value: number): string {
  return value >= 0 ? 'portfolio-summary__profit--green' : 'portfolio-summary__profit--red'
}

interface AssetRowProps {
  item: PortfolioAssetSummaryItemDto
  rowPrice: RowPriceState
}

function AssetRow({ item, rowPrice }: AssetRowProps) {
  const currentValue =
    rowPrice.currentPrice !== null ? rowPrice.currentPrice * item.currentQuantity : null

  const profitPercent =
    currentValue !== null && item.totalInvested !== 0
      ? ((currentValue - item.totalInvested) / item.totalInvested) * 100
      : null

  const profitWithCreditsPercent =
    currentValue !== null && item.totalInvested !== 0
      ? ((currentValue + item.totalCredits - item.totalInvested) / item.totalInvested) * 100
      : null

  const xirrValue = (() => {
    if (currentValue === null) return null
    const today = new Date()
    const series = [
      ...item.cashFlows.map(cf => ({ date: new Date(cf.date), amount: cf.amount })),
      { date: today, amount: currentValue },
    ].sort((a, b) => a.date.getTime() - b.date.getTime())
    return xirr(series)
  })()

  return (
    <tr>
      <td>{item.assetName}</td>
      <td>{formatShortDate(item.firstInvestmentDate)}</td>
      <td>{formatN8(item.currentQuantity)}</td>
      <td>{formatN2(item.totalInvested)}</td>
      <td>{formatPortfolioWeight(item.portfolioWeight)}</td>
      <td>{formatN2(item.totalCredits)}</td>
      <td>
        {rowPrice.isLoading ? (
          <span className="portfolio-summary__loading-cell">...</span>
        ) : rowPrice.fetchFailed || currentValue === null ? (
          '—'
        ) : (
          formatN2(currentValue)
        )}
      </td>
      <td>
        {rowPrice.isLoading ? (
          <span className="portfolio-summary__loading-cell">...</span>
        ) : rowPrice.fetchFailed || profitPercent === null ? (
          '—'
        ) : (
          <span className={getProfitClass(profitPercent)}>{formatN2(profitPercent)}%</span>
        )}
      </td>
      <td>
        {rowPrice.isLoading ? (
          <span className="portfolio-summary__loading-cell">...</span>
        ) : rowPrice.fetchFailed || profitWithCreditsPercent === null ? (
          '—'
        ) : (
          <span className={getProfitClass(profitWithCreditsPercent)}>
            {formatN2(profitWithCreditsPercent)}%
          </span>
        )}
      </td>
      <td>
        {rowPrice.isLoading ? (
          <span className="portfolio-summary__loading-cell">...</span>
        ) : rowPrice.fetchFailed || xirrValue === null ? (
          '—'
        ) : (
          <span className={getProfitClass(xirrValue)}>{formatN2(xirrValue * 100)}%</span>
        )}
      </td>
    </tr>
  )
}

export default function PortfolioSummaryTab() {
  const { items, rowPrices, isLoading, error, retry } = usePortfolioAssetSummary()

  return (
    <div className="portfolio-summary">
      <div className="portfolio-summary__totals">
        <AggregatedSummaryTab />
      </div>

      <div className="portfolio-summary__table-section">
        {isLoading && <LoadingState />}
        {error && <ErrorState message={error} onRetry={retry} />}
        {!isLoading && !error && items && (
          <table className="portfolio-summary__table">
            <thead>
              <tr>
                <th>Asset Name</th>
                <th>First Investment</th>
                <th>Quantity</th>
                <th>Total Invested</th>
                <th>% Portfolio</th>
                <th>Total Credits</th>
                <th>Current Value</th>
                <th>% Profit</th>
                <th>% Profit w/ Credits</th>
                <th>XIRR</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item, index) => (
                <AssetRow
                  key={item.assetName}
                  item={item}
                  rowPrice={rowPrices[index] ?? { isLoading: false, currentPrice: null, fetchFailed: false }}
                />
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
