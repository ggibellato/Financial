import { useState } from 'react'
import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import RecurringBillFormDialog, { type RecurringBillFormValues } from '../components/RecurringBillFormDialog'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import SortableColumnHeader from '../components/grid/SortableColumnHeader'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useRecurringBills } from '../hooks/useRecurringBills'
import type { RecurringBillDto } from '../api/types'
import { formatN2 } from '../utils/formatters'
import './RecurringBillsPage.css'

export default function RecurringBillsPage() {
  const {
    recurringBills,
    isLoading,
    error,
    retry,
    createRecurringBill,
    updateRecurringBill,
    deletingId,
    deleteError,
    deleteRecurringBill,
  } = useRecurringBills()
  const [editingBill, setEditingBill] = useState<RecurringBillDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState<RecurringBillDto | null>(null)

  const accessors: Record<string, SortAccessor<RecurringBillDto>> = {
    dueDay: (bill) => bill.dueDay,
    description: (bill) => bill.description,
    value: (bill) => bill.value,
    area: (bill) => bill.area,
    status: (bill) => bill.status,
  }
  const { sortedRows: sortedBills, sortState, requestSort } = useSortableRows(recurringBills, accessors)

  const handleSubmit = (values: RecurringBillFormValues) =>
    editingBill
      ? updateRecurringBill(editingBill.id, {
          dueDay: values.dueDay,
          description: values.description,
          value: values.value,
          area: values.area,
          note: values.note,
          nitNumber: values.nitNumber,
          minimumWageValue: values.minimumWageValue,
          status: values.status,
        })
      : createRecurringBill({
          dueDay: values.dueDay,
          description: values.description,
          value: values.value,
          area: values.area,
          note: values.note,
        })

  const closeFormDialog = () => {
    setEditingBill(null)
    setIsCreating(false)
  }

  const handleFormSubmit = async (values: RecurringBillFormValues) => {
    const result = await handleSubmit(values)
    closeFormDialog()
    return result
  }

  const handleConfirmDelete = () => {
    if (!confirmingDelete) return
    deleteRecurringBill(confirmingDelete.id)
    setConfirmingDelete(null)
  }

  return (
    <section className="recurring-bills-page">
      <header className="recurring-bills-page__header">
        <h2>Recurring Bills</h2>
        <Button appearance="primary" icon={<AddRegular />} onClick={() => setIsCreating(true)}>
          Create Recurring Bill
        </Button>
      </header>

      {deleteError && (
        <MessageBar intent="error">
          <MessageBarBody>{deleteError}</MessageBarBody>
        </MessageBar>
      )}

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : recurringBills.length === 0 ? (
        <p className="recurring-bills-page__empty">No recurring bills yet — create one to get started.</p>
      ) : (
        <table className="data-table" aria-label="Recurring Bills">
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
                label="Value"
                columnKey="value"
                numeric
                sortDirection={sortState?.columnKey === 'value' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Area"
                columnKey="area"
                sortDirection={sortState?.columnKey === 'area' ? sortState.direction : undefined}
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
              <tr key={bill.id}>
                <td>
                  <Button
                    appearance="subtle"
                    size="small"
                    icon={<EditRegular />}
                    aria-label={`Edit ${bill.description}`}
                    onClick={() => setEditingBill(bill)}
                  />
                </td>
                <td>
                  <Button
                    appearance="subtle"
                    size="small"
                    icon={<DeleteRegular />}
                    aria-label={`Delete ${bill.description}`}
                    disabled={deletingId === bill.id}
                    onClick={() => setConfirmingDelete(bill)}
                  />
                </td>
                <td>{bill.dueDay}</td>
                <td>{bill.description}</td>
                <td className="data-table__col--numeric">{formatN2(bill.value)}</td>
                <td>{bill.area}</td>
                <td>{bill.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {(isCreating || editingBill) && (
        <RecurringBillFormDialog recurringBill={editingBill} onCancel={closeFormDialog} onSubmit={handleFormSubmit} />
      )}

      {confirmingDelete && (
        <Dialog open onOpenChange={(_, data) => { if (!data.open) setConfirmingDelete(null) }}>
          <DialogSurface aria-describedby={undefined}>
            <DialogBody>
              <DialogTitle>Delete Recurring Bill</DialogTitle>
              <DialogContent>
                <p>&ldquo;{confirmingDelete.description}&rdquo; will be permanently removed.</p>
              </DialogContent>
              <DialogActions>
                <Button appearance="primary" onClick={handleConfirmDelete}>
                  Delete
                </Button>
                <Button appearance="secondary" onClick={() => setConfirmingDelete(null)}>
                  Cancel
                </Button>
              </DialogActions>
            </DialogBody>
          </DialogSurface>
        </Dialog>
      )}
    </section>
  )
}
