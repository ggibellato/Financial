import { useId, type ReactNode } from 'react'
import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import ErrorState from './ErrorState'
import PriceBadge from './PriceBadge'
import LoadingState from './LoadingState'
import TruncatedText from './TruncatedText'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useBrokerHoldings } from '../hooks/useBrokerHoldings'
import { useSortableRows, type SortAccessor, type SortDirection, type SortState } from '../hooks/useSortableRows'
import type { HoldingRowDto, HoldingsTotalsDto } from '../api/types'
import { formatCreditMonth, parseCreditMonth } from '../utils/creditMonth'
import { formatN2, formatN8, formatPercentN2, formatShortDate, signClass } from '../utils/formatters'
import { orMissing } from '../utils/orMissing'
import './BrokerHoldingsTab.css'

interface GroupedRow {
  holding: HoldingRowDto
  groupIndex: number
}

interface HoldingColumn {
  key: string
  label: string
  cellLabel: string
  numeric?: boolean
  sortValue: SortAccessor<HoldingRowDto>
  render: (holding: HoldingRowDto) => ReactNode
  total?: (totals: HoldingsTotalsDto) => ReactNode
}

interface ColumnGroup {
  label: string | null
  columns: HoldingColumn[]
}

function signed(value: number, format: (v: number) => string): ReactNode {
  return <span className={signClass(value, 'broker-holdings__profit')}>{format(value)}</span>
}

function currentPrice(holding: HoldingRowDto): number | null {
  const { marketValue, currentQuantity } = holding.asset
  return marketValue === null || currentQuantity <= 0 ? null : marketValue / currentQuantity
}

function lastCreditAmount(holding: HoldingRowDto): number | null {
  return holding.asset.lastCreditMonth === null ? null : holding.asset.lastMonthCredits
}

const ONLY_HOLDING = 'Only holding in the portfolio'
const STALE_PRICE_MEANING = 'This price is older than the most recent weekday.'
const NO_PRICE_MEANING = 'No price or value has been recorded for this holding yet.'

const percent = (value: number | null | undefined) => orMissing(value, formatPercentN2)
const amount = (value: number | null | undefined) => orMissing(value, formatN2)
const signedPercent = (value: number | null | undefined) => orMissing(value, v => signed(v, formatPercentN2))
const signedAmount = (value: number | null | undefined) => orMissing(value, v => signed(v, formatN2))

