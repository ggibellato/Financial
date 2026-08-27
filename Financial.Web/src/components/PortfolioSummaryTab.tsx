import type { ReactNode } from 'react'
import { ArrowSortDownRegular, ArrowSortUpRegular } from '@fluentui/react-icons'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import { useSortableRows, type SortAccessor, type SortDirection } from '../hooks/useSortableRows'
import { usePortfolioAssetSummary } from '../hooks/usePortfolioAssetSummary'
import type { RowPriceState } from '../hooks/usePortfolioAssetSummary'
import type { PortfolioAssetSummaryItemDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { formatMonthYear, formatN2, formatN8, formatPercent1, formatShortDate, signClass } from '../utils/formatters'
import AggregatedSummaryTab from './AggregatedSummaryTab'
import './PortfolioSummaryTab.css'

function parseCreditMonth(yearMonth: string): Date {
  const [year, month] = yearMonth.split('-').map(Number)
  return new Date(year, month - 1, 1)
}

function formatCreditMonth(yearMonth: string): string {
  return formatMonthYear(parseCreditMonth(yearMonth))
}

function getProfitClass(value: number): string {
  return signClass(value, 'portfolio-summary__profit')
}

function computeCostBasis(item: PortfolioAssetSummaryItemDto): number {
  return item.currentQuantity * item.averagePrice
}

function computeCurrentValue(
  item: PortfolioAssetSummaryItemDto,
  rowPrice: RowPriceState,
  isHistoric: boolean,
): number | null {
  return !isHistoric && rowPrice.currentPrice !== null ? rowPrice.currentPrice * item.currentQuantity : null
}

// Historic positions are closed: "Profit %" reflects the realized capital gain alone
// (credits excluded, matching the active-scope semantic where credits are a separate
// "w/ Credits" column), while "Profit % w/ Credits" uses the full realized gain/loss.
function computeProfitPercent(
  item: PortfolioAssetSummaryItemDto,
  currentValue: number | null,
  isHistoric: boolean,
): number | null {
  if (isHistoric) {
    return item.totalBought !== 0 ? ((item.realizedGainLoss - item.totalCredits) / item.totalBought) * 100 : null
  }
  const costBasis = computeCostBasis(item)
  return currentValue !== null && costBasis !== 0 ? ((currentValue - costBasis) / costBasis) * 100 : null
}

function computeProfitWithCreditsPercent(
  item: PortfolioAssetSummaryItemDto,
  currentValue: number | null,
  isHistoric: boolean,
): number | null {
  if (isHistoric) {
    return item.totalBought !== 0 ? (item.realizedGainLoss / item.totalBought) * 100 : null
  }
  const costBasis = computeCostBasis(item)
  return currentValue !== null && costBasis !== 0
    ? ((currentValue + item.totalCredits - costBasis) / costBasis) * 100
    : null
}

interface AssetTableRow {
  item: PortfolioAssetSummaryItemDto
  rowPrice: RowPriceState
}

const DEFAULT_ROW_PRICE: RowPriceState = {
  isLoading: false,
  currentPrice: null,
  fetchFailed: false,
  isManual: false,
  xirr: null,
  isLoadingXirr: false,
}

interface SortableHeaderCellProps {
  label: ReactNode
  columnKey: string
  sortDirection?: SortDirection
  onSort: (columnKey: string) => void
  numeric?: boolean
  rowSpan?: number
  className?: string
}

// This table's header spans two <tr> rows (rowSpan cells alongside grouped sub-column
// headers), which the shared SortableColumnHeader has no prop for, and its "Last Month"
// group uses a credits-separator accent class SortableColumnHeader can't accept either.
// This mirrors SortableColumnHeader's markup/classes exactly (so its CSS still applies)
// while supporting rowSpan and an extra className.
function SortableHeaderCell({ label, columnKey, sortDirection, onSort, numeric, rowSpan, className }: SortableHeaderCellProps) {
  return (
    <th
      rowSpan={rowSpan}
      className={['sortable-column-header', numeric ? 'data-table__col--numeric' : '', className ?? '']
        .filter(Boolean)
        .join(' ')}
      aria-sort={sortDirection ?? 'none'}
    >
      <button type="button" className="sortable-column-header__button" onClick={() => onSort(columnKey)}>
        <span className="sortable-column-header__label">{label}</span>
        {sortDirection === 'ascending' && (
          <ArrowSortUpRegular className="sortable-column-header__icon" aria-hidden="true" />
        )}
        {sortDirection === 'descending' && (
          <ArrowSortDownRegular className="sortable-column-header__icon" aria-hidden="true" />
        )}
      </button>
    </th>
  )
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

interface AssetRowProps {
  item: PortfolioAssetSummaryItemDto
  rowPrice: RowPriceState
  isHistoric: boolean
}

function AssetRow({ item, rowPrice, isHistoric }: AssetRowProps) {
  const currentValue = computeCurrentValue(item, rowPrice, isHistoric)
  const profitPercent = computeProfitPercent(item, currentValue, isHistoric)
  const profitWithCreditsPercent = computeProfitWithCreditsPercent(item, currentValue, isHistoric)

  // Solved server-side by POST /xirr/calculate, the same solver the asset tab uses, so a
  // correction to it cannot reach one surface and miss the other.
  const xirrValue = rowPrice.xirr

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
          {!rowPrice.isLoading && !rowPrice.fetchFailed && rowPrice.isManual && (
            <span
              className="portfolio-summary__manual-badge"
              title="This value came from a manually-entered price, not a live fetch."
            >
              {' '}
              (M)
            </span>
          )}
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
        {renderGatedCell(rowPrice.isLoadingXirr, false, xirrValue, v => (
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

  const tableRows: AssetTableRow[] = (items ?? []).map((item, index) => ({
    item,
    rowPrice: rowPrices[index] ?? DEFAULT_ROW_PRICE,
  }))

  const sortAccessors: Record<string, SortAccessor<AssetTableRow>> = {
    assetName: (r) => r.item.assetName,
    firstInvestment: (r) => (r.item.firstInvestmentDate === null ? null : new Date(r.item.firstInvestmentDate)),
    quantity: (r) => r.item.currentQuantity,
    portfolioWeight: (r) => r.item.portfolioWeight,
    totalInvested: (r) => r.item.totalInvested,
    realizedGainLoss: (r) => r.item.realizedGainLoss,
    currentValue: (r) => computeCurrentValue(r.item, r.rowPrice, isHistoric),
    totalCredits: (r) => r.item.totalCredits,
    averagePrice: (r) => r.item.averagePrice,
    price: (r) => (isHistoric ? r.item.averageSellPrice : r.rowPrice.currentPrice),
    profitPercent: (r) => computeProfitPercent(r.item, computeCurrentValue(r.item, r.rowPrice, isHistoric), isHistoric),
    profitWithCreditsPercent: (r) =>
      computeProfitWithCreditsPercent(r.item, computeCurrentValue(r.item, r.rowPrice, isHistoric), isHistoric),
    xirr: (r) => r.rowPrice.xirr,
    lastMonthCredits: (r) => (r.item.lastCreditMonth === null ? null : r.item.lastMonthCredits),
    lastCreditMonth: (r) => (r.item.lastCreditMonth === null ? null : parseCreditMonth(r.item.lastCreditMonth)),
    lastMonthCreditsPercent: (r) => r.item.lastMonthCreditsPercent,
    estimatedAnnualCredits: (r) => r.item.estimatedAnnualCredits,
    estimatedAnnualPercent: (r) => r.item.estimatedAnnualPercent,
  }

  const { sortedRows, sortState, requestSort } = useSortableRows(tableRows, sortAccessors)
  const sortDirectionFor = (columnKey: string): SortDirection | undefined =>
    sortState?.columnKey === columnKey ? sortState.direction : undefined

  const creditsLabel = `Credits ${formatMonthYear(new Date())}`

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
                <SortableHeaderCell rowSpan={2} label="Asset Name" columnKey="assetName" sortDirection={sortDirectionFor('assetName')} onSort={requestSort} />
                <SortableHeaderCell rowSpan={2} label="First Investment" columnKey="firstInvestment" sortDirection={sortDirectionFor('firstInvestment')} onSort={requestSort} />
                <SortableHeaderCell rowSpan={2} numeric label="Quantity" columnKey="quantity" sortDirection={sortDirectionFor('quantity')} onSort={requestSort} />
                <SortableHeaderCell rowSpan={2} numeric label="% Portfolio" columnKey="portfolioWeight" sortDirection={sortDirectionFor('portfolioWeight')} onSort={requestSort} />
                <SortableHeaderCell rowSpan={2} numeric label="Total Invested" columnKey="totalInvested" sortDirection={sortDirectionFor('totalInvested')} onSort={requestSort} />
                {isHistoric && (
                  <SortableHeaderCell rowSpan={2} numeric label="Realized Gain/Loss" columnKey="realizedGainLoss" sortDirection={sortDirectionFor('realizedGainLoss')} onSort={requestSort} />
                )}
                {!isHistoric && (
                  <SortableHeaderCell rowSpan={2} numeric label="Current Value" columnKey="currentValue" sortDirection={sortDirectionFor('currentValue')} onSort={requestSort} />
                )}
                <SortableHeaderCell rowSpan={2} numeric label="Total Credits" columnKey="totalCredits" sortDirection={sortDirectionFor('totalCredits')} onSort={requestSort} />
                <SortableHeaderCell rowSpan={2} numeric label="Average Price" columnKey="averagePrice" sortDirection={sortDirectionFor('averagePrice')} onSort={requestSort} />
                <SortableHeaderCell rowSpan={2} numeric label={isHistoric ? 'Sold Price' : 'Current Price'} columnKey="price" sortDirection={sortDirectionFor('price')} onSort={requestSort} />
                <th colSpan={2} className="portfolio-summary__group-header">Profit</th>
                <SortableHeaderCell rowSpan={2} numeric label="XIRR" columnKey="xirr" sortDirection={sortDirectionFor('xirr')} onSort={requestSort} />
                <th colSpan={3} className="portfolio-summary__group-header portfolio-summary__credits-separator">Last Month</th>
                <th colSpan={2} className="portfolio-summary__group-header">Est. Annual</th>
              </tr>
              <tr>
                <SortableHeaderCell numeric label="%" columnKey="profitPercent" sortDirection={sortDirectionFor('profitPercent')} onSort={requestSort} />
                <SortableHeaderCell numeric label="w/ Credits" columnKey="profitWithCreditsPercent" sortDirection={sortDirectionFor('profitWithCreditsPercent')} onSort={requestSort} />
                <SortableHeaderCell numeric label="Credits" columnKey="lastMonthCredits" sortDirection={sortDirectionFor('lastMonthCredits')} onSort={requestSort} className="portfolio-summary__credits-separator" />
                <SortableHeaderCell numeric label="Month" columnKey="lastCreditMonth" sortDirection={sortDirectionFor('lastCreditMonth')} onSort={requestSort} />
                <SortableHeaderCell numeric label="%" columnKey="lastMonthCreditsPercent" sortDirection={sortDirectionFor('lastMonthCreditsPercent')} onSort={requestSort} />
                <SortableHeaderCell numeric label="Credits" columnKey="estimatedAnnualCredits" sortDirection={sortDirectionFor('estimatedAnnualCredits')} onSort={requestSort} />
                <SortableHeaderCell numeric label="%" columnKey="estimatedAnnualPercent" sortDirection={sortDirectionFor('estimatedAnnualPercent')} onSort={requestSort} />
              </tr>
            </thead>
            <tbody>
              {sortedRows.map((row) => (
                <AssetRow key={row.item.assetName} item={row.item} rowPrice={row.rowPrice} isHistoric={isHistoric} />
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
