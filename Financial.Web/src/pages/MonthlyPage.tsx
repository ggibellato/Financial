import ErrorState from '../components/ErrorState'
import ExpensesSection from '../components/ExpensesSection'
import IncomeSection from '../components/IncomeSection'
import LoadingState from '../components/LoadingState'
import {
  useMonthly,
  INCOME_SOURCES_WITH_GROSS_VALUE,
  type CreateFormField,
  type CreateIncomeField,
  type EditField,
  type EditIncomeField,
  type PaymentMode,
} from '../hooks/useMonthly'
import type { BankDto } from '../api/types'
import { formatN2 } from '../utils/formatters'
import './MonthlyPage.css'

type ExpenseFormField = 'date' | 'description' | 'value' | 'category' | 'paymentSource' | 'cardTag' | 'roundUpAmount'

const CREATE_FIELD_BY_FORM_FIELD: Record<ExpenseFormField, CreateFormField> = {
  date: 'createDate',
  description: 'createDescription',
  value: 'createValue',
  category: 'createCategory',
  paymentSource: 'createPaymentSource',
  cardTag: 'createCardTag',
  roundUpAmount: 'createRoundUpAmount',
}

const EDIT_FIELD_BY_FORM_FIELD: Record<ExpenseFormField, EditField> = {
  date: 'editDate',
  description: 'editDescription',
  value: 'editValue',
  category: 'editCategory',
  paymentSource: 'editPaymentSource',
  cardTag: 'editCardTag',
  roundUpAmount: 'editRoundUpAmount',
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

const INCOME_SOURCES = ['Gleison', 'Ariana', 'Lottery', 'DividendoJuros']

type IncomeFormField = 'date' | 'incomeSource' | 'grossValue' | 'netValue' | 'bank'

const CREATE_INCOME_FIELD_BY_FORM_FIELD: Record<IncomeFormField, CreateIncomeField> = {
  date: 'createIncomeDate',
  incomeSource: 'createIncomeSource',
  grossValue: 'createIncomeGrossValue',
  netValue: 'createIncomeNetValue',
  bank: 'createIncomeBank',
}

const EDIT_INCOME_FIELD_BY_FORM_FIELD: Record<IncomeFormField, EditIncomeField> = {
  date: 'editIncomeDate',
  incomeSource: 'editIncomeSource',
  grossValue: 'editIncomeGrossValue',
  netValue: 'editIncomeNetValue',
  bank: 'editIncomeBank',
}

interface ExpenseFormProps {
  isEditing: boolean
  date: string
  description: string
  value: string
  category: string
  paymentSource: string
  cardTag: string
  roundUpAmount: string
  paymentMode: PaymentMode
  banks: BankDto[]
  isSettled: boolean
  isSaving: boolean
  saveError: string | null
  onFieldChange: (field: ExpenseFormField, value: string) => void
  onModeChange: (mode: PaymentMode) => void
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
  roundUpAmount,
  paymentMode,
  banks,
  isSettled,
  isSaving,
  saveError,
  onFieldChange,
  onModeChange,
  onSave,
  onCancel,
}: ExpenseFormProps) {
  const selectedBank = banks.find((b) => b.name === paymentSource)
  const showRoundUpField = paymentMode === 'bank' && selectedBank?.roundUpEnabled === true
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
        {isSettled ? (
          <div className="monthly-page__form-field">
            <label>Payment</label>
            <p className="monthly-page__settled-note">
              Paid by {paymentSource} via card {cardTag}. Settled via its card statement — unmark the
              statement paid to change these fields.
            </p>
          </div>
        ) : (
          <>
            <div className="monthly-page__form-field">
              <span>Payment</span>
              <label>
                <input
                  type="radio"
                  name="expense-payment-mode"
                  checked={paymentMode === 'bank'}
                  onChange={() => onModeChange('bank')}
                />
                Pay immediately
              </label>
              <label>
                <input
                  type="radio"
                  name="expense-payment-mode"
                  checked={paymentMode === 'card'}
                  onChange={() => onModeChange('card')}
                />
                Charge to card
              </label>
            </div>
            {paymentMode === 'bank' ? (
              <>
                <div className="monthly-page__form-field">
                  <label htmlFor="expense-payment-source">Payment Source</label>
                  <select
                    id="expense-payment-source"
                    value={paymentSource}
                    onChange={(e) => onFieldChange('paymentSource', e.target.value)}
                  >
                    {banks.map((b) => (
                      <option key={b.name} value={b.name}>
                        {b.name}
                      </option>
                    ))}
                  </select>
                </div>
                {showRoundUpField && (
                  <div className="monthly-page__form-field">
                    <label htmlFor="expense-round-up-amount">Round-Up</label>
                    <input
                      id="expense-round-up-amount"
                      type="number"
                      step="0.01"
                      value={roundUpAmount}
                      onChange={(e) => onFieldChange('roundUpAmount', e.target.value)}
                    />
                  </div>
                )}
              </>
            ) : (
              <div className="monthly-page__form-field">
                <label htmlFor="expense-card-tag">Card</label>
                <select
                  id="expense-card-tag"
                  value={cardTag}
                  onChange={(e) => onFieldChange('cardTag', e.target.value)}
                >
                  <option value="">Select card…</option>
                  {CARDS.map((c) => (
                    <option key={c} value={c}>
                      {c}
                    </option>
                  ))}
                </select>
              </div>
            )}
          </>
        )}
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

interface IncomeFormProps {
  isEditing: boolean
  date: string
  incomeSource: string
  grossValue: string
  netValue: string
  bank: string
  banks: BankDto[]
  isSaving: boolean
  saveError: string | null
  onFieldChange: (field: IncomeFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

function IncomeForm({
  isEditing,
  date,
  incomeSource,
  grossValue,
  netValue,
  bank,
  banks,
  isSaving,
  saveError,
  onFieldChange,
  onSave,
  onCancel,
}: IncomeFormProps) {
  const showGrossValueField = INCOME_SOURCES_WITH_GROSS_VALUE.includes(incomeSource)
  return (
    <div className="monthly-page__form-panel">
      <p className="monthly-page__form-title">{isEditing ? 'Edit Income' : 'New Income'}</p>
      <div className="monthly-page__form">
        <div className="monthly-page__form-field">
          <label htmlFor="income-date">Date</label>
          <input
            id="income-date"
            type="date"
            value={date}
            onChange={(e) => onFieldChange('date', e.target.value)}
          />
        </div>
        <div className="monthly-page__form-field">
          <label htmlFor="income-source">Source</label>
          <select
            id="income-source"
            value={incomeSource}
            onChange={(e) => onFieldChange('incomeSource', e.target.value)}
          >
            {INCOME_SOURCES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>
        {showGrossValueField && (
          <div className="monthly-page__form-field">
            <label htmlFor="income-gross-value">Gross Value</label>
            <input
              id="income-gross-value"
              type="number"
              step="0.01"
              value={grossValue}
              onChange={(e) => onFieldChange('grossValue', e.target.value)}
            />
          </div>
        )}
        <div className="monthly-page__form-field">
          <label htmlFor="income-net-value">Net Value</label>
          <input
            id="income-net-value"
            type="number"
            step="0.01"
            value={netValue}
            onChange={(e) => onFieldChange('netValue', e.target.value)}
          />
        </div>
        <div className="monthly-page__form-field">
          <label htmlFor="income-bank">Bank</label>
          <select id="income-bank" value={bank} onChange={(e) => onFieldChange('bank', e.target.value)}>
            {banks.map((b) => (
              <option key={b.name} value={b.name}>
                {b.name}
              </option>
            ))}
          </select>
        </div>
      </div>
      <div className="monthly-page__form-actions">
        <button className="monthly-page__submit-btn" type="button" disabled={isSaving} onClick={onSave}>
          {isSaving ? 'Saving...' : isEditing ? 'Save' : 'Add Income'}
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
    banks,
    adjustmentTotal,
    bankTotals,
    bankTotalsSum,
    roundUpTotalsSum,
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
    createRoundUpAmount,
    createPaymentMode,
    setCreatePaymentMode,
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
    editRoundUpAmount,
    editPaymentMode,
    editIsSettled,
    setEditPaymentMode,
    isSaving,
    saveError,
    setEditField,
    showEditForm,
    cancelEdit,
    saveEdit,
    deleteExpense,
    markPaidSources,
    setMarkPaidSource,
    markStatementPaid,
    unmarkStatementPaid,
    incomes,
    isIncomeCreateFormOpen,
    createIncomeDate,
    createIncomeSource,
    createIncomeGrossValue,
    createIncomeNetValue,
    createIncomeBank,
    isCreatingIncome,
    createIncomeError,
    showCreateIncomeForm,
    cancelCreateIncomeForm,
    setCreateIncomeField,
    submitCreateIncome,
    editingIncomeId,
    editIncomeDate,
    editIncomeSource,
    editIncomeGrossValue,
    editIncomeNetValue,
    editIncomeBank,
    isSavingIncome,
    saveIncomeError,
    setEditIncomeField,
    showEditIncomeForm,
    cancelEditIncome,
    saveEditIncome,
    deleteIncome,
    incomeTotals,
    totalIncoming,
    titheSummary,
  } = useMonthly()

  const isEditing = editingId !== null
  const isFormVisible = isCreateFormOpen || isEditing
  const isIncomeEditing = editingIncomeId !== null
  const isIncomeFormVisible = isIncomeCreateFormOpen || isIncomeEditing

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
          roundUpAmount={isEditing ? editRoundUpAmount : createRoundUpAmount}
          paymentMode={isEditing ? editPaymentMode : createPaymentMode}
          banks={banks}
          isSettled={isEditing && editIsSettled}
          isSaving={isEditing ? isSaving : isCreating}
          saveError={isEditing ? saveError : createError}
          onFieldChange={(field, value) =>
            isEditing
              ? setEditField(EDIT_FIELD_BY_FORM_FIELD[field], value)
              : setCreateField(CREATE_FIELD_BY_FORM_FIELD[field], value)
          }
          onModeChange={isEditing ? setEditPaymentMode : setCreatePaymentMode}
          onSave={isEditing ? saveEdit : submitCreate}
          onCancel={isEditing ? cancelEdit : cancelCreateForm}
        />
      )}

      {isIncomeFormVisible && (
        <IncomeForm
          isEditing={isIncomeEditing}
          date={isIncomeEditing ? editIncomeDate : createIncomeDate}
          incomeSource={isIncomeEditing ? editIncomeSource : createIncomeSource}
          grossValue={isIncomeEditing ? editIncomeGrossValue : createIncomeGrossValue}
          netValue={isIncomeEditing ? editIncomeNetValue : createIncomeNetValue}
          bank={isIncomeEditing ? editIncomeBank : createIncomeBank}
          banks={banks}
          isSaving={isIncomeEditing ? isSavingIncome : isCreatingIncome}
          saveError={isIncomeEditing ? saveIncomeError : createIncomeError}
          onFieldChange={(field, value) =>
            isIncomeEditing
              ? setEditIncomeField(EDIT_INCOME_FIELD_BY_FORM_FIELD[field], value)
              : setCreateIncomeField(CREATE_INCOME_FIELD_BY_FORM_FIELD[field], value)
          }
          onSave={isIncomeEditing ? saveEditIncome : submitCreateIncome}
          onCancel={isIncomeEditing ? cancelEditIncome : cancelCreateIncomeForm}
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
                          {s.isPaid ? (
                            <button type="button" onClick={() => unmarkStatementPaid(s.id)}>
                              Unmark Paid
                            </button>
                          ) : (
                            <>
                              <select
                                aria-label={`Paying bank for ${s.card}`}
                                value={markPaidSources[s.id] ?? ''}
                                onChange={(e) => setMarkPaidSource(s.id, e.target.value)}
                              >
                                <option value="">Bank…</option>
                                {banks.map((b) => (
                                  <option key={b.name} value={b.name}>
                                    {b.name}
                                  </option>
                                ))}
                              </select>{' '}
                              <button
                                type="button"
                                disabled={!markPaidSources[s.id]}
                                onClick={() => markStatementPaid(s.id, markPaidSources[s.id])}
                              >
                                Mark Paid
                              </button>
                            </>
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
                      <th className="data-table__col--numeric">Bank Balance</th>
                      <th className="data-table__col--numeric">Round-Up</th>
                    </tr>
                  </thead>
                  <tbody>
                    {bankTotals.map((b) => (
                      <tr key={b.bank}>
                        <td>{b.bank}</td>
                        <td className="data-table__col--numeric">{formatN2(b.balance)}</td>
                        <td className="data-table__col--numeric">{formatN2(b.roundUpTotal)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <p className="monthly-page__section-total">
                Bank Balance: <strong>{formatN2(bankTotalsSum)}</strong> · Round-Up: <strong>{formatN2(roundUpTotalsSum)}</strong>
              </p>
            </section>

            <section className="monthly-page__section monthly-page__section--grid">
              <h2>Incoming</h2>
              <div className="monthly-page__table-scroll">
                <table className="monthly-page__table data-table">
                  <thead>
                    <tr>
                      <th>Source</th>
                      <th className="data-table__col--numeric">Gross</th>
                      <th className="data-table__col--numeric">Net</th>
                    </tr>
                  </thead>
                  <tbody>
                    {incomeTotals.map((i) => (
                      <tr key={i.source}>
                        <td>{i.source}</td>
                        <td className="data-table__col--numeric">
                          {i.grossValue != null ? formatN2(i.grossValue) : '—'}
                        </td>
                        <td className="data-table__col--numeric">{formatN2(i.netValue)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <p className="monthly-page__section-total">
                Total Incoming: <strong>{formatN2(totalIncoming)}</strong>
                {titheSummary && (
                  <>
                    {' '}
                    · Calculated Tithe: <strong>{formatN2(titheSummary.calculatedTithe)}</strong> · Tithe Balance:{' '}
                    <strong>{formatN2(titheSummary.titheBalance)}</strong>
                  </>
                )}
              </p>
            </section>
          </div>

          <ExpensesSection
            expenses={expenses}
            onEdit={showEditForm}
            onDelete={deleteExpense}
            onNewExpense={showCreateForm}
          />

          <IncomeSection
            incomes={incomes}
            onEdit={showEditIncomeForm}
            onDelete={deleteIncome}
            onNewIncome={showCreateIncomeForm}
          />
        </div>
      )}
    </div>
  )
}
