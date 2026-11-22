import { useEffect, useRef, type ReactNode } from 'react'
import {
  Badge,
  Button,
  Field,
  Input,
  MessageBar,
  MessageBarActions,
  MessageBarBody,
  Select,
  Skeleton,
  SkeletonItem,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableRow,
  Text,
} from '@fluentui/react-components'
import type { AccountStatementDto } from '../api/types'
import { isStoredStatement, type CardStatements, type PayForm, type PayFormKind } from '../hooks/useCardStatements'
import { useSortableRows, type SortAccessor, type SortState } from '../hooks/useSortableRows'
import { formatCurrency, formatDayMonthYearUtc, formatLongMonthYear, formatN2, formatShortDateUtc, todayIsoDate, sumPence } from '../utils/formatters'
import ActionErrorAlert from './ActionErrorAlert'
import ConfirmDialog from './ConfirmDialog'
import ErrorState from './ErrorState'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import ReadOnlyInput from './ReadOnlyInput'
import StatementDueDateForm from './StatementDueDateForm'
import StatementPotWithdrawalsNote from './StatementPotWithdrawalsNote'
import TruncatedText from './TruncatedText'
import { canSettleStatement, isClosedStatement, isSettledStatement, statementStatusText, UNPAID_STATEMENT_STATUS } from '../utils/owedBalance'
import { selectableBanks } from '../utils/selectableBanks'
import ActionsColumnHeader from './grid/ActionsColumnHeader'
import MissingValue from './MissingValue'

const STALE_STATEMENTS_MESSAGE = 'Couldn’t refresh the statements, so the list below may be out of date.'

interface StatementColumn {
  key: string
  header: string
  numeric?: boolean
  accessor: SortAccessor<AccountStatementDto>
  render: (statement: AccountStatementDto, isViewed: boolean) => ReactNode
}

