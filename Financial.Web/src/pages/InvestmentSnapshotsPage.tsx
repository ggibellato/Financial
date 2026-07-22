import type { InvestmentSnapshotDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useInvestmentSnapshots } from '../hooks/useInvestmentSnapshots'
import { formatN2 } from '../utils/formatters'
import './InvestmentSnapshotsPage.css'

interface SnapshotRowProps {
  snapshot: InvestmentSnapshotDto
  onEdit: (snapshot: InvestmentSnapshotDto) => void
}

function SnapshotRow({ snapshot, onEdit }: SnapshotRowProps) {
  const label = snapshot.isLiability ? `${snapshot.account} (liability)` : snapshot.account

  return (
    <tr>
      <td>{label}</td>
      <td className="data-table__col--numeric">{formatN2(snapshot.value)}</td>
      <td>
        <button type="button" onClick={() => onEdit(snapshot)}>
          Edit
        </button>
      </td>
    </tr>
  )
}

export default function InvestmentSnapshotsPage() {
  const {
    monthInputValue,
    setMonthInputValue,
    snapshots,
    isLoading,
    error,
    retry,
    editingId,
    editValue,
    isSaving,
    saveError,
    setEditValue,
    showEditForm,
    cancelEdit,
    saveEdit,
  } = useInvestmentSnapshots()

  const isEditing = editingId !== null

  return (
    <div className="investment-snapshots-page">
      <div className="investment-snapshots-page__month-picker">
        <label htmlFor="investment-snapshots-month">Month</label>
        <input
          id="investment-snapshots-month"
          type="month"
          value={monthInputValue}
          onChange={(e) => setMonthInputValue(e.target.value)}
        />
      </div>

      {isEditing && (
        <div className="investment-snapshots-page__form-panel">
          <p className="investment-snapshots-page__form-title">Edit Snapshot</p>
          <div className="investment-snapshots-page__form">
            <div className="investment-snapshots-page__form-field">
              <label htmlFor="snapshot-edit-value">Value</label>
              <input
                id="snapshot-edit-value"
                type="number"
                step="0.01"
                min="0"
                value={editValue}
                onChange={(e) => setEditValue(e.target.value)}
              />
            </div>
          </div>
          <div className="investment-snapshots-page__form-actions">
            <button type="button" disabled={isSaving} onClick={saveEdit}>
              {isSaving ? 'Saving...' : 'Save'}
            </button>
            <button type="button" onClick={cancelEdit}>
              Cancel
            </button>
          </div>
          {saveError && <p className="investment-snapshots-page__error">{saveError}</p>}
        </div>
      )}

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <div className="investment-snapshots-page__content">
          <section className="investment-snapshots-page__section">
            <table className="investment-snapshots-page__table data-table">
              <thead>
                <tr>
                  <th>Account</th>
                  <th className="data-table__col--numeric">Value</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {snapshots.map((snapshot) => (
                  <SnapshotRow key={snapshot.id} snapshot={snapshot} onEdit={showEditForm} />
                ))}
              </tbody>
            </table>
          </section>
        </div>
      )}
    </div>
  )
}
