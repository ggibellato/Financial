import { useState } from 'react'
import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import AdminEntityPage from '../components/AdminEntityPage'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import PortfolioFormDialog from '../components/PortfolioFormDialog'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import { useBrokers } from '../hooks/useBrokers'
import { portfolioKey, usePortfolios } from '../hooks/usePortfolios'
import type { PortfolioDto } from '../api/types'

export default function PortfoliosPage() {
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
      ? await updatePortfolio(
          editingPortfolio.brokerName,
          editingPortfolio.name,
          { name },
          editingPortfolio.brokerStatus === 'Active' ? 'active' : 'historic',
        )
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
    <AdminEntityPage
      title="Portfolios"
      createLabel="Create Portfolio"
      onCreate={() => setIsCreating(true)}
      actionError={deleteError}
      isLoading={isLoading}
      loadError={error}
      onRetry={retry}
      isEmpty={portfolios.length === 0}
      emptyMessage="No portfolios yet — create one to get started."
      dialogs={
        <>
          {(isCreating || editingPortfolio) && (
            <PortfolioFormDialog
              portfolio={editingPortfolio}
              activeBrokers={activeBrokers}
              onCancel={closeFormDialog}
              onSubmit={handleFormSubmit}
            />
          )}
          {confirmingDelete && (
            <ConfirmDeleteDialog
              entityLabel="Portfolio"
              message={
                confirmingDelete.assetCount > 0 ? (
                  <>
                    &ldquo;{confirmingDelete.name}&rdquo; still holds {confirmingDelete.assetCount} asset(s) and
                    cannot be deleted.
                  </>
                ) : (
                  <>&ldquo;{confirmingDelete.name}&rdquo; holds no assets and will be permanently removed.</>
                )
              }
              confirmDisabled={confirmingDelete.assetCount > 0}
              onConfirm={handleConfirmDelete}
              onCancel={() => setConfirmingDelete(null)}
            />
          )}
        </>
      }
    >
      <Table aria-label="Portfolios" className="data-table">
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Name</TableHeaderCell>
            <TableHeaderCell>Broker</TableHeaderCell>
            <TableHeaderCell>Broker Status</TableHeaderCell>
            <TableHeaderCell>Assets</TableHeaderCell>
            <TableHeaderCell className="data-table__col--action" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {portfolios.map((portfolio) => {
            const key = portfolioKey(portfolio.brokerName, portfolio.name)
            return (
              <TableRow key={key}>
                <DataTableCell label="Name">{portfolio.name}</DataTableCell>
                <DataTableCell label="Broker">{portfolio.brokerName}</DataTableCell>
                <DataTableCell label="Broker Status">{portfolio.brokerStatus}</DataTableCell>
                <DataTableCell label="Assets">{portfolio.assetCount}</DataTableCell>
                <RowActionsCell
                  itemName={portfolio.name}
                  deleteDisabled={deletingKey === key}
                  onEdit={() => setEditingPortfolio(portfolio)}
                  onDelete={() => setConfirmingDelete(portfolio)}
                />
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
