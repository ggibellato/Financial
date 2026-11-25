import { useId, useMemo, type ReactNode } from 'react'
import { mergeClasses, Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import ErrorState from './ErrorState'
import PriceBadge from './PriceBadge'
import LoadingState from './LoadingState'
import TruncatedText from './TruncatedText'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useBrokerHoldings } from '../hooks/useBrokerHoldings'
import { useVisuallyHiddenStyles } from '../styles/visuallyHidden'
import { useSortableRows, type SortAccessor, type SortDirection, type SortState } from '../hooks/useSortableRows'
import type { HoldingRowDto, HoldingsGroupDto, HoldingsSegmentDto, HoldingsTotalsDto, MonthStartColumnDto } from '../api/types'
import MonthStartValue from './MonthStartValue'
import { NO_SEGMENT } from '../utils/assetSegments'
import { pickSourceLabel } from '../utils/pickSource'
import { formatCreditMonth, parseCreditMonth } from '../utils/creditMonth'
import { formatDayMonthUtc, formatN2, formatN8, formatPercentN2, formatShortDate, formatSignedPercentN2, signClass } from '../utils/formatters'
import { orMissing } from '../utils/orMissing'
import { NO_PRICE_MEANING, STALE_PRICE_MEANING } from '../utils/priceStatus'
import './BrokerHoldingsTab.css'

interface GroupedRow {
  holding: HoldingRowDto
  sectionKey: string
}

interface HoldingColumn {
  key: string
  label: string
  cellLabel?: string
  text?: boolean
  sortValue: SortAccessor<HoldingRowDto>
  headerLabel?: (months: MonthColumns) => string
  render: (holding: HoldingRowDto, months: MonthColumns) => ReactNode
  total?: (totals: HoldingsTotalsDto) => ReactNode
  shareOfGroup?: keyof ShareOfGroup
}

interface ShareOfGroup {
  invested: number | null
  market: number | null
}

