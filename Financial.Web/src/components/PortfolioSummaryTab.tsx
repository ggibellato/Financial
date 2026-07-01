import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import { usePortfolioAssetSummary } from '../hooks/usePortfolioAssetSummary'
import type { RowPriceState } from '../hooks/usePortfolioAssetSummary'
import type { PortfolioAssetSummaryItemDto } from '../api/types'
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

  const profitClass =
    profitPercent !== null && profitPercent >= 0
      ? 'portfolio-summary__profit--green'
      : 'portfolio-summary__profit--red'

  return (
    <tr>
      <td>{item.assetName}</td>
      <td>{formatShortDate(item.firstInvestmentDate)}</td>
      <td>{formatN8(item.currentQuantity)}</td>
      <td>{formatN2(item.totalInvested)}</td>
      <td>{formatPortfolioWeight(item.portfolioWeight)}</td>
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
        ) : item.totalInvested === 0 ? (
          '—'
        ) : (
          <span className={profitClass}>{formatN2(profitPercent)}%</span>
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
                <th>Current Value</th>
                <th>% Profit</th>
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
