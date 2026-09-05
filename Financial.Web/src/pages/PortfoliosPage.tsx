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
import PortfolioFormDialog from '../components/PortfolioFormDialog'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useBrokers } from '../hooks/useBrokers'
import { portfolioKey, usePortfolios } from '../hooks/usePortfolios'
import type { PortfolioDto } from '../api/types'
import './PortfoliosPage.css'

export default function PortfoliosPage() {
  const styles = useFormPanelStyles()
  const {
    portfolios,
    isLoading,
    error,
    retry,
    createPortfolio,
    updatePortfolio,
    deletingKey,
    deleteError,
    deletePortfolio,
  } = usePortfolios()
  const { brokers } = useBrokers()
  const activeBrokers = brokers.filter((b) => b.status === 'Active')

  const [editingPortfolio, setEditingPortfolio] = useState<PortfolioDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState<PortfolioDto | null>(null)

  const closeFormDialog = () => {
    setEditingPortfolio(null)
    setIsCreating(false)
  }

  const handleFormSubmit = async (brokerName: string, name: string) => {
    const result = editingPortfolio
      ? await updatePortfolio(editingPortfolio.brokerName, editingPortfolio.name, { name })
      : await createPortfolio({ brokerName, name })
    closeFormDialog()
    return result
  }

  const handleConfirmDelete = () => {
    if (!confirmingDelete) return
    const scope = confirmingDelete.brokerStatus === 'Active' ? 'active' : 'historic'
    deletePortfolio(confirmingDelete.brokerName, confirmingDelete.name, scope)
    setConfirmingDelete(null)
  }

  return (
    <section className="portfolios-page">
      <header className="portfolios-page__header">
        <h2>Portfolios</h2>
        <Button appearance="primary" icon={<AddRegular />} onClick={() => setIsCreating(true)}>
          Create Portfolio
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
      ) : portfolios.length === 0 ? (
        <p className="portfolios-page__empty">No portfolios yet — create one to get started.</p>
      ) : (
        <Table aria-label="Portfolios">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Name</TableHeaderCell>
              <TableHeaderCell>Broker</TableHeaderCell>
              <TableHeaderCell>Broker Status</TableHeaderCell>
              <TableHeaderCell>Assets</TableHeaderCell>
              <TableHeaderCell />
            </TableRow>
          </TableHeader>
          <TableBody>
            {portfolios.map((portfolio) => {
              const key = portfolioKey(portfolio.brokerName, portfolio.name)
              return (
                <TableRow key={key}>
                  <TableCell>{portfolio.name}</TableCell>
                  <TableCell>{portfolio.brokerName}</TableCell>
                  <TableCell>{portfolio.brokerStatus}</TableCell>
                  <TableCell>{portfolio.assetCount}</TableCell>
                  <TableCell>
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<EditRegular />}
                      aria-label={`Edit ${portfolio.name}`}
                      onClick={() => setEditingPortfolio(portfolio)}
                    />
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<DeleteRegular />}
                      aria-label={`Delete ${portfolio.name}`}
                      disabled={deletingKey === key}
                      onClick={() => setConfirmingDelete(portfolio)}
                    />
                  </TableCell>
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      )}

      {(isCreating || editingPortfolio) && (
        <PortfolioFormDialog
          portfolio={editingPortfolio}
          activeBrokers={activeBrokers}
          onCancel={closeFormDialog}
          onSubmit={handleFormSubmit}
        />
      )}

      {confirmingDelete && (
        <Dialog open onOpenChange={(_, data) => { if (!data.open) setConfirmingDelete(null) }}>
          <DialogSurface aria-describedby={undefined}>
            <DialogBody>
              <DialogTitle>Delete Portfolio</DialogTitle>
              <DialogContent>
                {confirmingDelete.assetCount > 0 ? (
                  <p>
                    &ldquo;{confirmingDelete.name}&rdquo; still holds {confirmingDelete.assetCount} asset(s) and
                    cannot be deleted.
                  </p>
                ) : (
                  <p>&ldquo;{confirmingDelete.name}&rdquo; holds no assets and will be permanently removed.</p>
                )}
              </DialogContent>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleConfirmDelete}
                  disabled={confirmingDelete.assetCount > 0}
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
