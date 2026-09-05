import type { BankDto, CardStatementDto, CreditCardDto, CreditCardUpdateDto } from '../api/types'
import SortableColumnHeader from './grid/SortableColumnHeader'
import ColumnFilterMenu from './grid/ColumnFilterMenu'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useColumnFilters } from '../hooks/useColumnFilters'
import { formatN2 } from '../utils/formatters'

interface CardsGridProps {
  cardStatements: CardStatementDto[]
  banks: BankDto[]
  adjustmentTotal: number
  markPaidSources: Record<string, string>
  setMarkPaidSource: (id: string, bank: string) => void
  markStatementPaid: (id: string, paymentSource: string) => void
  unmarkStatementPaid: (id: string) => void
  /** Outcome of the last mark/unmark action. An error is a failure the action did not complete; a
   * warning is a completed call that changed nothing - "already paid", say. Both were being
   * discarded, so a failed mark-paid and a no-op mark-paid looked exactly like a successful one. */
  statementActionError?: string | null
  statementActionWarning?: string | null
  /** When provided (Credit Card tab), adds Next Invoice Due Date/Active columns, one row per
   * credit card (not just cards with a statement this month, so a deactivated card - which stops
   * getting new monthly statements - stays manageable here). Omitted on the read-only Summary tab. */
  creditCards?: CreditCardDto[]
  updatingCardId?: string | null
  updateError?: string | null
  onUpdateCreditCard?: (id: string, request: CreditCardUpdateDto) => Promise<unknown>
}

interface CardRow {
  key: string
  creditCardId: string
  creditCardName: string
  nextInvoiceDueDate: string | null
  isActive: boolean
  statement: CardStatementDto | null
}

function buildRows(cardStatements: CardStatementDto[], creditCards: CreditCardDto[] | undefined): CardRow[] {
  if (!creditCards) {
    return cardStatements.map((s) => ({
      key: s.id,
      creditCardId: s.creditCardId,
      creditCardName: s.creditCardName,
      nextInvoiceDueDate: null,
      isActive: false,
      statement: s,
    }))
  }

  return creditCards.map((c) => ({
    key: c.id,
    creditCardId: c.id,
    creditCardName: c.name,
    nextInvoiceDueDate: c.nextInvoiceDueDate,
    isActive: c.isActive,
    statement: cardStatements.find((s) => s.creditCardId === c.id) ?? null,
  }))
}

