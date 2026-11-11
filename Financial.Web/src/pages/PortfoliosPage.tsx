import { useState } from 'react'
import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import AdminEntityPage from '../components/AdminEntityPage'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import PortfolioFormDialog from '../components/PortfolioFormDialog'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import { rowDeleteState } from '../components/grid/rowDeleteState'
import { useBrokers } from '../hooks/useBrokers'
import { portfolioKey, usePortfolios } from '../hooks/usePortfolios'
import { usePendingDelete } from '../hooks/usePendingDelete'
import type { PortfolioDto } from '../api/types'
import ActionsColumnHeader from '../components/grid/ActionsColumnHeader'

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
  const deletion = usePendingDelete((row: PortfolioDto) => {
    const scope = row.brokerStatus === 'Active' ? 'active' : 'historic'
    deletePortfolio(row.brokerName, row.name, scope)
  })
  const pendingDelete = deletion.pendingRow

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

  return (
    <AdminEntityPage
      title="Portfolios"
      createLabel="New portfolio"
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
          {pendingDelete && (
            <ConfirmDeleteDialog
              entityLabel="portfolio"
              message={
                pendingDelete.assetCount > 0 ? (
                  <>
                    &ldquo;{pendingDelete.name}&rdquo; still holds {pendingDelete.assetCount} asset(s) and
                    cannot be deleted.
                  </>
                ) : (
                  <>&ldquo;{pendingDelete.name}&rdquo; holds no assets and will be permanently removed.</>
                )
              }
              confirmDisabled={pendingDelete.assetCount > 0}
              onConfirm={deletion.confirm}
              onCancel={deletion.cancel}
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
            <TableHeaderCell>Broker status</TableHeaderCell>
            <TableHeaderCell className="data-table__col--numeric">Assets</TableHeaderCell>
            <ActionsColumnHeader />
          </TableRow>
        </TableHeader>
        <TableBody>
          {portfolios.map((portfolio) => {
            const key = portfolioKey(portfolio.brokerName, portfolio.name)
            return (
              <TableRow key={key}>
                <DataTableCell label="Name">{portfolio.name}</DataTableCell>
                <DataTableCell label="Broker">{portfolio.brokerName}</DataTableCell>
                <DataTableCell label="Broker status">{portfolio.brokerStatus}</DataTableCell>
                <DataTableCell label="Assets" className="data-table__col--numeric">
                  {portfolio.assetCount}
                </DataTableCell>
                <RowActionsCell
                  itemName={portfolio.name}
                  {...rowDeleteState(deletingKey, key)}
                  onEdit={() => setEditingPortfolio(portfolio)}
                  onDelete={() => deletion.request(portfolio)}
                />
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
