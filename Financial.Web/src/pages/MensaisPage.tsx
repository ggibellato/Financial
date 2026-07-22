import type { RecurringBillInstanceDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useMensais } from '../hooks/useMensais'
import { formatN2 } from '../utils/formatters'
import './MensaisPage.css'

const STATUSES = ['Unset', 'Scheduled', 'Paid']

interface InstanceRowProps {
  instance: RecurringBillInstanceDto
  isEditing: boolean
  editStatus: string
  editValue: string
  isSaving: boolean
  onEdit: (instance: RecurringBillInstanceDto) => void
  onFieldChange: (field: 'editStatus' | 'editValue', value: string) => void
  onSave: () => void
  onCancel: () => void
}

function InstanceRow({
  instance,
  isEditing,
  editStatus,
  editValue,
  isSaving,
  onEdit,
  onFieldChange,
  onSave,
  onCancel,
}: InstanceRowProps) {
  if (isEditing) {
    return (
      <tr>
        <td>{instance.dueDay}</td>
        <td>{instance.description}</td>
        <td className="data-table__col--numeric">
          <input
            type="number"
            step="0.01"
            value={editValue}
            onChange={(e) => onFieldChange('editValue', e.target.value)}
          />
        </td>
        <td>
          <select value={editStatus} onChange={(e) => onFieldChange('editStatus', e.target.value)}>
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
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
  editingId: string | null
  editStatus: string
  editValue: string
  isSaving: boolean
  onEdit: (instance: RecurringBillInstanceDto) => void
  onFieldChange: (field: 'editStatus' | 'editValue', value: string) => void
  onSave: () => void
  onCancel: () => void
}

function InstanceTable({
  title,
  instances,
  editingId,
  editStatus,
  editValue,
  isSaving,
  onEdit,
  onFieldChange,
  onSave,
  onCancel,
}: InstanceTableProps) {
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
            <InstanceRow
              key={instance.id}
              instance={instance}
              isEditing={editingId === instance.id}
              editStatus={editStatus}
              editValue={editValue}
              isSaving={isSaving}
              onEdit={onEdit}
              onFieldChange={onFieldChange}
              onSave={onSave}
              onCancel={onCancel}
            />
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

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <>
          <InstanceTable
            title="Brasil"
            instances={brasilInstances}
            editingId={editingId}
            editStatus={editStatus}
            editValue={editValue}
            isSaving={isSaving}
            onEdit={showEditForm}
            onFieldChange={setEditField}
            onSave={saveEdit}
            onCancel={cancelEdit}
          />
          <InstanceTable
            title="UK"
            instances={ukInstances}
            editingId={editingId}
            editStatus={editStatus}
            editValue={editValue}
            isSaving={isSaving}
            onEdit={showEditForm}
            onFieldChange={setEditField}
            onSave={saveEdit}
            onCancel={cancelEdit}
          />
          {saveError && <p className="mensais-page__error">{saveError}</p>}
        </>
      )}
    </div>
  )
}
