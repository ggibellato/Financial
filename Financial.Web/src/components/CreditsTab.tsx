import { useCallback, useRef, useState } from 'react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  LabelList,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { CreditDto } from '../api/types'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import type { CreditFormField, FilterOption, ViewMode } from '../hooks/useCredits'
import { useCredits } from '../hooks/useCredits'
import './CreditsTab.css'

const DEFAULT_LEFT_WIDTH = 400
const MIN_LEFT_WIDTH = 200

function formatN2(value: number): string {
  return new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

function formatDate(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`
}

function toInputDate(iso: string): string {
  return iso.split('T')[0]
}

function buildMonthKey(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${pad(date.getMonth() + 1)}/${date.getFullYear()}`
}

const FILTER_OPTIONS: { value: FilterOption; label: string }[] = [
  { value: 'this-month', label: 'This month' },
  { value: 'last-3-months', label: 'Last 3 months' },
  { value: 'last-6-months', label: 'Last 6 months' },
  { value: 'last-year', label: 'Last year' },
  { value: 'all', label: 'All' },
]

const DIVIDEND_COLOR = '#4682b4'
const RENT_COLOR = '#6aabdb'

interface CreditRowProps {
  credit: CreditDto
  onEdit: (c: CreditDto) => void
  onDelete: (id: string) => void
}

function CreditRow({ credit, onEdit, onDelete }: CreditRowProps) {
  const typeClass =
    credit.type === 'Dividend'
      ? 'credits-tab__type--dividend'
      : 'credits-tab__type--rent'

  return (
    <tr>
      <td>
        <button
          className="credits-tab__action-btn"
          type="button"
          aria-label="Edit credit"
          onClick={() => onEdit(credit)}
        >
          ✏
        </button>
      </td>
      <td>
        <button
          className="credits-tab__action-btn"
          type="button"
          aria-label="Delete credit"
          onClick={() => onDelete(credit.id)}
        >
          ✕
        </button>
      </td>
      <td>{formatDate(credit.date)}</td>
      <td className={typeClass}>{credit.type}</td>
      <td className="credits-tab__value">{formatN2(credit.value)}</td>
    </tr>
  )
}

interface InlineFormProps {
  editingId: string | null
  formDate: string
  formType: string
  formValue: string
  isSaving: boolean
  saveError: string | null
  onFieldChange: (field: CreditFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

function InlineForm({
  editingId,
  formDate,
  formType,
  formValue,
  isSaving,
  saveError,
  onFieldChange,
  onSave,
  onCancel,
}: InlineFormProps) {
  const title = editingId ? 'Edit credit' : 'New credit'

  return (
    <div className="credits-tab__form">
      <p className="credits-tab__form-title">{title}</p>
      <div className="credits-tab__form-fields">
        <div className="credits-tab__form-field">
          <label htmlFor="cr-date">Date</label>
          <input
            id="cr-date"
            type="date"
            value={formDate}
            required
            onChange={(e) => onFieldChange('formDate', e.target.value)}
          />
        </div>
        <div className="credits-tab__form-field">
          <label htmlFor="cr-type">Type</label>
          <select
            id="cr-type"
            value={formType}
            onChange={(e) => onFieldChange('formType', e.target.value)}
          >
            <option value="Dividend">Dividend</option>
            <option value="Rent">Rent</option>
          </select>
        </div>
        <div className="credits-tab__form-field">
          <label htmlFor="cr-value">Value</label>
          <input
            id="cr-value"
            type="number"
            step="0.01"
            min="0"
            value={formValue}
            required
            onChange={(e) => onFieldChange('formValue', e.target.value)}
          />
        </div>
      </div>
      <div className="credits-tab__form-actions">
        <button
          className="credits-tab__save-btn"
          type="button"
          disabled={isSaving}
          onClick={onSave}
        >
          {isSaving ? 'Saving...' : 'Save'}
        </button>
        <button className="credits-tab__cancel-btn" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
      {saveError && <p className="credits-tab__error">{saveError}</p>}
    </div>
  )
}

interface ChartPanelProps {
  chartData: ReturnType<typeof useCredits>['chartData']
  selectedMode: ViewMode
}

function ChartPanel({ chartData, selectedMode }: ChartPanelProps) {
  const isStacked = selectedMode === 'Stacked'
  return (
    <div className="credits-tab__chart-panel">
      <p className="credits-tab__chart-title">Credits by Month</p>
      <div className="credits-tab__chart-container">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={chartData} margin={{ top: 8, right: 16, left: 8, bottom: 8 }}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="month" tick={{ fontSize: 11 }} />
            <YAxis tickFormatter={formatN2} tick={{ fontSize: 11 }} width={70} />
            <Tooltip formatter={(v) => (typeof v === 'number' ? formatN2(v) : v)} />
            <Legend />
            <Bar
              dataKey="Dividend"
              fill={DIVIDEND_COLOR}
              stackId={isStacked ? 'credits' : undefined}
            >
              <LabelList dataKey="Dividend" position="inside" formatter={(v: unknown) => typeof v === 'number' && v > 0 ? formatN2(v) : ''} style={{ fontSize: 10 }} />
            </Bar>
            <Bar
              dataKey="Rent"
              fill={RENT_COLOR}
              stackId={isStacked ? 'credits' : undefined}
            >
              <LabelList dataKey="Rent" position="inside" formatter={(v: unknown) => typeof v === 'number' && v > 0 ? formatN2(v) : ''} style={{ fontSize: 10 }} />
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  )
}

export default function CreditsTab() {
  const {
    credits,
    chartData,
    isLoading,
    error,
    retry,
    selectedFilter,
    selectedMode,
    setFilter,
    setMode,
    isFormVisible,
    editingId,
    formDate,
    formType,
    formValue,
    isSaving,
    saveError,
    deleteError,
    nodeType,
    showNewForm,
    showEditForm,
    cancelForm,
    setFormField,
    saveForm,
    deleteCredit,
  } = useCredits()

  const [leftWidth, setLeftWidth] = useState(DEFAULT_LEFT_WIDTH)
  const startX = useRef(0)
  const startWidth = useRef(0)

  const onHandleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      startX.current = e.clientX
      startWidth.current = leftWidth
      document.body.style.cursor = 'col-resize'
      document.body.style.userSelect = 'none'

      const handleMouseMove = (ev: MouseEvent) => {
        const delta = ev.clientX - startX.current
        const maxWidth = window.innerWidth / 2
        setLeftWidth(Math.max(MIN_LEFT_WIDTH, Math.min(startWidth.current + delta, maxWidth)))
      }

      const handleMouseUp = () => {
        document.removeEventListener('mousemove', handleMouseMove)
        document.removeEventListener('mouseup', handleMouseUp)
        document.body.style.cursor = ''
        document.body.style.userSelect = ''
      }

      document.addEventListener('mousemove', handleMouseMove)
      document.addEventListener('mouseup', handleMouseUp)
    },
    [leftWidth],
  )

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  const isAsset = nodeType === 'Asset'

  const toolbar = (
    <div className="credits-tab__controls">
      <div className="credits-tab__filters">
        {FILTER_OPTIONS.map((opt) => (
          <button
            key={opt.value}
            type="button"
            className={`credits-tab__filter-btn${selectedFilter === opt.value ? ' credits-tab__filter-btn--active' : ''}`}
            onClick={() => setFilter(opt.value)}
          >
            {opt.label}
          </button>
        ))}
      </div>
      <div className="credits-tab__modes">
        <span className="credits-tab__mode-label">View:</span>
        {(['Stacked', 'Grouped'] as ViewMode[]).map((mode) => (
          <button
            key={mode}
            type="button"
            className={`credits-tab__mode-btn${selectedMode === mode ? ' credits-tab__mode-btn--active' : ''}`}
            onClick={() => setMode(mode)}
          >
            {mode}
          </button>
        ))}
      </div>
    </div>
  )

  if (!isAsset) {
    return (
      <div className="credits-tab">
        {toolbar}
        <ChartPanel chartData={chartData} selectedMode={selectedMode} />
      </div>
    )
  }

  return (
    <div className="credits-tab">
      {toolbar}
      <div className="credits-tab__split">
        <div className="credits-tab__left" style={{ width: leftWidth }}>
          <div className="credits-tab__table-toolbar">
            <button className="credits-tab__new-btn" type="button" onClick={showNewForm}>
              New
            </button>
          </div>

          {isFormVisible && (
            <InlineForm
              editingId={editingId}
              formDate={formDate}
              formType={formType}
              formValue={formValue}
              isSaving={isSaving}
              saveError={saveError}
              onFieldChange={setFormField}
              onSave={saveForm}
              onCancel={cancelForm}
            />
          )}

          <div className="credits-tab__table-wrapper">
            <table className="credits-tab__table">
              <thead>
                <tr>
                  <th />
                  <th />
                  <th>Date</th>
                  <th>Type</th>
                  <th className="credits-tab__value">Value</th>
                </tr>
              </thead>
              <tbody>
                {credits.map((c) => (
                  <CreditRow
                    key={c.id}
                    credit={c}
                    onEdit={showEditForm}
                    onDelete={deleteCredit}
                  />
                ))}
              </tbody>
            </table>
          </div>

          {deleteError && <p className="credits-tab__delete-error">{deleteError}</p>}
        </div>

        <div
          className="credits-tab__handle"
          onMouseDown={onHandleMouseDown}
          aria-label="Resize panel"
        />

        <div className="credits-tab__right">
          <ChartPanel chartData={chartData} selectedMode={selectedMode} />
        </div>
      </div>
    </div>
  )
}

export { formatN2, formatDate, toInputDate, buildMonthKey }
