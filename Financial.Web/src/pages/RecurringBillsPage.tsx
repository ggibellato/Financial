import { useState } from 'react'
import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import AdminEntityPage from '../components/AdminEntityPage'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import RecurringBillFormDialog, { type RecurringBillFormValues } from '../components/RecurringBillFormDialog'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import SortableColumnHeader from '../components/grid/SortableColumnHeader'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useRecurringBills } from '../hooks/useRecurringBills'
import type { RecurringBillDto } from '../api/types'
import { formatN2 } from '../utils/formatters'

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
    <AdminEntityPage
      title="Recurring Bills"
      createLabel="Create Recurring Bill"
      onCreate={() => setIsCreating(true)}
      actionError={deleteError}
      isLoading={isLoading}
      loadError={error}
      onRetry={retry}
      isEmpty={recurringBills.length === 0}
      emptyMessage="No recurring bills yet — create one to get started."
      dialogs={
        <>
          {(isCreating || editingBill) && (
            <RecurringBillFormDialog recurringBill={editingBill} onCancel={closeFormDialog} onSubmit={handleFormSubmit} />
          )}
          {confirmingDelete && (
            <ConfirmDeleteDialog
              entityLabel="Recurring Bill"
              message={<>&ldquo;{confirmingDelete.description}&rdquo; will be permanently removed.</>}
              onConfirm={handleConfirmDelete}
              onCancel={() => setConfirmingDelete(null)}
            />
          )}
        </>
      }
    >
      <Table className="data-table" aria-label="Recurring Bills">
        <TableHeader>
          <TableRow>
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
            <TableHeaderCell className="data-table__col--action" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {sortedBills.map((bill) => (
            <TableRow key={bill.id}>
              <DataTableCell label="Due Day">{bill.dueDay}</DataTableCell>
              <DataTableCell label="Description">{bill.description}</DataTableCell>
              <DataTableCell label="Value" className="data-table__col--numeric">
                {formatN2(bill.value)}
              </DataTableCell>
              <DataTableCell label="Area">{bill.area}</DataTableCell>
              <DataTableCell label="Status">{bill.status}</DataTableCell>
              <RowActionsCell
                itemName={bill.description}
                deleteDisabled={deletingId === bill.id}
                onEdit={() => setEditingBill(bill)}
                onDelete={() => setConfirmingDelete(bill)}
              />
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
