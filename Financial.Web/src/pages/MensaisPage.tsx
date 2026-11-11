import { useEffect, useId, useRef, useState } from 'react'
import {
  Button,
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
import DiscardChangesDialog from '../components/DiscardChangesDialog'
import { AddBillForm, EditBillForm } from '../components/MensaisBillForms'
import EmptyState from '../components/EmptyState'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import StatusMenuButton from '../components/StatusMenuButton'
import TruncatedText from '../components/TruncatedText'
import UkExpensePromptDialog from '../components/UkExpensePromptDialog'
import DataTableCell from '../components/grid/DataTableCell'
import SortableColumnHeader from '../components/grid/SortableColumnHeader'
import { usePendingDelete } from '../hooks/usePendingDelete'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useDiscardPrompt } from '../hooks/useDiscardPrompt'
import { BILL_STATUSES, useMensais } from '../hooks/useMensais'
import { useRowFocusRecovery } from '../hooks/useRowFocusRecovery'
import { formatN2 } from '../utils/formatters'
import { isFocusLost } from '../utils/focus'
import { orMissing } from '../utils/orMissing'
import MissingValue from '../components/MissingValue'
import ActionsColumnHeader from '../components/grid/ActionsColumnHeader'
import RowActionsCell from '../components/grid/RowActionsCell'
import { rowDeleteState, type RowDeleteState } from '../components/grid/rowDeleteState'
import './MensaisPage.css'

const NEW_BILL_FORM = 'new'
const FIRST_FIELD = 'input, select, textarea'

interface BillRowProps {
  bill: RecurringBillDto
  showBrasilFields: boolean
  deleteState: RowDeleteState
  isUpdatingStatus: boolean
  onEdit: (bill: RecurringBillDto) => void
  onDelete: (bill: RecurringBillDto) => void
  onStatusChange: (id: string, status: string) => void
}

function BillRow({
  bill,
  showBrasilFields,
  deleteState,
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
      {showBrasilFields && <DataTableCell label="NIT">{bill.nitNumber ?? <MissingValue />}</DataTableCell>}
      {showBrasilFields && (
        <DataTableCell label="Min. Wage" className="data-table__col--numeric">
          {orMissing(bill.minimumWageValue, formatN2)}
        </DataTableCell>
      )}
      <DataTableCell label="Value" className="data-table__col--numeric">
        {formatN2(bill.value)}
      </DataTableCell>
      <DataTableCell label="Status">
        <StatusMenuButton
          statuses={BILL_STATUSES}
          status={bill.status}
          isUpdating={isUpdatingStatus}
          onChange={(status) => onStatusChange(bill.id, status)}
        />
      </DataTableCell>
      <RowActionsCell
        itemName={`bill ${bill.description}`}
        {...deleteState}
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
          <Table className="mensais-page__table data-table" aria-labelledby={headingId}>
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
                  deleteState={rowDeleteState(deletingBillId, bill.id)}
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
    isFormDirty,
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
  const discardPrompt = useDiscardPrompt(isFormDirty)
  const formAreaRef = useRef<HTMLDivElement | null>(null)
  const openerRef = useRef<HTMLElement | null>(null)
  const deletion = usePendingDelete((row: RecurringBillDto) => deleteBill(row.id))
  const [isConfirmingReset, setIsConfirmingReset] = useState(false)
  const billsRegionRef = useRef<HTMLDivElement | null>(null)
  const addBillRef = useRef<HTMLButtonElement | null>(null)
  const recordRowFocus = useRowFocusRecovery(billsRegionRef, addBillRef, isLoading)
  const openFormKey = isAddFormOpen ? NEW_BILL_FORM : editingId
  const previousFormKey = useRef<string | null>(null)

  const restoreFocus = () => {
    const opener = openerRef.current?.isConnected ? openerRef.current : addBillRef.current
    opener?.focus()
  }

  useEffect(() => {
    const wasOpen = previousFormKey.current !== null
    previousFormKey.current = openFormKey
    if (openFormKey) formAreaRef.current?.querySelector<HTMLElement>(FIRST_FIELD)?.focus()
    else if (wasOpen && isFocusLost()) restoreFocus()
  }, [openFormKey])

  const openForm = (open: () => void) => {
    const focused = document.activeElement
    const opener = focused instanceof HTMLElement && focused !== document.body ? focused : null
    discardPrompt.confirmDiscard(() => {
      openerRef.current = opener
      open()
    })
  }

  const closeForm = (close: () => void) => {
    discardPrompt.confirmDiscard(() => {
      close()
      restoreFocus()
    })
  }

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
          <Button ref={addBillRef} appearance="primary" icon={<AddRegular />} onClick={() => openForm(showAddForm)}>
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

      <div ref={formAreaRef}>
        {isAddFormOpen && (
          <AddBillForm
            values={{ newArea, newDescription, newDueDay, newValue, newNote }}
            isSaving={isAdding}
            error={addError}
            errorFields={addErrorFields}
            onFieldChange={setAddField}
            onSave={submitAdd}
            onCancel={() => closeForm(cancelAdd)}
          />
        )}

        {isEditing && (
          <EditBillForm
            values={{ editValue, editStatus }}
            isSaving={isSaving}
            error={saveError}
            errorFields={saveErrorFields}
            onFieldChange={setEditField}
            onSave={saveEdit}
            onCancel={() => closeForm(cancelEdit)}
          />
        )}
      </div>

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
            onEdit={(bill) => openForm(() => showEditForm(bill))}
            onDelete={deletion.request}
            onStatusChange={updateBillStatus}
          />
          <BillTable
            bills={ukBills}
            showBrasilFields={false}
            deletingBillId={deletingBillId}
            updatingStatusBillId={updatingStatusBillId}
            onEdit={(bill) => openForm(() => showEditForm(bill))}
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

      <DiscardChangesDialog
        open={discardPrompt.isPromptOpen}
        onKeepEditing={discardPrompt.keepEditing}
        onDiscard={discardPrompt.discard}
      />

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
