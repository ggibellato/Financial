import type { ReactNode } from 'react'
import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import MissingValue from './MissingValue'
import PriceBadge from './PriceBadge'
import TruncatedText from './TruncatedText'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useSortableRows, type SortAccessor, type SortDirection } from '../hooks/useSortableRows'
import { usePortfolioAssetSummary } from '../hooks/usePortfolioAssetSummary'
import type { RowPriceState } from '../hooks/usePortfolioAssetSummary'
import { useAggregatedSummary } from '../hooks/useAggregatedSummary'
import type { PortfolioAssetSummaryItemDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { formatMonthYear, formatN2, formatN8, formatPercentN2, formatShortDate, signClass } from '../utils/formatters'
import { orMissing } from '../utils/orMissing'
import { formatCreditMonth, parseCreditMonth } from '../utils/creditMonth'
import './PortfolioHoldingsTab.css'

function getProfitClass(value: number): string {
  return signClass(value, 'portfolio-holdings__profit')
}

function computeCurrentValue(item: PortfolioAssetSummaryItemDto, isHistoric: boolean): number | null {
  return isHistoric ? null : item.marketValue
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
}

const LOADING_ANNOUNCED = { role: 'img', 'aria-label': 'Loading' } as const
const LOADING_SILENT = { 'aria-hidden': true } as const

function renderGatedCell(
  loading: boolean,
  unavailable: boolean,
  value: number | null,
  render: (v: number) => ReactNode,
  { announcesLoading = false } = {},
) {
  if (loading) {
    return (
      <span className="portfolio-holdings__loading-cell" {...(announcesLoading ? LOADING_ANNOUNCED : LOADING_SILENT)}>
        …
      </span>
    )
  }
  if (unavailable) return <MissingValue />
  return orMissing(value, render)
}

const MANUAL_PRICE_MEANING = 'This value came from a manually-entered price, not a live fetch.'
const STALE_PRICE_MEANING = 'This price is older than the most recent weekday.'
const NO_PRICE_MEANING = 'No price or value has been recorded for this holding yet.'

function FooterTotal({ label, value, children }: { label: string; value: ReactNode; children?: ReactNode }) {
  return (
    <div className="portfolio-holdings__footer-item">
      <span className="portfolio-holdings__footer-label">{label}</span>
      <strong className="portfolio-holdings__footer-value">{value}</strong>
      {children}
    </div>
  )
}

interface AssetRowProps {
  item: PortfolioAssetSummaryItemDto
  rowPrice: RowPriceState
  isHistoric: boolean
}

function AssetRow({ item, rowPrice, isHistoric }: AssetRowProps) {
  const currentValue = computeCurrentValue(item, isHistoric)
  const xirrValue = item.priceOnlyReturn

  const priceValue = isHistoric ? item.averageSellPrice : rowPrice.currentPrice
  const cellLoading = !isHistoric && rowPrice.isLoading
  const cellUnavailable = !isHistoric && rowPrice.fetchFailed
  const showsPriceStatus = !isHistoric && !cellLoading && !cellUnavailable

  return (
    <TableRow aria-busy={cellLoading || undefined}>
      <DataTableCell label="Asset name" className="portfolio-holdings__name-cell">
        <TruncatedText text={item.assetName} />
      </DataTableCell>
      <DataTableCell label="First investment">{formatShortDate(item.firstInvestmentDate)}</DataTableCell>
      <DataTableCell label="Quantity">{formatN8(item.currentQuantity)}</DataTableCell>
      <DataTableCell label={isHistoric ? '% Portfolio' : 'Market % of portfolio'}>
        {orMissing(item.portfolioWeight, formatPercentN2)}
      </DataTableCell>
      <DataTableCell label="Total invested">{formatN2(item.totalInvested)}</DataTableCell>
      {!isHistoric && (
        <DataTableCell label="Invested % of portfolio">{orMissing(item.investedWeight, formatPercentN2)}</DataTableCell>
      )}
      {isHistoric && (
        <DataTableCell label="Realised gain/loss">
          <span className={getProfitClass(item.realisedGainLoss)}>{formatN2(item.realisedGainLoss)}</span>
        </DataTableCell>
      )}
      {isHistoric && (
        <DataTableCell label="Realised (shares only)">
          <span className={getProfitClass(item.realisedGainLossSharesOnly)}>
            {formatN2(item.realisedGainLossSharesOnly)}
          </span>
        </DataTableCell>
      )}
      {!isHistoric && (
        <DataTableCell label="Current value">
          {renderGatedCell(rowPrice.isLoading, rowPrice.fetchFailed, currentValue, formatN2)}
          {!rowPrice.isLoading && !rowPrice.fetchFailed && rowPrice.isManual && (
            <PriceBadge abbreviation="M" meaning={MANUAL_PRICE_MEANING} className="portfolio-holdings__manual-badge" />
          )}
        </DataTableCell>
      )}
      <DataTableCell label="Total credits">{formatN2(item.totalCredits)}</DataTableCell>
      {!isHistoric && (
        <>
          <DataTableCell label="Credits % of invested">{orMissing(item.creditsPercentOfInvested, formatPercentN2)}</DataTableCell>
          <DataTableCell label="Monthly yield">{orMissing(item.monthlyYieldPercent, formatPercentN2)}</DataTableCell>
          <DataTableCell label="APR">{orMissing(item.aprPercent, formatPercentN2)}</DataTableCell>
        </>
      )}
      <DataTableCell label="Average price">{formatN2(item.averagePrice)}</DataTableCell>
      <DataTableCell label={isHistoric ? 'Sold price' : 'Current price'}>
        {renderGatedCell(cellLoading, cellUnavailable, priceValue, formatN2, { announcesLoading: true })}
        {showsPriceStatus && item.marketStatus === 'Stale' && (
          <PriceBadge abbreviation="S" meaning={STALE_PRICE_MEANING} className="portfolio-holdings__stale-badge" />
        )}
        {showsPriceStatus && item.marketStatus === 'Unavailable' && (
          <PriceBadge abbreviation="U" meaning={NO_PRICE_MEANING} className="portfolio-holdings__stale-badge" />
        )}
      </DataTableCell>
      <DataTableCell label="Profit %">
        {renderGatedCell(cellLoading, cellUnavailable, item.resultPercent, v => (
          <span className={getProfitClass(v)}>{formatPercentN2(v)}</span>
        ))}
      </DataTableCell>
      {!isHistoric && (
        <DataTableCell label="Profit amount">
          {renderGatedCell(cellLoading, cellUnavailable, item.unrealisedGain, v => (
            <span className={getProfitClass(v)}>{formatN2(v)}</span>
          ))}
        </DataTableCell>
      )}
      <DataTableCell label="Profit % w/ credits">
        {renderGatedCell(cellLoading, cellUnavailable, item.resultWithCreditsPercent, v => (
          <span className={getProfitClass(v)}>{formatPercentN2(v)}</span>
        ))}
      </DataTableCell>
      <DataTableCell label="XIRR">
        {renderGatedCell(cellLoading, false, xirrValue, v => (
          <span className={getProfitClass(v)}>{formatPercentN2(v * 100)}</span>
        ))}
      </DataTableCell>
      <DataTableCell label="Last month credits" className="portfolio-holdings__credits-separator">
        {item.lastCreditMonth === null ? <MissingValue label="None" /> : formatN2(item.lastMonthCredits)}
      </DataTableCell>
      <DataTableCell label="Last credit month">
        {orMissing(item.lastCreditMonth, formatCreditMonth, 'None')}
      </DataTableCell>
      <DataTableCell label="Last month credits %">
        {orMissing(item.lastMonthCreditsPercent, formatPercentN2)}
      </DataTableCell>
      {!isHistoric && (
        <DataTableCell label="Last credit % of market value">
          {orMissing(item.lastCreditPercentOfMarketValue, formatPercentN2)}
        </DataTableCell>
      )}
      <DataTableCell label="Est. annual credits">
        {orMissing(item.estimatedAnnualCredits, formatN2)}
      </DataTableCell>
      <DataTableCell label="Est. annual %">
        {orMissing(item.estimatedAnnualPercent, formatPercentN2)}
      </DataTableCell>
    </TableRow>
  )
}

function computeCurrentValueFooter(
  items: PortfolioAssetSummaryItemDto[],
  rowPrices: RowPriceState[],
): { display: ReactNode; partial: boolean } {
  const anyLoading = rowPrices.some(r => r.isLoading)
  const resolved = items
    .map((item, i) => {
      const rp = rowPrices[i]
      return rp && !rp.isLoading ? item.marketValue : null
    })
    .filter((v): v is number => v !== null)

  if (anyLoading && resolved.length === 0) return { display: 'Calculating…', partial: false }
  if (!anyLoading && resolved.length === 0) return { display: <MissingValue />, partial: false }
  const sum = resolved.reduce((acc, v) => acc + v, 0)
  if (anyLoading) return { display: `${formatN2(sum)} *`, partial: true }
  return { display: formatN2(sum), partial: false }
}

export default function PortfolioHoldingsTab() {
  const { scope } = useSelectedNode()
  const isHistoric = scope === 'historic'
  const { items, rowPrices, isLoading, error, retry } = usePortfolioAssetSummary()
  const { summary } = useAggregatedSummary()

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
    realisedGainLoss: (r) => r.item.realisedGainLoss,
    realisedGainLossSharesOnly: (r) => r.item.realisedGainLossSharesOnly,
    currentValue: (r) => computeCurrentValue(r.item, isHistoric),
    totalCredits: (r) => r.item.totalCredits,
    investedWeight: (r) => r.item.investedWeight,
    creditsPercent: (r) => r.item.creditsPercentOfInvested,
    monthlyYield: (r) => r.item.monthlyYieldPercent,
    apr: (r) => r.item.aprPercent,
    profitAmount: (r) => r.item.unrealisedGain,
    lastCreditPercent: (r) => r.item.lastCreditPercentOfMarketValue,
    averagePrice: (r) => r.item.averagePrice,
    price: (r) => (isHistoric ? r.item.averageSellPrice : r.rowPrice.currentPrice),
    profitPercent: (r) => r.item.resultPercent,
    profitWithCreditsPercent: (r) => r.item.resultWithCreditsPercent,
    xirr: (r) => r.item.priceOnlyReturn,
    lastMonthCredits: (r) => (r.item.lastCreditMonth === null ? null : r.item.lastMonthCredits),
    lastCreditMonth: (r) => (r.item.lastCreditMonth === null ? null : parseCreditMonth(r.item.lastCreditMonth)),
    lastMonthCreditsPercent: (r) => r.item.lastMonthCreditsPercent,
    estimatedAnnualCredits: (r) => r.item.estimatedAnnualCredits,
    estimatedAnnualPercent: (r) => r.item.estimatedAnnualPercent,
  }

  const { sortedRows, sortState, requestSort } = useSortableRows(tableRows, sortAccessors, {
    columnKey: 'firstInvestment',
    direction: 'descending',
  })
  const sortDirectionFor = (columnKey: string): SortDirection | undefined =>
    sortState?.columnKey === columnKey ? sortState.direction : undefined

  const creditsLabel = `Credits ${formatMonthYear(new Date())}`

  const footer =
    items && items.length > 0 && summary
      ? (() => {
          const totalInvested = summary.totalInvested
          const totalCredits = summary.totalCredits
          const currentMonthCredits = items.reduce((acc, it) => acc + it.currentMonthCredits, 0)
          const hasAnyAnnual = items.some(it => it.estimatedAnnualCredits !== null)
          const estAnnualCredits = hasAnyAnnual
            ? items.reduce((acc, it) => acc + (it.estimatedAnnualCredits ?? 0), 0)
            : null
          const realisedGainLoss = items.reduce((acc, it) => acc + it.realisedGainLoss, 0)
          const realisedGainLossSharesOnly = items.reduce((acc, it) => acc + it.realisedGainLossSharesOnly, 0)
          const cv = computeCurrentValueFooter(items, rowPrices)
          return { totalInvested, totalCredits, currentMonthCredits, estAnnualCredits, realisedGainLoss, realisedGainLossSharesOnly, cv }
        })()
      : null

  return (
    <div className="portfolio-holdings">
      <div className="portfolio-holdings__table-section">
        {isLoading && <LoadingState />}
        {error && <ErrorState message={error} onRetry={retry} />}
        {!isLoading && !error && items && (
          <Table className="portfolio-holdings__table data-table" aria-label="Holdings">
            <TableHeader>
              <TableRow>
                <SortableColumnHeader rowSpan={2} label="Asset name" columnKey="assetName" sortDirection={sortDirectionFor('assetName')} onSort={requestSort} />
                <SortableColumnHeader rowSpan={2} label="First investment" columnKey="firstInvestment" sortDirection={sortDirectionFor('firstInvestment')} onSort={requestSort} />
                <SortableColumnHeader rowSpan={2} numeric label="Quantity" columnKey="quantity" sortDirection={sortDirectionFor('quantity')} onSort={requestSort} />
                <SortableColumnHeader rowSpan={2} numeric label={isHistoric ? '% Portfolio' : 'Market % of portfolio'} columnKey="portfolioWeight" sortDirection={sortDirectionFor('portfolioWeight')} onSort={requestSort} />
                <SortableColumnHeader rowSpan={2} numeric label="Total invested" columnKey="totalInvested" sortDirection={sortDirectionFor('totalInvested')} onSort={requestSort} />
                {!isHistoric && (
                  <SortableColumnHeader rowSpan={2} numeric label="Invested % of portfolio" columnKey="investedWeight" sortDirection={sortDirectionFor('investedWeight')} onSort={requestSort} />
                )}
                {isHistoric && (
                  <SortableColumnHeader rowSpan={2} numeric label="Realised gain/loss" columnKey="realisedGainLoss" sortDirection={sortDirectionFor('realisedGainLoss')} onSort={requestSort} />
                )}
                {isHistoric && (
                  <SortableColumnHeader rowSpan={2} numeric label="Realised (shares only)" columnKey="realisedGainLossSharesOnly" sortDirection={sortDirectionFor('realisedGainLossSharesOnly')} onSort={requestSort} />
                )}
                {!isHistoric && (
                  <SortableColumnHeader rowSpan={2} numeric label="Current value" columnKey="currentValue" sortDirection={sortDirectionFor('currentValue')} onSort={requestSort} />
                )}
                <SortableColumnHeader rowSpan={2} numeric label="Total credits" columnKey="totalCredits" sortDirection={sortDirectionFor('totalCredits')} onSort={requestSort} />
                {!isHistoric && (
                  <>
                    <SortableColumnHeader rowSpan={2} numeric label="Credits % of invested" columnKey="creditsPercent" sortDirection={sortDirectionFor('creditsPercent')} onSort={requestSort} />
                    <SortableColumnHeader rowSpan={2} numeric label="Monthly yield" columnKey="monthlyYield" sortDirection={sortDirectionFor('monthlyYield')} onSort={requestSort} />
                    <SortableColumnHeader rowSpan={2} numeric label="APR" columnKey="apr" sortDirection={sortDirectionFor('apr')} onSort={requestSort} />
                  </>
                )}
                <SortableColumnHeader rowSpan={2} numeric label="Average price" columnKey="averagePrice" sortDirection={sortDirectionFor('averagePrice')} onSort={requestSort} />
                <SortableColumnHeader rowSpan={2} numeric label={isHistoric ? 'Sold price' : 'Current price'} columnKey="price" sortDirection={sortDirectionFor('price')} onSort={requestSort} />
                <TableHeaderCell colSpan={isHistoric ? 2 : 3} className="portfolio-holdings__group-header">Profit</TableHeaderCell>
                <SortableColumnHeader rowSpan={2} numeric label="XIRR" columnKey="xirr" sortDirection={sortDirectionFor('xirr')} onSort={requestSort} />
                <TableHeaderCell colSpan={isHistoric ? 3 : 4} className="portfolio-holdings__group-header portfolio-holdings__credits-separator">Last month</TableHeaderCell>
                <TableHeaderCell colSpan={2} className="portfolio-holdings__group-header">Est. annual</TableHeaderCell>
              </TableRow>
              <TableRow>
                <SortableColumnHeader numeric label="%" columnKey="profitPercent" sortDirection={sortDirectionFor('profitPercent')} onSort={requestSort} />
                {!isHistoric && (
                  <SortableColumnHeader numeric label="Amount" columnKey="profitAmount" sortDirection={sortDirectionFor('profitAmount')} onSort={requestSort} />
                )}
                <SortableColumnHeader numeric label="w/ credits" columnKey="profitWithCreditsPercent" sortDirection={sortDirectionFor('profitWithCreditsPercent')} onSort={requestSort} />
                <SortableColumnHeader numeric label="Credits" columnKey="lastMonthCredits" sortDirection={sortDirectionFor('lastMonthCredits')} onSort={requestSort} className="portfolio-holdings__credits-separator" />
                <SortableColumnHeader numeric label="Month" columnKey="lastCreditMonth" sortDirection={sortDirectionFor('lastCreditMonth')} onSort={requestSort} />
                <SortableColumnHeader numeric label="%" columnKey="lastMonthCreditsPercent" sortDirection={sortDirectionFor('lastMonthCreditsPercent')} onSort={requestSort} />
                {!isHistoric && (
                  <SortableColumnHeader numeric label="% of market value" columnKey="lastCreditPercent" sortDirection={sortDirectionFor('lastCreditPercent')} onSort={requestSort} />
                )}
                <SortableColumnHeader numeric label="Credits" columnKey="estimatedAnnualCredits" sortDirection={sortDirectionFor('estimatedAnnualCredits')} onSort={requestSort} />
                <SortableColumnHeader numeric label="%" columnKey="estimatedAnnualPercent" sortDirection={sortDirectionFor('estimatedAnnualPercent')} onSort={requestSort} />
              </TableRow>
            </TableHeader>
            <TableBody>
              {sortedRows.map((row) => (
                <AssetRow key={row.item.assetName} item={row.item} rowPrice={row.rowPrice} isHistoric={isHistoric} />
              ))}
            </TableBody>
          </Table>
        )}
      </div>

      {footer && (
        <div className="portfolio-holdings__footer" role="group" aria-label="Totals for holdings">
          <FooterTotal label="Total invested" value={formatN2(footer.totalInvested)} />
          <FooterTotal label="Total credits" value={formatN2(footer.totalCredits)} />
          {isHistoric && <FooterTotal label="Realised gain/loss" value={formatN2(footer.realisedGainLoss)} />}
          {isHistoric && <FooterTotal label="Realised (shares only)" value={formatN2(footer.realisedGainLossSharesOnly)} />}
          {!isHistoric && (
            <FooterTotal label="Current value" value={footer.cv.display}>
              {footer.cv.partial && (
                <span className="portfolio-holdings__footer-footnote">excludes assets with pending prices</span>
              )}
            </FooterTotal>
          )}
          <FooterTotal label={creditsLabel} value={formatN2(footer.currentMonthCredits)} />
          <FooterTotal label="Est. annual credits" value={orMissing(footer.estAnnualCredits, formatN2)} />
        </div>
      )}
    </div>
  )
}
