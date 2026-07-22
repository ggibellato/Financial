import type { ExpenseDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useMonthly } from '../hooks/useMonthly'
import { formatN2, formatShortDate } from '../utils/formatters'
import './MonthlyPage.css'

const CATEGORIES = [
  'Ariana',
  'Carro',
  'Casa',
  'Estudo',
  'Extras',
  'Familia',
  'Gleison',
  'Mercado',
  'Samuel',
  'Saude',
  'Viagem',
  'Dizimo',
  'Investimento',
  'Reserva',
]

const PAYMENT_SOURCES = ['Barclays', 'Trading212', 'Chase']

const CARDS = ['BarclaysPlatinumVisa8003', 'BarclaysPlatinumVisa6007', 'ChaseMaster4023', 'BaAmex', 'PaypalCredit']

interface ExpenseRowProps {
  expense: ExpenseDto
  isEditing: boolean
  editDate: string
  editDescription: string
  editValue: string
  editCategory: string
  editPaymentSource: string
  editCardTag: string
  isSaving: boolean
  onEdit: (expense: ExpenseDto) => void
  onFieldChange: (
    field: 'editDate' | 'editDescription' | 'editValue' | 'editCategory' | 'editPaymentSource' | 'editCardTag',
    value: string,
  ) => void
  onSave: () => void
  onCancel: () => void
  onDelete: (id: string) => void
}

