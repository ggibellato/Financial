import { useId, useRef, useState } from 'react'
import {
  Button,
  Field,
  Input,
  Select,
  Table,
  TableBody,
  TableHeader,
  TableRow,
  Text,
} from '@fluentui/react-components'
import { AddRegular, ArrowResetRegular } from '@fluentui/react-icons'
import type { RecurringBillDto } from '../api/types'
import ActionErrorAlert from '../components/ActionErrorAlert'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import ConfirmDialog from '../components/ConfirmDialog'
import EmptyState from '../components/EmptyState'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import StatusMenuButton from '../components/StatusMenuButton'
import TruncatedText from '../components/TruncatedText'
import UkExpensePromptDialog from '../components/UkExpensePromptDialog'
import DataTableCell from '../components/grid/DataTableCell'
import SortableColumnHeader from '../components/grid/SortableColumnHeader'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { usePendingDelete } from '../hooks/usePendingDelete'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useFieldError } from '../hooks/useFieldError'
import { useMensais } from '../hooks/useMensais'
import { useRowFocusRecovery } from '../hooks/useRowFocusRecovery'
import { formatN2, SAVING_LABEL } from '../utils/formatters'
import ActionsColumnHeader from '../components/grid/ActionsColumnHeader'
import RowActionsCell from '../components/grid/RowActionsCell'
import { generalErrorOf } from '../utils/fieldErrors'
import './MensaisPage.css'

const STATUSES = ['Unset', 'Scheduled', 'Paid']
const AREAS = ['Brasil', 'UK']

interface BillRowProps {
  bill: RecurringBillDto
  showBrasilFields: boolean
  isDeleting: boolean
  deleteDisabled: boolean
  isUpdatingStatus: boolean
  onEdit: (bill: RecurringBillDto) => void
  onDelete: (bill: RecurringBillDto) => void
  onStatusChange: (id: string, status: string) => void
}

function BillRow({
  bill,
  showBrasilFields,
  isDeleting,
  deleteDisabled,
  isUpdatingStatus,
  onEdit,
  onDelete,
  onStatusChange,
}: BillRowProps) {
  return (
    <TableRow>
      <DataTableCell label="Due day">{bill.dueDay}</DataTableCell>
      <DataTableCell label="Description">
        <TruncatedText text={bill.description} />
      </DataTableCell>
      <DataTableCell label="Note">
        <TruncatedText text={bill.note} />
      </DataTableCell>
      {showBrasilFields && <DataTableCell label="NIT">{bill.nitNumber ?? ''}</DataTableCell>}
      {showBrasilFields && (
        <DataTableCell label="Min. Wage" className="data-table__col--numeric">
          {bill.minimumWageValue !== null ? formatN2(bill.minimumWageValue) : ''}
        </DataTableCell>
      )}
      <DataTableCell label="Value" className="data-table__col--numeric">
        {formatN2(bill.value)}
      </DataTableCell>
      <DataTableCell label="Status">
        <StatusMenuButton
          statuses={STATUSES}
          status={bill.status}
          isUpdating={isUpdatingStatus}
          onChange={(status) => onStatusChange(bill.id, status)}
        />
      </DataTableCell>
      <RowActionsCell
        itemName={`bill ${bill.description}`}
        isDeleting={isDeleting}
        deleteDisabled={deleteDisabled}
        onEdit={() => onEdit(bill)}
        onDelete={() => onDelete(bill)}
      />
    </TableRow>
  )
}

interface BillTableProps {
  bills: RecurringBillDto[]
  showBrasilFields: boolean
  deletingBillId: string | null
  updatingStatusBillId: string | null
  onEdit: (bill: RecurringBillDto) => void
  onDelete: (bill: RecurringBillDto) => void
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
  const headingId = useId()
  const area = showBrasilFields ? 'Brasil' : 'UK'

  return (
    <section className="mensais-page__section" aria-labelledby={headingId}>
      <Text as="h2" id={headingId} weight="semibold" size={400}>
        {area} bills
      </Text>
      {bills.length === 0 ? (
        <EmptyState>No {area} bills yet. Add one with New bill.</EmptyState>
      ) : (
        <div className="mensais-page__table-scroll">
          <Table className="mensais-page__table data-table">
            <TableHeader>
              <TableRow>
                <SortableColumnHeader
                  label="Due day"
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
                <ActionsColumnHeader />
              </TableRow>
            </TableHeader>
            <TableBody>
              {sortedBills.map((bill) => (
                <BillRow
                  key={bill.id}
                  bill={bill}
                  showBrasilFields={showBrasilFields}
                  isDeleting={deletingBillId === bill.id}
                  deleteDisabled={deletingBillId !== null && deletingBillId !== bill.id}
                  isUpdatingStatus={updatingStatusBillId === bill.id}
                  onEdit={onEdit}
                  onDelete={onDelete}
                  onStatusChange={onStatusChange}
                />
              ))}
            </TableBody>
          </Table>
        </div>
      )}
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
    saveErrorFields,
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
    addErrorFields,
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
    confirmExpensePrompt,
    skipExpensePrompt,
    closeExpensePrompt,
  } = useMensais()

