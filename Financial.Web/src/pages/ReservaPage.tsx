import { Fragment } from 'react'
import { Button, Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import EditMovementForm from '../components/EditMovementForm'
import ErrorState from '../components/ErrorState'
import IncomeSplitForm from '../components/IncomeSplitForm'
import LoadingState from '../components/LoadingState'
import TruncatedText from '../components/TruncatedText'
import WithdrawalForm from '../components/WithdrawalForm'
import DataTableCell from '../components/grid/DataTableCell'
import SortableColumnHeader from '../components/grid/SortableColumnHeader'
import { useSortableRows } from '../hooks/useSortableRows'
import type { ReserveBucketBalanceDto } from '../api/types'
import { LOCKED_MOVEMENT_MESSAGE, useReserva } from '../hooks/useReserva'
import { confirmThenRun } from '../utils/confirmThenRun'
import { formatN2, formatShortDate } from '../utils/formatters'
import './ReservaPage.css'

const BALANCE_ACCESSORS = {
  bucket: (b: ReserveBucketBalanceDto) => b.bucketName,
  balance: (b: ReserveBucketBalanceDto) => b.balance,
}

function BalanceColumns() {
  return (
    <colgroup>
      <col />
      <col className="reserva-page__col-value" />
    </colgroup>
  )
}

function MovementColumns() {
  return (
    <colgroup>
      <col className="reserva-page__col-actions" />
      <col className="reserva-page__col-date" />
      <col className="reserva-page__col-bucket" />
      <col />
      <col className="reserva-page__col-value" />
      <col className="reserva-page__col-row-actions" />
    </colgroup>
  )
}

export default function ReservaPage() {
  const {
    balances,
    totalBalance,
    movementRows,
    buckets,
    splitPercentageWarning,
    isLoading,
    error,
    retry,
    isSplitFormOpen,
    splitDate,
    splitAmount,
    splitDescription,
    isSubmittingSplit,
    splitError,
    splitErrorFields,
    lastSplitResult,
    showSplitForm,
    cancelSplitForm,
    setSplitField,
    submitIncomeSplit,
    dismissSplitResult,
    isWithdrawalFormOpen,
    withdrawalBucketId,
    withdrawalAmount,
    withdrawalDate,
    withdrawalDescription,
    isSubmittingWithdrawal,
    withdrawalError,
    withdrawalErrorFields,
    showWithdrawalForm,
    cancelWithdrawalForm,
    setWithdrawalField,
    submitWithdrawal,
    editingMovementId,
    editMovementBucketId,
    editMovementAmount,
    editMovementDate,
    editMovementDescription,
    isSavingMovement,
    saveMovementError,
    saveMovementErrorFields,
    showEditMovementForm,
    cancelEditMovement,
    setEditMovementField,
    saveMovementEdit,
    deletingMovementId,
    deleteMovementError,
    deleteMovement,
  } = useReserva()

  const { sortedRows: sortedBalances, sortState: balanceSortState, requestSort: requestBalanceSort } =
    useSortableRows(balances, BALANCE_ACCESSORS)

  // useReserva asks whether to proceed when the server rejects a withdrawal with 409; how to ask,
  // and in what words, is presentation and belongs here. This page already owned its other
  // confirmation, on deleting a movement.
  const confirmProceedWithWithdrawal = (serverMessage: string) =>
    window.confirm(`${serverMessage}\n\nProceed anyway?`)

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  return (
    <div className="reserva-page">
      <div className="reserva-page__header">
        <div className="reserva-page__toolbar">
          <Button appearance="primary" icon={<AddRegular />} onClick={showSplitForm}>
            New Income Split
          </Button>
          <Button appearance="primary" icon={<AddRegular />} onClick={showWithdrawalForm}>
            New Withdrawal
          </Button>
        </div>
      </div>

      {splitPercentageWarning && <p className="reserva-page__warning" role="alert">{splitPercentageWarning}</p>}

      {(isSplitFormOpen || lastSplitResult) && (
        <IncomeSplitForm
          date={splitDate}
          amount={splitAmount}
          description={splitDescription}
          isSubmitting={isSubmittingSplit}
          error={splitError}
          errorFields={splitErrorFields}
          lastResult={lastSplitResult}
          onFieldChange={setSplitField}
          onSubmit={submitIncomeSplit}
          onCancel={cancelSplitForm}
          onDismissResult={dismissSplitResult}
        />
      )}

      {isWithdrawalFormOpen && (
        <WithdrawalForm
          bucketId={withdrawalBucketId}
          amount={withdrawalAmount}
          date={withdrawalDate}
          description={withdrawalDescription}
          buckets={buckets}
          isSubmitting={isSubmittingWithdrawal}
          error={withdrawalError}
          errorFields={withdrawalErrorFields}
          onFieldChange={setWithdrawalField}
          onSubmit={() => submitWithdrawal(confirmProceedWithWithdrawal)}
          onCancel={cancelWithdrawalForm}
        />
      )}

      {editingMovementId && (
        <EditMovementForm
          bucketId={editMovementBucketId}
          amount={editMovementAmount}
          date={editMovementDate}
          description={editMovementDescription}
          buckets={buckets}
          isSaving={isSavingMovement}
          error={saveMovementError}
          errorFields={saveMovementErrorFields}
          onFieldChange={setEditMovementField}
          onSave={saveMovementEdit}
          onCancel={cancelEditMovement}
        />
      )}

      {deleteMovementError && <p className="reserva-page__error">{deleteMovementError}</p>}

      <div className="reserva-page__content">
        <div className="reserva-page__grids-row">
          <section className="reserva-page__section reserva-page__section--grid reserva-page__section--balances">
            <div className="reserva-page__table-scroll">
              <Table className="reserva-page__table data-table">
                <BalanceColumns />
                <TableHeader>
                  <TableRow>
                    <SortableColumnHeader
                      label="Bucket"
                      columnKey="bucket"
                      sortDirection={balanceSortState?.columnKey === 'bucket' ? balanceSortState.direction : undefined}
                      onSort={requestBalanceSort}
                    />
                    <SortableColumnHeader
                      label="Balance"
                      columnKey="balance"
                      numeric
                      sortDirection={balanceSortState?.columnKey === 'balance' ? balanceSortState.direction : undefined}
                      onSort={requestBalanceSort}
                    />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {sortedBalances.map((b) => (
                    <TableRow key={b.bucketId}>
                      <DataTableCell label="Bucket">{b.bucketName}</DataTableCell>
                      <DataTableCell label="Balance" className="data-table__col--numeric">
                        {formatN2(b.balance)}
                      </DataTableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
            <Table className="reserva-page__table reserva-page__totals-table data-table">
              <BalanceColumns />
              <TableBody>
                <TableRow className="reserva-page__totals-row">
                  <DataTableCell label="Bucket">Total</DataTableCell>
                  <DataTableCell label="Balance" className="data-table__col--numeric">
                    {formatN2(totalBalance)}
                  </DataTableCell>
                </TableRow>
              </TableBody>
            </Table>
          </section>

          <section className="reserva-page__section reserva-page__section--grid reserva-page__section--movements">
            <div className="reserva-page__table-scroll">
              <Table className="reserva-page__table data-table">
                <MovementColumns />
                <TableHeader>
                  <TableRow>
                    <TableHeaderCell />
                    <TableHeaderCell>Date</TableHeaderCell>
                    <TableHeaderCell>Bucket</TableHeaderCell>
                    <TableHeaderCell>Description</TableHeaderCell>
                    <TableHeaderCell className="data-table__col--numeric">Amount</TableHeaderCell>
                    <TableHeaderCell />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {movementRows.map((m) => (
                    <Fragment key={m.id}>
                      <TableRow>
                        <TableCell>
                          {m.isLocked && (
                            <span
                              className="reserva-page__lock-icon"
                              role="img"
                              aria-label={LOCKED_MOVEMENT_MESSAGE}
                              title={LOCKED_MOVEMENT_MESSAGE}
                            >
                              🔒
                            </span>
                          )}
                        </TableCell>
                        <DataTableCell label="Date">{formatShortDate(m.date)}</DataTableCell>
                        <DataTableCell label="Bucket">{m.bucketName}</DataTableCell>
                        <DataTableCell label="Description">
                          <TruncatedText text={m.description} />
                        </DataTableCell>
                        <DataTableCell label="Amount" className="data-table__col--numeric">
                          {formatN2(m.amount)}
                        </DataTableCell>
                        <DataTableCell label="Actions">
                          <div className="data-table__actions-cell">
                            <button
                              className="data-table__action-btn"
                              type="button"
                              aria-label="Edit movement"
                              disabled={m.isLocked}
                              title={m.isLocked ? LOCKED_MOVEMENT_MESSAGE : undefined}
                              onClick={() => showEditMovementForm(m)}
                            >
                              <EditRegular />
                            </button>
                            <button
                              className="data-table__action-btn"
                              type="button"
                              aria-label={deletingMovementId === m.id ? 'Deleting movement' : 'Delete movement'}
                              disabled={m.isLocked || deletingMovementId === m.id}
                              title={m.isLocked ? LOCKED_MOVEMENT_MESSAGE : undefined}
                              onClick={() => {
                                const warning = m.isPartOfGroup
                                  ? `Delete "${m.description}"? This is part of a split and will delete all 4 lines.`
                                  : `Delete "${m.description}"? This removes it for good.`
                                confirmThenRun(warning, () => deleteMovement(m.id))
                              }}
                            >
                              <DeleteRegular />
                            </button>
                          </div>
                        </DataTableCell>
                      </TableRow>
                      {m.groupTotal !== null && (
                        <TableRow className="reserva-page__totals-row">
                          <TableCell />
                          <TableCell className="data-table__cell--message" colSpan={3}>
                            Total split for {m.description}
                          </TableCell>
                          <DataTableCell label="Amount" className="data-table__col--numeric">
                            {formatN2(m.groupTotal)}
                          </DataTableCell>
                          <TableCell />
                        </TableRow>
                      )}
                    </Fragment>
                  ))}
                </TableBody>
              </Table>
            </div>
          </section>
        </div>
      </div>
    </div>
  )
}
