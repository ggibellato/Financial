import { useEffect, useRef } from 'react'
import {
  Badge,
  Button,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Select,
  Skeleton,
  SkeletonItem,
  Spinner,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
  Text,
  Tooltip,
} from '@fluentui/react-components'
import type { AccountStatementDto } from '../api/types'
import type { CardStatements } from '../hooks/useCardStatements'
import { useSortableRows, type SortAccessor, type SortState } from '../hooks/useSortableRows'
import { formatDayMonthYearUtc, formatLongMonthYear, formatN2, todayIsoDate } from '../utils/formatters'
import ConfirmDialog from './ConfirmDialog'
import ErrorState from './ErrorState'
import { useFormPanelStyles } from './formPanelStyles'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'

interface StatementColumn {
  key: string
  header: string
  numeric?: boolean
  accessor: SortAccessor<AccountStatementDto>
}

const COLUMNS: StatementColumn[] = [
  { key: 'month', header: 'Month', accessor: (statement) => statement.year * 100 + statement.month },
  { key: 'total', header: 'Total', numeric: true, accessor: (statement) => statement.total },
  { key: 'dueDate', header: 'Due date', accessor: (statement) => statement.dueDate },
  { key: 'status', header: 'Status', accessor: (statement) => statusOf(statement) },
]

const ACCESSORS = Object.fromEntries(COLUMNS.map((column) => [column.key, column.accessor]))

const MONTH_DESC_SORT: SortState = { columnKey: 'month', direction: 'descending' }

const COLUMN_COUNT = COLUMNS.length + 1

interface CardStatementsTabProps {
  cardStatements: CardStatements
  viewedYear: number
  viewedMonth: number
  canAct: boolean
}

function monthOf(statement: AccountStatementDto): string {
  return formatLongMonthYear(statement.year, statement.month)
}

function statusOf(statement: AccountStatementDto): string {
  if (!statement.isPaid) return 'Unpaid'
  if (!statement.paidOn) return 'Paid'
  const paidOn = formatDayMonthYearUtc(statement.paidOn)
  return statement.paidFromAccountName ? `Paid ${paidOn} from ${statement.paidFromAccountName}` : `Paid ${paidOn}`
}

function PayStatementForm({ cardStatements }: { cardStatements: CardStatements }) {
  const styles = useFormPanelStyles()
  const bankRef = useRef<HTMLSelectElement | null>(null)
  const { payForm, payers, payersError, retryPayers, setPayField, submitPayForm, closePayForm } = cardStatements

  useEffect(() => {
    bankRef.current?.focus()
  }, [])

  if (!payForm) return null

  return (
    <form
      className={styles.panel}
      noValidate
      aria-labelledby="pay-statement-title"
      onSubmit={(event) => {
        event.preventDefault()
        submitPayForm()
      }}
    >
      <fieldset disabled={payForm.isSaving} className={styles.savingFieldset}>
        <Text as="h3" id="pay-statement-title" weight="semibold" size={400}>
          Pay {monthOf(payForm.statement)} statement
        </Text>
        {payForm.error && (
          <MessageBar intent="error">
            <MessageBarBody>{payForm.error}</MessageBarBody>
          </MessageBar>
        )}
        {payersError && <ErrorState message={payersError} onRetry={retryPayers} />}
        <div className={styles.grid}>
          <Field
            label="Bank"
            required
            validationState={payForm.fieldErrors.bankId ? 'error' : 'none'}
            validationMessage={payForm.fieldErrors.bankId}
          >
            <Select ref={bankRef} value={payForm.bankId} onChange={(event) => setPayField('bankId', event.target.value)}>
              <option value="">Select a bank</option>
              {payers.map((bank) => (
                <option key={bank.id} value={bank.id}>
                  {bank.name}
                </option>
              ))}
            </Select>
          </Field>
          <Field
            label="Date"
            required
            validationState={payForm.fieldErrors.date ? 'error' : 'none'}
            validationMessage={payForm.fieldErrors.date}
          >
            <Input type="date" max={todayIsoDate()} value={payForm.date} onChange={(event) => setPayField('date', event.target.value)} />
          </Field>
          <Field label="Amount">
            <Text weight="semibold">{formatN2(payForm.statement.total)}</Text>
          </Field>
        </div>
        <div className={styles.actions}>
          <Button
            type="submit"
            appearance="primary"
            disabled={payForm.isSaving}
            icon={payForm.isSaving ? <Spinner size="tiny" /> : undefined}
          >
            {payForm.isSaving ? 'Saving...' : 'Pay statement'}
          </Button>
          <Button appearance="secondary" onClick={closePayForm}>
            Cancel
          </Button>
        </div>
      </fieldset>
    </form>
  )
}

