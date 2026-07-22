import type { MaeLedgerEntryDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useControleMae } from '../hooks/useControleMae'
import { formatN2, formatShortDate } from '../utils/formatters'
import './ControleMaePage.css'

interface EntryRowProps {
  entry: MaeLedgerEntryDto
  isEditing: boolean
  editBrlValue: string
  editGbpValue: string
  isSaving: boolean
  onEdit: (entry: MaeLedgerEntryDto) => void
  onFieldChange: (field: 'editBrlValue' | 'editGbpValue', value: string) => void
  onSave: () => void
  onCancel: () => void
}

function EntryRow({
  entry,
  isEditing,
  editBrlValue,
  editGbpValue,
  isSaving,
  onEdit,
  onFieldChange,
  onSave,
  onCancel,
}: EntryRowProps) {
  if (isEditing) {
    return (
      <tr>
        <td>{formatShortDate(entry.date)}</td>
        <td>{entry.description}</td>
        <td>{entry.note}</td>
        <td className="data-table__col--numeric">
          <input
            type="number"
            step="0.01"
            value={editBrlValue}
            onChange={(e) => onFieldChange('editBrlValue', e.target.value)}
          />
        </td>
        <td className="data-table__col--numeric">
          <input
            type="number"
            step="0.01"
            value={editGbpValue}
            onChange={(e) => onFieldChange('editGbpValue', e.target.value)}
          />
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
      <td>{formatShortDate(entry.date)}</td>
      <td>{entry.description}</td>
      <td>{entry.note}</td>
      <td className="data-table__col--numeric">{entry.brlValue !== null ? formatN2(entry.brlValue) : '—'}</td>
      <td className="data-table__col--numeric">{entry.gbpValue !== null ? formatN2(entry.gbpValue) : '—'}</td>
      <td>
        <button type="button" onClick={() => onEdit(entry)}>
          Edit
        </button>
      </td>
    </tr>
  )
}

export default function ControleMaePage() {
  const {
    monthInputValue,
    setMonthInputValue,
    entries,
    isLoading,
    error,
    retry,
    createDate,
    createDescription,
    createNote,
    createSourceCurrency,
    createSourceValue,
    isCreating,
    createError,
    setCreateField,
    submitCreate,
    editingId,
    editBrlValue,
    editGbpValue,
    isSaving,
    saveError,
    setEditField,
    showEditForm,
    cancelEdit,
    saveEdit,
  } = useControleMae()

  return (
    <div className="controle-mae-page">
      <div className="controle-mae-page__month-picker">
        <label htmlFor="controle-mae-month">Month</label>
        <input
          id="controle-mae-month"
          type="month"
          value={monthInputValue}
          onChange={(e) => setMonthInputValue(e.target.value)}
        />
      </div>

      <section className="controle-mae-page__section">
        <h2>New Entry</h2>
        <div className="controle-mae-page__form">
          <div className="controle-mae-page__form-field">
            <label htmlFor="create-date">Date</label>
            <input
              id="create-date"
              type="date"
              value={createDate}
              onChange={(e) => setCreateField('createDate', e.target.value)}
            />
          </div>
          <div className="controle-mae-page__form-field">
            <label htmlFor="create-description">Description</label>
            <input
              id="create-description"
              type="text"
              value={createDescription}
              onChange={(e) => setCreateField('createDescription', e.target.value)}
            />
          </div>
          <div className="controle-mae-page__form-field">
            <label htmlFor="create-note">Note</label>
            <input
              id="create-note"
              type="text"
              value={createNote}
              onChange={(e) => setCreateField('createNote', e.target.value)}
            />
          </div>
          <div className="controle-mae-page__form-field">
            <label htmlFor="create-currency">Currency</label>
            <select
              id="create-currency"
              value={createSourceCurrency}
              onChange={(e) => setCreateField('createSourceCurrency', e.target.value)}
            >
              <option value="BRL">BRL</option>
              <option value="GBP">GBP</option>
            </select>
          </div>
          <div className="controle-mae-page__form-field">
            <label htmlFor="create-value">Value</label>
            <input
              id="create-value"
              type="number"
              step="0.01"
              value={createSourceValue}
              onChange={(e) => setCreateField('createSourceValue', e.target.value)}
            />
          </div>
          <button type="button" disabled={isCreating} onClick={submitCreate}>
            {isCreating ? 'Saving...' : 'Add Entry'}
          </button>
          {createError && <p className="controle-mae-page__error">{createError}</p>}
        </div>
      </section>

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <section className="controle-mae-page__section">
          <h2>Ledger</h2>
          <table className="controle-mae-page__table data-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Description</th>
                <th>Note</th>
                <th className="data-table__col--numeric">BRL</th>
                <th className="data-table__col--numeric">GBP</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {entries.map((entry) => (
                <EntryRow
                  key={entry.id}
                  entry={entry}
                  isEditing={editingId === entry.id}
                  editBrlValue={editBrlValue}
                  editGbpValue={editGbpValue}
                  isSaving={isSaving}
                  onEdit={showEditForm}
                  onFieldChange={setEditField}
                  onSave={saveEdit}
                  onCancel={cancelEdit}
                />
              ))}
            </tbody>
          </table>
          {saveError && <p className="controle-mae-page__error">{saveError}</p>}
        </section>
      )}
    </div>
  )
}
