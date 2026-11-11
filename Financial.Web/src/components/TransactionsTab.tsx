import { useRef } from 'react'
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
import { Button, Field, Input, Select, Table, TableBody, TableHeader, TableHeaderCell, TableRow, Text } from '@fluentui/react-components'
import { AddRegular } from '@fluentui/react-icons'
import EmptyState from './EmptyState'
import type { TransactionDto } from '../api/types'
import ActionErrorAlert from './ActionErrorAlert'
import ConfirmDeleteDialog from './ConfirmDeleteDialog'
import ErrorState from './ErrorState'
import FilterTabList from './FilterTabList'
import FxProvenanceTooltip from './FxProvenanceTooltip'
import LoadingState from './LoadingState'
import LotAllocationPicker from './LotAllocationPicker'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useFormPanelStyles } from './formPanelStyles'
import { usePendingDelete } from '../hooks/usePendingDelete'
import { useSortableRows, DATE_DESC_SORT, type SortAccessor } from '../hooks/useSortableRows'
import { useFieldError } from '../hooks/useFieldError'
import { useOpenLots } from '../hooks/useOpenLots'
import { useRowFocusRecovery } from '../hooks/useRowFocusRecovery'
import type { ChartDisplayMode, TransactionFormField, TransactionMonthBucket } from '../hooks/useTransactions'
import { useTransactions } from '../hooks/useTransactions'
import { PERIOD_FILTER_OPTIONS } from '../utils/periodFilter'
import type { PeriodFilterOption } from '../utils/periodFilter'
import { formatN2, formatN8, formatShortDate, parseValidatedNumber, SAVING_LABEL } from '../utils/formatters'
import { hasOverAllocatedLot, isAllocationExact, sumAllocations } from '../utils/lotAllocation'
import ActionsColumnHeader from './grid/ActionsColumnHeader'
import RowActionsCell from './grid/RowActionsCell'
import './TransactionsTab.css'

// Matches the blue already established by CreditsTab/PriceHistoryTab
// (docs/ui/forms-data-and-visualisations.md's "Series color" rule) — not a
// neutral/grey, single-series charts are blue on both platforms.
const CHART_COLOR = '#4682b4'

const CHART_MODE_OPTIONS: { value: ChartDisplayMode; label: string }[] = [
  { value: 'Bar', label: 'Bar' },
  { value: 'Line', label: 'Line' },
]

const TRANSACTION_TYPE_OPTIONS = [
  { value: 'Buy', label: 'Buy' },
  { value: 'Sell', label: 'Sell' },
  { value: 'Fee', label: 'Fee' },
  { value: 'Redemption', label: 'Redemption' },
  { value: 'TransferIn', label: 'Transfer in' },
  { value: 'TransferOut', label: 'Transfer out' },
  { value: 'CapitalCall', label: 'Capital call' },
  { value: 'ReturnOfCapital', label: 'Return of capital' },
]

const SORT_ACCESSORS: Record<string, SortAccessor<TransactionDto>> = {
  date: (t) => new Date(t.date),
  type: (t) => t.type,
  quantity: (t) => t.quantity,
  unitPrice: (t) => t.unitPrice,
  fees: (t) => t.fees,
  withheld: (t) => t.withheld,
  total: (t) => t.netCash,
}

// Purely a display grouping (color coding), independent of the server's TransactionTypeEffect
// table: an outflow-typed transaction reads as "buy-like", an inflow-typed one as "sell-like",
// and a type with no cash effect of its own (a Transfer) as neutral.
const OUTFLOW_TYPES = new Set(['Buy', 'Fee', 'CapitalCall'])
const INFLOW_TYPES = new Set(['Sell', 'Redemption', 'ReturnOfCapital'])

function typeDisplayClass(type: string): string {
  if (OUTFLOW_TYPES.has(type)) return 'transactions-tab__type--buy'
  if (INFLOW_TYPES.has(type)) return 'transactions-tab__type--sell'
  return 'transactions-tab__type--neutral'
}

function typeDisplayLabel(type: string): string {
  return TRANSACTION_TYPE_OPTIONS.find((o) => o.value === type)?.label ?? type
}

function transactionName(transaction: TransactionDto): string {
  return `transaction ${typeDisplayLabel(transaction.type)} ${formatN2(transaction.netCash)}, ${formatShortDate(transaction.date)}`
}

interface TransactionRowProps {
  transaction: TransactionDto
  isDeleting: boolean
  onEdit: (t: TransactionDto) => void
  onDelete: (t: TransactionDto) => void
}

