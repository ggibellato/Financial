import type { BalanceAdjustmentDto, BankDto, TransferDto } from '../api/types'
import { ALL_BANKS_FILTER, type BankOperationEntry } from '../hooks/useBankOperations'
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
          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M20 20H7L3 16a2 2 0 0 1 0-2.83L14.59 1.58a2 2 0 0 1 2.83 0l4 4a2 2 0 0 1 0 2.83L8 20" />
            <path d="M6.5 15.5 15 7" />
          </svg>
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

/** Bank tab body: the two entry-point buttons, the bank filter, and the flat operations list. */
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
  return (
    <section className="bank-operations-section">
      <div className="bank-operations-section__header">
        <div className="bank-operations-section__actions">
          <button className="bank-operations-section__new-btn" type="button" onClick={onNewTransfer}>
            + New Transfer
          </button>
          <button className="bank-operations-section__new-btn" type="button" onClick={onNewBalanceCorrection}>
            + New Balance Correction
          </button>
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
                <th>Date</th>
                <th>Type</th>
                <th>Bank(s)</th>
                <th className="data-table__col--numeric">Amount/Delta</th>
                <th>Note</th>
              </tr>
            </thead>
            <tbody>
              {operations.map((entry) => (
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
