import { Fragment } from 'react'
import type { BalanceAdjustmentDto, TransferDto } from '../api/types'
import type { BankHistoryEntry } from '../hooks/useBankHistory'
import type { BankTotal } from '../hooks/useMonthly'
import { formatN2, formatShortDate } from '../utils/formatters'
import './BanksGrid.css'

const TYPE_LABELS: Record<BankHistoryEntry['kind'], string> = {
  transferIn: 'Transfer In',
  transferOut: 'Transfer Out',
  adjustment: 'Adjustment',
}

interface HistoryRowProps {
  entry: BankHistoryEntry
  bankName: string
  currentBalance: number
  onEditTransfer: (transfer: TransferDto) => void
  onEditAdjustment: (bankName: string, currentBalance: number, adjustment: BalanceAdjustmentDto) => void
  onDeleteTransfer: (id: string) => void
  onDeleteAdjustment: (bankName: string, id: string) => void
}

function HistoryRow({
  entry,
  bankName,
  currentBalance,
  onEditTransfer,
  onEditAdjustment,
  onDeleteTransfer,
  onDeleteAdjustment,
}: HistoryRowProps) {
  if (entry.kind === 'adjustment') {
    return (
      <tr>
        <td>
          <button
            className="data-table__action-btn"
            type="button"
            aria-label="Edit balance adjustment"
            onClick={() => onEditAdjustment(bankName, currentBalance, entry.adjustment)}
          >
            ✏
          </button>
        </td>
        <td>
          <button
            className="data-table__action-btn"
            type="button"
            aria-label="Delete balance adjustment"
            onClick={() => onDeleteAdjustment(bankName, entry.id)}
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <path d="M20 20H7L3 16a2 2 0 0 1 0-2.83L14.59 1.58a2 2 0 0 1 2.83 0l4 4a2 2 0 0 1 0 2.83L8 20" />
              <path d="M6.5 15.5 15 7" />
            </svg>
          </button>
        </td>
        <td>{formatShortDate(entry.date)}</td>
        <td>{TYPE_LABELS[entry.kind]}</td>
        <td>{formatN2(entry.delta)}</td>
        <td>{entry.note ?? ''}</td>
      </tr>
    )
  }

  return (
    <tr>
      <td>
        <button
          className="data-table__action-btn"
          type="button"
          aria-label="Edit transfer"
          onClick={() => onEditTransfer(entry.transfer)}
        >
          ✏
        </button>
      </td>
      <td>
        <button
          className="data-table__action-btn"
          type="button"
          aria-label="Delete transfer"
          onClick={() => onDeleteTransfer(entry.id)}
        >
          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M20 20H7L3 16a2 2 0 0 1 0-2.83L14.59 1.58a2 2 0 0 1 2.83 0l4 4a2 2 0 0 1 0 2.83L8 20" />
            <path d="M6.5 15.5 15 7" />
          </svg>
        </button>
      </td>
      <td>{formatShortDate(entry.date)}</td>
      <td>{TYPE_LABELS[entry.kind]}</td>
      <td>{entry.counterpartBank}</td>
      <td>{entry.note ?? ''}</td>
    </tr>
  )
}

interface BanksGridProps {
  bankTotals: BankTotal[]
  bankTotalsSum: number
  roundUpTotalsSum: number
  historyByBank: Record<string, BankHistoryEntry[]>
  expandedBank: string | null
  onToggleExpand: (bankName: string) => void
  onMoveMoney: (bankName: string) => void
  onCorrectBalance: (bankName: string, currentBalance: number) => void
  onEditTransfer: (transfer: TransferDto) => void
  onEditAdjustment: (bankName: string, currentBalance: number, adjustment: BalanceAdjustmentDto) => void
  onDeleteTransfer: (id: string) => void
  onDeleteAdjustment: (bankName: string, id: string) => void
}

export default function BanksGrid({
  bankTotals,
  bankTotalsSum,
  roundUpTotalsSum,
  historyByBank,
  expandedBank,
  onToggleExpand,
  onMoveMoney,
  onCorrectBalance,
  onEditTransfer,
  onEditAdjustment,
  onDeleteTransfer,
  onDeleteAdjustment,
}: BanksGridProps) {
  return (
    <section className="monthly-page__section monthly-page__section--grid">
      <div className="monthly-page__table-scroll">
        <table className="monthly-page__table data-table">
          <thead>
            <tr>
              <th />
              <th>Bank</th>
              <th className="data-table__col--numeric">Bank Balance</th>
              <th className="data-table__col--numeric">Round-Up</th>
              <th />
              <th />
            </tr>
          </thead>
          <tbody>
            {bankTotals.map((b) => {
              const isExpanded = expandedBank === b.bank
              const history = historyByBank[b.bank] ?? []

              return (
                <Fragment key={b.bank}>
                  <tr>
                    <td>
                      <button
                        className="banks-grid__expand-btn"
                        type="button"
                        aria-label={isExpanded ? `Collapse history for ${b.bank}` : `Expand history for ${b.bank}`}
                        aria-expanded={isExpanded}
                        onClick={() => onToggleExpand(b.bank)}
                      >
                        {isExpanded ? '▾' : '▸'}
                      </button>
                    </td>
                    <td>{b.bank}</td>
                    <td className="data-table__col--numeric">{formatN2(b.balance)}</td>
                    <td className="data-table__col--numeric">{formatN2(b.roundUpTotal)}</td>
                    <td>
                      <button
                        className="banks-grid__action-btn"
                        type="button"
                        onClick={() => onMoveMoney(b.bank)}
                      >
                        Move Money
                      </button>
                    </td>
                    <td>
                      <button
                        className="banks-grid__action-btn"
                        type="button"
                        onClick={() => onCorrectBalance(b.bank, b.balance)}
                      >
                        Correct Balance
                      </button>
                    </td>
                  </tr>
                  {isExpanded && (
                    <tr>
                      <td colSpan={6}>
                        {history.length === 0 ? (
                          <p className="banks-grid__history-empty">No transfers or adjustments this month.</p>
                        ) : (
                          <table className="banks-grid__history-table data-table">
                            <thead>
                              <tr>
                                <th />
                                <th />
                                <th>Date</th>
                                <th>Type</th>
                                <th>Counterpart / Delta</th>
                                <th>Note</th>
                              </tr>
                            </thead>
                            <tbody>
                              {history.map((entry) => (
                                <HistoryRow
                                  key={`${entry.kind}-${entry.id}`}
                                  entry={entry}
                                  bankName={b.bank}
                                  currentBalance={b.balance}
                                  onEditTransfer={onEditTransfer}
                                  onEditAdjustment={onEditAdjustment}
                                  onDeleteTransfer={onDeleteTransfer}
                                  onDeleteAdjustment={onDeleteAdjustment}
                                />
                              ))}
                            </tbody>
                          </table>
                        )}
                      </td>
                    </tr>
                  )}
                </Fragment>
              )
            })}
          </tbody>
        </table>
      </div>
      <p className="monthly-page__section-total">
        Bank Balance: <strong>{formatN2(bankTotalsSum)}</strong> · Round-Up: <strong>{formatN2(roundUpTotalsSum)}</strong>
      </p>
    </section>
  )
}
