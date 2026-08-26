import {
  Bar,
  BarChart,
  CartesianGrid,
  LabelList,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { DeleteRegular } from '@fluentui/react-icons'
import type { TransactionDto } from '../api/types'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import type { ChartDisplayMode, TransactionFormField, TransactionMonthBucket } from '../hooks/useTransactions'
import { useTransactions } from '../hooks/useTransactions'
import { PERIOD_FILTER_OPTIONS } from '../utils/periodFilter'
import type { PeriodFilterOption } from '../utils/periodFilter'
import { formatN2, formatN8, formatShortDate } from '../utils/formatters'
import './TransactionsTab.css'

// Matches the blue already established by CreditsTab/PriceHistoryTab
// (docs/ui/forms-data-and-visualisations.md's "Series color" rule) — not a
// neutral/grey, single-series charts are blue on both platforms.
const CHART_COLOR = '#4682b4'

interface TransactionRowProps {
  transaction: TransactionDto
  onEdit: (t: TransactionDto) => void
  onDelete: (id: string) => void
}

function TransactionRow({ transaction, onEdit, onDelete }: TransactionRowProps) {
  const typeClass =
    transaction.type === 'Buy'
      ? 'transactions-tab__type--buy'
      : 'transactions-tab__type--sell'

  return (
    <tr>
      <td>
        <button
          className="transactions-tab__action-btn"
          type="button"
          aria-label="Edit transaction"
          onClick={() => onEdit(transaction)}
        >
          ✏
        </button>
      </td>
      <td>
        <button
          className="transactions-tab__action-btn"
          type="button"
          aria-label="Delete transaction"
          onClick={() => onDelete(transaction.id)}
        >
          <DeleteRegular />
        </button>
      </td>
      <td>{formatShortDate(transaction.date)}</td>
      <td className={typeClass}>{transaction.type}</td>
      <td className="data-table__col--numeric">{formatN8(transaction.quantity)}</td>
      <td className="data-table__col--numeric">{formatN2(transaction.unitPrice)}</td>
      <td className="data-table__col--numeric">{formatN2(transaction.fees)}</td>
      <td className="data-table__col--numeric transactions-tab__total">{formatN2(transaction.totalPrice)}</td>
    </tr>
  )
}

interface InlineFormProps {
  editingId: string | null
  formDate: string
  formType: string
  formQuantity: string
  formUnitPrice: string
  formFees: string
  isSaving: boolean
  saveError: string | null
  onFieldChange: (field: TransactionFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

function InlineForm({
  editingId,
  formDate,
  formType,
  formQuantity,
  formUnitPrice,
  formFees,
  isSaving,
  saveError,
  onFieldChange,
  onSave,
  onCancel,
}: InlineFormProps) {
  const title = editingId ? 'Edit transaction' : 'New transaction'

  return (
    <div className="transactions-tab__form">
      <p className="transactions-tab__form-title">{title}</p>
      <div className="transactions-tab__form-fields">
        <div className="transactions-tab__form-field">
          <label htmlFor="tx-date">Date</label>
          <input
            id="tx-date"
            type="date"
            value={formDate}
            required
            onChange={(e) => onFieldChange('formDate', e.target.value)}
          />
        </div>
        <div className="transactions-tab__form-field">
          <label htmlFor="tx-type">Type</label>
          <select
            id="tx-type"
            value={formType}
            onChange={(e) => onFieldChange('formType', e.target.value)}
          >
            <option value="Buy">Buy</option>
            <option value="Sell">Sell</option>
          </select>
        </div>
        <div className="transactions-tab__form-field">
          <label htmlFor="tx-quantity">Quantity</label>
          <input
            id="tx-quantity"
            type="number"
            step="0.0001"
            min="0"
            value={formQuantity}
            required
            onChange={(e) => onFieldChange('formQuantity', e.target.value)}
          />
        </div>
        <div className="transactions-tab__form-field">
          <label htmlFor="tx-unit-price">Unit Price</label>
          <input
            id="tx-unit-price"
            type="number"
            step="0.0001"
            min="0"
            value={formUnitPrice}
            required
            onChange={(e) => onFieldChange('formUnitPrice', e.target.value)}
          />
        </div>
        <div className="transactions-tab__form-field">
          <label htmlFor="tx-fees">Fees</label>
          <input
            id="tx-fees"
            type="number"
            step="0.0001"
            min="0"
            value={formFees}
            onChange={(e) => onFieldChange('formFees', e.target.value)}
          />
        </div>
      </div>
      <div className="transactions-tab__form-actions">
        <button
          className="transactions-tab__save-btn"
          type="button"
          disabled={isSaving}
          onClick={onSave}
        >
          {isSaving ? 'Saving...' : 'Save'}
        </button>
        <button className="transactions-tab__cancel-btn" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
      {saveError && <p className="transactions-tab__error">{saveError}</p>}
    </div>
  )
}

interface TransactionsChartProps {
  chartData: TransactionMonthBucket[]
  selectedFilter: PeriodFilterOption
  selectedChartMode: ChartDisplayMode
  setFilter: (filter: PeriodFilterOption) => void
  setChartMode: (mode: ChartDisplayMode) => void
  compact?: boolean
}

function TransactionsChart({
  chartData,
  selectedFilter,
  selectedChartMode,
  setFilter,
  setChartMode,
  compact,
}: TransactionsChartProps) {
  return (
    <div
      className={`transactions-tab__chart-panel${compact ? ' transactions-tab__chart-panel--compact' : ''}`}
    >
      <div className="transactions-tab__controls">
        <div className="transactions-tab__filters">
          {PERIOD_FILTER_OPTIONS.map((opt) => (
            <button
              key={opt.value}
              type="button"
              className={`transactions-tab__filter-btn${selectedFilter === opt.value ? ' transactions-tab__filter-btn--active' : ''}`}
              onClick={() => setFilter(opt.value)}
            >
              {opt.label}
            </button>
          ))}
        </div>
        <div className="transactions-tab__modes">
          <span className="transactions-tab__mode-label">View:</span>
          {(['Bar', 'Line'] as ChartDisplayMode[]).map((mode) => (
            <button
              key={mode}
              type="button"
              className={`transactions-tab__mode-btn${selectedChartMode === mode ? ' transactions-tab__mode-btn--active' : ''}`}
              onClick={() => setChartMode(mode)}
            >
              {mode}
            </button>
          ))}
        </div>
      </div>
      <p className="transactions-tab__chart-title">Net Invested by Month</p>
      <div className="transactions-tab__chart-container">
        <ResponsiveContainer width="100%" height="100%">
          {selectedChartMode === 'Bar' ? (
            <BarChart data={chartData} margin={{ top: 8, right: 16, left: 8, bottom: 8 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="month" tick={{ fontSize: 11 }} />
              <YAxis tickFormatter={formatN2} tick={{ fontSize: 11 }} width={70} />
              <Tooltip formatter={(v) => (typeof v === 'number' ? formatN2(v) : v)} />
              <Bar dataKey="netInvested" fill={CHART_COLOR}>
                <LabelList
                  dataKey="netInvested"
                  position="top"
                  fill="#111"
                  formatter={(v: unknown) => (typeof v === 'number' && v !== 0 ? formatN2(v) : '')}
                  style={{ fontSize: 10 }}
                />
              </Bar>
            </BarChart>
          ) : (
            <LineChart data={chartData} margin={{ top: 8, right: 16, left: 8, bottom: 8 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="month" tick={{ fontSize: 11 }} />
              <YAxis tickFormatter={formatN2} tick={{ fontSize: 11 }} width={70} />
              <Tooltip formatter={(v) => (typeof v === 'number' ? formatN2(v) : v)} />
              <Line
                type="monotone"
                dataKey="netInvested"
                stroke={CHART_COLOR}
                strokeWidth={2}
                dot={{ r: 3 }}
              />
            </LineChart>
          )}
        </ResponsiveContainer>
      </div>
    </div>
  )
}

export default function TransactionsTab() {
  const {
    isLoading,
    error,
    retry,
    transactions,
    chartData,
    selectedFilter,
    selectedChartMode,
    setFilter,
    setChartMode,
    isFormVisible,
    editingId,
    formDate,
    formType,
    formQuantity,
    formUnitPrice,
    formFees,
    isSaving,
    saveError,
    deleteError,
    nodeType,
    showNewForm,
    showEditForm,
    cancelForm,
    setFormField,
    saveForm,
    deleteTransaction,
  } = useTransactions()

  // Confirmation belongs to the caller, not to the data hook. A hook that calls window.confirm
  // can only be tested by stubbing a browser global, and it decides for every caller that a
  // prompt is wanted at all. Same reasoning as ControleMaePage and MensaisPage, which already
  // did it this way.
  const confirmAndDeleteTransaction = (id: string) => {
    if (window.confirm('Delete this transaction?')) deleteTransaction(id)
  }

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  if (nodeType !== 'Asset') {
    return (
      <div className="transactions-tab">
        <TransactionsChart
          chartData={chartData}
          selectedFilter={selectedFilter}
          selectedChartMode={selectedChartMode}
          setFilter={setFilter}
          setChartMode={setChartMode}
        />
      </div>
    )
  }

  return (
    <div className="transactions-tab">
      <TransactionsChart
        chartData={chartData}
        selectedFilter={selectedFilter}
        selectedChartMode={selectedChartMode}
        setFilter={setFilter}
        setChartMode={setChartMode}
        compact
      />

      <div className="transactions-tab__toolbar">
        <button className="transactions-tab__new-btn" type="button" onClick={showNewForm}>
          New
        </button>
      </div>

      {isFormVisible && (
        <InlineForm
          editingId={editingId}
          formDate={formDate}
          formType={formType}
          formQuantity={formQuantity}
          formUnitPrice={formUnitPrice}
          formFees={formFees}
          isSaving={isSaving}
          saveError={saveError}
          onFieldChange={setFormField}
          onSave={saveForm}
          onCancel={cancelForm}
        />
      )}

      <div className="transactions-tab__table-wrapper">
        <table className="transactions-tab__table data-table">
          <thead>
            <tr>
              <th />
              <th />
              <th>Date</th>
              <th>Type</th>
              <th className="data-table__col--numeric">Quantity</th>
              <th className="data-table__col--numeric">Unit Price</th>
              <th className="data-table__col--numeric">Fees</th>
              <th className="data-table__col--numeric transactions-tab__total">Total</th>
            </tr>
          </thead>
          <tbody>
            {transactions.map((t) => (
              <TransactionRow
                key={t.id}
                transaction={t}
                onEdit={showEditForm}
                onDelete={confirmAndDeleteTransaction}
              />
            ))}
          </tbody>
        </table>
      </div>

      {deleteError && <p className="transactions-tab__delete-error">{deleteError}</p>}
    </div>
  )
}