interface MonthColumns {
  lastMonth: MonthStartColumnDto
  thisMonth: MonthStartColumnDto
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

const percent = (value: number | null | undefined) => orMissing(value, formatPercentN2)
const amount = (value: number | null | undefined) => orMissing(value, formatN2)
const signedPercent = (value: number | null | undefined) => orMissing(value, v => signed(v, formatPercentN2))
const signedAmount = (value: number | null | undefined) => orMissing(value, v => signed(v, formatN2))
const MISSING_MONTH_CHANGE = 'No change: a month-start price is missing'
const monthChange = (value: number | null | undefined) => orMissing(value, v => signed(v, formatSignedPercentN2), MISSING_MONTH_CHANGE)

function monthStartNotice(totals: HoldingsTotalsDto): string | null {
  return totals.monthStartExcludedCount === 0
    ? null
    : `* Excludes ${totals.monthStartExcludedCount} holdings without a month-start price`
}

function starredWhenExcluding(totals: HoldingsTotalsDto, content: ReactNode): ReactNode {
  return totals.monthStartExcludedCount === 0 ? content : <span>{content}<span aria-hidden="true">*</span></span>
}

const COLUMN_GROUPS: ColumnGroup[] = [
  {
    label: null,
    columns: [
      {
        key: 'pick', label: 'Pick', text: true,
        sortValue: h => pickSourceLabel(h.asset.pickSource),
        render: h => orMissing(pickSourceLabel(h.asset.pickSource), v => v),
      },
      {
        key: 'firstPurchase', label: 'First purchase', text: true,
        sortValue: h => (h.asset.firstInvestmentDate === null ? null : new Date(h.asset.firstInvestmentDate)),
        render: h => formatShortDate(h.asset.firstInvestmentDate),
      },
      {
        key: 'quantity', label: 'Quantity',
        sortValue: h => h.asset.currentQuantity,
        render: h => formatN8(h.asset.currentQuantity),
      },
    ],
  },
  {
    label: 'Invested',
    columns: [
      {
        key: 'invested', label: 'Invested',
        sortValue: h => h.asset.totalInvested,
        render: h => formatN2(h.asset.totalInvested),
        total: t => formatN2(t.invested),
      },
      {
        key: 'investedShareOfGroup', label: '% of group', cellLabel: 'Invested % of group',
        sortValue: h => h.investedShareOfGroup,
        render: h => orMissing(h.investedShareOfGroup, formatPercentN2, ONLY_HOLDING),
        shareOfGroup: 'invested',
      },
      {
        key: 'investedShareOfBase', label: '% of broker', cellLabel: 'Invested % of broker',
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
        key: 'currentPrice', label: 'Current price',
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
        key: 'currentValue', label: 'Current value',
        sortValue: h => h.asset.marketValue,
        render: h => amount(h.asset.marketValue),
        total: t => amount(t.marketValue),
      },
      {
        key: 'marketShareOfGroup', label: '% of group', cellLabel: 'Market % of group',
        sortValue: h => h.marketShareOfGroup,
        render: h => orMissing(h.marketShareOfGroup, formatPercentN2, ONLY_HOLDING),
        shareOfGroup: 'market',
      },
      {
        key: 'marketShareOfBase', label: '% of broker', cellLabel: 'Market % of broker',
        sortValue: h => h.marketShareOfBase,
        render: h => percent(h.marketShareOfBase),
        total: t => percent(t.marketShareOfBase),
      },
    ],
  },
  {
    label: 'Month',
    columns: [
      {
        key: 'lastMonthStart', label: 'Start of last month',
        headerLabel: m => formatDayMonthUtc(m.lastMonth.date),
        sortValue: h => h.lastMonthStart.value,
        render: (h, m) => <MonthStartValue value={h.lastMonthStart} column={m.lastMonth} />,
        total: t => starredWhenExcluding(t, amount(t.lastMonthStartValue)),
      },
      {
        key: 'thisMonthStart', label: 'Start of this month',
        headerLabel: m => formatDayMonthUtc(m.thisMonth.date),
        sortValue: h => h.thisMonthStart.value,
        render: (h, m) => <MonthStartValue value={h.thisMonthStart} column={m.thisMonth} />,
        total: t => starredWhenExcluding(t, amount(t.thisMonthStartValue)),
      },
      {
        key: 'monthChange', label: 'Change %', cellLabel: 'Month change %',
        sortValue: h => h.monthChangePercent,
        render: h => monthChange(h.monthChangePercent),
        total: t => starredWhenExcluding(t, monthChange(t.monthChangePercent)),
      },
    ],
  },
  {
    label: 'Profit',
    columns: [
      {
        key: 'profitPercent', label: '%', cellLabel: 'Profit %',
        sortValue: h => h.asset.resultPercent,
        render: h => signedPercent(h.asset.resultPercent),
        total: t => signedPercent(t.profitPercent),
      },
      {
        key: 'profitAmount', label: 'Amount', cellLabel: 'Profit amount',
        sortValue: h => h.asset.unrealisedGain,
        render: h => signedAmount(h.asset.unrealisedGain),
        total: t => signedAmount(t.profitAmount),
      },
      {
        key: 'profitWithCreditsPercent', label: '% with credits', cellLabel: 'Profit % with credits',
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
        key: 'totalCredits', label: 'Total', cellLabel: 'Total credits',
        sortValue: h => h.asset.totalCredits,
        render: h => formatN2(h.asset.totalCredits),
        total: t => formatN2(t.totalCredits),
      },
      {
        key: 'creditsPercent', label: '% of invested', cellLabel: 'Credits % of invested',
        sortValue: h => h.asset.creditsPercentOfInvested,
        render: h => percent(h.asset.creditsPercentOfInvested),
        total: t => percent(t.creditsPercentOfInvested),
      },
      {
        key: 'monthlyYield', label: 'Monthly yield',
        sortValue: h => h.asset.monthlyYieldPercent,
        render: h => percent(h.asset.monthlyYieldPercent),
      },
      {
        key: 'apr', label: 'APR',
        sortValue: h => h.asset.aprPercent,
        render: h => percent(h.asset.aprPercent),
      },
    ],
  },
  {
    label: 'Last credit',
    columns: [
      {
        key: 'lastCreditAmount', label: 'Amount', cellLabel: 'Last credit amount',
        sortValue: lastCreditAmount,
        render: h => orMissing(lastCreditAmount(h), formatN2, 'None'),
        total: t => formatN2(t.lastCreditAmount),
      },
      {
        key: 'lastCreditMonth', label: 'Month', cellLabel: 'Last credit month',
        sortValue: h => (h.asset.lastCreditMonth === null ? null : parseCreditMonth(h.asset.lastCreditMonth)),
        render: h => orMissing(h.asset.lastCreditMonth, formatCreditMonth, 'None'),
      },
      {
        key: 'lastCreditPercent', label: '% of market value', cellLabel: 'Last credit % of market value',
        sortValue: h => h.asset.lastCreditPercentOfMarketValue,
        render: h => percent(h.asset.lastCreditPercentOfMarketValue),
        total: t => percent(t.lastCreditPercentOfMarketValue),
      },
    ],
  },
]

const COLUMNS = COLUMN_GROUPS.flatMap(g => g.columns)

const cellLabelOf = (column: HoldingColumn, months: MonthColumns) =>
  column.headerLabel ? `${column.label} (${column.headerLabel(months)})` : (column.cellLabel ?? column.label)
const numericClass = (column: HoldingColumn) => (column.text ? undefined : 'data-table__col--numeric')
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

function groupRows(rows: GroupedRow[]): Map<string, GroupedRow[]> {
  const bySection = new Map<string, GroupedRow[]>()
  for (const row of rows) {
    bySection.set(row.sectionKey, [...(bySection.get(row.sectionKey) ?? []), row])
  }
  return bySection
}

function sectionRows(group: HoldingsGroupDto, groupIndex: number): GroupedRow[] {
  return group.segments.length === 0
    ? group.holdings.map(holding => ({ holding, sectionKey: `${groupIndex}` }))
    : group.segments.flatMap((segment, segmentIndex) =>
        segment.holdings.map(holding => ({ holding, sectionKey: `${groupIndex}:${segmentIndex}` })),
      )
}

function unvaluedNotice(totals: HoldingsTotalsDto): string | null {
  return totals.unvaluedHoldingCount === 0
    ? null
    : `${totals.unvaluedHoldingCount} of ${totals.holdingCount} holdings could not be valued; the total is incomplete.`
}

interface TotalsRowProps {
  label: string
  totals: HoldingsTotalsDto
  months: MonthColumns
  shareOfGroup?: ShareOfGroup
}

function totalCell(column: HoldingColumn, totals: HoldingsTotalsDto, shareOfGroup?: ShareOfGroup): ReactNode | undefined {
  if (column.shareOfGroup && shareOfGroup) return percent(shareOfGroup[column.shareOfGroup])
  return column.total?.(totals)
}

function TotalsRow({ label, totals, months, shareOfGroup }: TotalsRowProps) {
  const notices = [unvaluedNotice(totals), monthStartNotice(totals)].filter(notice => notice !== null)
  return (
    <TableRow className="broker-holdings__totals-row">
      <TableHeaderCell scope="row" className="broker-holdings__name-cell">
        <span>{label}</span>
        {notices.map(notice => (
          <span key={notice} className="broker-holdings__notice">{notice}</span>
        ))}
      </TableHeaderCell>
      {COLUMNS.map(column => {
        const value = totalCell(column, totals, shareOfGroup)
        return (
          <DataTableCell
            key={column.key}
            label={cellLabelOf(column, months)}
            className={mergeClasses(numericClass(column), value === undefined && 'data-table__cell--no-total')}
          >
            {value}
          </DataTableCell>
        )
      })}
    </TableRow>
  )
}

function SegmentSection({ segment, rows, months }: { segment: HoldingsSegmentDto; rows: GroupedRow[]; months: MonthColumns }) {
  const name = segment.name ?? NO_SEGMENT
  const hidden = useVisuallyHiddenStyles()
  return (
    <>
      <TableRow className="broker-holdings__segment-row">
        <TableHeaderCell scope="row" colSpan={COLUMN_COUNT} className="broker-holdings__group-cell">
          <span className="broker-holdings__group-name">Segment: {name}</span>
        </TableHeaderCell>
      </TableRow>
      {rows.map(row => (
        <HoldingRow
          key={row.holding.asset.assetName}
          holding={row.holding}
          months={months}
          segmentLabel={<span className={hidden.visuallyHidden}>Segment {name}: </span>}
        />
      ))}
      <TotalsRow
        label={`Segment subtotal: ${name}`}
        totals={segment.subtotal}
        months={months}
        shareOfGroup={{ invested: segment.investedShareOfGroup, market: segment.marketShareOfGroup }}
      />
    </>
  )
}

function HoldingRow({ holding, months, segmentLabel }: { holding: HoldingRowDto; months: MonthColumns; segmentLabel?: ReactNode }) {
  return (
    <TableRow>
      <DataTableCell label="Asset name" className="broker-holdings__name-cell">
        {segmentLabel}
        <TruncatedText text={holding.asset.assetName} />
      </DataTableCell>
      {COLUMNS.map(column => (
        <DataTableCell key={column.key} label={cellLabelOf(column, months)} className={numericClass(column)}>
          {column.render(holding, months)}
        </DataTableCell>
      ))}
    </TableRow>
  )
}

export default function BrokerHoldingsTab() {
  const { holdings, isLoading, error, retry } = useBrokerHoldings()
  const captionId = useId()

  const rows = useMemo<GroupedRow[]>(
    () => (holdings?.groups ?? []).flatMap(sectionRows),
    [holdings],
  )
  const { sortedRows, sortState, requestSort } = useSortableRows(rows, SORT_ACCESSORS, FIRST_PURCHASE_DESC)
  const rowsBySection = useMemo(() => groupRows(sortedRows), [sortedRows])
  const sortDirectionFor = (columnKey: string): SortDirection | undefined =>
    sortState?.columnKey === columnKey ? sortState.direction : undefined

  if (isLoading) return <LoadingState />
  if (error) return <ErrorState message={error} onRetry={retry} />
  if (!holdings) return null
  if (holdings.groups.length === 0) {
    return <p className="broker-holdings__empty">This broker has no active holdings.</p>
  }
  const months: MonthColumns = { lastMonth: holdings.lastMonthStart, thisMonth: holdings.thisMonthStart }
  const headerLabelOf = (column: HoldingColumn) => column.headerLabel?.(months) ?? column.label
  const headerAriaLabelOf = (column: HoldingColumn) =>
    column.headerLabel ? `${column.headerLabel(months)}, ${column.label.toLowerCase()}` : cellLabelOf(column, months)

  return (
    <div className="broker-holdings">
      <p id={captionId} className="broker-holdings__caption">{currencyCaption(holdings.currencies)}</p>
      <div className="broker-holdings__table-section">
        <Table className="broker-holdings__table data-table data-table--fit-content" aria-labelledby={captionId}>
          <TableHeader>
            <TableRow>
              <SortableColumnHeader rowSpan={2} label="Asset name" columnKey="assetName" sortDirection={sortDirectionFor('assetName')} onSort={requestSort} className="broker-holdings__name-cell" />
              {COLUMN_GROUPS.map(group =>
                group.label === null
                  ? group.columns.map(column => (
                      <SortableColumnHeader key={column.key} rowSpan={2} numeric={!column.text} label={column.label} columnKey={column.key} sortDirection={sortDirectionFor(column.key)} onSort={requestSort} />
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
                  <SortableColumnHeader key={column.key} numeric={!column.text} label={headerLabelOf(column)} ariaLabel={headerAriaLabelOf(column)} columnKey={column.key} sortDirection={sortDirectionFor(column.key)} onSort={requestSort} />
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
              {group.segments.length === 0
                ? (rowsBySection.get(`${groupIndex}`) ?? []).map(row => (
                    <HoldingRow key={row.holding.asset.assetName} holding={row.holding} months={months} />
                  ))
                : group.segments.map((segment, segmentIndex) => (
                    <SegmentSection
                      key={segmentIndex}
                      segment={segment}
                      rows={rowsBySection.get(`${groupIndex}:${segmentIndex}`) ?? []}
                      months={months}
                    />
                  ))}
              <TotalsRow label={`Subtotal ${group.name}`} totals={group.subtotal} months={months} />
            </TableBody>
          ))}
          <tfoot>
            <TotalsRow label="Broker total" totals={holdings.total} months={months} />
          </tfoot>
        </Table>
      </div>
    </div>
  )
}