function TransactionRow({ transaction, isDeleting, onEdit, onDelete }: TransactionRowProps) {
  const date = formatShortDate(transaction.date)
  const typeLabel = typeDisplayLabel(transaction.type)
  const net = formatN2(transaction.netCash)

  return (
    <TableRow>
      <DataTableCell label="Date">{date}</DataTableCell>
      <DataTableCell label="Type" className={typeDisplayClass(transaction.type)}>
        {typeLabel}
      </DataTableCell>
      <DataTableCell label="Quantity" className="data-table__col--numeric">{formatN8(transaction.quantity)}</DataTableCell>
      <DataTableCell label="Unit price" className="data-table__col--numeric">{formatN2(transaction.unitPrice)}</DataTableCell>
      <DataTableCell label="Fees" className="data-table__col--numeric">{formatN2(transaction.fees)}</DataTableCell>
      <DataTableCell label="Withheld" className="data-table__col--numeric">{formatN2(transaction.withheld)}</DataTableCell>
      <DataTableCell label="Net" className="data-table__col--numeric transactions-tab__total">
        {net}
      </DataTableCell>
      <DataTableCell label="FX">
        <FxProvenanceTooltip currency={transaction.currency} fxRateSnapshot={transaction.fxRateSnapshot} />
      </DataTableCell>
      <RowActionsCell
        itemName={transactionName(transaction)}
        isDeleting={isDeleting}
        onEdit={() => onEdit(transaction)}
        onDelete={() => onDelete(transaction)}
      />
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
  formWithheld: string
  formLotAllocations: Record<string, string>
  formTypeHasQuantityEffect: boolean
  typeEffectsLoaded: boolean
  typeEffectsError: string | null
  onRetryTypeEffects: () => void
  requiresLotAllocation: boolean
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<TransactionFormField, string>>
  onFieldChange: (field: TransactionFormField, value: string) => void
  onLotAllocationChange: (sourceTransactionId: string, value: string) => void
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
  formWithheld,
  formLotAllocations,
  formTypeHasQuantityEffect,
  typeEffectsLoaded,
  typeEffectsError,
  onRetryTypeEffects,
  requiresLotAllocation,
  isSaving,
  saveError,
  saveErrorFields,
  onFieldChange,
  onLotAllocationChange,
  onSave,
  onCancel,
}: InlineFormProps) {
  const { openLots, isLoading: isLoadingOpenLots, error: openLotsError, retry: retryOpenLots } =
    useOpenLots(requiresLotAllocation)
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(saveErrorFields)
  const title = editingId ? 'Edit transaction' : 'New transaction'

  const saleQuantity = parseValidatedNumber(formQuantity) ?? 0
  const allocatedTotal = sumAllocations(formLotAllocations)
  const allocationSatisfied =
    !requiresLotAllocation ||
    (isAllocationExact(allocatedTotal, saleQuantity) && !hasOverAllocatedLot(openLots, formLotAllocations))

  if (typeEffectsError) {
    return (
      <div className={styles.panel}>
        <Text as="h2" weight="semibold" size={400}>
          {title}
        </Text>
        <ErrorState message={typeEffectsError} onRetry={onRetryTypeEffects} />
        <div className={styles.actions}>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </div>
    )
  }

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

        <Field label="Type">
          <Select value={formType} onChange={(e) => onFieldChange('formType', e.target.value)}>
            {TRANSACTION_TYPE_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </Field>

        {formTypeHasQuantityEffect && (
          <>
            <Field
              label="Quantity"
              required
              validationState={fieldError('formQuantity') ? 'error' : 'none'}
              validationMessage={fieldError('formQuantity')}
            >
              <Input
                type="number"
                step="0.0001"
                min="0"
                value={formQuantity}
                onChange={(e) => onFieldChange('formQuantity', e.target.value)}
              />
            </Field>

            <Field
              label="Unit price"
              required
              validationState={fieldError('formUnitPrice') ? 'error' : 'none'}
              validationMessage={fieldError('formUnitPrice')}
            >
              <Input
                type="number"
                step="0.0001"
                min="0"
                value={formUnitPrice}
                onChange={(e) => onFieldChange('formUnitPrice', e.target.value)}
              />
            </Field>
          </>
        )}

        <Field label="Fees">
          <Input
            type="number"
            step="0.0001"
            min="0"
            value={formFees}
            onChange={(e) => onFieldChange('formFees', e.target.value)}
          />
        </Field>

        <Field label="Withheld">
          <Input
            type="number"
            step="0.0001"
            min="0"
            value={formWithheld}
            onChange={(e) => onFieldChange('formWithheld', e.target.value)}
          />
        </Field>
      </div>

      {requiresLotAllocation && (
        <LotAllocationPicker
          openLots={openLots}
          isLoading={isLoadingOpenLots}
          error={openLotsError}
          onRetry={retryOpenLots}
          saleQuantity={saleQuantity}
          allocations={formLotAllocations}
          onChange={onLotAllocationChange}
          errorMessage={fieldError('formLotAllocations')}
        />
      )}

      <div className={styles.actions}>
        <Button appearance="primary" disabled={isSaving || !typeEffectsLoaded || !allocationSatisfied} onClick={onSave}>
          {isSaving ? SAVING_LABEL : editingId ? 'Save' : 'Add transaction'}
        </Button>
        <Button appearance="secondary" onClick={onCancel}>
          Cancel
        </Button>
      </div>

      <ActionErrorAlert message={Object.keys(saveErrorFields).length === 0 ? saveError : null} />
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
        <FilterTabList options={PERIOD_FILTER_OPTIONS} selected={selectedFilter} onSelect={setFilter} />
        <FilterTabList
          label="View:"
          options={CHART_MODE_OPTIONS}
          selected={selectedChartMode}
          onSelect={setChartMode}
        />
      </div>
      <p className="transactions-tab__chart-title">Net invested by month</p>
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
    formWithheld,
    formLotAllocations,
    formTypeHasQuantityEffect,
    typeEffectsLoaded,
    typeEffectsError,
    retryTypeEffects,
    requiresLotAllocation,
    isSaving,
    saveError,
    saveErrorFields,
    deletingId,
    deleteError,
    nodeType,
    showNewForm,
    showEditForm,
    cancelForm,
    setFormField,
    setLotAllocation,
    saveForm,
    deleteTransaction,
  } = useTransactions()

  const { sortedRows, sortState, requestSort } = useSortableRows(transactions, SORT_ACCESSORS, DATE_DESC_SORT)

  const deletion = usePendingDelete((row: TransactionDto) => deleteTransaction(row.id))
  const regionRef = useRef<HTMLDivElement | null>(null)
  const newTransactionRef = useRef<HTMLButtonElement | null>(null)
  const recordRowFocus = useRowFocusRecovery(regionRef, newTransactionRef, isLoading)

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
        <Button ref={newTransactionRef} appearance="primary" icon={<AddRegular />} onClick={showNewForm}>
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
          formWithheld={formWithheld}
          formLotAllocations={formLotAllocations}
          formTypeHasQuantityEffect={formTypeHasQuantityEffect}
          typeEffectsLoaded={typeEffectsLoaded}
          typeEffectsError={typeEffectsError}
          onRetryTypeEffects={retryTypeEffects}
          requiresLotAllocation={requiresLotAllocation}
          isSaving={isSaving}
          saveError={saveError}
          saveErrorFields={saveErrorFields}
          onFieldChange={setFormField}
          onLotAllocationChange={setLotAllocation}
          onSave={saveForm}
          onCancel={cancelForm}
        />
      )}

      {transactions.length === 0 ? (
        <EmptyState className="transactions-tab__empty">No transactions yet — nothing has been bought or sold in this holding.</EmptyState>
      ) : (
        <div ref={regionRef} className="transactions-tab__table-wrapper" onFocus={recordRowFocus}>
          <Table className="transactions-tab__table data-table">
            <TableHeader>
              <TableRow>
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
                  label="Unit price"
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
                  label="Withheld"
                  columnKey="withheld"
                  numeric
                  sortDirection={sortState?.columnKey === 'withheld' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <SortableColumnHeader
                  label="Net"
                  columnKey="total"
                  numeric
                  sortDirection={sortState?.columnKey === 'total' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <TableHeaderCell>FX</TableHeaderCell>
                <ActionsColumnHeader />
              </TableRow>
            </TableHeader>
            <TableBody>
              {sortedRows.map((t) => (
                <TransactionRow
                  key={t.id}
                  transaction={t}
                  isDeleting={deletingId === t.id}
                  onEdit={showEditForm}
                  onDelete={deletion.request}
                />
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <ActionErrorAlert message={deleteError} className="transactions-tab__delete-error" />

      {deletion.pendingRow && (
        <ConfirmDeleteDialog
          entityLabel="transaction"
          message={`Delete the ${transactionName(deletion.pendingRow)}? This cannot be undone.`}
          onConfirm={deletion.confirm}
          onCancel={deletion.cancel}
        />
      )}
    </div>
  )
}
