import { Fragment } from 'react'
import { Button } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import EditMovementForm from '../components/EditMovementForm'
import ErrorState from '../components/ErrorState'
import IncomeSplitForm from '../components/IncomeSplitForm'
import LoadingState from '../components/LoadingState'
import WithdrawalForm from '../components/WithdrawalForm'
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
      <col className="reserva-page__col-actions" />
      <col className="reserva-page__col-actions" />
      <col className="reserva-page__col-date" />
      <col className="reserva-page__col-bucket" />
      <col />
      <col className="reserva-page__col-value" />
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
              <table className="reserva-page__table data-table">
                <BalanceColumns />
                <thead>
                  <tr>
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
                  </tr>
                </thead>
                <tbody>
                  {sortedBalances.map((b) => (
                    <tr key={b.bucketId}>
                      <td>{b.bucketName}</td>
                      <td className="data-table__col--numeric">{formatN2(b.balance)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <table className="reserva-page__table reserva-page__totals-table data-table">
              <BalanceColumns />
              <tbody>
                <tr className="reserva-page__totals-row">
                  <td>Total</td>
                  <td className="data-table__col--numeric">{formatN2(totalBalance)}</td>
                </tr>
              </tbody>
            </table>
          </section>

          <section className="reserva-page__section reserva-page__section--grid reserva-page__section--movements">
            <div className="reserva-page__table-scroll">
              <table className="reserva-page__table data-table">
                <MovementColumns />
                <thead>
                  <tr>
                    <th />
                    <th />
                    <th />
                    <th>Date</th>
                    <th>Bucket</th>
                    <th>Description</th>
                    <th className="data-table__col--numeric">Amount</th>
                  </tr>
                </thead>
                <tbody>
                  {movementRows.map((m) => (
                    <Fragment key={m.id}>
                      <tr>
                        <td>
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
                        </td>
                        <td>
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
                        </td>
                        <td>
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
                        </td>
                        <td>{formatShortDate(m.date)}</td>
                        <td>{m.bucketName}</td>
                        <td>{m.description}</td>
                        <td className="data-table__col--numeric">{formatN2(m.amount)}</td>
                      </tr>
                      {m.groupTotal !== null && (
                        <tr className="reserva-page__totals-row">
                          <td />
                          <td />
                          <td />
                          <td colSpan={3}>Total split for {m.description}</td>
                          <td className="data-table__col--numeric">{formatN2(m.groupTotal)}</td>
                        </tr>
                      )}
                    </Fragment>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </div>
      </div>
    </div>
  )
}
