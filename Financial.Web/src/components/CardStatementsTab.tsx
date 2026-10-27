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
import { formatCurrency, formatDayMonthYearUtc, formatLongMonthYear, formatN2, formatShortDateUtc, todayIsoDate } from '../utils/formatters'
import ConfirmDialog from './ConfirmDialog'
import ErrorState from './ErrorState'
import { useFormPanelStyles } from './formPanelStyles'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import TruncatedText from './TruncatedText'

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
  { key: 'status', header: 'Status', accessor: (statement) => (statement.isPaid ? `1 ${statement.paidOn ?? ''}` : '0') },
]

const ACCESSORS = Object.fromEntries(COLUMNS.map((column) => [column.key, column.accessor]))

const MONTH_DESC_SORT: SortState = { columnKey: 'month', direction: 'descending' }

const COLUMN_COUNT = COLUMNS.length + 1

interface CardStatementsTabProps {
  cardStatements: CardStatements
  viewedYear: number
  viewedMonth: number
  currency: string
  canAct: boolean
  guardChange: (change: () => void) => void
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

function PayStatementForm({ cardStatements, currency, onCancel }: { cardStatements: CardStatements; currency: string; onCancel: () => void }) {
  const styles = useFormPanelStyles()
  const bankRef = useRef<HTMLSelectElement | null>(null)
  const dateRef = useRef<HTMLInputElement | null>(null)
  const { payForm, payers, isLoadingPayers, payersError, retryPayers, failedSubmit, setPayField, submitPayForm } = cardStatements

  useEffect(() => {
    bankRef.current?.focus()
  }, [])

  useEffect(() => {
    if (failedSubmit?.firstInvalidField === 'bankId') bankRef.current?.focus()
    else if (failedSubmit?.firstInvalidField === 'date') dateRef.current?.focus()
  }, [failedSubmit])

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
            <Select
              ref={bankRef}
              value={payForm.bankId}
              disabled={isLoadingPayers}
              onChange={(event) => setPayField('bankId', event.target.value)}
            >
              <option value="">{isLoadingPayers ? 'Loading banks…' : 'Select a bank'}</option>
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
            <Input
              ref={dateRef}
              type="date"
              max={todayIsoDate()}
              value={payForm.date}
              onChange={(event) => setPayField('date', event.target.value)}
            />
          </Field>
          <Field label="Amount">
            <Input readOnly value={formatCurrency(payForm.statement.total, currency)} />
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
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </fieldset>
    </form>
  )
}

export default function CardStatementsTab({
  cardStatements,
  viewedYear,
  viewedMonth,
  currency,
  canAct,
  guardChange,
}: CardStatementsTabProps) {
  const { statements, isLoading, error, retry, payForm, savedCount, openPayForm, closePayForm, requestUnmark, unmark, cancelUnmark, confirmUnmark } =
    cardStatements
  const headingRef = useRef<HTMLHeadingElement | null>(null)
  const viewedRowRef = useRef<HTMLTableRowElement | null>(null)
  const payOpenerRef = useRef<HTMLButtonElement | null>(null)
  const { sortedRows, sortState, requestSort } = useSortableRows(statements, ACCESSORS, MONTH_DESC_SORT)

  useEffect(() => {
    viewedRowRef.current?.scrollIntoView?.({ block: 'nearest' })
  }, [statements, viewedYear, viewedMonth])

  useEffect(() => {
    if (savedCount > 0) headingRef.current?.focus()
  }, [savedCount])

  const cancelPayForm = () => {
    closePayForm()
    if (payOpenerRef.current?.isConnected) payOpenerRef.current.focus()
  }

  const isViewed = (statement: AccountStatementDto) => statement.year === viewedYear && statement.month === viewedMonth

  return (
    <>
      <Text as="h2" ref={headingRef} tabIndex={-1} size={400} weight="semibold">
        Statements
      </Text>
      {!canAct && (
        <MessageBar intent="info">
          <MessageBarBody>This card is inactive, so its statements can't be paid or unmarked here.</MessageBarBody>
        </MessageBar>
      )}
      {error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <>
          {payForm && <PayStatementForm cardStatements={cardStatements} currency={currency} onCancel={cancelPayForm} />}
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
                  <TableHeaderCell className="data-table__col--action-wide" />
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
                        <DataTableCell label="Due date">{statement.dueDate ? formatShortDateUtc(statement.dueDate) : '—'}</DataTableCell>
                        <DataTableCell label="Status">
                          <TruncatedText text={statusOf(statement)} />
                        </DataTableCell>
                        <DataTableCell label="Actions" className="data-table__col--action-wide">
                          {canAct && !statement.isPaid && (
                            <PayButton
                              month={month}
                              nothingToPay={statement.total <= 0}
                              onPay={(button) =>
                                guardChange(() => {
                                  payOpenerRef.current = button
                                  openPayForm(statement)
                                })
                              }
                            />
                          )}
                          {canAct && statement.isPaid && (
                            <Button aria-label={`Unmark ${month} statement`} onClick={() => requestUnmark(statement)}>
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
        </>
      )}
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