const COLUMN_GROUPS: ColumnGroup[] = [
  {
    label: null,
    columns: [
      {
        key: 'firstPurchase', label: 'First purchase', cellLabel: 'First purchase',
        sortValue: h => (h.asset.firstInvestmentDate === null ? null : new Date(h.asset.firstInvestmentDate)),
        render: h => formatShortDate(h.asset.firstInvestmentDate),
      },
      {
        key: 'quantity', label: 'Quantity', cellLabel: 'Quantity', numeric: true,
        sortValue: h => h.asset.currentQuantity,
        render: h => formatN8(h.asset.currentQuantity),
      },
    ],
  },
  {
    label: 'Invested',
    columns: [
      {
        key: 'invested', label: 'Invested', cellLabel: 'Invested', numeric: true,
        sortValue: h => h.asset.totalInvested,
        render: h => formatN2(h.asset.totalInvested),
        total: t => formatN2(t.invested),
      },
      {
        key: 'investedShareOfGroup', label: '% of group', cellLabel: 'Invested % of group', numeric: true,
        sortValue: h => h.investedShareOfGroup,
        render: h => orMissing(h.investedShareOfGroup, formatPercentN2, ONLY_HOLDING),
      },
      {
        key: 'investedShareOfBase', label: '% of broker', cellLabel: 'Invested % of broker', numeric: true,
        sortValue: h => h.investedShareOfBase,
        render: h => percent(h.investedShareOfBase),
        total: t => percent(t.investedShareOfBase),
      },
    ],
  },
  {
    label: 'Market',
    columns: [
      {
        key: 'currentPrice', label: 'Current price', cellLabel: 'Current price', numeric: true,
        sortValue: currentPrice,
        render: h => (
          <>
            {amount(currentPrice(h))}
            {h.asset.marketStatus === 'Stale' && <PriceBadge abbreviation="S" meaning={STALE_PRICE_MEANING} className="broker-holdings__price-badge" />}
            {h.asset.marketStatus === 'Unavailable' && <PriceBadge abbreviation="U" meaning={NO_PRICE_MEANING} className="broker-holdings__price-badge" />}
          </>
        ),
      },
      {
        key: 'currentValue', label: 'Current value', cellLabel: 'Current value', numeric: true,
        sortValue: h => h.asset.marketValue,
        render: h => amount(h.asset.marketValue),
        total: t => amount(t.marketValue),
      },
      {
        key: 'marketShareOfGroup', label: '% of group', cellLabel: 'Market % of group', numeric: true,
        sortValue: h => h.marketShareOfGroup,
        render: h => orMissing(h.marketShareOfGroup, formatPercentN2, ONLY_HOLDING),
      },
      {
        key: 'marketShareOfBase', label: '% of broker', cellLabel: 'Market % of broker', numeric: true,
        sortValue: h => h.marketShareOfBase,
        render: h => percent(h.marketShareOfBase),
        total: t => percent(t.marketShareOfBase),
      },
    ],
  },
  {
    label: 'Profit',
    columns: [
      {
        key: 'profitPercent', label: '%', cellLabel: 'Profit %', numeric: true,
        sortValue: h => h.asset.resultPercent,
        render: h => signedPercent(h.asset.resultPercent),
        total: t => signedPercent(t.profitPercent),
      },
      {
        key: 'profitAmount', label: 'Amount', cellLabel: 'Profit amount', numeric: true,
        sortValue: h => h.asset.unrealisedGain,
        render: h => signedAmount(h.asset.unrealisedGain),
        total: t => signedAmount(t.profitAmount),
      },
      {
        key: 'profitWithCreditsPercent', label: '% with credits', cellLabel: 'Profit % with credits', numeric: true,
        sortValue: h => h.asset.resultWithCreditsPercent,
        render: h => signedPercent(h.asset.resultWithCreditsPercent),
        total: t => signedPercent(t.profitWithCreditsPercent),
      },
    ],
  },
  {
    label: 'Credits',
    columns: [
      {
        key: 'totalCredits', label: 'Total', cellLabel: 'Total credits', numeric: true,
        sortValue: h => h.asset.totalCredits,
        render: h => formatN2(h.asset.totalCredits),
        total: t => formatN2(t.totalCredits),
      },
      {
        key: 'creditsPercent', label: '% of invested', cellLabel: 'Credits % of invested', numeric: true,
        sortValue: h => h.asset.creditsPercentOfInvested,
        render: h => percent(h.asset.creditsPercentOfInvested),
        total: t => percent(t.creditsPercentOfInvested),
      },
      {
        key: 'monthlyYield', label: 'Monthly yield', cellLabel: 'Monthly yield', numeric: true,
        sortValue: h => h.asset.monthlyYieldPercent,
        render: h => percent(h.asset.monthlyYieldPercent),
      },
      {
        key: 'apr', label: 'APR', cellLabel: 'APR', numeric: true,
        sortValue: h => h.asset.aprPercent,
        render: h => percent(h.asset.aprPercent),
      },
    ],
  },
  {
    label: 'Last credit',
    columns: [
      {
        key: 'lastCreditAmount', label: 'Amount', cellLabel: 'Last credit amount', numeric: true,
        sortValue: lastCreditAmount,
        render: h => orMissing(lastCreditAmount(h), formatN2, 'None'),
        total: t => formatN2(t.lastCreditAmount),
      },
      {
        key: 'lastCreditMonth', label: 'Month', cellLabel: 'Last credit month', numeric: true,
        sortValue: h => (h.asset.lastCreditMonth === null ? null : parseCreditMonth(h.asset.lastCreditMonth)),
        render: h => orMissing(h.asset.lastCreditMonth, formatCreditMonth, 'None'),
      },
      {
        key: 'lastCreditPercent', label: '% of market value', cellLabel: 'Last credit % of market value', numeric: true,
        sortValue: h => h.asset.lastCreditPercentOfMarketValue,
        render: h => percent(h.asset.lastCreditPercentOfMarketValue),
        total: t => percent(t.lastCreditPercentOfMarketValue),
      },
    ],
  },
]

const COLUMNS = COLUMN_GROUPS.flatMap(g => g.columns)
const COLUMN_COUNT = COLUMNS.length + 1

const SORT_ACCESSORS: Record<string, SortAccessor<GroupedRow>> = Object.fromEntries([
  ['assetName', (r: GroupedRow) => r.holding.asset.assetName],
  ...COLUMNS.map(c => [c.key, (r: GroupedRow) => c.sortValue(r.holding)]),
])

const FIRST_PURCHASE_DESC: SortState = { columnKey: 'firstPurchase', direction: 'descending' }

function currencyCaption(currencies: string[]): string {
  return currencies.length === 1
    ? `Holdings, amounts in ${currencies[0]}`
    : `Holdings, amounts in each holding's own currency (${currencies.join(', ')}); subtotals and totals add them without conversion`
}

