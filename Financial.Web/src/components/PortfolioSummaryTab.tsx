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

function formatCreditMonth(yearMonth: string): string {
  const [year, month] = yearMonth.split('-').map(Number)
  const d = new Date(year, month - 1, 1)
  return d.toLocaleDateString('en-GB', { month: 'short', year: 'numeric' })
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
      <td className="portfolio-summary__credits-separator">
        {item.lastCreditMonth === null ? '—' : formatN2(item.lastMonthCredits)}
      </td>
      <td>{item.lastCreditMonth === null ? '—' : formatCreditMonth(item.lastCreditMonth)}</td>
      <td>{item.lastMonthCreditsPercent === null ? '—' : `${formatN2(item.lastMonthCreditsPercent)}%`}</td>
      <td>{item.estimatedAnnualCredits === null ? '—' : formatN2(item.estimatedAnnualCredits)}</td>
      <td>{item.estimatedAnnualPercent === null ? '—' : `${formatN2(item.estimatedAnnualPercent)}%`}</td>
    </tr>
  )
}

function computeCurrentValueFooter(
  items: PortfolioAssetSummaryItemDto[],
  rowPrices: RowPriceState[],
): { display: string; partial: boolean } {
  const anyLoading = rowPrices.some(r => r.isLoading)
  const resolved = items
    .map((item, i) => {
      const rp = rowPrices[i]
      return rp && !rp.isLoading && rp.currentPrice !== null
        ? rp.currentPrice * item.currentQuantity
        : null
    })
    .filter((v): v is number => v !== null)

  if (anyLoading && resolved.length === 0) return { display: 'Calculating…', partial: false }
  const sum = resolved.reduce((acc, v) => acc + v, 0)
  if (anyLoading) return { display: `${formatN2(sum)} *`, partial: true }
  return { display: formatN2(sum), partial: false }
}

export default function PortfolioSummaryTab() {
  const { items, rowPrices, isLoading, error, retry } = usePortfolioAssetSummary()

  const creditsLabel = (() => {
    const now = new Date()
    return `Credits ${now.toLocaleDateString('en-GB', { month: 'short', year: 'numeric' })}`
  })()

  const footer =
    items && items.length > 0
      ? (() => {
          const totalInvested = items.reduce((acc, it) => acc + it.totalInvested, 0)
          const totalCredits = items.reduce((acc, it) => acc + it.totalCredits, 0)
          const currentMonthCredits = items.reduce((acc, it) => acc + it.currentMonthCredits, 0)
          const hasAnyAnnual = items.some(it => it.estimatedAnnualCredits !== null)
          const estAnnualCredits = hasAnyAnnual
            ? items.reduce((acc, it) => acc + (it.estimatedAnnualCredits ?? 0), 0)
            : null
          const cv = computeCurrentValueFooter(items, rowPrices)
          return { totalInvested, totalCredits, currentMonthCredits, estAnnualCredits, cv }
        })()
      : null

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
                <th className="portfolio-summary__credits-separator">Last Month Credits</th>
                <th>Last Credit Month</th>
                <th>Last Month %</th>
                <th>Est. Annual Credits</th>
                <th>Est. Annual %</th>
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

      {footer && (
        <div className="portfolio-summary__footer">
          <div className="portfolio-summary__footer-item">
            <span className="portfolio-summary__footer-label" data-label="Total Invested" />
            <input type="text" readOnly className="portfolio-summary__footer-value" value={formatN2(footer.totalInvested)} tabIndex={-1} />
          </div>
          <div className="portfolio-summary__footer-item">
            <span className="portfolio-summary__footer-label" data-label="Total Credits" />
            <input type="text" readOnly className="portfolio-summary__footer-value" value={formatN2(footer.totalCredits)} tabIndex={-1} />
          </div>
          <div className="portfolio-summary__footer-item">
            <span className="portfolio-summary__footer-label" data-label="Current Value" />
            <input type="text" readOnly className="portfolio-summary__footer-value" value={footer.cv.display} tabIndex={-1} />
            {footer.cv.partial && (
              <span className="portfolio-summary__footer-footnote">excludes assets with pending prices</span>
            )}
          </div>
          <div className="portfolio-summary__footer-item">
            <span className="portfolio-summary__footer-label">{creditsLabel}</span>
            <input type="text" readOnly className="portfolio-summary__footer-value" value={formatN2(footer.currentMonthCredits)} tabIndex={-1} />
          </div>
          <div className="portfolio-summary__footer-item">
            <span className="portfolio-summary__footer-label" data-label="Est. Annual Credits" />
            <input type="text" readOnly className="portfolio-summary__footer-value" value={footer.estAnnualCredits === null ? '—' : formatN2(footer.estAnnualCredits)} tabIndex={-1} />
          </div>
        </div>
      )}
    </div>
  )
}
