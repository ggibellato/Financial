import type { BankDto, CreditCardDto } from '../api/types'
import type { PaymentMode } from '../hooks/useMonthly'

export type ExpenseFormField =
  | 'date'
  | 'description'
  | 'value'
  | 'category'
  | 'paymentSource'
  | 'creditCardId'
  | 'invoiceDate'
  | 'roundUpAmount'

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

interface ExpenseFormProps {
  isEditing: boolean
  date: string
  description: string
  value: string
  category: string
  paymentSource: string
  creditCardId: string
  creditCardName: string
  invoiceDate: string
  roundUpAmount: string
  paymentMode: PaymentMode
  banks: BankDto[]
  creditCards: CreditCardDto[]
  isSettled: boolean
  isSaving: boolean
  saveError: string | null
  onFieldChange: (field: ExpenseFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

export default function ExpenseForm({
  isEditing,
  date,
  description,
  value,
  category,
  paymentSource,
  creditCardId,
  creditCardName,
  invoiceDate,
  roundUpAmount,
  paymentMode,
  banks,
  creditCards,
  isSettled,
  isSaving,
  saveError,
  onFieldChange,
  onSave,
  onCancel,
}: ExpenseFormProps) {
  const selectedBank = banks.find((b) => b.id === paymentSource)
  const showRoundUpField = paymentMode === 'bank' && selectedBank?.roundUpEnabled === true
  const invoiceDateDisplay = invoiceDate || (date ? date.slice(0, 7) : '')
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
          <>
            <div className="monthly-page__form-field">
              <label>Payment</label>
              <p className="monthly-page__settled-note">
                Paid by {selectedBank?.name ?? paymentSource} via card {creditCardName || creditCardId}. Settled via its
                card statement — unmark the statement paid to change these fields.
              </p>
            </div>
            <div className="monthly-page__form-field">
              <label htmlFor="expense-invoice-date">Invoice Month</label>
              <input id="expense-invoice-date" type="month" value={invoiceDateDisplay} disabled />
            </div>
          </>
        ) : (
          <>
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
                      <option key={b.id} value={b.id}>
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
              <>
                <div className="monthly-page__form-field">
                  <label htmlFor="expense-card-tag">Card</label>
                  <select
                    id="expense-card-tag"
                    value={creditCardId}
                    onChange={(e) => onFieldChange('creditCardId', e.target.value)}
                  >
                    <option value="">Select card…</option>
                    {creditCards.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="monthly-page__form-field">
                  <label htmlFor="expense-invoice-date">Invoice Month</label>
                  <input
                    id="expense-invoice-date"
                    type="month"
                    value={invoiceDateDisplay}
                    onChange={(e) => onFieldChange('invoiceDate', e.target.value)}
                  />
                </div>
              </>
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
