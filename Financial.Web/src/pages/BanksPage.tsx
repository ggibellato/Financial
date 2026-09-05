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
import BankFormDialog from '../components/BankFormDialog'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useBanks } from '../hooks/useBanks'
import type { BankDto } from '../api/types'
import './BanksPage.css'

export default function BanksPage() {
  const styles = useFormPanelStyles()
  const { banks, isLoading, error, retry, createBank, updateBank, deletingId, deleteError, deleteBank } = useBanks()
  const [editingBank, setEditingBank] = useState<BankDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState<BankDto | null>(null)

  const handleSubmit = (name: string, roundUpEnabled: boolean) =>
    editingBank
      ? updateBank(editingBank.id, { name, roundUpEnabled })
      : createBank({ name, roundUpEnabled })

  const closeFormDialog = () => {
    setEditingBank(null)
    setIsCreating(false)
  }

  const handleFormSubmit = async (name: string, roundUpEnabled: boolean) => {
    const result = await handleSubmit(name, roundUpEnabled)
    closeFormDialog()
    return result
  }

  const handleConfirmDelete = () => {
    if (!confirmingDelete) return
    deleteBank(confirmingDelete.id)
    setConfirmingDelete(null)
  }

  return (
    <section className="banks-page">
      <header className="banks-page__header">
        <h2>Banks</h2>
        <Button appearance="primary" icon={<AddRegular />} onClick={() => setIsCreating(true)}>
          Create Bank
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
      ) : banks.length === 0 ? (
        <p className="banks-page__empty">No banks yet — create one to get started.</p>
      ) : (
        <Table aria-label="Banks">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Name</TableHeaderCell>
              <TableHeaderCell>Round-up</TableHeaderCell>
              <TableHeaderCell>Opening Balance</TableHeaderCell>
              <TableHeaderCell className="data-table__col--action" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {banks.map((bank) => (
              <TableRow key={bank.id}>
                <TableCell>{bank.name}</TableCell>
                <TableCell>{bank.roundUpEnabled ? 'Enabled' : 'Disabled'}</TableCell>
                <TableCell>{bank.openingBalance}</TableCell>
                <TableCell className="data-table__col--action">
                  <div className="data-table__actions-cell">
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<EditRegular />}
                      aria-label={`Edit ${bank.name}`}
                      onClick={() => setEditingBank(bank)}
                    />
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<DeleteRegular />}
                      aria-label={`Delete ${bank.name}`}
                      disabled={deletingId === bank.id}
                      onClick={() => setConfirmingDelete(bank)}
                    />
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {(isCreating || editingBank) && (
        <BankFormDialog bank={editingBank} onCancel={closeFormDialog} onSubmit={handleFormSubmit} />
      )}

      {confirmingDelete && (
        <Dialog open onOpenChange={(_, data) => { if (!data.open) setConfirmingDelete(null) }}>
          <DialogSurface aria-describedby={undefined}>
            <DialogBody>
              <DialogTitle>Delete Bank</DialogTitle>
              <DialogContent>
                {confirmingDelete.hasReferences ? (
                  <p>
                    &ldquo;{confirmingDelete.name}&rdquo; still has balance history or transactions and cannot be
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
