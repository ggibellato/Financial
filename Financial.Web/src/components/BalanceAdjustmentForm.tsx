import type { BankDto } from '../api/types'
import type { BalanceAdjustmentFormField } from '../hooks/mapBalanceAdjustmentErrorToField'
import { formatN2 } from '../utils/formatters'

interface BalanceAdjustmentFormProps {
  isEditing: boolean
  bankName: string
  bankDisplayName: string
  banks: BankDto[]
  currentBalance: number
  date: string
  targetBalance: string
  note: string
  isSaving: boolean
  saveError: string | null
  saveErrorField: BalanceAdjustmentFormField | null
  savedDelta: number | null
  onFieldChange: (field: BalanceAdjustmentFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

export default function BalanceAdjustmentForm({
  isEditing,
  bankName,
  bankDisplayName,
  banks,
  currentBalance,
  date,
  targetBalance,
  note,
  isSaving,
  saveError,
  saveErrorField,
  savedDelta,
  onFieldChange,
  onSave,
  onCancel,
}: BalanceAdjustmentFormProps) {
  if (savedDelta !== null) {
    const sign = savedDelta < 0 ? '-' : ''
    return (
      <div className="monthly-page__form-panel">
        <p className="monthly-page__form-title">Balance Corrected</p>
        <p>
          Adjustment of {sign}£{formatN2(Math.abs(savedDelta))} recorded
        </p>
        <div className="monthly-page__form-actions">
          <button className="monthly-page__submit-btn" type="button" onClick={onCancel}>
            Close
          </button>
        </div>
      </div>
    )
  }

  const fieldError = (field: BalanceAdjustmentFormField): string | null =>
    saveErrorField === field ? saveError : null

  const generalError = saveErrorField === null ? saveError : null

  const bankChosen = bankName !== ''
  const saveDisabled = isSaving || (!isEditing && !bankChosen)

  return (
    <div className="monthly-page__form-panel">
      <p className="monthly-page__form-title">{isEditing ? 'Edit Balance Adjustment' : 'Correct Balance'}</p>
      {!isEditing && (
        <div className="monthly-page__form-field">
          <label htmlFor="adjustment-bank">Bank</label>
          <select
            id="adjustment-bank"
            value={bankName}
            onChange={(e) => onFieldChange('bankName', e.target.value)}
          >
            <option value="">Select a bank</option>
            {banks.map((b) => (
              <option key={b.id} value={b.id}>
                {b.name}
              </option>
            ))}
          </select>
          {fieldError('bankName') && <p className="monthly-page__error">{fieldError('bankName')}</p>}
        </div>
      )}
      {bankChosen && (
        <>
          <p className="monthly-page__form-reference">
            Current calculated balance for {bankDisplayName}: £{formatN2(currentBalance)}
          </p>
          <div className="monthly-page__form">
            <div className="monthly-page__form-field">
              <label htmlFor="adjustment-date">Date</label>
              <input
                id="adjustment-date"
                type="date"
                value={date}
                onChange={(e) => onFieldChange('date', e.target.value)}
              />
              {fieldError('date') && <p className="monthly-page__error">{fieldError('date')}</p>}
            </div>
            <div className="monthly-page__form-field">
              <label htmlFor="adjustment-target-balance">Target Balance</label>
              <input
                id="adjustment-target-balance"
                type="number"
                step="0.01"
                value={targetBalance}
                onChange={(e) => onFieldChange('targetBalance', e.target.value)}
              />
              {fieldError('targetBalance') && <p className="monthly-page__error">{fieldError('targetBalance')}</p>}
            </div>
            <div className="monthly-page__form-field">
              <label htmlFor="adjustment-note">Note</label>
              <input
                id="adjustment-note"
                type="text"
                value={note}
                onChange={(e) => onFieldChange('note', e.target.value)}
              />
            </div>
          </div>
        </>
      )}
      <div className="monthly-page__form-actions">
        <button className="monthly-page__submit-btn" type="button" disabled={saveDisabled} onClick={onSave}>
          {isSaving ? 'Saving...' : isEditing ? 'Save' : 'Correct Balance'}
        </button>
        <button className="monthly-page__cancel-btn" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
      {generalError && <p className="monthly-page__error">{generalError}</p>}
    </div>
  )
}
