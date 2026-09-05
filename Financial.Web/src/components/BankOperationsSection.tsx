import { Button, Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import type { BalanceAdjustmentDto, TransferDto } from '../api/types'
import type { BankOperationEntry } from '../hooks/useBankOperations'
import SortableColumnHeader from './grid/SortableColumnHeader'
import ColumnFilterMenu from './grid/ColumnFilterMenu'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useColumnFilters } from '../hooks/useColumnFilters'
import { formatN2, formatShortDate } from '../utils/formatters'
import './BankOperationsSection.css'

interface OperationRowProps {
  entry: BankOperationEntry
  onEditTransfer: (transfer: TransferDto) => void
  onEditAdjustment: (adjustment: BalanceAdjustmentDto) => void
  onDeleteTransfer: (id: string) => void
  onDeleteAdjustment: (bankName: string, id: string) => void
}

function OperationRow({ entry, onEditTransfer, onEditAdjustment, onDeleteTransfer, onDeleteAdjustment }: OperationRowProps) {
  const isTransfer = entry.kind === 'transfer'

  return (
    <TableRow>
      <TableCell>{formatShortDate(entry.date)}</TableCell>
      <TableCell>{isTransfer ? 'Transfer' : 'Adjustment'}</TableCell>
      <TableCell>{isTransfer ? `${entry.sourceBank} → ${entry.destinationBank}` : entry.bank}</TableCell>
      <TableCell className="data-table__col--numeric">{formatN2(isTransfer ? entry.amount : entry.delta)}</TableCell>
      <TableCell>{entry.note ?? ''}</TableCell>
      <TableCell className="data-table__col--action">
        <div className="data-table__actions-cell">
          <Button
            appearance="subtle"
            size="small"
            icon={<EditRegular />}
            aria-label={isTransfer ? 'Edit transfer' : 'Edit balance adjustment'}
            onClick={() => (isTransfer ? onEditTransfer(entry.transfer) : onEditAdjustment(entry.adjustment))}
          />
          <Button
            appearance="subtle"
            size="small"
            icon={<DeleteRegular />}
            aria-label={isTransfer ? 'Delete transfer' : 'Delete balance adjustment'}
            onClick={() => (isTransfer ? onDeleteTransfer(entry.id) : onDeleteAdjustment(entry.bankId, entry.id))}
          />
        </div>
      </TableCell>
    </TableRow>
  )
}

const SORT_ACCESSORS: Record<string, SortAccessor<BankOperationEntry>> = {
  date: (entry) => new Date(entry.date),
  type: (entry) => (entry.kind === 'transfer' ? 'Transfer' : 'Adjustment'),
  bank: (entry) => (entry.kind === 'transfer' ? `${entry.sourceBank} → ${entry.destinationBank}` : entry.bank),
  amount: (entry) => (entry.kind === 'transfer' ? entry.amount : entry.delta),
  note: (entry) => entry.note,
}

const FILTER_ACCESSORS = {
  bank: (entry: BankOperationEntry) =>
    entry.kind === 'transfer' ? [entry.sourceBank, entry.destinationBank] : [entry.bank],
}

interface BankOperationsSectionProps {
  operations: BankOperationEntry[]
  onNewTransfer: () => void
  onNewBalanceCorrection: () => void
  onEditTransfer: (transfer: TransferDto) => void
  onEditAdjustment: (adjustment: BalanceAdjustmentDto) => void
  onDeleteTransfer: (id: string) => void
  onDeleteAdjustment: (bankName: string, id: string) => void
}

export default function BankOperationsSection({
  operations,
  onNewTransfer,
  onNewBalanceCorrection,
  onEditTransfer,
  onEditAdjustment,
  onDeleteTransfer,
  onDeleteAdjustment,
}: BankOperationsSectionProps) {
  const { filteredRows, availableValues, selectedValues, toggleValue, toggleAll, isColumnFiltered } =
    useColumnFilters(operations, FILTER_ACCESSORS)
  const { sortedRows, sortState, requestSort } = useSortableRows(filteredRows, SORT_ACCESSORS)

  return (
    <section className="bank-operations-section">
      <div className="bank-operations-section__header">
        <div className="bank-operations-section__actions">
          <Button appearance="primary" icon={<AddRegular />} onClick={onNewTransfer}>
            New Transfer
          </Button>
          <Button appearance="primary" icon={<AddRegular />} onClick={onNewBalanceCorrection}>
            New Balance Correction
          </Button>
        </div>
      </div>

      {operations.length === 0 ? (
        <p className="bank-operations-section__empty">No transfers or balance corrections this month.</p>
      ) : (
        <div className="bank-operations-section__table-wrapper">
          <Table className="bank-operations-section__table data-table">
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
                  label="Bank(s)"
                  columnKey="bank"
                  sortDirection={sortState?.columnKey === 'bank' ? sortState.direction : undefined}
                  onSort={requestSort}
                >
                  <ColumnFilterMenu
                    columnKey="bank"
                    label="Bank"
                    availableValues={availableValues.bank}
                    selectedValues={selectedValues.bank}
                    onToggleValue={toggleValue}
                    onToggleAll={toggleAll}
                    isFiltered={isColumnFiltered('bank')}
                  />
                </SortableColumnHeader>
                <SortableColumnHeader
                  label="Amount/Delta"
                  columnKey="amount"
                  numeric
                  sortDirection={sortState?.columnKey === 'amount' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <SortableColumnHeader
                  label="Note"
                  columnKey="note"
                  sortDirection={sortState?.columnKey === 'note' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <TableHeaderCell className="data-table__col--action" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {sortedRows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6}>No rows match the current filters</TableCell>
                </TableRow>
              ) : (
                sortedRows.map((entry) => (
                  <OperationRow
                    key={`${entry.kind}-${entry.id}`}
                    entry={entry}
                    onEditTransfer={onEditTransfer}
                    onEditAdjustment={onEditAdjustment}
                    onDeleteTransfer={onDeleteTransfer}
                    onDeleteAdjustment={onDeleteAdjustment}
                  />
                ))
              )}
            </TableBody>
          </Table>
        </div>
      )}
    </section>
  )
}