export default function CardsGrid({
  cardStatements,
  banks,
  adjustmentTotal,
  markPaidSources,
  setMarkPaidSource,
  markStatementPaid,
  unmarkStatementPaid,
  statementActionError,
  statementActionWarning,
  creditCards,
  updatingCardId,
  updateError,
  onUpdateCreditCard,
}: CardsGridProps) {
  const showCardManagementColumns = creditCards !== undefined
  const rows = buildRows(cardStatements, creditCards)

  const { filteredRows, availableValues, selectedValues, toggleValue, toggleAll, isColumnFiltered } =
    useColumnFilters(rows, { card: (row: CardRow) => row.creditCardName })

  const accessors: Record<string, SortAccessor<CardRow>> = {
    card: (row) => row.creditCardName,
    outstanding: (row) => row.statement?.outstandingTotal,
    accumulatedOutstanding: (row) => row.statement?.accumulatedOutstandingTotal,
    status: (row) => (row.statement ? (row.statement.isPaid ? 'Paid' : 'Unpaid') : undefined),
    nextInvoiceDueDate: (row) => (row.nextInvoiceDueDate ? new Date(row.nextInvoiceDueDate) : undefined),
    active: (row) => (row.isActive ? 1 : 0),
  }
  const { sortedRows, sortState, requestSort } = useSortableRows(filteredRows, accessors)

  return (
    <section className="monthly-page__section monthly-page__section--grid">
      {statementActionError && (
        <p className="monthly-page__action-error" role="alert">
          {statementActionError}
        </p>
      )}
      {statementActionWarning && (
        <p className="monthly-page__action-warning" role="status">
          {statementActionWarning}
        </p>
      )}
      <div className="monthly-page__table-scroll">
        <table className="monthly-page__table data-table">
          <thead>
            <tr>
              <SortableColumnHeader
                label="Card"
                columnKey="card"
                sortDirection={sortState?.columnKey === 'card' ? sortState.direction : undefined}
                onSort={requestSort}
              >
                <ColumnFilterMenu
                  columnKey="card"
                  label="Card"
                  availableValues={availableValues.card}
                  selectedValues={selectedValues.card}
                  onToggleValue={toggleValue}
                  onToggleAll={toggleAll}
                  isFiltered={isColumnFiltered('card')}
                />
              </SortableColumnHeader>
              <SortableColumnHeader
                label="Outstanding (period)"
                columnKey="outstanding"
                numeric
                sortDirection={sortState?.columnKey === 'outstanding' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Accumulated outstanding"
                columnKey="accumulatedOutstanding"
                numeric
                sortDirection={sortState?.columnKey === 'accumulatedOutstanding' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Status"
                columnKey="status"
                sortDirection={sortState?.columnKey === 'status' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <th />
              {showCardManagementColumns && (
                <>
                  <SortableColumnHeader
                    label="Next Invoice Due Date"
                    columnKey="nextInvoiceDueDate"
                    sortDirection={sortState?.columnKey === 'nextInvoiceDueDate' ? sortState.direction : undefined}
                    onSort={requestSort}
                  />
                  <SortableColumnHeader
                    label="Active"
                    columnKey="active"
                    sortDirection={sortState?.columnKey === 'active' ? sortState.direction : undefined}
                    onSort={requestSort}
                  />
                </>
              )}
            </tr>
          </thead>
          <tbody>
            {sortedRows.length === 0 && isColumnFiltered('card') && (
              <tr>
                <td colSpan={showCardManagementColumns ? 7 : 5}>No rows match the current filters</td>
              </tr>
            )}
            {sortedRows.map((row) => (
              <tr key={row.key}>
                <td>{row.creditCardName}</td>
                <td className="data-table__col--numeric">
                  {row.statement ? formatN2(row.statement.outstandingTotal) : '—'}
                </td>
                <td className="data-table__col--numeric">
                  {row.statement ? formatN2(row.statement.accumulatedOutstandingTotal) : '—'}
                </td>
                <td>{row.statement ? (row.statement.isPaid ? 'Paid' : 'Unpaid') : '—'}</td>
                <td>
                  {row.statement &&
                    (row.statement.isPaid ? (
                      <button type="button" onClick={() => unmarkStatementPaid(row.statement!.id)}>
                        Unmark Paid
                      </button>
                    ) : (
                      <>
                        <select
                          aria-label={`Paying bank for ${row.creditCardName}`}
                          value={markPaidSources[row.statement.id] ?? ''}
                          onChange={(e) => setMarkPaidSource(row.statement!.id, e.target.value)}
                        >
                          <option value="">Bank…</option>
                          {banks.map((b) => (
                            <option key={b.id} value={b.id}>
                              {b.name}
                            </option>
                          ))}
                        </select>{' '}
                        <button
                          type="button"
                          disabled={!markPaidSources[row.statement.id]}
                          onClick={() => markStatementPaid(row.statement!.id, markPaidSources[row.statement!.id])}
                        >
                          Mark Paid
                        </button>
                      </>
                    ))}
                </td>
                {showCardManagementColumns && (
                  <>
                    <td>
                      <input
                        aria-label={`Next invoice due date for ${row.creditCardName}`}
                        type="date"
                        value={row.nextInvoiceDueDate ?? ''}
                        disabled={updatingCardId === row.creditCardId}
                        onChange={(e) =>
                          onUpdateCreditCard
                            ?.(row.creditCardId, {
                              name: row.creditCardName,
                              nextInvoiceDueDate: e.target.value === '' ? null : e.target.value,
                              isActive: row.isActive,
                            })
                            .catch(() => {})
                        }
                      />
                    </td>
                    <td>
                      <input
                        aria-label={`Active for ${row.creditCardName}`}
                        type="checkbox"
                        checked={row.isActive}
                        disabled={updatingCardId === row.creditCardId}
                        onChange={(e) =>
                          onUpdateCreditCard
                            ?.(row.creditCardId, {
                              name: row.creditCardName,
                              nextInvoiceDueDate: row.nextInvoiceDueDate,
                              isActive: e.target.checked,
                            })
                            .catch(() => {})
                        }
                      />
                    </td>
                  </>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="monthly-page__section-total">
        Combined adjustment figure: <strong>{formatN2(adjustmentTotal)}</strong>
      </p>
      {updateError && (
        <p className="monthly-page__error" role="alert">
          {updateError}
        </p>
      )}
    </section>
  )
}
