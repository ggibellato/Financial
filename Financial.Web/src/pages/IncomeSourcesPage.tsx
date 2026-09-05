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
import IncomeSourceFormDialog from '../components/IncomeSourceFormDialog'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useIncomeSources } from '../hooks/useIncomeSources'
import type { IncomeSourceDto } from '../api/types'
import './IncomeSourcesPage.css'

export default function IncomeSourcesPage() {
  const styles = useFormPanelStyles()
  const {
    incomeSources,
    isLoading,
    error,
    retry,
    createIncomeSource,
    updateIncomeSource,
    deletingId,
    deleteError,
    deleteIncomeSource,
  } = useIncomeSources()
  const [editingIncomeSource, setEditingIncomeSource] = useState<IncomeSourceDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState<IncomeSourceDto | null>(null)

  const handleSubmit = (name: string, group: string, isActive: boolean, autoSplitToReserve: boolean) =>
    editingIncomeSource
      ? updateIncomeSource(editingIncomeSource.id, { name, group, isActive, autoSplitToReserve })
      : createIncomeSource({ name, group, isActive, autoSplitToReserve })

  const closeFormDialog = () => {
    setEditingIncomeSource(null)
    setIsCreating(false)
  }

  const handleFormSubmit = async (name: string, group: string, isActive: boolean, autoSplitToReserve: boolean) => {
    const result = await handleSubmit(name, group, isActive, autoSplitToReserve)
    closeFormDialog()
    return result
  }

  const handleConfirmDelete = () => {
    if (!confirmingDelete) return
    deleteIncomeSource(confirmingDelete.id)
    setConfirmingDelete(null)
  }

  return (
    <section className="income-sources-page">
      <header className="income-sources-page__header">
        <h2>Income Sources</h2>
        <Button appearance="primary" icon={<AddRegular />} onClick={() => setIsCreating(true)}>
          Create Income Source
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
      ) : incomeSources.length === 0 ? (
        <p className="income-sources-page__empty">No income sources yet — create one to get started.</p>
      ) : (
        <Table aria-label="Income Sources">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Name</TableHeaderCell>
              <TableHeaderCell>Group</TableHeaderCell>
              <TableHeaderCell>Active</TableHeaderCell>
              <TableHeaderCell>Auto-split to reserve</TableHeaderCell>
              <TableHeaderCell className="data-table__col--action" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {incomeSources.map((incomeSource) => (
              <TableRow key={incomeSource.id}>
                <TableCell>{incomeSource.name}</TableCell>
                <TableCell>{incomeSource.group}</TableCell>
                <TableCell>{incomeSource.isActive ? 'Yes' : 'No'}</TableCell>
                <TableCell>{incomeSource.autoSplitToReserve ? 'Yes' : 'No'}</TableCell>
                <TableCell className="data-table__col--action">
                  <div className="data-table__actions-cell">
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<EditRegular />}
                      aria-label={`Edit ${incomeSource.name}`}
                      onClick={() => setEditingIncomeSource(incomeSource)}
                    />
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<DeleteRegular />}
                      aria-label={`Delete ${incomeSource.name}`}
                      disabled={deletingId === incomeSource.id}
                      onClick={() => setConfirmingDelete(incomeSource)}
                    />
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {(isCreating || editingIncomeSource) && (
        <IncomeSourceFormDialog
          incomeSource={editingIncomeSource}
          onCancel={closeFormDialog}
          onSubmit={handleFormSubmit}
        />
      )}

      {confirmingDelete && (
        <Dialog open onOpenChange={(_, data) => { if (!data.open) setConfirmingDelete(null) }}>
          <DialogSurface aria-describedby={undefined}>
            <DialogBody>
              <DialogTitle>Delete Income Source</DialogTitle>
              <DialogContent>
                {confirmingDelete.hasReferences ? (
                  <p>&ldquo;{confirmingDelete.name}&rdquo; is still used by an income entry and cannot be deleted.</p>
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
