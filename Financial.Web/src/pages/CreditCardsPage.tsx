import { useState } from 'react'
import {
  Button,
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  MessageBar,
  MessageBarBody,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
} from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import CreditCardFormDialog from '../components/CreditCardFormDialog'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useCreditCards } from '../hooks/useCreditCards'
import type { CreditCardDto } from '../api/types'
import './CreditCardsPage.css'

export default function CreditCardsPage() {
  const styles = useFormPanelStyles()
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
    <section className="credit-cards-page">
      <header className="credit-cards-page__header">
        <h2>Credit Cards</h2>
        <Button appearance="primary" icon={<AddRegular />} onClick={() => setIsCreating(true)}>
          Create Credit Card
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
      ) : creditCards.length === 0 ? (
        <p className="credit-cards-page__empty">No credit cards yet — create one to get started.</p>
      ) : (
        <Table aria-label="Credit Cards">
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
                <TableCell>{creditCard.name}</TableCell>
                <TableCell>{creditCard.isActive ? 'Active' : 'Inactive'}</TableCell>
                <TableCell>{creditCard.nextInvoiceDueDate ?? '—'}</TableCell>
                <TableCell className="data-table__col--action">
                  <div className="data-table__actions-cell">
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<EditRegular />}
                      aria-label={`Edit ${creditCard.name}`}
                      onClick={() => setEditingCard(creditCard)}
                    />
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<DeleteRegular />}
                      aria-label={`Delete ${creditCard.name}`}
                      disabled={deletingId === creditCard.id}
                      onClick={() => setConfirmingDelete(creditCard)}
                    />
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {(isCreating || editingCard) && (
        <CreditCardFormDialog creditCard={editingCard} onCancel={closeFormDialog} onSubmit={handleFormSubmit} />
      )}

      {confirmingDelete && (
        <Dialog open onOpenChange={(_, data) => { if (!data.open) setConfirmingDelete(null) }}>
          <DialogSurface aria-describedby={undefined}>
            <DialogBody>
              <DialogTitle>Delete Credit Card</DialogTitle>
              <DialogContent>
                {confirmingDelete.hasReferences ? (
                  <p>
                    &ldquo;{confirmingDelete.name}&rdquo; is still referenced by a statement or expense and cannot be
                    deleted.
                  </p>
                ) : (
                  <p>&ldquo;{confirmingDelete.name}&rdquo; will be permanently removed.</p>
                )}
              </DialogContent>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleConfirmDelete}
                  disabled={confirmingDelete.hasReferences}
                >
                  Delete
                </Button>
                <Button appearance="secondary" onClick={() => setConfirmingDelete(null)}>
                  Cancel
                </Button>
              </div>
            </DialogBody>
          </DialogSurface>
        </Dialog>
      )}
    </section>
  )
}
