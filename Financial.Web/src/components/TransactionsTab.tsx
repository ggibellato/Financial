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
import { Button, Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import type { TransactionDto } from '../api/types'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import type { ChartDisplayMode, TransactionFormField, TransactionMonthBucket } from '../hooks/useTransactions'
import { useTransactions } from '../hooks/useTransactions'
import { confirmThenRun } from '../utils/confirmThenRun'
import { PERIOD_FILTER_OPTIONS } from '../utils/periodFilter'
import type { PeriodFilterOption } from '../utils/periodFilter'
import { formatN2, formatN8, formatShortDate } from '../utils/formatters'
import './TransactionsTab.css'

// Matches the blue already established by CreditsTab/PriceHistoryTab
// (docs/ui/forms-data-and-visualisations.md's "Series color" rule) — not a
// neutral/grey, single-series charts are blue on both platforms.
const CHART_COLOR = '#4682b4'

const SORT_ACCESSORS: Record<string, SortAccessor<TransactionDto>> = {
  date: (t) => new Date(t.date),
  type: (t) => t.type,
  quantity: (t) => t.quantity,
  unitPrice: (t) => t.unitPrice,
  fees: (t) => t.fees,
  total: (t) => t.totalPrice,
}

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
    <TableRow>
      <TableCell>
        <Button
          appearance="subtle"
          size="small"
          icon={<EditRegular />}
          aria-label="Edit transaction"
          onClick={() => onEdit(transaction)}
        />
      </TableCell>
      <TableCell>
        <Button
          appearance="subtle"
          size="small"
          icon={<DeleteRegular />}
          aria-label="Delete transaction"
          onClick={() => onDelete(transaction.id)}
        />
      </TableCell>
      <TableCell>{formatShortDate(transaction.date)}</TableCell>
      <TableCell className={typeClass}>{transaction.type}</TableCell>
      <TableCell className="data-table__col--numeric">{formatN8(transaction.quantity)}</TableCell>
      <TableCell className="data-table__col--numeric">{formatN2(transaction.unitPrice)}</TableCell>
      <TableCell className="data-table__col--numeric">{formatN2(transaction.fees)}</TableCell>
      <TableCell className="data-table__col--numeric transactions-tab__total">
        {formatN2(transaction.totalPrice)}
      </TableCell>
    </TableRow>
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
          {isSaving ? 'Saving...' : editingId ? 'Save' : 'Add transaction'}
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

  const { sortedRows, sortState, requestSort } = useSortableRows(transactions, SORT_ACCESSORS)

  const confirmAndDeleteTransaction = (id: string) =>
    confirmThenRun('Delete this transaction?', () => deleteTransaction(id))

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
        <Button appearance="primary" icon={<AddRegular />} onClick={showNewForm}>
          New transaction
        </Button>
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
        <Table className="transactions-tab__table data-table">
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
                label="Type"
                columnKey="type"
                sortDirection={sortState?.columnKey === 'type' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Quantity"
                columnKey="quantity"
                numeric
                sortDirection={sortState?.columnKey === 'quantity' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Unit Price"
                columnKey="unitPrice"
                numeric
                sortDirection={sortState?.columnKey === 'unitPrice' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Fees"
                columnKey="fees"
                numeric
                sortDirection={sortState?.columnKey === 'fees' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Total"
                columnKey="total"
                numeric
                sortDirection={sortState?.columnKey === 'total' ? sortState.direction : undefined}
                onSort={requestSort}
              />
            </TableRow>
          </TableHeader>
          <TableBody>
            {sortedRows.map((t) => (
              <TransactionRow
                key={t.id}
                transaction={t}
                onEdit={showEditForm}
                onDelete={confirmAndDeleteTransaction}
              />
            ))}
          </TableBody>
        </Table>
      </div>

      {deleteError && <p className="transactions-tab__delete-error">{deleteError}</p>}
    </div>
  )
}
