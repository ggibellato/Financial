import type { BankDto, IncomeSourceDto } from '../api/types'
import { INCOME_SOURCES_WITH_GROSS_VALUE, selectActiveIncomeSources } from '../hooks/useMonthly'

export type IncomeFormField = 'date' | 'incomeSource' | 'grossValue' | 'netValue' | 'bank'

interface IncomeFormProps {
  isEditing: boolean
  date: string
  incomeSource: string
  grossValue: string
  netValue: string
  bank: string
  banks: BankDto[]
  incomeSources: IncomeSourceDto[]
  isSaving: boolean
  saveError: string | null
  onFieldChange: (field: IncomeFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

export default function IncomeForm({
  isEditing,
  date,
  incomeSource,
  grossValue,
  netValue,
  bank,
  banks,
  incomeSources,
  isSaving,
  saveError,
  onFieldChange,
  onSave,
  onCancel,
}: IncomeFormProps) {
  const showGrossValueField = INCOME_SOURCES_WITH_GROSS_VALUE.includes(incomeSource)
  const activeIncomeSources = selectActiveIncomeSources(incomeSources)
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
            {activeIncomeSources.map((s) => (
              <option key={s.id} value={s.name}>
                {s.name}
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