function ExpenseRow({
  expense,
  isEditing,
  editDate,
  editDescription,
  editValue,
  editCategory,
  editPaymentSource,
  editCardTag,
  isSaving,
  onEdit,
  onFieldChange,
  onSave,
  onCancel,
  onDelete,
}: ExpenseRowProps) {
  if (isEditing) {
    return (
      <tr>
        <td>
          <input type="date" value={editDate} onChange={(e) => onFieldChange('editDate', e.target.value)} />
        </td>
        <td>
          <input
            type="text"
            value={editDescription}
            onChange={(e) => onFieldChange('editDescription', e.target.value)}
          />
        </td>
        <td>
          <select value={editCategory} onChange={(e) => onFieldChange('editCategory', e.target.value)}>
            {CATEGORIES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </td>
        <td className="data-table__col--numeric">
          <input type="number" step="0.01" value={editValue} onChange={(e) => onFieldChange('editValue', e.target.value)} />
        </td>
        <td>
          <select value={editPaymentSource} onChange={(e) => onFieldChange('editPaymentSource', e.target.value)}>
            {PAYMENT_SOURCES.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>
        </td>
        <td>
          <select value={editCardTag} onChange={(e) => onFieldChange('editCardTag', e.target.value)}>
            <option value="">—</option>
            {CARDS.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </td>
        <td>
          <button type="button" disabled={isSaving} onClick={onSave}>
            {isSaving ? 'Saving...' : 'Save'}
          </button>
          <button type="button" onClick={onCancel}>
            Cancel
          </button>
        </td>
      </tr>
    )
  }

  return (
    <tr>
      <td>{formatShortDate(expense.date)}</td>
      <td>{expense.description}</td>
      <td>{expense.category}</td>
      <td className="data-table__col--numeric">{formatN2(expense.value)}</td>
      <td>{expense.paymentSource}</td>
      <td>{expense.cardTag ?? '—'}</td>
      <td>
        <button type="button" onClick={() => onEdit(expense)}>
          Edit
        </button>
        <button type="button" onClick={() => onDelete(expense.id)}>
          Delete
        </button>
      </td>
    </tr>
  )
}

export default function MonthlyPage() {
  const {
    monthInputValue,
    setMonthInputValue,
    expenses,
    categoryTotals,
    cardStatements,
    adjustmentTotal,
    isLoading,
    error,
    retry,
    createDate,
    createDescription,
    createValue,
    createCategory,
    createPaymentSource,
    createCardTag,
    isCreating,
    createError,
    setCreateField,
    submitCreate,
    editingId,
    editDate,
    editDescription,
    editValue,
    editCategory,
    editPaymentSource,
    editCardTag,
    isSaving,
    saveError,
    setEditField,
    showEditForm,
    cancelEdit,
    saveEdit,
    deleteExpense,
    markStatementPaid,
  } = useMonthly()

  return (
    <div className="monthly-page">
      <div className="monthly-page__month-picker">
        <label htmlFor="monthly-month">Month</label>
        <input
          id="monthly-month"
          type="month"
          value={monthInputValue}
          onChange={(e) => setMonthInputValue(e.target.value)}
        />
      </div>

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <>
          <section className="monthly-page__section">
            <h2>Category Totals</h2>
            <table className="monthly-page__table data-table">
              <thead>
                <tr>
                  <th>Category</th>
                  <th className="data-table__col--numeric">Total</th>
                </tr>
              </thead>
              <tbody>
                {categoryTotals.map((c) => (
                  <tr key={c.category}>
                    <td>{c.category}</td>
                    <td className="data-table__col--numeric">{formatN2(c.totalValue)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>

          <section className="monthly-page__section">
            <h2>Cards</h2>
            <table className="monthly-page__table data-table">
              <thead>
                <tr>
                  <th>Card</th>
                  <th className="data-table__col--numeric">Outstanding</th>
                  <th>Status</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {cardStatements.map((s) => (
                  <tr key={s.id}>
                    <td>{s.card}</td>
                    <td className="data-table__col--numeric">{formatN2(s.outstandingTotal)}</td>
                    <td>{s.isPaid ? 'Paid' : 'Unpaid'}</td>
                    <td>
                      {!s.isPaid && (
                        <button type="button" onClick={() => markStatementPaid(s.id)}>
                          Mark Paid
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <p className="monthly-page__adjustment">
              Combined adjustment figure: <strong>{formatN2(adjustmentTotal)}</strong>
            </p>
          </section>

          <section className="monthly-page__section">
            <h2>New Expense</h2>
            <div className="monthly-page__form">
              <div className="monthly-page__form-field">
                <label htmlFor="create-date">Date</label>
                <input
                  id="create-date"
                  type="date"
                  value={createDate}
                  onChange={(e) => setCreateField('createDate', e.target.value)}
                />
              </div>
              <div className="monthly-page__form-field">
                <label htmlFor="create-description">Description</label>
                <input
                  id="create-description"
                  type="text"
                  value={createDescription}
                  onChange={(e) => setCreateField('createDescription', e.target.value)}
                />
              </div>
              <div className="monthly-page__form-field">
                <label htmlFor="create-category">Category</label>
                <select
                  id="create-category"
                  value={createCategory}
                  onChange={(e) => setCreateField('createCategory', e.target.value)}
                >
                  {CATEGORIES.map((c) => (
                    <option key={c} value={c}>
                      {c}
                    </option>
                  ))}
                </select>
              </div>
              <div className="monthly-page__form-field">
                <label htmlFor="create-value">Value</label>
                <input
                  id="create-value"
                  type="number"
                  step="0.01"
                  value={createValue}
                  onChange={(e) => setCreateField('createValue', e.target.value)}
                />
              </div>
              <div className="monthly-page__form-field">
                <label htmlFor="create-payment-source">Payment Source</label>
                <select
                  id="create-payment-source"
                  value={createPaymentSource}
                  onChange={(e) => setCreateField('createPaymentSource', e.target.value)}
                >
                  {PAYMENT_SOURCES.map((p) => (
                    <option key={p} value={p}>
                      {p}
                    </option>
                  ))}
                </select>
              </div>
              <div className="monthly-page__form-field">
                <label htmlFor="create-card-tag">Card</label>
                <select
                  id="create-card-tag"
                  value={createCardTag}
                  onChange={(e) => setCreateField('createCardTag', e.target.value)}
                >
                  <option value="">—</option>
                  {CARDS.map((c) => (
                    <option key={c} value={c}>
                      {c}
                    </option>
                  ))}
                </select>
              </div>
              <button type="button" disabled={isCreating} onClick={submitCreate}>
                {isCreating ? 'Saving...' : 'Add Expense'}
              </button>
              {createError && <p className="monthly-page__error">{createError}</p>}
            </div>
          </section>

          <section className="monthly-page__section">
            <h2>Expenses</h2>
            <table className="monthly-page__table data-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Description</th>
                  <th>Category</th>
                  <th className="data-table__col--numeric">Value</th>
                  <th>Payment Source</th>
                  <th>Card</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {expenses.map((expense) => (
                  <ExpenseRow
                    key={expense.id}
                    expense={expense}
                    isEditing={editingId === expense.id}
                    editDate={editDate}
                    editDescription={editDescription}
                    editValue={editValue}
                    editCategory={editCategory}
                    editPaymentSource={editPaymentSource}
                    editCardTag={editCardTag}
                    isSaving={isSaving}
                    onEdit={showEditForm}
                    onFieldChange={setEditField}
                    onSave={saveEdit}
                    onCancel={cancelEdit}
                    onDelete={deleteExpense}
                  />
                ))}
              </tbody>
            </table>
            {saveError && <p className="monthly-page__error">{saveError}</p>}
          </section>
        </>
      )}
    </div>
  )
}