export default function CardStatementsTab({ cardStatements, viewedYear, viewedMonth, canAct }: CardStatementsTabProps) {
  const { statements, isLoading, error, retry, payForm, openPayForm, requestUnmark, unmark, cancelUnmark, confirmUnmark } =
    cardStatements
  const viewedRowRef = useRef<HTMLTableRowElement | null>(null)
  const payOpenerRef = useRef<HTMLButtonElement | null>(null)
  const wasPayFormOpen = useRef(false)
  const { sortedRows, sortState, requestSort } = useSortableRows(statements, ACCESSORS, MONTH_DESC_SORT)

  useEffect(() => {
    viewedRowRef.current?.scrollIntoView?.({ block: 'nearest' })
  }, [statements, viewedYear, viewedMonth])

  useEffect(() => {
    if (wasPayFormOpen.current && !payForm && payOpenerRef.current?.isConnected) payOpenerRef.current.focus()
    wasPayFormOpen.current = payForm !== null
  }, [payForm])

  if (error) return <ErrorState message={error} onRetry={retry} />

  const isViewed = (statement: AccountStatementDto) => statement.year === viewedYear && statement.month === viewedMonth

  return (
    <>
      {payForm && <PayStatementForm cardStatements={cardStatements} />}
      <div className="account-screen__grid-scroll">
        <Table className="data-table" aria-label="Statements">
          <TableHeader>
            <TableRow>
              {COLUMNS.map((column) => (
                <SortableColumnHeader
                  key={column.key}
                  label={column.header}
                  columnKey={column.key}
                  numeric={column.numeric}
                  sortDirection={sortState?.columnKey === column.key ? sortState.direction : undefined}
                  onSort={requestSort}
                />
              ))}
              <TableHeaderCell className="data-table__col--action" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={COLUMN_COUNT}>
                  <Skeleton aria-label="Loading statements" className="account-screen__grid-skeleton">
                    <SkeletonItem />
                    <SkeletonItem />
                    <SkeletonItem />
                  </Skeleton>
                </TableCell>
              </TableRow>
            ) : sortedRows.length === 0 ? (
              <TableRow>
                <TableCell className="data-table__cell--message" colSpan={COLUMN_COUNT}>
                  No statements yet
                </TableCell>
              </TableRow>
            ) : (
              sortedRows.map((statement) => {
                const viewed = isViewed(statement)
                const month = monthOf(statement)
                return (
                  <TableRow
                    key={statement.id}
                    ref={viewed ? viewedRowRef : undefined}
                    aria-current={viewed ? 'true' : undefined}
                    className={viewed ? 'card-statements__row--viewed' : undefined}
                  >
                    <DataTableCell label="Month">
                      <span className="card-statements__month">
                        {month}
                        {viewed && (
                          <Badge appearance="outline" color="brand">
                            Viewed month
                          </Badge>
                        )}
                      </span>
                    </DataTableCell>
                    <DataTableCell label="Total" className="data-table__col--numeric">
                      {formatN2(statement.total)}
                    </DataTableCell>
                    <DataTableCell label="Due date">{statement.dueDate ? formatDayMonthYearUtc(statement.dueDate) : '—'}</DataTableCell>
                    <DataTableCell label="Status">{statusOf(statement)}</DataTableCell>
                    <DataTableCell label="Actions" className="data-table__col--action">
                      {canAct && !statement.isPaid && (
                        <PayButton
                          month={month}
                          nothingToPay={statement.total <= 0}
                          onPay={(button) => {
                            payOpenerRef.current = button
                            openPayForm(statement)
                          }}
                        />
                      )}
                      {canAct && statement.isPaid && (
                        <Button size="small" aria-label={`Unmark ${month} statement`} onClick={() => requestUnmark(statement)}>
                          Unmark
                        </Button>
                      )}
                    </DataTableCell>
                  </TableRow>
                )
              })
            )}
          </TableBody>
        </Table>
      </div>
      <ConfirmDialog
        open={unmark !== null}
        title="Unmark statement"
        message={unmark ? `Unmark ${monthOf(unmark.statement)} as paid? The payment will be deleted.` : ''}
        confirmLabel="Unmark"
        cancelLabel="Cancel"
        isBusy={unmark?.isBusy ?? false}
        error={unmark?.error}
        onConfirm={confirmUnmark}
        onCancel={cancelUnmark}
      />
    </>
  )
}

interface PayButtonProps {
  month: string
  nothingToPay: boolean
  onPay: (button: HTMLButtonElement) => void
}

function PayButton({ month, nothingToPay, onPay }: PayButtonProps) {
  const button = (
    <Button
      size="small"
      appearance="primary"
      aria-label={`Pay ${month} statement`}
      disabledFocusable={nothingToPay}
      onClick={(event) => onPay(event.currentTarget)}
    >
      Pay
    </Button>
  )
  return nothingToPay ? (
    <Tooltip content="Nothing to pay" relationship="description">
      {button}
    </Tooltip>
  ) : (
    button
  )
}
