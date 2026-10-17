import { useState } from 'react'
import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import AdminEntityPage from '../components/AdminEntityPage'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import CreditCardFormDialog from '../components/CreditCardFormDialog'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import { useCreditCards } from '../hooks/useCreditCards'
import type { CreditCardDto } from '../api/types'
import TruncatedText from '../components/TruncatedText'

export default function CreditCardsPage() {
  const { creditCards, isLoading, error, retry, createCreditCard, updateCreditCard, deletingId, deleteError, deleteCreditCard } =
    useCreditCards()
  const [editingCard, setEditingCard] = useState<CreditCardDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState<CreditCardDto | null>(null)

  const closeFormDialog = () => {
    setEditingCard(null)
    setIsCreating(false)
  }

  const handleFormSubmit = async (name: string, isActive: boolean, nextInvoiceDueDate: string | null) => {
    const result = editingCard
      ? await updateCreditCard(editingCard.id, { name, isActive, nextInvoiceDueDate })
      : await createCreditCard({ name, isActive })
    closeFormDialog()
    return result
  }

  const handleConfirmDelete = () => {
    if (!confirmingDelete) return
    deleteCreditCard(confirmingDelete.id)
    setConfirmingDelete(null)
  }

  return (
    <AdminEntityPage
      title="Credit Cards"
      createLabel="Create Credit Card"
      onCreate={() => setIsCreating(true)}
      actionError={deleteError}
      isLoading={isLoading}
      loadError={error}
      onRetry={retry}
      isEmpty={creditCards.length === 0}
      emptyMessage="No credit cards yet — create one to get started."
      dialogs={
        <>
          {(isCreating || editingCard) && (
            <CreditCardFormDialog creditCard={editingCard} onCancel={closeFormDialog} onSubmit={handleFormSubmit} />
          )}
          {confirmingDelete && (
            <ConfirmDeleteDialog
              entityLabel="Credit Card"
              message={
                confirmingDelete.hasReferences ? (
                  <>
                    &ldquo;{confirmingDelete.name}&rdquo; is still referenced by a statement or expense and cannot be
                    deleted.
                  </>
                ) : (
                  <>&ldquo;{confirmingDelete.name}&rdquo; will be permanently removed.</>
                )
              }
              confirmDisabled={confirmingDelete.hasReferences}
              onConfirm={handleConfirmDelete}
              onCancel={() => setConfirmingDelete(null)}
            />
          )}
        </>
      }
    >
      <Table aria-label="Credit Cards" className="data-table">
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Name</TableHeaderCell>
            <TableHeaderCell>Active</TableHeaderCell>
            <TableHeaderCell>Next Invoice Due Date</TableHeaderCell>
            <TableHeaderCell className="data-table__col--action" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {creditCards.map((creditCard) => (
            <TableRow key={creditCard.id}>
              <DataTableCell label="Name"><TruncatedText text={creditCard.name} /></DataTableCell>
              <DataTableCell label="Active">{creditCard.isActive ? 'Active' : 'Inactive'}</DataTableCell>
              <DataTableCell label="Next Invoice Due Date">{creditCard.nextInvoiceDueDate ?? '—'}</DataTableCell>
              <RowActionsCell
                itemName={creditCard.name}
                deleteDisabled={deletingId === creditCard.id}
                onEdit={() => setEditingCard(creditCard)}
                onDelete={() => setConfirmingDelete(creditCard)}
              />
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
