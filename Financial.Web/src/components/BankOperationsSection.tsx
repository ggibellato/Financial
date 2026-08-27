import { Button } from '@fluentui/react-components'
import { AddRegular, DeleteRegular } from '@fluentui/react-icons'
import type { BalanceAdjustmentDto, BankDto, TransferDto } from '../api/types'
import { ALL_BANKS_FILTER, type BankOperationEntry } from '../hooks/useBankOperations'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
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
    <tr>
      <td>
        <button
          className="data-table__action-btn"
          type="button"
          aria-label={isTransfer ? 'Edit transfer' : 'Edit balance adjustment'}
          onClick={() => (isTransfer ? onEditTransfer(entry.transfer) : onEditAdjustment(entry.adjustment))}
        >
          ✏
        </button>
      </td>
      <td>
        <button
          className="data-table__action-btn"
          type="button"
          aria-label={isTransfer ? 'Delete transfer' : 'Delete balance adjustment'}
          onClick={() => (isTransfer ? onDeleteTransfer(entry.id) : onDeleteAdjustment(entry.bankId, entry.id))}
        >
          <DeleteRegular />
        </button>
      </td>
      <td>{formatShortDate(entry.date)}</td>
      <td>{isTransfer ? 'Transfer' : 'Adjustment'}</td>
      <td>{isTransfer ? `${entry.sourceBank} → ${entry.destinationBank}` : entry.bank}</td>
      <td className="data-table__col--numeric">{formatN2(isTransfer ? entry.amount : entry.delta)}</td>
      <td>{entry.note ?? ''}</td>
    </tr>
  )
}

const SORT_ACCESSORS: Record<string, SortAccessor<BankOperationEntry>> = {
  date: (entry) => new Date(entry.date),
  type: (entry) => (entry.kind === 'transfer' ? 'Transfer' : 'Adjustment'),
  bank: (entry) => (entry.kind === 'transfer' ? `${entry.sourceBank} → ${entry.destinationBank}` : entry.bank),
  amount: (entry) => (entry.kind === 'transfer' ? entry.amount : entry.delta),
  note: (entry) => entry.note,
}

function emptyStateMessage(bankFilter: string): string {
  return bankFilter === ALL_BANKS_FILTER
    ? 'No transfers or balance corrections this month.'
    : `No transfers or balance corrections this month for ${bankFilter}.`
}

interface BankOperationsSectionProps {
  operations: BankOperationEntry[]
  bankFilter: string
  banks: BankDto[]
  onBankFilterChange: (bankFilter: string) => void
  onNewTransfer: () => void
  onNewBalanceCorrection: () => void
  onEditTransfer: (transfer: TransferDto) => void
  onEditAdjustment: (adjustment: BalanceAdjustmentDto) => void
  onDeleteTransfer: (id: string) => void
  onDeleteAdjustment: (bankName: string, id: string) => void
}

export default function BankOperationsSection({
  operations,
  bankFilter,
  banks,
  onBankFilterChange,
  onNewTransfer,
  onNewBalanceCorrection,
  onEditTransfer,
  onEditAdjustment,
  onDeleteTransfer,
  onDeleteAdjustment,
}: BankOperationsSectionProps) {
  const { sortedRows, sortState, requestSort } = useSortableRows(operations, SORT_ACCESSORS)

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
        <div className="bank-operations-section__filter">
          <label htmlFor="bank-operations-filter">Filter by Bank</label>
          <select
            id="bank-operations-filter"
            value={bankFilter}
            onChange={(e) => onBankFilterChange(e.target.value)}
          >
            <option value={ALL_BANKS_FILTER}>{ALL_BANKS_FILTER}</option>
            {banks.map((b) => (
              <option key={b.name} value={b.name}>
                {b.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {operations.length === 0 ? (
        <p className="bank-operations-section__empty">{emptyStateMessage(bankFilter)}</p>
      ) : (
        <div className="bank-operations-section__table-wrapper">
          <table className="bank-operations-section__table data-table">
            <thead>
              <tr>
                <th />
                <th />
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
                />
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
              </tr>
            </thead>
            <tbody>
              {sortedRows.map((entry) => (
                <OperationRow
                  key={`${entry.kind}-${entry.id}`}
                  entry={entry}
                  onEditTransfer={onEditTransfer}
                  onEditAdjustment={onEditAdjustment}
                  onDeleteTransfer={onDeleteTransfer}
                  onDeleteAdjustment={onDeleteAdjustment}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
