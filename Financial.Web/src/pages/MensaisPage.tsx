import type { RecurringBillDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useMensais } from '../hooks/useMensais'
import { formatN2 } from '../utils/formatters'
import './MensaisPage.css'

const STATUSES = ['Unset', 'Scheduled', 'Paid']
const AREAS = ['Brasil', 'UK']

interface BillRowProps {
  bill: RecurringBillDto
  showBrasilFields: boolean
  isDeleting: boolean
  onEdit: (bill: RecurringBillDto) => void
  onDelete: (id: string) => void
}

function BillRow({ bill, showBrasilFields, isDeleting, onEdit, onDelete }: BillRowProps) {
  return (
    <tr>
      <td>{bill.dueDay}</td>
      <td>{bill.description}</td>
      {showBrasilFields && <td>{bill.nitNumber ?? ''}</td>}
      {showBrasilFields && <td className="data-table__col--numeric">{bill.minimumWageValue !== null ? formatN2(bill.minimumWageValue) : ''}</td>}
      <td className="data-table__col--numeric">{formatN2(bill.value)}</td>
      <td>{bill.status}</td>
      <td>
        <button type="button" onClick={() => onEdit(bill)}>
          Edit
        </button>
        <button
          type="button"
          disabled={isDeleting}
          onClick={() => {
            if (window.confirm(`Delete "${bill.description}"? This removes it for good.`)) {
              onDelete(bill.id)
            }
          }}
        >
          {isDeleting ? 'Deleting...' : 'Delete'}
        </button>
      </td>
    </tr>
  )
}

interface BillTableProps {
  title: string
  bills: RecurringBillDto[]
  showBrasilFields: boolean
  deletingBillId: string | null
  onEdit: (bill: RecurringBillDto) => void
  onDelete: (id: string) => void
}

function BillTable({ title, bills, showBrasilFields, deletingBillId, onEdit, onDelete }: BillTableProps) {
  return (
    <section className="mensais-page__section">
      <h2>{title}</h2>
      <table className="mensais-page__table data-table">
        <thead>
          <tr>
            <th>Due Day</th>
            <th>Description</th>
            {showBrasilFields && <th>NIT</th>}
            {showBrasilFields && <th className="data-table__col--numeric">Min. Wage</th>}
            <th className="data-table__col--numeric">Value</th>
            <th>Status</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {bills.map((bill) => (
            <BillRow
              key={bill.id}
              bill={bill}
              showBrasilFields={showBrasilFields}
              isDeleting={deletingBillId === bill.id}
              onEdit={onEdit}
              onDelete={onDelete}
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
    brasilBills,
    ukBills,
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
    isAddFormOpen,
    newDueDay,
    newDescription,
    newValue,
    newArea,
    newNote,
    isAdding,
    addError,
    setAddField,
    showAddForm,
    cancelAdd,
    submitAdd,
    deletingBillId,
    deleteError,
    deleteBill,
    isResetting,
    resetError,
    resetAllToUnset,
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
        {!isAddFormOpen && (
          <button type="button" onClick={showAddForm}>
            Add Bill
          </button>
        )}
        <button
          type="button"
          disabled={isResetting}
          onClick={() => {
            if (window.confirm('Reset every bill back to Unset for the new month?')) {
              resetAllToUnset()
            }
          }}
        >
          {isResetting ? 'Resetting...' : 'Reset All to Unset'}
        </button>
      </div>

      {deleteError && <p className="mensais-page__error">{deleteError}</p>}
      {resetError && <p className="mensais-page__error">{resetError}</p>}

      {isAddFormOpen && (
        <div className="mensais-page__form-panel">
          <p className="mensais-page__form-title">Add Bill</p>
          <div className="mensais-page__form">
            <div className="mensais-page__form-field">
              <label htmlFor="mensais-new-description">Description</label>
              <input
                id="mensais-new-description"
                type="text"
                value={newDescription}
                onChange={(e) => setAddField('newDescription', e.target.value)}
              />
            </div>
            <div className="mensais-page__form-field">
              <label htmlFor="mensais-new-due-day">Due Day</label>
              <input
                id="mensais-new-due-day"
                type="number"
                min="1"
                max="31"
                value={newDueDay}
                onChange={(e) => setAddField('newDueDay', e.target.value)}
              />
            </div>
            <div className="mensais-page__form-field">
              <label htmlFor="mensais-new-value">Value</label>
              <input
                id="mensais-new-value"
                type="number"
                step="0.01"
                value={newValue}
                onChange={(e) => setAddField('newValue', e.target.value)}
              />
            </div>
            <div className="mensais-page__form-field">
              <label htmlFor="mensais-new-area">Area</label>
              <select id="mensais-new-area" value={newArea} onChange={(e) => setAddField('newArea', e.target.value)}>
                {AREAS.map((a) => (
                  <option key={a} value={a}>
                    {a}
                  </option>
                ))}
              </select>
            </div>
            <div className="mensais-page__form-field">
              <label htmlFor="mensais-new-note">Note</label>
              <input
                id="mensais-new-note"
                type="text"
                value={newNote}
                onChange={(e) => setAddField('newNote', e.target.value)}
              />
            </div>
          </div>
          <div className="mensais-page__form-actions">
            <button type="button" disabled={isAdding} onClick={submitAdd}>
              {isAdding ? 'Adding...' : 'Add'}
            </button>
            <button type="button" onClick={cancelAdd}>
              Cancel
            </button>
          </div>
          {addError && <p className="mensais-page__error">{addError}</p>}
        </div>
      )}

      {isEditing && (
        <div className="mensais-page__form-panel">
          <p className="mensais-page__form-title">Edit Bill</p>
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
          <BillTable
            title="Brasil"
            bills={brasilBills}
            showBrasilFields
            deletingBillId={deletingBillId}
            onEdit={showEditForm}
            onDelete={deleteBill}
          />
          <BillTable
            title="UK"
            bills={ukBills}
            showBrasilFields={false}
            deletingBillId={deletingBillId}
            onEdit={showEditForm}
            onDelete={deleteBill}
          />
        </div>
      )}
    </div>
  )
}