  const isEditing = editingId !== null
  const styles = useFormPanelStyles()
  const addFieldError = useFieldError(addErrorFields)
  const editFieldError = useFieldError(saveErrorFields)
  const deletion = usePendingDelete((row: RecurringBillDto) => deleteBill(row.id))
  const [isConfirmingReset, setIsConfirmingReset] = useState(false)
  const billsRegionRef = useRef<HTMLDivElement | null>(null)
  const addBillRef = useRef<HTMLButtonElement | null>(null)
  const recordRowFocus = useRowFocusRecovery(billsRegionRef, addBillRef, isLoading)

  const confirmReset = () => {
    resetAllToUnset()
    setIsConfirmingReset(false)
  }

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
          <Button ref={addBillRef} appearance="primary" icon={<AddRegular />} onClick={showAddForm}>
            New bill
          </Button>
          <Button
            appearance="primary"
            icon={<ArrowResetRegular />}
            disabledFocusable={isResetting}
            onClick={() => setIsConfirmingReset(true)}
          >
            {isResetting ? 'Resetting…' : 'Reset all to Unset'}
          </Button>
        </div>
      </div>

      <ActionErrorAlert message={deleteError} />
      <ActionErrorAlert message={resetError} />
      <ActionErrorAlert message={expensePromptBill ? null : statusUpdateError} />

      {isAddFormOpen && (
        <div className={styles.panel}>
          <Text as="h2" weight="semibold" size={400}>
            New bill
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
              label="Due day"
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
              {isAdding ? 'Adding bill…' : 'Add bill'}
            </Button>
            <Button appearance="secondary" onClick={cancelAdd}>
              Cancel
            </Button>
          </div>

          <ActionErrorAlert message={generalErrorOf(addError, addErrorFields)} />
        </div>
      )}

      {isEditing && (
        <div className={styles.panel}>
          <Text as="h2" weight="semibold" size={400}>
            Edit bill
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
              {isSaving ? SAVING_LABEL : 'Save'}
            </Button>
            <Button appearance="secondary" onClick={cancelEdit}>
              Cancel
            </Button>
          </div>

          <ActionErrorAlert message={generalErrorOf(saveError, saveErrorFields)} />
        </div>
      )}

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <div ref={billsRegionRef} className="mensais-page__content" onFocus={recordRowFocus}>
          <BillTable
            bills={brasilBills}
            showBrasilFields
            deletingBillId={deletingBillId}
            updatingStatusBillId={updatingStatusBillId}
            onEdit={showEditForm}
            onDelete={deletion.request}
            onStatusChange={updateBillStatus}
          />
          <BillTable
            bills={ukBills}
            showBrasilFields={false}
            deletingBillId={deletingBillId}
            updatingStatusBillId={updatingStatusBillId}
            onEdit={showEditForm}
            onDelete={deletion.request}
            onStatusChange={updateBillStatus}
          />
        </div>
      )}

      {deletion.pendingRow && (
        <ConfirmDeleteDialog
          entityLabel="bill"
          message={`Delete the bill "${deletion.pendingRow.description}"? This cannot be undone.`}
          onConfirm={deletion.confirm}
          onCancel={deletion.cancel}
        />
      )}

      <ConfirmDialog
        open={isConfirmingReset}
        title="Reset all bills to Unset"
        message="Set the status of every bill back to Unset for the new month?"
        confirmLabel="Reset all to Unset"
        cancelLabel="Cancel"
        onConfirm={confirmReset}
        onCancel={() => setIsConfirmingReset(false)}
      />

      {expensePromptBill && (
        <UkExpensePromptDialog
          bill={expensePromptBill}
          banks={banks}
          categories={categories}
          isSaving={updatingStatusBillId === expensePromptBill.id}
          error={statusUpdateError}
          onConfirm={confirmExpensePrompt}
          onSkip={skipExpensePrompt}
          onCancel={closeExpensePrompt}
        />
      )}
    </div>
  )
}
