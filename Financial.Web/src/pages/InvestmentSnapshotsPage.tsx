import type { InvestmentSnapshotDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useInvestmentSnapshots } from '../hooks/useInvestmentSnapshots'
import { formatN2 } from '../utils/formatters'
import './InvestmentSnapshotsPage.css'

interface SnapshotRowProps {
  snapshot: InvestmentSnapshotDto
  isEditing: boolean
  editValue: string
  isSaving: boolean
  onEdit: (snapshot: InvestmentSnapshotDto) => void
  onValueChange: (value: string) => void
  onSave: () => void
  onCancel: () => void
}

function SnapshotRow({
  snapshot,
  isEditing,
  editValue,
  isSaving,
  onEdit,
  onValueChange,
  onSave,
  onCancel,
}: SnapshotRowProps) {
  const label = snapshot.isLiability ? `${snapshot.account} (liability)` : snapshot.account

  if (isEditing) {
    return (
      <tr>
        <td>{label}</td>
        <td className="data-table__col--numeric">
          <input type="number" step="0.01" min="0" value={editValue} onChange={(e) => onValueChange(e.target.value)} />
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

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
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
                <SnapshotRow
                  key={snapshot.id}
                  snapshot={snapshot}
                  isEditing={editingId === snapshot.id}
                  editValue={editValue}
                  isSaving={isSaving}
                  onEdit={showEditForm}
                  onValueChange={setEditValue}
                  onSave={saveEdit}
                  onCancel={cancelEdit}
                />
              ))}
            </tbody>
          </table>
          {saveError && <p className="investment-snapshots-page__error">{saveError}</p>}
        </section>
      )}
    </div>
  )
}
