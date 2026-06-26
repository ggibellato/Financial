import type { TransactionDto } from '../api/types'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import type { TransactionFormField } from '../hooks/useTransactions'
import { useTransactions } from '../hooks/useTransactions'
import './TransactionsTab.css'

function formatN2(value: number): string {
  return new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

function formatN8(value: number): string {
  return new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 8,
    maximumFractionDigits: 8,
  }).format(value)
}

function formatDate(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`
}

interface TransactionRowProps {
  transaction: TransactionDto
  onEdit: (t: TransactionDto) => void
  onDelete: (id: string) => void
}

function TransactionRow({ transaction, onEdit, onDelete }: TransactionRowProps) {
  const typeClass =
    transaction.type === 'Buy'
      ? 'transactions-tab__type--buy'
      : 'transactions-tab__type--sell'

  return (
    <tr>
      <td>
        <button
          className="transactions-tab__action-btn"
          type="button"
          aria-label="Edit transaction"
          onClick={() => onEdit(transaction)}
        >
          ✏
        </button>
      </td>
      <td>
        <button
          className="transactions-tab__action-btn"
          type="button"
          aria-label="Delete transaction"
          onClick={() => onDelete(transaction.id)}
        >
          ✕
        </button>
      </td>
      <td>{formatDate(transaction.date)}</td>
      <td className={typeClass}>{transaction.type}</td>
      <td className="transactions-tab__amount">{formatN8(transaction.quantity)}</td>
      <td className="transactions-tab__amount">{formatN2(transaction.unitPrice)}</td>
      <td className="transactions-tab__amount">{formatN2(transaction.fees)}</td>
      <td className="transactions-tab__total">{formatN2(transaction.totalPrice)}</td>
    </tr>
  )
}

interface InlineFormProps {
  editingId: string | null
  formDate: string
  formType: string
  formQuantity: string
  formUnitPrice: string
  formFees: string
  isSaving: boolean
  saveError: string | null
  onFieldChange: (field: TransactionFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

function InlineForm({
  editingId,
  formDate,
  formType,
  formQuantity,
  formUnitPrice,
  formFees,
  isSaving,
  saveError,
  onFieldChange,
  onSave,
  onCancel,
}: InlineFormProps) {
  const title = editingId ? 'Edit transaction' : 'New transaction'

  return (
    <div className="transactions-tab__form">
      <p className="transactions-tab__form-title">{title}</p>
      <div className="transactions-tab__form-fields">
        <div className="transactions-tab__form-field">
          <label htmlFor="tx-date">Date</label>
          <input
            id="tx-date"
            type="date"
            value={formDate}
            required
            onChange={(e) => onFieldChange('formDate', e.target.value)}
          />
        </div>
        <div className="transactions-tab__form-field">
          <label htmlFor="tx-type">Type</label>
          <select
            id="tx-type"
            value={formType}
            onChange={(e) => onFieldChange('formType', e.target.value)}
          >
            <option value="Buy">Buy</option>
            <option value="Sell">Sell</option>
          </select>
        </div>
        <div className="transactions-tab__form-field">
          <label htmlFor="tx-quantity">Quantity</label>
          <input
            id="tx-quantity"
            type="number"
            step="0.0001"
            min="0"
            value={formQuantity}
            required
            onChange={(e) => onFieldChange('formQuantity', e.target.value)}
          />
        </div>
        <div className="transactions-tab__form-field">
          <label htmlFor="tx-unit-price">Unit Price</label>
          <input
            id="tx-unit-price"
            type="number"
            step="0.0001"
            min="0"
            value={formUnitPrice}
            required
            onChange={(e) => onFieldChange('formUnitPrice', e.target.value)}
          />
        </div>
        <div className="transactions-tab__form-field">
          <label htmlFor="tx-fees">Fees</label>
          <input
            id="tx-fees"
            type="number"
            step="0.0001"
            min="0"
            value={formFees}
            onChange={(e) => onFieldChange('formFees', e.target.value)}
          />
        </div>
      </div>
      <div className="transactions-tab__form-actions">
        <button
          className="transactions-tab__save-btn"
          type="button"
          disabled={isSaving}
          onClick={onSave}
        >
          {isSaving ? 'Saving...' : 'Save'}
        </button>
        <button className="transactions-tab__cancel-btn" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
      {saveError && <p className="transactions-tab__error">{saveError}</p>}
    </div>
  )
}

export default function TransactionsTab() {
  const {
    isLoading,
    error,
    retry,
    transactions,
    isFormVisible,
    editingId,
    formDate,
    formType,
    formQuantity,
    formUnitPrice,
    formFees,
    isSaving,
    saveError,
    deleteError,
    nodeType,
    showNewForm,
    showEditForm,
    cancelForm,
    setFormField,
    saveForm,
    deleteTransaction,
  } = useTransactions()

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  if (nodeType !== 'Asset') {
    return (
      <p className="transactions-tab__placeholder">
        Transactions are only available for individual assets
      </p>
    )
  }

  return (
    <div className="transactions-tab">
      <div className="transactions-tab__toolbar">
        <button className="transactions-tab__new-btn" type="button" onClick={showNewForm}>
          New
        </button>
      </div>

      {isFormVisible && (
        <InlineForm
          editingId={editingId}
          formDate={formDate}
          formType={formType}
          formQuantity={formQuantity}
          formUnitPrice={formUnitPrice}
          formFees={formFees}
          isSaving={isSaving}
          saveError={saveError}
          onFieldChange={setFormField}
          onSave={saveForm}
          onCancel={cancelForm}
        />
      )}

      <div className="transactions-tab__table-wrapper">
        <table className="transactions-tab__table">
          <thead>
            <tr>
              <th />
              <th />
              <th>Date</th>
              <th>Type</th>
              <th className="transactions-tab__amount">Quantity</th>
              <th className="transactions-tab__amount">Unit Price</th>
              <th className="transactions-tab__amount">Fees</th>
              <th className="transactions-tab__amount">Total</th>
            </tr>
          </thead>
          <tbody>
            {transactions.map((t) => (
              <TransactionRow
                key={t.id}
                transaction={t}
                onEdit={showEditForm}
                onDelete={deleteTransaction}
              />
            ))}
          </tbody>
        </table>
      </div>

      {deleteError && <p className="transactions-tab__delete-error">{deleteError}</p>}
    </div>
  )
}
