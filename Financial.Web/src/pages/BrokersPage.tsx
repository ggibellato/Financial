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
import BrokerFormDialog from '../components/BrokerFormDialog'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useBrokers } from '../hooks/useBrokers'
import type { BrokerDto } from '../api/types'
import './BrokersPage.css'

export default function BrokersPage() {
  const styles = useFormPanelStyles()
  const { brokers, isLoading, error, retry, createBroker, updateBroker, deletingName, deleteError, deleteBroker } =
    useBrokers()
  const [editingBroker, setEditingBroker] = useState<BrokerDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState<BrokerDto | null>(null)

  const handleSubmit = (name: string, currency: string) =>
    editingBroker
      ? updateBroker(editingBroker.name, { name, currency })
      : createBroker({ name, currency })

  const closeFormDialog = () => {
    setEditingBroker(null)
    setIsCreating(false)
  }

  const handleFormSubmit = async (name: string, currency: string) => {
    const result = await handleSubmit(name, currency)
    closeFormDialog()
    return result
  }

  const handleConfirmDelete = () => {
    if (!confirmingDelete) return
    deleteBroker(confirmingDelete.name)
    setConfirmingDelete(null)
  }

  return (
    <section className="brokers-page">
      <header className="brokers-page__header">
        <h2>Brokers</h2>
        <Button appearance="primary" icon={<AddRegular />} onClick={() => setIsCreating(true)}>
          Create Broker
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
      ) : brokers.length === 0 ? (
        <p className="brokers-page__empty">No brokers yet — create one to get started.</p>
      ) : (
        <Table aria-label="Brokers">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Name</TableHeaderCell>
              <TableHeaderCell>Currency</TableHeaderCell>
              <TableHeaderCell>Status</TableHeaderCell>
              <TableHeaderCell>Portfolios</TableHeaderCell>
              <TableHeaderCell className="data-table__col--action" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {brokers.map((broker) => (
              <TableRow key={broker.name}>
                <TableCell>{broker.name}</TableCell>
                <TableCell>{broker.currency}</TableCell>
                <TableCell>{broker.status}</TableCell>
                <TableCell>{broker.portfolioCount}</TableCell>
                <TableCell className="data-table__col--action">
                  <div className="data-table__actions-cell">
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<EditRegular />}
                      aria-label={`Edit ${broker.name}`}
                      onClick={() => setEditingBroker(broker)}
                    />
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<DeleteRegular />}
                      aria-label={`Delete ${broker.name}`}
                      disabled={deletingName === broker.name}
                      onClick={() => setConfirmingDelete(broker)}
                    />
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {(isCreating || editingBroker) && (
        <BrokerFormDialog broker={editingBroker} onCancel={closeFormDialog} onSubmit={handleFormSubmit} />
      )}

      {confirmingDelete && (
        <Dialog open onOpenChange={(_, data) => { if (!data.open) setConfirmingDelete(null) }}>
          <DialogSurface aria-describedby={undefined}>
            <DialogBody>
              <DialogTitle>Delete Broker</DialogTitle>
              <DialogContent>
                {confirmingDelete.portfolioCount > 0 ? (
                  <p>
                    &ldquo;{confirmingDelete.name}&rdquo; still has {confirmingDelete.portfolioCount} portfolio(s)
                    and cannot be deleted.
                  </p>
                ) : confirmingDelete.status === 'Active' ? (
                  <p>
                    &ldquo;{confirmingDelete.name}&rdquo; has no portfolios. It will move to the Historic list
                    rather than be removed.
                  </p>
                ) : (
                  <p>
                    &ldquo;{confirmingDelete.name}&rdquo; has no portfolios. It will be permanently removed.
                  </p>
                )}
              </DialogContent>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleConfirmDelete}
                  disabled={confirmingDelete.portfolioCount > 0}
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
