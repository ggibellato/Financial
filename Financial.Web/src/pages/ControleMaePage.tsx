import type { MaeLedgerEntryDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useControleMae } from '../hooks/useControleMae'
import { formatN2, formatShortDate } from '../utils/formatters'
import './ControleMaePage.css'

function LedgerColumns() {
  return (
    <colgroup>
      <col className="controle-mae-page__col-actions" />
      <col className="controle-mae-page__col-date" />
      <col className="controle-mae-page__col-description" />
      <col className="controle-mae-page__col-note" />
      <col className="controle-mae-page__col-value" />
      <col className="controle-mae-page__col-value" />
    </colgroup>
  )
}

interface EntryRowProps {
  entry: MaeLedgerEntryDto
  onEdit: (entry: MaeLedgerEntryDto) => void
}

function EntryRow({ entry, onEdit }: EntryRowProps) {
  return (
    <tr>
      <td>
        <button
          className="data-table__action-btn"
          type="button"
          aria-label="Edit entry"
          onClick={() => onEdit(entry)}
        >
          ✏
        </button>
      </td>
      <td>{formatShortDate(entry.date)}</td>
      <td>{entry.description}</td>
      <td>{entry.note}</td>
      <td className="data-table__col--numeric">{entry.brlValue !== null ? formatN2(entry.brlValue) : '—'}</td>
      <td className="data-table__col--numeric">{entry.gbpValue !== null ? formatN2(entry.gbpValue) : '—'}</td>
    </tr>
  )
}

export default function ControleMaePage() {
  const {
    fromDateInputValue,
    setFromDateInputValue,
    entries,
    totals,
    isLoading,
    error,
    retry,
    isCreateFormOpen,
    createDate,
    createDescription,
    createNote,
    createSourceCurrency,
    createSourceValue,
    isCreating,
    createError,
    showCreateForm,
    cancelCreateForm,
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

  const isEditing = editingId !== null
  const isFormVisible = isCreateFormOpen || isEditing

  return (
    <div className="controle-mae-page">
      <div className="controle-mae-page__header">
        <div className="controle-mae-page__month-picker">
          <label htmlFor="controle-mae-from-date">From</label>
          <input
            id="controle-mae-from-date"
            type="date"
            value={fromDateInputValue}
            onChange={(e) => setFromDateInputValue(e.target.value)}
          />
        </div>
        <button className="controle-mae-page__new-btn" type="button" onClick={showCreateForm}>
          New Entry
        </button>
      </div>

      {isFormVisible && (
        <div className="controle-mae-page__form-panel">
          <p className="controle-mae-page__form-title">{isEditing ? 'Edit Entry' : 'New Entry'}</p>
          {isEditing ? (
            <div className="controle-mae-page__form">
              <div className="controle-mae-page__form-field">
                <label htmlFor="edit-brl-value">BRL</label>
                <input
                  id="edit-brl-value"
                  type="number"
                  step="0.01"
                  value={editBrlValue}
                  onChange={(e) => setEditField('editBrlValue', e.target.value)}
                />
              </div>
              <div className="controle-mae-page__form-field">
                <label htmlFor="edit-gbp-value">GBP</label>
                <input
                  id="edit-gbp-value"
                  type="number"
                  step="0.01"
                  value={editGbpValue}
                  onChange={(e) => setEditField('editGbpValue', e.target.value)}
                />
              </div>
            </div>
          ) : (
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
            </div>
          )}
          <div className="controle-mae-page__form-actions">
            <button type="button" disabled={isEditing ? isSaving : isCreating} onClick={isEditing ? saveEdit : submitCreate}>
              {isEditing ? (isSaving ? 'Saving...' : 'Save') : isCreating ? 'Saving...' : 'Add Entry'}
            </button>
            <button type="button" onClick={isEditing ? cancelEdit : cancelCreateForm}>
              Cancel
            </button>
          </div>
          {(isEditing ? saveError : createError) && (
            <p className="controle-mae-page__error">{isEditing ? saveError : createError}</p>
          )}
        </div>
      )}

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <div className="controle-mae-page__content">
          <section className="controle-mae-page__section">
            <h2>Ledger</h2>
            <table className="controle-mae-page__table data-table">
              <LedgerColumns />
              <thead>
                <tr>
                  <th />
                  <th>Date</th>
                  <th>Description</th>
                  <th>Note</th>
                  <th className="data-table__col--numeric">BRL</th>
                  <th className="data-table__col--numeric">GBP</th>
                </tr>
              </thead>
              <tbody>
                {entries.map((entry) => (
                  <EntryRow key={entry.id} entry={entry} onEdit={showEditForm} />
                ))}
              </tbody>
            </table>
          </section>
        </div>
      )}

      {!isLoading && !error && (
        <table className="controle-mae-page__table controle-mae-page__totals-table data-table">
          <LedgerColumns />
          <tbody>
            <tr className="controle-mae-page__totals-row">
              <td />
              <td colSpan={3}>Total (all entries)</td>
              <td className="data-table__col--numeric">{totals ? formatN2(totals.totalBrlValue) : '—'}</td>
              <td className="data-table__col--numeric">{totals ? formatN2(totals.totalGbpValue) : '—'}</td>
            </tr>
          </tbody>
        </table>
      )}
    </div>
  )
}
