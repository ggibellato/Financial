import type { BankDto } from '../api/types'
import type { TransferFormField } from '../hooks/mapTransferErrorToField'

interface TransferFormProps {
  isEditing: boolean
  date: string
  sourceBank: string
  destinationBank: string
  amount: string
  note: string
  banks: BankDto[]
  isSaving: boolean
  saveError: string | null
  saveErrorField: TransferFormField | null
  onFieldChange: (field: TransferFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

export default function TransferForm({
  isEditing,
  date,
  sourceBank,
  destinationBank,
  amount,
  note,
  banks,
  isSaving,
  saveError,
  saveErrorField,
  onFieldChange,
  onSave,
  onCancel,
}: TransferFormProps) {
  const sameBankError =
    sourceBank !== '' && destinationBank !== '' && sourceBank === destinationBank
      ? 'Source and destination must be different banks.'
      : null

  const destinationBanks = banks.filter((b) => b.name !== sourceBank)

  const fieldError = (field: TransferFormField): string | null => {
    if (field === 'destinationBank' && sameBankError) return sameBankError
    return saveErrorField === field ? saveError : null
  }

  const generalError = saveErrorField === null ? saveError : null

  return (
    <div className="monthly-page__form-panel">
      <p className="monthly-page__form-title">{isEditing ? 'Edit Transfer' : 'Move Money'}</p>
      <div className="monthly-page__form">
        <div className="monthly-page__form-field">
          <label htmlFor="transfer-date">Date</label>
          <input
            id="transfer-date"
            type="date"
            value={date}
            onChange={(e) => onFieldChange('date', e.target.value)}
          />
          {fieldError('date') && <p className="monthly-page__error">{fieldError('date')}</p>}
        </div>
        <div className="monthly-page__form-field">
          <label htmlFor="transfer-source-bank">From</label>
          <select
            id="transfer-source-bank"
            value={sourceBank}
            onChange={(e) => onFieldChange('sourceBank', e.target.value)}
          >
            {banks.map((b) => (
              <option key={b.name} value={b.name}>
                {b.name}
              </option>
            ))}
          </select>
          {fieldError('sourceBank') && <p className="monthly-page__error">{fieldError('sourceBank')}</p>}
        </div>
        <div className="monthly-page__form-field">
          <label htmlFor="transfer-destination-bank">To</label>
          <select
            id="transfer-destination-bank"
            value={destinationBank}
            onChange={(e) => onFieldChange('destinationBank', e.target.value)}
          >
            <option value="">Select a bank</option>
            {destinationBanks.map((b) => (
              <option key={b.name} value={b.name}>
                {b.name}
              </option>
            ))}
          </select>
          {fieldError('destinationBank') && <p className="monthly-page__error">{fieldError('destinationBank')}</p>}
        </div>
        <div className="monthly-page__form-field">
          <label htmlFor="transfer-amount">Amount</label>
          <input
            id="transfer-amount"
            type="number"
            step="0.01"
            value={amount}
            onChange={(e) => onFieldChange('amount', e.target.value)}
          />
          {fieldError('amount') && <p className="monthly-page__error">{fieldError('amount')}</p>}
        </div>
        <div className="monthly-page__form-field">
          <label htmlFor="transfer-note">Note</label>
          <input id="transfer-note" type="text" value={note} onChange={(e) => onFieldChange('note', e.target.value)} />
        </div>
      </div>
      <div className="monthly-page__form-actions">
        <button
          className="monthly-page__submit-btn"
          type="button"
          disabled={isSaving || sameBankError !== null}
          onClick={onSave}
        >
          {isSaving ? 'Saving...' : isEditing ? 'Save' : 'Move Money'}
        </button>
        <button className="monthly-page__cancel-btn" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
      {generalError && <p className="monthly-page__error">{generalError}</p>}
    </div>
  )
}
