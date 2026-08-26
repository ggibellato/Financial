import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { DeleteRegular } from '@fluentui/react-icons'
import type { AssetPriceSnapshotDto } from '../api/types'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import SplitPanel from './SplitPanel'
import type { PriceHistoryFormField } from '../hooks/usePriceHistory'
import { usePriceHistory } from '../hooks/usePriceHistory'
import { PERIOD_FILTER_OPTIONS } from '../utils/periodFilter'
import { formatN2, formatShortDate } from '../utils/formatters'
import './PriceHistoryTab.css'

const DEFAULT_LEFT_WIDTH = 400
const MIN_LEFT_WIDTH = 200
const LINE_COLOR = '#4682b4'
const MANUAL_DOT_COLOR = '#e65100'
const AUTOMATIC_DOT_COLOR = '#4682b4'

interface PriceRowProps {
  entry: AssetPriceSnapshotDto
  onEdit: (entry: AssetPriceSnapshotDto) => void
  onDelete: (date: string) => void
}

function PriceRow({ entry, onEdit, onDelete }: PriceRowProps) {
  return (
    <tr>
      <td>
        {entry.isManual && (
          <button
            className="price-history-tab__action-btn"
            type="button"
            aria-label="Edit price"
            onClick={() => onEdit(entry)}
          >
            ✏
          </button>
        )}
      </td>
      <td>
        {entry.isManual && (
          <button
            className="price-history-tab__action-btn"
            type="button"
            aria-label="Delete price"
            onClick={() => onDelete(entry.date)}
          >
            <DeleteRegular />
          </button>
        )}
      </td>
      <td>{formatShortDate(entry.date)}</td>
      <td className="data-table__col--numeric price-history-tab__price">{formatN2(entry.price)}</td>
      <td className={entry.isManual ? 'price-history-tab__source--manual' : 'price-history-tab__source--automatic'}>
        {entry.isManual ? 'Manual' : 'Automatic'}
      </td>
    </tr>
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
          {isSaving ? 'Saving...' : 'Save'}
        </button>
        <button className="price-history-tab__cancel-btn" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
      {saveError && <p className="price-history-tab__error">{saveError}</p>}
    </div>
  )
}

interface ChartPanelProps {
  entries: AssetPriceSnapshotDto[]
}

function ChartPanel({ entries }: ChartPanelProps) {
  const chartData = [...entries]
    .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())
    .map((entry) => ({ date: formatShortDate(entry.date), price: entry.price, isManual: entry.isManual }))

  return (
    <div className="price-history-tab__chart-panel">
      <p className="price-history-tab__chart-title">Price History</p>
      <div className="price-history-tab__chart-container">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={chartData} margin={{ top: 8, right: 16, left: 8, bottom: 8 }}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="date" tick={{ fontSize: 11 }} />
            <YAxis tickFormatter={formatN2} tick={{ fontSize: 11 }} width={70} />
            <Tooltip formatter={(v) => (typeof v === 'number' ? formatN2(v) : v)} />
            <Line
              type="monotone"
              dataKey="price"
              name="Price"
              stroke={LINE_COLOR}
              strokeWidth={2}
              dot={(props: {
                cx?: number
                cy?: number
                payload?: { isManual: boolean }
                key?: React.Key | null
              }) => {
                const { cx, cy, payload, key } = props
                if (cx === undefined || cy === undefined) return <g key={key ?? undefined} />
                return (
                  <circle
                    key={key ?? undefined}
                    cx={cx}
                    cy={cy}
                    r={3}
                    fill={payload?.isManual ? MANUAL_DOT_COLOR : AUTOMATIC_DOT_COLOR}
                  />
                )
              }}
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

  // Confirmation belongs to the caller, not to the data hook. A hook that calls window.confirm
  // can only be tested by stubbing a browser global, and it decides for every caller that a
  // prompt is wanted at all. Same reasoning as ControleMaePage and MensaisPage, which already
  // did it this way.
  const confirmAndDeleteEntry = (date: string) => {
    if (window.confirm('Delete this price entry?')) deleteEntry(date)
  }

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
        <button className="price-history-tab__new-btn" type="button" onClick={showNewForm}>
          New
        </button>
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
        <table className="price-history-tab__table data-table">
          <thead>
            <tr>
              <th />
              <th />
              <th>Date</th>
              <th className="data-table__col--numeric price-history-tab__price">Price</th>
              <th>Source</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((entry) => (
              <PriceRow key={entry.date} entry={entry} onEdit={showEditForm} onDelete={confirmAndDeleteEntry} />
            ))}
          </tbody>
        </table>
      </div>

      {deleteError && <p className="price-history-tab__delete-error">{deleteError}</p>}
    </div>
  )

  const rightPanel = (
    <div className="price-history-tab__right">
      <ChartPanel entries={filteredEntries} />
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
