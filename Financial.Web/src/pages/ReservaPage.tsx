import { Fragment } from 'react'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { RESERVE_BUCKETS, useReserva } from '../hooks/useReserva'
import type { EditMovementField, WithdrawalFormField } from '../hooks/useReserva'
import { formatN2, formatShortDate } from '../utils/formatters'
import './ReservaPage.css'

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
    isLoading,
    error,
    retry,
    isSplitFormOpen,
    splitDate,
    splitAmount,
    splitDescription,
    isSubmittingSplit,
    splitError,
    lastSplitResult,
    showSplitForm,
    cancelSplitForm,
    setSplitField,
    submitIncomeSplit,
    dismissSplitResult,
    isWithdrawalFormOpen,
    withdrawalBucket,
    withdrawalAmount,
    withdrawalDate,
    withdrawalDescription,
    isSubmittingWithdrawal,
    withdrawalError,
    showWithdrawalForm,
    cancelWithdrawalForm,
    setWithdrawalField,
    submitWithdrawal,
    editingMovementId,
    editMovementBucket,
    editMovementAmount,
    editMovementDate,
    editMovementDescription,
    isSavingMovement,
    saveMovementError,
    showEditMovementForm,
    cancelEditMovement,
    setEditMovementField,
    saveMovementEdit,
    deletingMovementId,
    deleteMovementError,
    deleteMovement,
  } = useReserva()

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  const withdrawalValues: Record<WithdrawalFormField, string> = {
    withdrawalBucket,
    withdrawalAmount,
    withdrawalDate,
    withdrawalDescription,
  }

  const editMovementValues: Record<EditMovementField, string> = {
    editMovementBucket,
    editMovementAmount,
    editMovementDate,
    editMovementDescription,
  }

  return (
    <div className="reserva-page">
      <div className="reserva-page__header">
        <div className="reserva-page__toolbar">
          <button className="reserva-page__new-btn" type="button" onClick={showSplitForm}>
            New Income Split
          </button>
          <button className="reserva-page__new-btn" type="button" onClick={showWithdrawalForm}>
            New Withdrawal
          </button>
        </div>
      </div>

      {isSplitFormOpen && (
        <div className="reserva-page__form-panel">
          <p className="reserva-page__form-title">Post Monthly Income Split</p>
          <div className="reserva-page__form">
            <div className="reserva-page__form-field">
              <label htmlFor="split-date">Date</label>
              <input
                id="split-date"
                type="date"
                value={splitDate}
                required
                onChange={(e) => setSplitField('splitDate', e.target.value)}
              />
            </div>
            <div className="reserva-page__form-field">
              <label htmlFor="split-amount">Amount to Split</label>
              <input
                id="split-amount"
                type="number"
                step="0.01"
                value={splitAmount}
                onChange={(e) => setSplitField('splitAmount', e.target.value)}
              />
            </div>
            <div className="reserva-page__form-field">
              <label htmlFor="split-description">Description</label>
              <input
                id="split-description"
                type="text"
                value={splitDescription}
                onChange={(e) => setSplitField('splitDescription', e.target.value)}
              />
            </div>
          </div>
          <div className="reserva-page__form-actions">
            <button type="button" disabled={isSubmittingSplit} onClick={submitIncomeSplit}>
              {isSubmittingSplit ? 'Posting...' : 'Post Income Split'}
            </button>
            <button type="button" onClick={cancelSplitForm}>
              Cancel
            </button>
          </div>
          {splitError && <p className="reserva-page__error">{splitError}</p>}
        </div>
      )}

      {lastSplitResult && (
        <div className="reserva-page__form-panel">
          <p className="reserva-page__form-title">Income Split Posted</p>
          <table className="reserva-page__table reserva-page__split-result-table data-table">
            <BalanceColumns />
            <tbody>
              <tr>
                <td>Investimento</td>
                <td className="data-table__col--numeric">{formatN2(lastSplitResult.investimento)}</td>
              </tr>
              <tr>
                <td>HouseTreats</td>
                <td className="data-table__col--numeric">{formatN2(lastSplitResult.houseTreats)}</td>
              </tr>
              <tr>
                <td>Ariana</td>
                <td className="data-table__col--numeric">{formatN2(lastSplitResult.ariana)}</td>
              </tr>
              <tr>
                <td>Gleison</td>
                <td className="data-table__col--numeric">{formatN2(lastSplitResult.gleison)}</td>
              </tr>
              <tr className="reserva-page__totals-row">
                <td>Total</td>
                <td className="data-table__col--numeric">{formatN2(lastSplitResult.total)}</td>
              </tr>
            </tbody>
          </table>
          <div className="reserva-page__form-actions">
            <button type="button" onClick={dismissSplitResult}>
              Dismiss
            </button>
          </div>
        </div>
      )}

      {isWithdrawalFormOpen && (
        <div className="reserva-page__form-panel">
          <p className="reserva-page__form-title">Record a Withdrawal</p>
          <div className="reserva-page__form">
            <div className="reserva-page__form-field">
              <label htmlFor="withdrawal-bucket">Bucket</label>
              <select
                id="withdrawal-bucket"
                value={withdrawalValues.withdrawalBucket}
                onChange={(e) => setWithdrawalField('withdrawalBucket', e.target.value)}
              >
                {RESERVE_BUCKETS.map((bucket) => (
                  <option key={bucket} value={bucket}>
                    {bucket}
                  </option>
                ))}
              </select>
            </div>
            <div className="reserva-page__form-field">
              <label htmlFor="withdrawal-amount">Amount</label>
              <input
                id="withdrawal-amount"
                type="number"
                step="0.01"
                min="0"
                value={withdrawalValues.withdrawalAmount}
                onChange={(e) => setWithdrawalField('withdrawalAmount', e.target.value)}
              />
            </div>
            <div className="reserva-page__form-field">
              <label htmlFor="withdrawal-date">Date</label>
              <input
                id="withdrawal-date"
                type="date"
                value={withdrawalValues.withdrawalDate}
                onChange={(e) => setWithdrawalField('withdrawalDate', e.target.value)}
              />
            </div>
            <div className="reserva-page__form-field">
              <label htmlFor="withdrawal-description">Description</label>
              <input
                id="withdrawal-description"
                type="text"
                value={withdrawalValues.withdrawalDescription}
                onChange={(e) => setWithdrawalField('withdrawalDescription', e.target.value)}
              />
            </div>
          </div>
          <div className="reserva-page__form-actions">
            <button type="button" disabled={isSubmittingWithdrawal} onClick={submitWithdrawal}>
              {isSubmittingWithdrawal ? 'Saving...' : 'Record Withdrawal'}
            </button>
            <button type="button" onClick={cancelWithdrawalForm}>
              Cancel
            </button>
          </div>
          {withdrawalError && <p className="reserva-page__error">{withdrawalError}</p>}
        </div>
      )}

      {editingMovementId && (
        <div className="reserva-page__form-panel">
          <p className="reserva-page__form-title">Edit Movement</p>
          <div className="reserva-page__form">
            <div className="reserva-page__form-field">
              <label htmlFor="edit-movement-bucket">Bucket</label>
              <select
                id="edit-movement-bucket"
                value={editMovementValues.editMovementBucket}
                onChange={(e) => setEditMovementField('editMovementBucket', e.target.value)}
              >
                {RESERVE_BUCKETS.map((bucket) => (
                  <option key={bucket} value={bucket}>
                    {bucket}
                  </option>
                ))}
              </select>
            </div>
            <div className="reserva-page__form-field">
              <label htmlFor="edit-movement-amount">Amount</label>
              <input
                id="edit-movement-amount"
                type="number"
                step="0.01"
                value={editMovementValues.editMovementAmount}
                onChange={(e) => setEditMovementField('editMovementAmount', e.target.value)}
              />
            </div>
            <div className="reserva-page__form-field">
              <label htmlFor="edit-movement-date">Date</label>
              <input
                id="edit-movement-date"
                type="date"
                value={editMovementValues.editMovementDate}
                onChange={(e) => setEditMovementField('editMovementDate', e.target.value)}
              />
            </div>
            <div className="reserva-page__form-field">
              <label htmlFor="edit-movement-description">Description</label>
              <input
                id="edit-movement-description"
                type="text"
                value={editMovementValues.editMovementDescription}
                onChange={(e) => setEditMovementField('editMovementDescription', e.target.value)}
              />
            </div>
          </div>
          <div className="reserva-page__form-actions">
            <button type="button" disabled={isSavingMovement} onClick={saveMovementEdit}>
              {isSavingMovement ? 'Saving...' : 'Save'}
            </button>
            <button type="button" onClick={cancelEditMovement}>
              Cancel
            </button>
          </div>
          {saveMovementError && <p className="reserva-page__error">{saveMovementError}</p>}
        </div>
      )}

      {deleteMovementError && <p className="reserva-page__error">{deleteMovementError}</p>}

      <div className="reserva-page__content">
        <div className="reserva-page__grids-row">
          <section className="reserva-page__section reserva-page__section--grid reserva-page__section--balances">
            <h2>Bucket Balances</h2>
            <div className="reserva-page__table-scroll">
              <table className="reserva-page__table data-table">
                <BalanceColumns />
                <thead>
                  <tr>
                    <th>Bucket</th>
                    <th className="data-table__col--numeric">Balance</th>
                  </tr>
                </thead>
                <tbody>
                  {balances.map((b) => (
                    <tr key={b.bucket}>
                      <td>{b.bucket}</td>
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
            <h2>Movement History</h2>
            <div className="reserva-page__table-scroll">
              <table className="reserva-page__table data-table">
                <MovementColumns />
                <thead>
                  <tr>
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
                          <button
                            className="data-table__action-btn"
                            type="button"
                            aria-label="Edit movement"
                            onClick={() => showEditMovementForm(m)}
                          >
                            ✏
                          </button>
                        </td>
                        <td>
                          <button
                            className="data-table__action-btn"
                            type="button"
                            aria-label={deletingMovementId === m.id ? 'Deleting movement' : 'Delete movement'}
                            disabled={deletingMovementId === m.id}
                            onClick={() => {
                              const warning = m.isPartOfGroup
                                ? `Delete "${m.description}"? This is part of a split and will delete all 4 lines.`
                                : `Delete "${m.description}"? This removes it for good.`
                              if (window.confirm(warning)) {
                                deleteMovement(m.id)
                              }
                            }}
                          >
                            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                              <path d="M20 20H7L3 16a2 2 0 0 1 0-2.83L14.59 1.58a2 2 0 0 1 2.83 0l4 4a2 2 0 0 1 0 2.83L8 20" />
                              <path d="M6.5 15.5 15 7" />
                            </svg>
                          </button>
                        </td>
                        <td>{formatShortDate(m.date)}</td>
                        <td>{m.bucket}</td>
                        <td>{m.description}</td>
                        <td className="data-table__col--numeric">{formatN2(m.amount)}</td>
                      </tr>
                      {m.groupTotal !== null && (
                        <tr className="reserva-page__totals-row">
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
