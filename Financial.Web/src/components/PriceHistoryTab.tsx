import {
  CartesianGrid,
  DefaultLegendContent,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { DefaultLegendContentProps } from 'recharts'
import { Button, Field, Input, MessageBar, MessageBarBody, Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow, Text } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import type { AssetPriceSnapshotDto, TransactionDto } from '../api/types'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import SplitPanel from './SplitPanel'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useFormPanelStyles } from './formPanelStyles'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useFieldError } from '../hooks/useFieldError'
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
  saveErrorFields: Partial<Record<PriceHistoryFormField, string>>
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
  saveErrorFields,
  onFieldChange,
  onSave,
  onCancel,
}: InlineFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(saveErrorFields)
  const title = editingDate ? 'Edit price' : 'New price'

  return (
    <div className={styles.panel}>
      <Text as="h2" weight="semibold" size={400}>
        {title}
      </Text>

      <div className={styles.grid}>
        <Field
          label="Date"
          required
          validationState={fieldError('formDate') ? 'error' : 'none'}
          validationMessage={fieldError('formDate')}
        >
          <Input type="date" value={formDate} onChange={(e) => onFieldChange('formDate', e.target.value)} />
        </Field>

        <Field
          label="Price"
          required
          validationState={fieldError('formPrice') ? 'error' : 'none'}
          validationMessage={fieldError('formPrice')}
        >
          <Input
            type="number"
            step="0.01"
            min="0"
            value={formPrice}
            onChange={(e) => onFieldChange('formPrice', e.target.value)}
          />
        </Field>
      </div>

      <div className={styles.actions}>
        <Button appearance="primary" disabled={isSaving} onClick={onSave}>
          {isSaving ? 'Saving...' : editingDate ? 'Save' : 'Add price'}
        </Button>
        <Button appearance="secondary" onClick={onCancel}>
          Cancel
        </Button>
      </div>

      {Object.keys(saveErrorFields).length === 0 && saveError && (
        <MessageBar intent="error">
          <MessageBarBody>{saveError}</MessageBarBody>
        </MessageBar>
      )}
    </div>
  )
}

interface DotProps {
  cx?: number
  cy?: number
  payload?: ChartPoint
  key?: React.Key | null
}

const DOT_LABELS: Record<ChartPoint['kind'], string> = {
  automatic: 'Automatic',
  manual: 'Manual',
  buy: 'Buy',
  sell: 'Sell',
}

function ChartDot({ cx, cy, payload }: DotProps) {
  if (cx === undefined || cy === undefined || !payload) return <g />
  const r = MARKER_RADIUS
  switch (payload.kind) {
    case 'automatic':
      return <circle cx={cx} cy={cy} r={3} fill={AUTOMATIC_DOT_COLOR} />
    case 'manual':
      return <circle cx={cx} cy={cy} r={3} fill={MANUAL_DOT_COLOR} />
    case 'buy':
      return <polygon points={`${cx},${cy - r} ${cx - r},${cy + r} ${cx + r},${cy + r}`} fill={BUY_COLOR} />
    case 'sell':
      return <polygon points={`${cx},${cy + r} ${cx - r},${cy - r} ${cx + r},${cy - r}`} fill={SELL_COLOR} />
  }
}

interface ChartTooltipProps {
  active?: boolean
  payload?: { payload: ChartPoint }[]
}

function ChartTooltip({ active, payload }: ChartTooltipProps) {
  const point = active ? payload?.[0]?.payload : undefined
  if (!point) return null

  return (
    <div className="price-history-tab__tooltip">
      <p>{DOT_LABELS[point.kind]}</p>
      <p>{point.date}</p>
      <p>{formatN2(point.value)}</p>
    </div>
  )
}

const LEGEND_PAYLOAD = [
  { value: 'Automatic', type: 'circle' as const, color: AUTOMATIC_DOT_COLOR },
  { value: 'Manual', type: 'circle' as const, color: MANUAL_DOT_COLOR },
  { value: 'Buy', type: 'triangle' as const, color: BUY_COLOR },
  { value: 'Sell', type: 'diamond' as const, color: SELL_COLOR },
]

// recharts' <Legend> clones a `content` *element* and injects its own auto-computed payload
// (derived from the chart's Line series) into it, overriding any payload we set on the element
// ourselves. Passing `content` as a function instead lets us apply our payload last.
function ChartLegendContent(props: DefaultLegendContentProps) {
  return <DefaultLegendContent {...props} payload={LEGEND_PAYLOAD} />
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
      <div className="price-history-tab__chart-container">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={chartData} margin={{ top: 8, right: 16, left: 8, bottom: 8 }}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="date" tick={{ fontSize: 11 }} />
            <YAxis tickFormatter={formatN2} tick={{ fontSize: 11 }} width={70} />
            <Tooltip content={<ChartTooltip />} />
            <Legend content={ChartLegendContent} />
            <Line
              type="monotone"
              dataKey="value"
              name="Price"
              stroke={LINE_COLOR}
              strokeWidth={2}
              dot={(props: DotProps) => <ChartDot key={props.key} {...props} />}
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
    saveErrorFields,
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
          saveErrorFields={saveErrorFields}
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