function unvaluedNotice(totals: HoldingsTotalsDto): string | null {
  return totals.unvaluedHoldingCount === 0
    ? null
    : `${totals.unvaluedHoldingCount} of ${totals.holdingCount} holdings could not be valued; the total is incomplete.`
}

function TotalsRow({ label, totals }: { label: string; totals: HoldingsTotalsDto }) {
  const notice = unvaluedNotice(totals)
  return (
    <TableRow className="broker-holdings__totals-row">
      <TableHeaderCell scope="row" className="broker-holdings__name-cell">
        <span>{label}</span>
        {notice && <span className="broker-holdings__notice">{notice}</span>}
      </TableHeaderCell>
      {COLUMNS.map(column => (
        <DataTableCell key={column.key} label={column.cellLabel} className={column.total ? undefined : 'data-table__cell--no-total'}>
          {column.total ? column.total(totals) : null}
        </DataTableCell>
      ))}
    </TableRow>
  )
}

function HoldingRow({ holding }: { holding: HoldingRowDto }) {
  return (
    <TableRow>
      <DataTableCell label="Asset name" className="broker-holdings__name-cell">
        <TruncatedText text={holding.asset.assetName} />
      </DataTableCell>
      {COLUMNS.map(column => (
        <DataTableCell key={column.key} label={column.cellLabel}>
          {column.render(holding)}
        </DataTableCell>
      ))}
    </TableRow>
  )
}

export default function BrokerHoldingsTab() {
  const { holdings, isLoading, error, retry } = useBrokerHoldings()
  const captionId = useId()

  const rows: GroupedRow[] = (holdings?.groups ?? []).flatMap((group, groupIndex) =>
    group.holdings.map(holding => ({ holding, groupIndex })),
  )
  const { sortedRows, sortState, requestSort } = useSortableRows(rows, SORT_ACCESSORS, FIRST_PURCHASE_DESC)
  const sortDirectionFor = (columnKey: string): SortDirection | undefined =>
    sortState?.columnKey === columnKey ? sortState.direction : undefined

  if (isLoading) return <LoadingState />
  if (error) return <ErrorState message={error} onRetry={retry} />
  if (!holdings) return null
  if (holdings.groups.length === 0) {
    return <p className="broker-holdings__empty">This broker has no active holdings.</p>
  }

  return (
    <div className="broker-holdings">
      <p id={captionId} className="broker-holdings__caption">{currencyCaption(holdings.currencies)}</p>
      <div className="broker-holdings__table-section">
        <Table className="broker-holdings__table data-table" aria-labelledby={captionId}>
          <TableHeader>
            <TableRow>
              <SortableColumnHeader rowSpan={2} label="Asset name" columnKey="assetName" sortDirection={sortDirectionFor('assetName')} onSort={requestSort} className="broker-holdings__name-cell" />
              {COLUMN_GROUPS.map(group =>
                group.label === null
                  ? group.columns.map(column => (
                      <SortableColumnHeader key={column.key} rowSpan={2} numeric={column.numeric} label={column.label} columnKey={column.key} sortDirection={sortDirectionFor(column.key)} onSort={requestSort} />
                    ))
                  : (
                      <TableHeaderCell key={group.label} scope="colgroup" colSpan={group.columns.length} className="broker-holdings__group-header">
                        {group.label}
                      </TableHeaderCell>
                    ),
              )}
            </TableRow>
            <TableRow>
              {COLUMN_GROUPS.filter(group => group.label !== null).flatMap(group =>
                group.columns.map(column => (
                  <SortableColumnHeader key={column.key} numeric={column.numeric} label={column.label} ariaLabel={column.cellLabel} columnKey={column.key} sortDirection={sortDirectionFor(column.key)} onSort={requestSort} />
                )),
              )}
            </TableRow>
          </TableHeader>
          {holdings.groups.map((group, groupIndex) => (
            <TableBody key={group.name}>
              <TableRow className="broker-holdings__group-row">
                <TableHeaderCell scope="rowgroup" colSpan={COLUMN_COUNT} className="broker-holdings__group-cell">
                  <span className="broker-holdings__group-name">{group.name}</span>
                </TableHeaderCell>
              </TableRow>
              {sortedRows
                .filter(row => row.groupIndex === groupIndex)
                .map(row => (
                  <HoldingRow key={row.holding.asset.assetName} holding={row.holding} />
                ))}
              <TotalsRow label={`Subtotal ${group.name}`} totals={group.subtotal} />
            </TableBody>
          ))}
          <tfoot>
            <TotalsRow label="Broker total" totals={holdings.total} />
          </tfoot>
        </Table>
      </div>
    </div>
  )
}
