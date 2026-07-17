import type { ReactNode } from 'react'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import { usePortfolioAssetSummary } from '../hooks/usePortfolioAssetSummary'
import type { RowPriceState } from '../hooks/usePortfolioAssetSummary'
import type { PortfolioAssetSummaryItemDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { xirr } from '../utils/xirr'
import { formatN2, formatN8, formatPercent1, formatShortDate } from '../utils/formatters'
import AggregatedSummaryTab from './AggregatedSummaryTab'
import './PortfolioSummaryTab.css'

function formatCreditMonth(yearMonth: string): string {
  const [year, month] = yearMonth.split('-').map(Number)
  const d = new Date(year, month - 1, 1)
  return d.toLocaleDateString('en-GB', { month: 'short', year: 'numeric' })
}

function getProfitClass(value: number): string {
  return value >= 0 ? 'portfolio-summary__profit--green' : 'portfolio-summary__profit--red'
}

function renderGatedCell(
  loading: boolean,
  unavailable: boolean,
  value: number | null,
  render: (v: number) => ReactNode,
) {
  if (loading) return <span className="portfolio-summary__loading-cell">...</span>
  if (unavailable || value === null) return '—'
  return render(value)
}

function computeXirr(cashFlows: PortfolioAssetSummaryItemDto['cashFlows'], terminalValue: number): number | null {
  const series = [
    ...cashFlows.map(cf => ({ date: new Date(cf.date), amount: cf.amount })),
    { date: new Date(), amount: terminalValue },
  ].sort((a, b) => a.date.getTime() - b.date.getTime())
  return xirr(series)
}

interface AssetRowProps {
  item: PortfolioAssetSummaryItemDto
  rowPrice: RowPriceState
  isHistoric: boolean
}

function AssetRow({ item, rowPrice, isHistoric }: AssetRowProps) {
  const currentValue =
    !isHistoric && rowPrice.currentPrice !== null ? rowPrice.currentPrice * item.currentQuantity : null

  const costBasis = item.currentQuantity * item.averagePrice

  // Historic positions are closed: "Profit %" reflects the realized capital gain alone
  // (credits excluded, matching the active-scope semantic where credits are a separate
  // "w/ Credits" column), while "Profit % w/ Credits" uses the full realized gain/loss.
  const profitPercent = isHistoric
    ? item.totalBought !== 0
      ? ((item.realizedGainLoss - item.totalCredits) / item.totalBought) * 100
      : null
    : currentValue !== null && costBasis !== 0
      ? ((currentValue - costBasis) / costBasis) * 100
      : null

  const profitWithCreditsPercent = isHistoric
    ? item.totalBought !== 0
      ? (item.realizedGainLoss / item.totalBought) * 100
      : null
    : currentValue !== null && costBasis !== 0
      ? ((currentValue + item.totalCredits - costBasis) / costBasis) * 100
      : null

  // Historic cash flows already contain every buy/sell/credit as a dated entry, so the
  // terminal value is 0 (no remaining position left to mark-to-market).
  const xirrValue = isHistoric
    ? computeXirr(item.cashFlows, 0)
    : currentValue !== null
      ? computeXirr(item.cashFlows, currentValue)
      : null

  const priceValue = isHistoric ? item.averageSellPrice : rowPrice.currentPrice
  const cellLoading = !isHistoric && rowPrice.isLoading
  const cellUnavailable = !isHistoric && rowPrice.fetchFailed

  return (
    <tr>
      <td>{item.assetName}</td>
      <td>{formatShortDate(item.firstInvestmentDate)}</td>
      <td>{formatN8(item.currentQuantity)}</td>
      <td>{formatPercent1(item.portfolioWeight)}</td>
      <td>{formatN2(item.totalInvested)}</td>
      {isHistoric && (
        <td>
          <span className={getProfitClass(item.realizedGainLoss)}>{formatN2(item.realizedGainLoss)}</span>
        </td>
      )}
      {!isHistoric && (
        <td>
          {renderGatedCell(rowPrice.isLoading, rowPrice.fetchFailed, currentValue, v => formatN2(v))}
        </td>
      )}
      <td>{formatN2(item.totalCredits)}</td>
      <td>{formatN2(item.averagePrice)}</td>
      <td>{renderGatedCell(cellLoading, cellUnavailable, priceValue, v => formatN2(v))}</td>
      <td>
        {renderGatedCell(cellLoading, cellUnavailable, profitPercent, v => (
          <span className={getProfitClass(v)}>{formatN2(v)}%</span>
        ))}
      </td>
      <td>
        {renderGatedCell(cellLoading, cellUnavailable, profitWithCreditsPercent, v => (
          <span className={getProfitClass(v)}>{formatN2(v)}%</span>
        ))}
      </td>
      <td>
        {renderGatedCell(cellLoading, cellUnavailable, xirrValue, v => (
          <span className={getProfitClass(v)}>{formatN2(v * 100)}%</span>
        ))}
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
  if (!anyLoading && resolved.length === 0) return { display: '—', partial: false }
  const sum = resolved.reduce((acc, v) => acc + v, 0)
  if (anyLoading) return { display: `${formatN2(sum)} *`, partial: true }
  return { display: formatN2(sum), partial: false }
}

export default function PortfolioSummaryTab() {
  const { scope } = useSelectedNode()
  const isHistoric = scope === 'historic'
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
          const realizedGainLoss = items.reduce((acc, it) => acc + it.realizedGainLoss, 0)
          const cv = computeCurrentValueFooter(items, rowPrices)
          return { totalInvested, totalCredits, currentMonthCredits, estAnnualCredits, realizedGainLoss, cv }
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
          <table className="portfolio-summary__table data-table">
            <thead>
              <tr>
                <th rowSpan={2}>Asset Name</th>
                <th rowSpan={2}>First Investment</th>
                <th rowSpan={2}>Quantity</th>
                <th rowSpan={2}>% Portfolio</th>
                <th rowSpan={2}>Total Invested</th>
                {isHistoric && <th rowSpan={2}>Realized Gain/Loss</th>}
                {!isHistoric && <th rowSpan={2}>Current Value</th>}
                <th rowSpan={2}>Total Credits</th>
                <th rowSpan={2}>Average Price</th>
                <th rowSpan={2}>{isHistoric ? 'Sold Price' : 'Current Price'}</th>
                <th colSpan={2} className="portfolio-summary__group-header">Profit</th>
                <th rowSpan={2}>XIRR</th>
                <th colSpan={3} className="portfolio-summary__group-header portfolio-summary__credits-separator">Last Month</th>
                <th colSpan={2} className="portfolio-summary__group-header">Est. Annual</th>
              </tr>
              <tr>
                <th>%</th>
                <th>w/ Credits</th>
                <th className="portfolio-summary__credits-separator">Credits</th>
                <th>Month</th>
                <th>%</th>
                <th>Credits</th>
                <th>%</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item, index) => (
                <AssetRow
                  key={item.assetName}
                  item={item}
                  rowPrice={rowPrices[index] ?? { isLoading: false, currentPrice: null, fetchFailed: false }}
                  isHistoric={isHistoric}
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
          {isHistoric && (
            <div className="portfolio-summary__footer-item">
              <span className="portfolio-summary__footer-label" data-label="Realized Gain/Loss" />
              <input type="text" readOnly className="portfolio-summary__footer-value" value={formatN2(footer.realizedGainLoss)} tabIndex={-1} />
            </div>
          )}
          {!isHistoric && (
            <div className="portfolio-summary__footer-item">
              <span className="portfolio-summary__footer-label" data-label="Current Value" />
              <input type="text" readOnly className="portfolio-summary__footer-value" value={footer.cv.display} tabIndex={-1} />
              {footer.cv.partial && (
                <span className="portfolio-summary__footer-footnote">excludes assets with pending prices</span>
              )}
            </div>
          )}
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
