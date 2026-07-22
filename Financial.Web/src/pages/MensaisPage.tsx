import type { RecurringBillInstanceDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useMensais } from '../hooks/useMensais'
import { formatN2 } from '../utils/formatters'
import './MensaisPage.css'

const STATUSES = ['Unset', 'Scheduled', 'Paid']

interface InstanceRowProps {
  instance: RecurringBillInstanceDto
  onEdit: (instance: RecurringBillInstanceDto) => void
}

function InstanceRow({ instance, onEdit }: InstanceRowProps) {
  return (
    <tr>
      <td>{instance.dueDay}</td>
      <td>{instance.description}</td>
      <td className="data-table__col--numeric">{formatN2(instance.value)}</td>
      <td>{instance.status}</td>
      <td>
        <button type="button" onClick={() => onEdit(instance)}>
          Edit
        </button>
      </td>
    </tr>
  )
}

interface InstanceTableProps {
  title: string
  instances: RecurringBillInstanceDto[]
  onEdit: (instance: RecurringBillInstanceDto) => void
}

function InstanceTable({ title, instances, onEdit }: InstanceTableProps) {
  return (
    <section className="mensais-page__section">
      <h2>{title}</h2>
      <table className="mensais-page__table data-table">
        <thead>
          <tr>
            <th>Due Day</th>
            <th>Description</th>
            <th className="data-table__col--numeric">Value</th>
            <th>Status</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {instances.map((instance) => (
            <InstanceRow key={instance.id} instance={instance} onEdit={onEdit} />
          ))}
        </tbody>
      </table>
    </section>
  )
}

export default function MensaisPage() {
  const {
    monthInputValue,
    setMonthInputValue,
    brasilInstances,
    ukInstances,
    isLoading,
    error,
    retry,
    editingId,
    editStatus,
    editValue,
    isSaving,
    saveError,
    setEditField,
    showEditForm,
    cancelEdit,
    saveEdit,
  } = useMensais()

  const isEditing = editingId !== null

  return (
    <div className="mensais-page">
      <div className="mensais-page__month-picker">
        <label htmlFor="mensais-month">Month</label>
        <input
          id="mensais-month"
          type="month"
          value={monthInputValue}
          onChange={(e) => setMonthInputValue(e.target.value)}
        />
      </div>

      {isEditing && (
        <div className="mensais-page__form-panel">
          <p className="mensais-page__form-title">Edit Instance</p>
          <div className="mensais-page__form">
            <div className="mensais-page__form-field">
              <label htmlFor="mensais-edit-value">Value</label>
              <input
                id="mensais-edit-value"
                type="number"
                step="0.01"
                value={editValue}
                onChange={(e) => setEditField('editValue', e.target.value)}
              />
            </div>
            <div className="mensais-page__form-field">
              <label htmlFor="mensais-edit-status">Status</label>
              <select
                id="mensais-edit-status"
                value={editStatus}
                onChange={(e) => setEditField('editStatus', e.target.value)}
              >
                {STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="mensais-page__form-actions">
            <button type="button" disabled={isSaving} onClick={saveEdit}>
              {isSaving ? 'Saving...' : 'Save'}
            </button>
            <button type="button" onClick={cancelEdit}>
              Cancel
            </button>
          </div>
          {saveError && <p className="mensais-page__error">{saveError}</p>}
        </div>
      )}

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <div className="mensais-page__content">
          <InstanceTable title="Brasil" instances={brasilInstances} onEdit={showEditForm} />
          <InstanceTable title="UK" instances={ukInstances} onEdit={showEditForm} />
        </div>
      )}
    </div>
  )
}