const COLUMNS: StatementColumn[] = [
  {
    key: 'month',
    header: 'Month',
    accessor: (statement) => statement.year * 100 + statement.month,
    render: (statement, isViewed) => (
      <span className="card-statements__month">
        {monthOf(statement)}
        {isViewed && (
          <Badge appearance="outline" color="brand">
            Viewed month
          </Badge>
        )}
      </span>
    ),
  },
  { key: 'total', header: 'Total', numeric: true, accessor: (statement) => statement.total, render: (statement) => formatN2(statement.total) },
  {
    key: 'creditIn',
    header: 'Credit in',
    numeric: true,
    accessor: (statement) => statement.creditIn,
    render: (statement) => (statement.creditIn === 0 ? <MissingValue label="None" /> : formatN2(statement.creditIn)),
  },
  {
    key: 'toPay',
    header: 'To pay',
    numeric: true,
    accessor: (statement) => statement.outstandingTotal,
    render: (statement) => formatN2(statement.outstandingTotal),
  },
  {
    key: 'dueDate',
    header: 'Due date',
    accessor: (statement) => statement.dueDate,
    render: (statement) => (statement.dueDate ? formatShortDateUtc(statement.dueDate) : <MissingValue label="None" />),
  },
  {
    key: 'status',
    header: 'Status',
    accessor: (statement) => `${isSettledStatement(statement) ? 'Settled' : statement.status} ${statement.paidOn ?? statement.settledOn ?? ''}`,
    render: (statement) => <TruncatedText text={statusOf(statement)} />,
  },
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

function investmentWithdrawalsText(statement: AccountStatementDto): string {
  const count = statement.potWithdrawals.length
  return `${count} investment withdrawal${count === 1 ? '' : 's'}`
}

function unmarkText(statement: AccountStatementDto): { title: string; message: string; confirmLabel: string } {
  const month = monthOf(statement)
  if (isSettledStatement(statement)) {
    return {
      title: 'Unmark settled statement',
      message: `Unmark ${month} as settled? This removes its ${investmentWithdrawalsText(statement)} and it goes back to Nothing due.`,
      confirmLabel: 'Unmark settled',
    }
  }
  const withdrawals = statement.potWithdrawals.length === 0 ? [] : [`This also removes ${investmentWithdrawalsText(statement)}.`]
  return { title: 'Unmark statement', message: [`Unmark ${month} as paid? The payment will be deleted.`, ...withdrawals].join(' '), confirmLabel: 'Unmark' }
}

function potWithdrawalsTotal(statement: AccountStatementDto): number {
  return sumPence(statement.potWithdrawals, (withdrawal) => withdrawal.amount) / 100
}

const BANK_FORM_TEXT: Record<PayFormKind, { title: (month: string) => string; amountLabel: string; saveLabel: string }> = {
  pay: { title: (month) => `Pay ${month} statement`, amountLabel: 'Amount', saveLabel: 'Pay statement' },
  settle: { title: (month) => `Settle ${month} pot withdrawals`, amountLabel: 'Total from pots', saveLabel: 'Settle pot withdrawals' },
}

function statusOf(statement: AccountStatementDto): string {
  if (!statement.isPaid) return statementStatusText(statement)
  const paidOn = statement.paidOn ? ` on ${formatDayMonthYearUtc(statement.paidOn)}` : ''
  const paidFrom = statement.paidFromAccountName ? ` from ${statement.paidFromAccountName}` : ''
  return `Paid ${formatN2(statement.paidAmount ?? 0)}${paidOn}${paidFrom}`
}

interface StatementBankFormProps {
  payForm: PayForm
  cardStatements: CardStatements
  currency: string
  onCancel: () => void
}

function StatementBankForm({ payForm, cardStatements, currency, onCancel }: StatementBankFormProps) {
  const styles = useFormPanelStyles()
  const bankRef = useRef<HTMLSelectElement | null>(null)
  const dateRef = useRef<HTMLInputElement | null>(null)
  const { payers, isLoadingPayers, payersError, retryPayers, failedSubmit, setPayField, submitPayForm } = cardStatements
  const text = BANK_FORM_TEXT[payForm.kind]
  const isSettling = payForm.kind === 'settle'
  const amount = isSettling ? potWithdrawalsTotal(payForm.statement) : payForm.statement.outstandingTotal

  useEffect(() => {
    bankRef.current?.focus()
  }, [])

  useEffect(() => {
    if (failedSubmit?.firstInvalidField === 'bankId') bankRef.current?.focus()
    else if (failedSubmit?.firstInvalidField === 'date') dateRef.current?.focus()
  }, [failedSubmit])

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
      <fieldset disabled={payForm.isSaving} className={styles.savingFieldset} role="presentation">
        <Text as="h3" id="pay-statement-title" weight="semibold" size={400}>
          {text.title(monthOf(payForm.statement))}
        </Text>
        <ActionErrorAlert message={payForm.error} />
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
              {selectableBanks(payers, payForm.bankId).map((bank) => (
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
          <Field label={text.amountLabel}>
            <ReadOnlyInput value={formatCurrency(amount, currency)} />
          </Field>
        </div>
        <StatementPotWithdrawalsNote
          withdrawals={payForm.statement.potWithdrawals}
          bankName={payers.find((bank) => bank.id === payForm.bankId)?.name}
          currency={currency}
          withCardPayment={!isSettling}
        />
      </fieldset>
      <FormSaveActions saveLabel={text.saveLabel} isSaving={payForm.isSaving} onCancel={onCancel} />
    </form>
  )
}

interface StatementRowActionsProps {
  statement: AccountStatementDto
  cardStatements: CardStatements
  disabled: boolean
  openFormFrom: (button: HTMLButtonElement, openForm: () => void) => void
}

function StatementRowActions({ statement, cardStatements, disabled, openFormFrom }: StatementRowActionsProps) {
  const { openPayForm, openSettleForm, requestUnmark, dueDateForm } = cardStatements
  const month = monthOf(statement)
  if (isClosedStatement(statement)) {
    if (!isStoredStatement(statement)) return null
    const isSettled = isSettledStatement(statement)
    return (
      <Button
        appearance="primary"
        aria-label={isSettled ? `Unmark settled for ${month}` : `Unmark ${month} statement`}
        disabled={disabled}
        onClick={() => requestUnmark(statement)}
      >
        {isSettled ? 'Unmark settled' : 'Unmark'}
      </Button>
    )
  }
  return (
    <>
      {isStoredStatement(statement) && statement.status === UNPAID_STATEMENT_STATUS && (
        <Button
          appearance="primary"
          aria-label={`Pay ${month} statement`}
          disabled={disabled}
          onClick={(event) => openFormFrom(event.currentTarget, () => openPayForm(statement))}
        >
          Pay
        </Button>
      )}
      {isStoredStatement(statement) && canSettleStatement(statement) && (
        <Button
          appearance="primary"
          aria-label={`Settle pot withdrawals for ${month}`}
          disabled={disabled}
          onClick={(event) => openFormFrom(event.currentTarget, () => openSettleForm(statement))}
        >
          Settle pot withdrawals
        </Button>
      )}
      <Button
        appearance="primary"
        aria-label={`Set the ${month} due date`}
        disabled={disabled}
        onClick={(event) => openFormFrom(event.currentTarget, () => dueDateForm.open(statement))}
      >
        Due date
      </Button>
    </>
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
  const { statements, isLoading, error, retry, payForm, dueDateForm, savedCount, closeForms, unmark, cancelUnmark, confirmUnmark } = cardStatements
  const headingRef = useRef<HTMLHeadingElement | null>(null)
  const viewedRowRef = useRef<HTMLTableRowElement | null>(null)
  const formOpenerRef = useRef<HTMLButtonElement | null>(null)
  const { sortedRows, sortState, requestSort } = useSortableRows(statements, ACCESSORS, MONTH_DESC_SORT)

  useEffect(() => {
    viewedRowRef.current?.scrollIntoView?.({ block: 'nearest' })
  }, [statements, viewedYear, viewedMonth])

  useEffect(() => {
    if (savedCount > 0) headingRef.current?.focus()
  }, [savedCount])

  const cancelForm = () => {
    closeForms()
    if (formOpenerRef.current?.isConnected) formOpenerRef.current.focus()
  }

  const openFormFrom = (button: HTMLButtonElement, openForm: () => void) =>
    guardChange(() => {
      formOpenerRef.current = button
      openForm()
    })

  const isFormSaving = (payForm?.isSaving ?? false) || Boolean(dueDateForm.form?.pendingAction)

  const retryKeepingFocus = () => {
    headingRef.current?.focus()
    retry()
  }

  const unmarkDialog = unmark ? unmarkText(unmark.statement) : null

  const isViewed = (statement: AccountStatementDto) => statement.year === viewedYear && statement.month === viewedMonth

  return (
    <>
      <Text as="h2" ref={headingRef} tabIndex={-1} size={400} weight="semibold">
        Statements
      </Text>
      {!canAct && (
        <MessageBar intent="info">
          <MessageBarBody>This card is inactive, so its statements can't be paid, settled, unmarked or given a due date here.</MessageBarBody>
        </MessageBar>
      )}
      {payForm && <StatementBankForm key={payForm.statement.id} payForm={payForm} cardStatements={cardStatements} currency={currency} onCancel={cancelForm} />}
      {dueDateForm.form && (
        <StatementDueDateForm
          key={`${dueDateForm.form.statement.year}-${dueDateForm.form.statement.month}`}
          form={dueDateForm.form}
          dueDateForm={dueDateForm}
          onCancel={cancelForm}
        />
      )}
      {error && statements.length === 0 ? (
        <ErrorState message={error} onRetry={retryKeepingFocus} />
      ) : (
        <>
          {error && (
            <MessageBar role="alert" intent="error">
              <MessageBarBody>{STALE_STATEMENTS_MESSAGE}</MessageBarBody>
              <MessageBarActions>
                <Button appearance="primary" aria-label="Try again to load the statements" onClick={retryKeepingFocus}>
                  Try again
                </Button>
              </MessageBarActions>
            </MessageBar>
          )}
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
                  <ActionsColumnHeader className="data-table__col--action-wide" />
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
                      No statements yet.
                    </TableCell>
                  </TableRow>
                ) : (
                  sortedRows.map((statement) => {
                    const viewed = isViewed(statement)
                    return (
                      <TableRow
                        key={`${statement.year}-${statement.month}`}
                        ref={viewed ? viewedRowRef : undefined}
                        aria-current={viewed ? 'true' : undefined}
                        className={viewed ? 'card-statements__row--viewed' : undefined}
                      >
                        {COLUMNS.map((column) => (
                          <DataTableCell
                            key={column.key}
                            label={column.header}
                            className={column.numeric ? 'data-table__col--numeric' : undefined}
                          >
                            {column.render(statement, viewed)}
                          </DataTableCell>
                        ))}
                        <TableCell className="data-table__col--action-wide">
                          {canAct && (
                            <div className="data-table__actions-cell">
                              <StatementRowActions statement={statement} cardStatements={cardStatements} disabled={isFormSaving} openFormFrom={openFormFrom} />
                            </div>
                          )}
                        </TableCell>
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
        title={unmarkDialog?.title ?? ''}
        message={unmarkDialog?.message ?? ''}
        confirmLabel={unmarkDialog?.confirmLabel ?? ''}
        cancelLabel="Cancel"
        isBusy={unmark?.isBusy ?? false}
        error={unmark?.error}
        onConfirm={confirmUnmark}
        onCancel={cancelUnmark}
      />
    </>
  )
}
