import {
  Bar,
  BarChart,
  CartesianGrid,
  LabelList,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { CreditDto } from '../api/types'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import SplitPanel from './SplitPanel'
import type { ChartType, CreditFormField, MonthBucket, ViewMode } from '../hooks/useCredits'
import { useCredits } from '../hooks/useCredits'
import { PERIOD_FILTER_OPTIONS } from '../utils/periodFilter'
import { formatN2, formatShortDate } from '../utils/formatters'
import './CreditsTab.css'

const DEFAULT_LEFT_WIDTH = 400
const MIN_LEFT_WIDTH = 200

const SINGLE_TYPE_COLOR = '#4682b4'
const PALETTE_START = { r: 173, g: 216, b: 230 }
const PALETTE_END = { r: 8, g: 81, b: 156 }

function lerpByte(from: number, to: number, t: number): number {
  return Math.round(from + (to - from) * t)
}

function buildPalette(count: number): string[] {
  if (count <= 0) return []
  if (count === 1) return [SINGLE_TYPE_COLOR]

  const colors: string[] = []
  for (let i = 0; i < count; i++) {
    const t = i / (count - 1)
    const r = lerpByte(PALETTE_START.r, PALETTE_END.r, t)
    const g = lerpByte(PALETTE_START.g, PALETTE_END.g, t)
    const b = lerpByte(PALETTE_START.b, PALETTE_END.b, t)
    colors.push(`rgb(${r}, ${g}, ${b})`)
  }
  return colors
}

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
          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M20 20H7L3 16a2 2 0 0 1 0-2.83L14.59 1.58a2 2 0 0 1 2.83 0l4 4a2 2 0 0 1 0 2.83L8 20" />
            <path d="M6.5 15.5 15 7" />
          </svg>
        </button>
      </td>
      <td>{formatShortDate(credit.date)}</td>
      <td className={typeClass}>{credit.type}</td>
      <td className="data-table__col--numeric credits-tab__value">{formatN2(credit.value)}</td>
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

function typeValue(type: string) {
  return (bucket: MonthBucket) => bucket.byType[type] ?? 0
}

interface ChartPanelProps {
  chartData: ReturnType<typeof useCredits>['chartData']
  creditTypes: string[]
  selectedMode: ViewMode
  selectedChartType: ChartType
}

function ChartPanel({ chartData, creditTypes, selectedMode, selectedChartType }: ChartPanelProps) {
  const isStacked = selectedMode === 'Stacked'
  const palette = buildPalette(creditTypes.length)

  return (
    <div className="credits-tab__chart-panel">
      <p className="credits-tab__chart-title">Credits by Month</p>
      <div className="credits-tab__chart-container">
        <ResponsiveContainer width="100%" height="100%">
          {selectedChartType === 'Bar' ? (
            <BarChart data={chartData} margin={{ top: 8, right: 16, left: 8, bottom: 8 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="month" tick={{ fontSize: 11 }} />
              <YAxis tickFormatter={formatN2} tick={{ fontSize: 11 }} width={70} />
              <Tooltip formatter={(v) => (typeof v === 'number' ? formatN2(v) : v)} />
              <Legend />
              {creditTypes.map((type, index) => (
                <Bar
                  key={type}
                  dataKey={typeValue(type)}
                  name={type}
                  fill={palette[index]}
                  stackId={isStacked ? 'credits' : undefined}
                >
                  <LabelList
                    dataKey={typeValue(type)}
                    position="inside"
                    formatter={(v: unknown) => (typeof v === 'number' && v > 0 ? formatN2(v) : '')}
                    style={{ fontSize: 10 }}
                  />
                </Bar>
              ))}
            </BarChart>
          ) : (
            <LineChart data={chartData} margin={{ top: 8, right: 16, left: 8, bottom: 8 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="month" tick={{ fontSize: 11 }} />
              <YAxis tickFormatter={formatN2} tick={{ fontSize: 11 }} width={70} />
              <Tooltip formatter={(v) => (typeof v === 'number' ? formatN2(v) : v)} />
              <Legend />
              {isStacked ? (
                creditTypes.map((type, index) => (
                  <Line
                    key={type}
                    type="monotone"
                    dataKey={typeValue(type)}
                    name={type}
                    stroke={palette[index]}
                    strokeWidth={2}
                    dot={{ r: 3 }}
                  />
                ))
              ) : (
                <Line
                  type="monotone"
                  dataKey="total"
                  name="Total"
                  stroke={SINGLE_TYPE_COLOR}
                  strokeWidth={2}
                  dot={{ r: 3 }}
                />
              )}
            </LineChart>
          )}
        </ResponsiveContainer>
      </div>
    </div>
  )
}

export default function CreditsTab() {
  const {
    credits,
    chartData,
    creditTypes,
    isLoading,
    error,
    retry,
    selectedFilter,
    selectedMode,
    selectedChartType,
    setFilter,
    setMode,
    setChartType,
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
        {PERIOD_FILTER_OPTIONS.map((opt) => (
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
        {(['Bar', 'Line'] as ChartType[]).map((chartType) => (
          <button
            key={chartType}
            type="button"
            className={`credits-tab__mode-btn${selectedChartType === chartType ? ' credits-tab__mode-btn--active' : ''}`}
            onClick={() => setChartType(chartType)}
          >
            {chartType}
          </button>
        ))}
      </div>
      <div className="credits-tab__modes">
        <span className="credits-tab__mode-label">Group:</span>
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
        <ChartPanel
          chartData={chartData}
          creditTypes={creditTypes}
          selectedMode={selectedMode}
          selectedChartType={selectedChartType}
        />
      </div>
    )
  }

  const leftPanel = (
    <div className="credits-tab__left">
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
        <table className="credits-tab__table data-table">
          <thead>
            <tr>
              <th />
              <th />
              <th>Date</th>
              <th>Type</th>
              <th className="data-table__col--numeric credits-tab__value">Value</th>
            </tr>
          </thead>
          <tbody>
            {credits.map((c) => (
              <CreditRow key={c.id} credit={c} onEdit={showEditForm} onDelete={deleteCredit} />
            ))}
          </tbody>
        </table>
      </div>

      {deleteError && <p className="credits-tab__delete-error">{deleteError}</p>}
    </div>
  )

  const rightPanel = (
    <div className="credits-tab__right">
      <ChartPanel
        chartData={chartData}
        creditTypes={creditTypes}
        selectedMode={selectedMode}
        selectedChartType={selectedChartType}
      />
    </div>
  )

  return (
    <div className="credits-tab">
      {toolbar}
      <div className="credits-tab__split">
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
