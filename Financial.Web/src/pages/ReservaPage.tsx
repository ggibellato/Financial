import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { RESERVE_BUCKETS, useReserva } from '../hooks/useReserva'
import type { SplitFormField, WithdrawalFormField } from '../hooks/useReserva'
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

const SPLIT_FIELDS: { field: SplitFormField; label: string }[] = [
  { field: 'gleisonSalaryGross', label: 'Salario Gleison (gross)' },
  { field: 'gleisonSalaryNet', label: 'Salario Gleison (net)' },
  { field: 'arianaSalaryGross', label: 'Salario Ariana (gross)' },
  { field: 'arianaSalaryNet', label: 'Salario Ariana (net)' },
  { field: 'lottery', label: 'Lottery' },
  { field: 'dividendoJuros', label: 'Dividendo/Juros' },
]

export default function ReservaPage() {
  const {
    balances,
    totalBalance,
    movements,
    isLoading,
    error,
    retry,
    isSplitFormOpen,
    splitDate,
    gleisonSalaryGross,
    gleisonSalaryNet,
    arianaSalaryGross,
    arianaSalaryNet,
    lottery,
    dividendoJuros,
    isSubmittingSplit,
    splitError,
    showSplitForm,
    cancelSplitForm,
    setSplitField,
    submitIncomeSplit,
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
  } = useReserva()

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  const splitValues: Record<SplitFormField, string> = {
    splitDate,
    gleisonSalaryGross,
    gleisonSalaryNet,
    arianaSalaryGross,
    arianaSalaryNet,
    lottery,
    dividendoJuros,
  }

  const withdrawalValues: Record<WithdrawalFormField, string> = {
    withdrawalBucket,
    withdrawalAmount,
    withdrawalDate,
    withdrawalDescription,
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
            {SPLIT_FIELDS.map(({ field, label }) => (
              <div className="reserva-page__form-field" key={field}>
                <label htmlFor={`split-${field}`}>{label}</label>
                <input
                  id={`split-${field}`}
                  type="number"
                  step="0.01"
                  value={splitValues[field]}
                  onChange={(e) => setSplitField(field, e.target.value)}
                />
              </div>
            ))}
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
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Bucket</th>
                    <th>Description</th>
                    <th className="data-table__col--numeric">Amount</th>
                  </tr>
                </thead>
                <tbody>
                  {movements.map((m) => (
                    <tr key={m.id}>
                      <td>{formatShortDate(m.date)}</td>
                      <td>{m.bucket}</td>
                      <td>{m.description}</td>
                      <td className="data-table__col--numeric">{formatN2(m.amount)}</td>
                    </tr>
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
