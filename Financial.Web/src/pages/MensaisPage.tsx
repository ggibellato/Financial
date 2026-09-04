import { Button, Field, Input, MessageBar, MessageBarBody, Select, Text } from '@fluentui/react-components'
import { AddRegular, ArrowResetRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import type { RecurringBillDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import StatusMenuButton from '../components/StatusMenuButton'
import UkExpensePromptDialog from '../components/UkExpensePromptDialog'
import SortableColumnHeader from '../components/grid/SortableColumnHeader'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useFieldError } from '../hooks/useFieldError'
import { useMensais } from '../hooks/useMensais'
import { confirmThenRun } from '../utils/confirmThenRun'
import { formatN2 } from '../utils/formatters'
import './MensaisPage.css'

const STATUSES = ['Unset', 'Scheduled', 'Paid']
const AREAS = ['Brasil', 'UK']

interface BillRowProps {
  bill: RecurringBillDto
  showBrasilFields: boolean
  isDeleting: boolean
  isUpdatingStatus: boolean
  onEdit: (bill: RecurringBillDto) => void
  onDelete: (id: string) => void
  onStatusChange: (id: string, status: string) => void
}

function BillRow({
  bill,
  showBrasilFields,
  isDeleting,
  isUpdatingStatus,
  onEdit,
  onDelete,
  onStatusChange,
}: BillRowProps) {
  return (
    <tr>
      <td>
        <Button
          appearance="subtle"
          size="small"
          icon={<EditRegular />}
          aria-label="Edit bill"
          onClick={() => onEdit(bill)}
        />
      </td>
      <td>
        <Button
          appearance="subtle"
          size="small"
          icon={<DeleteRegular />}
          aria-label={isDeleting ? 'Deleting bill' : 'Delete bill'}
          disabled={isDeleting}
          onClick={() =>
            confirmThenRun(`Delete "${bill.description}"? This removes it for good.`, () => onDelete(bill.id))
          }
        />
      </td>
      <td>{bill.dueDay}</td>
      <td>{bill.description}</td>
      <td>{bill.note}</td>
      {showBrasilFields && <td>{bill.nitNumber ?? ''}</td>}
      {showBrasilFields && <td className="data-table__col--numeric">{bill.minimumWageValue !== null ? formatN2(bill.minimumWageValue) : ''}</td>}
      <td className="data-table__col--numeric">{formatN2(bill.value)}</td>
      <td>
        <StatusMenuButton
          statuses={STATUSES}
          status={bill.status}
          isUpdating={isUpdatingStatus}
          onChange={(status) => onStatusChange(bill.id, status)}
        />
      </td>
    </tr>
  )
}

interface BillTableProps {
  bills: RecurringBillDto[]
  showBrasilFields: boolean
  deletingBillId: string | null
  updatingStatusBillId: string | null
  onEdit: (bill: RecurringBillDto) => void
  onDelete: (id: string) => void
  onStatusChange: (id: string, status: string) => void
}

function BillTable({
  bills,
  showBrasilFields,
  deletingBillId,
  updatingStatusBillId,
  onEdit,
  onDelete,
  onStatusChange,
}: BillTableProps) {
  const accessors: Record<string, SortAccessor<RecurringBillDto>> = {
    dueDay: (bill) => bill.dueDay,
    description: (bill) => bill.description,
    note: (bill) => bill.note,
    nit: (bill) => bill.nitNumber,
    minWage: (bill) => bill.minimumWageValue,
    value: (bill) => bill.value,
    status: (bill) => bill.status,
  }
  const { sortedRows: sortedBills, sortState, requestSort } = useSortableRows(bills, accessors)

  return (
    <section className="mensais-page__section">
      <div className="mensais-page__table-scroll">
        <table className="mensais-page__table data-table">
          <thead>
            <tr>
              <th />
              <th />
              <SortableColumnHeader
                label="Due Day"
                columnKey="dueDay"
                sortDirection={sortState?.columnKey === 'dueDay' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Description"
                columnKey="description"
                sortDirection={sortState?.columnKey === 'description' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Note"
                columnKey="note"
                sortDirection={sortState?.columnKey === 'note' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              {showBrasilFields && (
                <SortableColumnHeader
                  label="NIT"
                  columnKey="nit"
                  sortDirection={sortState?.columnKey === 'nit' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
              )}
              {showBrasilFields && (
                <SortableColumnHeader
                  label="Min. Wage"
                  columnKey="minWage"
                  numeric
                  sortDirection={sortState?.columnKey === 'minWage' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
              )}
              <SortableColumnHeader
                label="Value"
                columnKey="value"
                numeric
                sortDirection={sortState?.columnKey === 'value' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Status"
                columnKey="status"
                sortDirection={sortState?.columnKey === 'status' ? sortState.direction : undefined}
                onSort={requestSort}
              />
            </tr>
          </thead>
          <tbody>
            {sortedBills.map((bill) => (
              <BillRow
                key={bill.id}
                bill={bill}
                showBrasilFields={showBrasilFields}
                isDeleting={deletingBillId === bill.id}
                isUpdatingStatus={updatingStatusBillId === bill.id}
                onEdit={onEdit}
                onDelete={onDelete}
                onStatusChange={onStatusChange}
              />
            ))}
          </tbody>
        </table>
      </div>
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
    saveErrorField,
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
    addErrorField,
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
    updatingStatusBillId,
    statusUpdateError,
    updateBillStatus,
    banks,
    categories,
    expensePromptBill,
    isCreatingExpense,
    expenseCreateError,
    expenseCreatedForRetry,
    confirmExpensePrompt,
    skipOrRetryExpensePrompt,
    closeExpensePrompt,
  } = useMensais()

  const isEditing = editingId !== null
  const styles = useFormPanelStyles()
  const addFieldError = useFieldError(addError, addErrorField)
  const editFieldError = useFieldError(saveError, saveErrorField)

  return (
    <div className="mensais-page">
      <div className="mensais-page__header">
        <div className="mensais-page__month-picker">
          <label htmlFor="mensais-month">Month</label>
          <input
            id="mensais-month"
            type="month"
            value={monthInputValue}
            onChange={(e) => setMonthInputValue(e.target.value)}
          />
        </div>
        <div className="mensais-page__toolbar">
          <Button appearance="primary" icon={<AddRegular />} onClick={showAddForm}>
            Add Bill
          </Button>
          <Button
            appearance="secondary"
            icon={<ArrowResetRegular />}
            disabled={isResetting}
            onClick={() => confirmThenRun('Reset every bill back to Unset for the new month?', resetAllToUnset)}
          >
            {isResetting ? 'Resetting...' : 'Reset All to Unset'}
          </Button>
        </div>
      </div>

      {deleteError && <p className="mensais-page__error">{deleteError}</p>}
      {resetError && <p className="mensais-page__error">{resetError}</p>}
      {statusUpdateError && <p className="mensais-page__error">{statusUpdateError}</p>}

      {isAddFormOpen && (
        <div className={styles.panel}>
          <Text as="h2" weight="semibold" size={400}>
            Add Bill
          </Text>

          <div className={styles.grid}>
            <Field label="Area">
              <Select value={newArea} onChange={(e) => setAddField('newArea', e.target.value)}>
                {AREAS.map((a) => (
                  <option key={a} value={a}>
                    {a}
                  </option>
                ))}
              </Select>
            </Field>

            <div className={styles.spanTwo}>
              <Field
                label="Description"
                required
                validationState={addFieldError('newDescription') ? 'error' : 'none'}
                validationMessage={addFieldError('newDescription')}
              >
                <Input value={newDescription} onChange={(e) => setAddField('newDescription', e.target.value)} />
              </Field>
            </div>

            <Field
              label="Due Day"
              required
              validationState={addFieldError('newDueDay') ? 'error' : 'none'}
              validationMessage={addFieldError('newDueDay')}
            >
              <Input
                type="number"
                min="1"
                max="31"
                value={newDueDay}
                onChange={(e) => setAddField('newDueDay', e.target.value)}
              />
            </Field>

            <Field
              label="Value"
              required
              validationState={addFieldError('newValue') ? 'error' : 'none'}
              validationMessage={addFieldError('newValue')}
            >
              <Input type="number" step="0.01" value={newValue} onChange={(e) => setAddField('newValue', e.target.value)} />
            </Field>

            <div className={styles.spanTwo}>
              <Field label="Note">
                <Input value={newNote} onChange={(e) => setAddField('newNote', e.target.value)} />
              </Field>
            </div>
          </div>

          <div className={styles.actions}>
            <Button appearance="primary" disabled={isAdding} onClick={submitAdd}>
              {isAdding ? 'Adding Bill...' : 'Add Bill'}
            </Button>
            <Button appearance="secondary" onClick={cancelAdd}>
              Cancel
            </Button>
          </div>

          {addErrorField === null && addError && (
            <MessageBar intent="error">
              <MessageBarBody>{addError}</MessageBarBody>
            </MessageBar>
          )}
        </div>
      )}

      {isEditing && (
        <div className={styles.panel}>
          <Text as="h2" weight="semibold" size={400}>
            Edit Bill
          </Text>

          <div className={styles.grid}>
            <Field
              label="Value"
              required
              validationState={editFieldError('editValue') ? 'error' : 'none'}
              validationMessage={editFieldError('editValue')}
            >
              <Input
                type="number"
                step="0.01"
                value={editValue}
                onChange={(e) => setEditField('editValue', e.target.value)}
              />
            </Field>

            <Field
              label="Status"
              required
              validationState={editFieldError('editStatus') ? 'error' : 'none'}
              validationMessage={editFieldError('editStatus')}
            >
              <Select value={editStatus} onChange={(e) => setEditField('editStatus', e.target.value)}>
                {STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </Select>
            </Field>
          </div>

          <div className={styles.actions}>
            <Button appearance="primary" disabled={isSaving} onClick={saveEdit}>
              {isSaving ? 'Saving...' : 'Save'}
            </Button>
            <Button appearance="secondary" onClick={cancelEdit}>
              Cancel
            </Button>
          </div>

          {saveErrorField === null && saveError && (
            <MessageBar intent="error">
              <MessageBarBody>{saveError}</MessageBarBody>
            </MessageBar>
          )}
        </div>
      )}

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <div className="mensais-page__content">
          <BillTable
            bills={brasilBills}
            showBrasilFields
            deletingBillId={deletingBillId}
            updatingStatusBillId={updatingStatusBillId}
            onEdit={showEditForm}
            onDelete={deleteBill}
            onStatusChange={updateBillStatus}
          />
          <BillTable
            bills={ukBills}
            showBrasilFields={false}
            deletingBillId={deletingBillId}
            updatingStatusBillId={updatingStatusBillId}
            onEdit={showEditForm}
            onDelete={deleteBill}
            onStatusChange={updateBillStatus}
          />
        </div>
      )}

      {expensePromptBill && (
        <UkExpensePromptDialog
          bill={expensePromptBill}
          banks={banks}
          categories={categories}
          isCreatingExpense={isCreatingExpense}
          isUpdatingStatus={updatingStatusBillId === expensePromptBill.id}
          expenseCreateError={expenseCreateError}
          statusUpdateError={statusUpdateError}
          isRetryOnly={expenseCreatedForRetry}
          onConfirm={confirmExpensePrompt}
          onSkip={skipOrRetryExpensePrompt}
          onRetry={skipOrRetryExpensePrompt}
          onCancel={closeExpensePrompt}
        />
      )}
    </div>
  )
}
