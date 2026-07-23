import ErrorState from '../components/ErrorState'
import ExpensesSection from '../components/ExpensesSection'
import LoadingState from '../components/LoadingState'
import { PAYMENT_SOURCES, useMonthly, type CreateFormField, type EditField } from '../hooks/useMonthly'
import { formatN2 } from '../utils/formatters'
import './MonthlyPage.css'

type ExpenseFormField = 'date' | 'description' | 'value' | 'category' | 'paymentSource' | 'cardTag'

const CREATE_FIELD_BY_FORM_FIELD: Record<ExpenseFormField, CreateFormField> = {
  date: 'createDate',
  description: 'createDescription',
  value: 'createValue',
  category: 'createCategory',
  paymentSource: 'createPaymentSource',
  cardTag: 'createCardTag',
}

const EDIT_FIELD_BY_FORM_FIELD: Record<ExpenseFormField, EditField> = {
  date: 'editDate',
  description: 'editDescription',
  value: 'editValue',
  category: 'editCategory',
  paymentSource: 'editPaymentSource',
  cardTag: 'editCardTag',
}

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

const CARDS = ['BarclaysPlatinumVisa8003', 'BarclaysPlatinumVisa6007', 'ChaseMaster4023', 'BaAmex', 'PaypalCredit']

interface ExpenseFormProps {
  isEditing: boolean
  date: string
  description: string
  value: string
  category: string
  paymentSource: string
  cardTag: string
  isSaving: boolean
  saveError: string | null
  onFieldChange: (field: ExpenseFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

function ExpenseForm({
  isEditing,
  date,
  description,
  value,
  category,
  paymentSource,
  cardTag,
  isSaving,
  saveError,
  onFieldChange,
  onSave,
  onCancel,
}: ExpenseFormProps) {
  return (
    <div className="monthly-page__form-panel">
      <p className="monthly-page__form-title">{isEditing ? 'Edit Expense' : 'New Expense'}</p>
      <div className="monthly-page__form">
        <div className="monthly-page__form-field">
          <label htmlFor="expense-date">Date</label>
          <input
            id="expense-date"
            type="date"
            value={date}
            onChange={(e) => onFieldChange('date', e.target.value)}
          />
        </div>
        <div className="monthly-page__form-field">
          <label htmlFor="expense-description">Description</label>
          <input
            id="expense-description"
            type="text"
            value={description}
            onChange={(e) => onFieldChange('description', e.target.value)}
          />
        </div>
        <div className="monthly-page__form-field">
          <label htmlFor="expense-category">Category</label>
          <select
            id="expense-category"
            value={category}
            onChange={(e) => onFieldChange('category', e.target.value)}
          >
            {CATEGORIES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </div>
        <div className="monthly-page__form-field">
          <label htmlFor="expense-value">Value</label>
          <input
            id="expense-value"
            type="number"
            step="0.01"
            value={value}
            onChange={(e) => onFieldChange('value', e.target.value)}
          />
        </div>
        <div className="monthly-page__form-field">
          <label htmlFor="expense-payment-source">Payment Source</label>
          <select
            id="expense-payment-source"
            value={paymentSource}
            onChange={(e) => onFieldChange('paymentSource', e.target.value)}
          >
            {PAYMENT_SOURCES.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>
        </div>
        <div className="monthly-page__form-field">
          <label htmlFor="expense-card-tag">Card</label>
          <select
            id="expense-card-tag"
            value={cardTag}
            onChange={(e) => onFieldChange('cardTag', e.target.value)}
          >
            <option value="">—</option>
            {CARDS.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </div>
      </div>
      <div className="monthly-page__form-actions">
        <button className="monthly-page__submit-btn" type="button" disabled={isSaving} onClick={onSave}>
          {isSaving ? 'Saving...' : isEditing ? 'Save' : 'Add Expense'}
        </button>
        <button className="monthly-page__cancel-btn" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
      {saveError && <p className="monthly-page__error">{saveError}</p>}
    </div>
  )
}

export default function MonthlyPage() {
  const {
    monthInputValue,
    setMonthInputValue,
    expenses,
    categoryTotals,
    categoryTotalsSum,
    cardStatements,
    adjustmentTotal,
    bankTotals,
    bankTotalsSum,
    isLoading,
    error,
    retry,
    isCreateFormOpen,
    createDate,
    createDescription,
    createValue,
    createCategory,
    createPaymentSource,
    createCardTag,
    isCreating,
    createError,
    showCreateForm,
    cancelCreateForm,
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

  const isEditing = editingId !== null
  const isFormVisible = isCreateFormOpen || isEditing

  return (
    <div className="monthly-page">
      <div className="monthly-page__header">
        <div className="monthly-page__month-picker">
          <label htmlFor="monthly-month">Month</label>
          <input
            id="monthly-month"
            type="month"
            value={monthInputValue}
            onChange={(e) => setMonthInputValue(e.target.value)}
          />
        </div>
      </div>

      {isFormVisible && (
        <ExpenseForm
          isEditing={isEditing}
          date={isEditing ? editDate : createDate}
          description={isEditing ? editDescription : createDescription}
          value={isEditing ? editValue : createValue}
          category={isEditing ? editCategory : createCategory}
          paymentSource={isEditing ? editPaymentSource : createPaymentSource}
          cardTag={isEditing ? editCardTag : createCardTag}
          isSaving={isEditing ? isSaving : isCreating}
          saveError={isEditing ? saveError : createError}
          onFieldChange={(field, value) =>
            isEditing
              ? setEditField(EDIT_FIELD_BY_FORM_FIELD[field], value)
              : setCreateField(CREATE_FIELD_BY_FORM_FIELD[field], value)
          }
          onSave={isEditing ? saveEdit : submitCreate}
          onCancel={isEditing ? cancelEdit : cancelCreateForm}
        />
      )}

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <div className="monthly-page__content">
          <div className="monthly-page__grids-row">
            <section className="monthly-page__section monthly-page__section--grid">
              <h2>Category Totals</h2>
              <div className="monthly-page__table-scroll">
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
              </div>
              <p className="monthly-page__section-total">
                Total: <strong>{formatN2(categoryTotalsSum)}</strong>
              </p>
            </section>

            <section className="monthly-page__section monthly-page__section--grid">
              <h2>Cards</h2>
              <div className="monthly-page__table-scroll">
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
              </div>
              <p className="monthly-page__section-total">
                Combined adjustment figure: <strong>{formatN2(adjustmentTotal)}</strong>
              </p>
            </section>

            <section className="monthly-page__section monthly-page__section--grid">
              <h2>Banks</h2>
              <div className="monthly-page__table-scroll">
                <table className="monthly-page__table data-table">
                  <thead>
                    <tr>
                      <th>Bank</th>
                      <th className="data-table__col--numeric">Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    {bankTotals.map((b) => (
                      <tr key={b.bank}>
                        <td>{b.bank}</td>
                        <td className="data-table__col--numeric">{formatN2(b.totalValue)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <p className="monthly-page__section-total">
                Total: <strong>{formatN2(bankTotalsSum)}</strong>
              </p>
            </section>
          </div>

          <ExpensesSection
            expenses={expenses}
            onEdit={showEditForm}
            onDelete={deleteExpense}
            onNewExpense={showCreateForm}
          />
        </div>
      )}
    </div>
  )
}
