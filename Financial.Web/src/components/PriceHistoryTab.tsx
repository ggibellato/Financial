import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { Button, Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import type { AssetPriceSnapshotDto, TransactionDto } from '../api/types'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import SplitPanel from './SplitPanel'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import type { PriceHistoryFormField } from '../hooks/usePriceHistory'
import { usePriceHistory } from '../hooks/usePriceHistory'
import { confirmThenRun } from '../utils/confirmThenRun'
import { PERIOD_FILTER_OPTIONS } from '../utils/periodFilter'
import { formatN2, formatShortDate } from '../utils/formatters'
import { buildChartData, type ChartPoint } from '../utils/priceHistoryChartData'
import './PriceHistoryTab.css'

const DEFAULT_LEFT_WIDTH = 400
const MIN_LEFT_WIDTH = 200
const LINE_COLOR = '#4682b4'
const MANUAL_DOT_COLOR = '#e65100'
const AUTOMATIC_DOT_COLOR = '#4682b4'
// Same Buy/Sell colors as TransactionsTab.css - keep the two in sync.
const BUY_COLOR = '#2e7d32'
const SELL_COLOR = '#c62828'
const MARKER_RADIUS = 4

const SORT_ACCESSORS: Record<string, SortAccessor<AssetPriceSnapshotDto>> = {
  date: (entry) => new Date(entry.date),
  price: (entry) => entry.price,
  source: (entry) => (entry.isManual ? 'Manual' : 'Automatic'),
}

interface PriceRowProps {
  entry: AssetPriceSnapshotDto
  onEdit: (entry: AssetPriceSnapshotDto) => void
  onDelete: (date: string) => void
}

function PriceRow({ entry, onEdit, onDelete }: PriceRowProps) {
  return (
    <TableRow>
      <TableCell>
        {entry.isManual && (
          <Button
            appearance="subtle"
            size="small"
            icon={<EditRegular />}
            aria-label="Edit price"
            onClick={() => onEdit(entry)}
          />
        )}
      </TableCell>
      <TableCell>
        {entry.isManual && (
          <Button
            appearance="subtle"
            size="small"
            icon={<DeleteRegular />}
            aria-label="Delete price"
            onClick={() => onDelete(entry.date)}
          />
        )}
      </TableCell>
      <TableCell>{formatShortDate(entry.date)}</TableCell>
      <TableCell className="data-table__col--numeric price-history-tab__price">{formatN2(entry.price)}</TableCell>
      <TableCell
        className={entry.isManual ? 'price-history-tab__source--manual' : 'price-history-tab__source--automatic'}
      >
        {entry.isManual ? 'Manual' : 'Automatic'}
      </TableCell>
    </TableRow>
  )
}

interface InlineFormProps {
  editingDate: string | null
  formDate: string
  formPrice: string
  isSaving: boolean
  saveError: string | null
  onFieldChange: (field: PriceHistoryFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

function InlineForm({
  editingDate,
  formDate,
  formPrice,
  isSaving,
  saveError,
  onFieldChange,
  onSave,
  onCancel,
}: InlineFormProps) {
  const title = editingDate ? 'Edit price' : 'New price'

  return (
    <div className="price-history-tab__form">
      <p className="price-history-tab__form-title">{title}</p>
      <div className="price-history-tab__form-fields">
        <div className="price-history-tab__form-field">
          <label htmlFor="ph-date">Date</label>
          <input
            id="ph-date"
            type="date"
            value={formDate}
            required
            onChange={(e) => onFieldChange('formDate', e.target.value)}
          />
        </div>
        <div className="price-history-tab__form-field">
          <label htmlFor="ph-price">Price</label>
          <input
            id="ph-price"
            type="number"
            step="0.01"
            min="0"
            value={formPrice}
            required
            onChange={(e) => onFieldChange('formPrice', e.target.value)}
          />
        </div>
      </div>
      <div className="price-history-tab__form-actions">
        <button
          className="price-history-tab__save-btn"
          type="button"
          disabled={isSaving}
          onClick={onSave}
        >
          {isSaving ? 'Saving...' : editingDate ? 'Save' : 'Add price'}
        </button>
        <button className="price-history-tab__cancel-btn" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
      {saveError && <p className="price-history-tab__error">{saveError}</p>}
    </div>
  )
}

interface DotProps {
  cx?: number
  cy?: number
  payload?: ChartPoint
  key?: React.Key | null
}

function HistoryDot({ cx, cy, payload }: DotProps) {
  if (cx === undefined || cy === undefined || payload?.price === undefined) return <g />
  return <circle cx={cx} cy={cy} r={3} fill={payload.isManual ? MANUAL_DOT_COLOR : AUTOMATIC_DOT_COLOR} />
}

function BuyDot({ cx, cy, payload }: DotProps) {
  if (cx === undefined || cy === undefined || payload?.buyPrice === undefined) return <g />
  const r = MARKER_RADIUS
  return <polygon points={`${cx},${cy - r} ${cx - r},${cy + r} ${cx + r},${cy + r}`} fill={BUY_COLOR} />
}

function SellDot({ cx, cy, payload }: DotProps) {
  if (cx === undefined || cy === undefined || payload?.sellPrice === undefined) return <g />
  const r = MARKER_RADIUS
  return <polygon points={`${cx},${cy + r} ${cx - r},${cy - r} ${cx + r},${cy - r}`} fill={SELL_COLOR} />
}

interface ChartTooltipProps {
  active?: boolean
  payload?: { payload: ChartPoint }[]
}

function ChartTooltip({ active, payload }: ChartTooltipProps) {
  const point = active ? payload?.[0]?.payload : undefined
  if (!point) return null

  if (point.price !== undefined) {
    return (
      <div className="price-history-tab__tooltip">
        <p>{point.isManual ? 'Manual' : 'Automatic'}</p>
        <p>{point.date}</p>
        <p>{formatN2(point.price)}</p>
      </div>
    )
  }
  if (point.buyPrice !== undefined) {
    return (
      <div className="price-history-tab__tooltip">
        <p>Buy</p>
        <p>{point.date}</p>
        <p>{formatN2(point.buyPrice)}</p>
      </div>
    )
  }
  if (point.sellPrice !== undefined) {
    return (
      <div className="price-history-tab__tooltip">
        <p>Sell</p>
        <p>{point.date}</p>
        <p>{formatN2(point.sellPrice)}</p>
      </div>
    )
  }
  return null
}

function ChartLegend() {
  return (
    <div className="price-history-tab__chart-legend">
      <span className="price-history-tab__legend-item">
        <span className="price-history-tab__legend-swatch price-history-tab__legend-swatch--automatic" />
        Automatic
      </span>
      <span className="price-history-tab__legend-item">
        <span className="price-history-tab__legend-swatch price-history-tab__legend-swatch--manual" />
        Manual
      </span>
      <span className="price-history-tab__legend-item">
        <span className="price-history-tab__legend-swatch price-history-tab__legend-swatch--buy" />
        Buy
      </span>
      <span className="price-history-tab__legend-item">
        <span className="price-history-tab__legend-swatch price-history-tab__legend-swatch--sell" />
        Sell
      </span>
    </div>
  )
}

interface ChartPanelProps {
  entries: AssetPriceSnapshotDto[]
  transactions: TransactionDto[]
}

function ChartPanel({ entries, transactions }: ChartPanelProps) {
  const chartData = buildChartData(entries, transactions)

  return (
    <div className="price-history-tab__chart-panel">
      <p className="price-history-tab__chart-title">Price History</p>
      <ChartLegend />
      <div className="price-history-tab__chart-container">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={chartData} margin={{ top: 8, right: 16, left: 8, bottom: 8 }}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="date" tick={{ fontSize: 11 }} />
            <YAxis tickFormatter={formatN2} tick={{ fontSize: 11 }} width={70} />
            <Tooltip content={<ChartTooltip />} />
            <Line
              type="monotone"
              dataKey="price"
              name="Price"
              stroke={LINE_COLOR}
              strokeWidth={2}
              connectNulls
              dot={(props: DotProps) => <HistoryDot key={props.key} {...props} />}
            />
            <Line
              dataKey="buyPrice"
              name="Buy"
              stroke="none"
              isAnimationActive={false}
              dot={(props: DotProps) => <BuyDot key={props.key} {...props} />}
            />
            <Line
              dataKey="sellPrice"
              name="Sell"
              stroke="none"
              isAnimationActive={false}
              dot={(props: DotProps) => <SellDot key={props.key} {...props} />}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  )
}

export default function PriceHistoryTab() {
  const {
    entries,
    filteredEntries,
    filteredTransactions,
    isLoading,
    error,
    retry,
    selectedFilter,
    setFilter,
    isFormVisible,
    editingDate,
    formDate,
    formPrice,
    isSaving,
    saveError,
    deleteError,
    showNewForm,
    showEditForm,
    cancelForm,
    setFormField,
    saveForm,
    deleteEntry,
  } = usePriceHistory()

  const { sortedRows, sortState, requestSort } = useSortableRows(entries, SORT_ACCESSORS)

  const confirmAndDeleteEntry = (date: string) => confirmThenRun('Delete this price entry?', () => deleteEntry(date))

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  const toolbar = (
    <div className="price-history-tab__controls">
      <div className="price-history-tab__filters">
        {PERIOD_FILTER_OPTIONS.map((opt) => (
          <button
            key={opt.value}
            type="button"
            className={`price-history-tab__filter-btn${selectedFilter === opt.value ? ' price-history-tab__filter-btn--active' : ''}`}
            onClick={() => setFilter(opt.value)}
          >
            {opt.label}
          </button>
        ))}
      </div>
    </div>
  )

  const leftPanel = (
    <div className="price-history-tab__left">
      <div className="price-history-tab__table-toolbar">
        <Button appearance="primary" icon={<AddRegular />} onClick={showNewForm}>
          New price
        </Button>
      </div>

      {isFormVisible && (
        <InlineForm
          editingDate={editingDate}
          formDate={formDate}
          formPrice={formPrice}
          isSaving={isSaving}
          saveError={saveError}
          onFieldChange={setFormField}
          onSave={saveForm}
          onCancel={cancelForm}
        />
      )}

      <div className="price-history-tab__table-wrapper">
        <Table className="price-history-tab__table data-table">
          <TableHeader>
            <TableRow>
              <TableHeaderCell />
              <TableHeaderCell />
              <SortableColumnHeader
                label="Date"
                columnKey="date"
                sortDirection={sortState?.columnKey === 'date' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Price"
                columnKey="price"
                numeric
                sortDirection={sortState?.columnKey === 'price' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Source"
                columnKey="source"
                sortDirection={sortState?.columnKey === 'source' ? sortState.direction : undefined}
                onSort={requestSort}
              />
            </TableRow>
          </TableHeader>
          <TableBody>
            {sortedRows.map((entry) => (
              <PriceRow key={entry.date} entry={entry} onEdit={showEditForm} onDelete={confirmAndDeleteEntry} />
            ))}
          </TableBody>
        </Table>
      </div>

      {deleteError && <p className="price-history-tab__delete-error">{deleteError}</p>}
    </div>
  )

  const rightPanel = (
    <div className="price-history-tab__right">
      <ChartPanel entries={filteredEntries} transactions={filteredTransactions} />
    </div>
  )

  return (
    <div className="price-history-tab">
      {toolbar}
      <div className="price-history-tab__split">
        <SplitPanel
          left={leftPanel}
          right={rightPanel}
          defaultWidth={DEFAULT_LEFT_WIDTH}
          minWidth={MIN_LEFT_WIDTH}
        />
      </div>
    </div>
  )
}
