import { useState } from 'react'
import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import AdminEntityPage from '../components/AdminEntityPage'
import BrokerFormDialog from '../components/BrokerFormDialog'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import { useBrokers } from '../hooks/useBrokers'
import type { BrokerDto, CostBasisMethod } from '../api/types'
import { getErrorMessage } from '../utils/formatters'
import TruncatedText from '../components/TruncatedText'

function deleteMessage(broker: BrokerDto) {
  if (broker.portfolioCount > 0) {
    return (
      <>
        &ldquo;{broker.name}&rdquo; still has {broker.portfolioCount} portfolio(s) and cannot be deleted.
      </>
    )
  }
  if (broker.status === 'Active') {
    return (
      <>
        &ldquo;{broker.name}&rdquo; has no portfolios. It will move to the Historic list rather than be removed.
      </>
    )
  }
  return <>&ldquo;{broker.name}&rdquo; has no portfolios. It will be permanently removed.</>
}

export default function BrokersPage() {
  const {
    brokers,
    isLoading,
    error,
    retry,
    createBroker,
    updateBroker,
    setCostBasisMethod,
    deletingName,
    deleteError,
    deleteBroker,
  } = useBrokers()
  const [editingBroker, setEditingBroker] = useState<BrokerDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState<BrokerDto | null>(null)

  const closeFormDialog = () => {
    setEditingBroker(null)
    setIsCreating(false)
  }

  const handleFormSubmit = async (name: string, currency: string, costBasisMethod: CostBasisMethod) => {
    if (!editingBroker) {
      const created = await createBroker({ name, currency, costBasisMethod })
      closeFormDialog()
      return created
    }

    const editingScope = editingBroker.status === 'Active' ? 'active' : 'historic'
    const renamed = await updateBroker(editingBroker.name, { name, currency }, editingScope)
    // The rename already committed server-side by this point, so a thrown error from here on
    // must not be reported as if nothing was saved - and a resubmit must target the new name,
    // not the one that no longer exists.
    setEditingBroker(renamed)
    if (costBasisMethod === editingBroker.costBasisMethod) {
      closeFormDialog()
      return renamed
    }

    try {
      const updated = await setCostBasisMethod(renamed.name, costBasisMethod, editingScope)
      closeFormDialog()
      return updated
    } catch (err: unknown) {
      throw new Error(
        `"${renamed.name}" was saved, but its cost basis method could not be changed: ${getErrorMessage(err, 'unknown error')}`,
        { cause: err },
      )
    }
  }

  const handleConfirmDelete = () => {
    if (!confirmingDelete) return
    deleteBroker(confirmingDelete.name)
    setConfirmingDelete(null)
  }

  return (
    <AdminEntityPage
      title="Brokers"
      createLabel="Create Broker"
      onCreate={() => setIsCreating(true)}
      actionError={deleteError}
      isLoading={isLoading}
      loadError={error}
      onRetry={retry}
      isEmpty={brokers.length === 0}
      emptyMessage="No brokers yet — create one to get started."
      dialogs={
        <>
          {(isCreating || editingBroker) && (
            <BrokerFormDialog broker={editingBroker} onCancel={closeFormDialog} onSubmit={handleFormSubmit} />
          )}
          {confirmingDelete && (
            <ConfirmDeleteDialog
              entityLabel="Broker"
              message={deleteMessage(confirmingDelete)}
              confirmDisabled={confirmingDelete.portfolioCount > 0}
              onConfirm={handleConfirmDelete}
              onCancel={() => setConfirmingDelete(null)}
            />
          )}
        </>
      }
    >
      <Table aria-label="Brokers" className="data-table">
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
              <DataTableCell label="Name"><TruncatedText text={broker.name} /></DataTableCell>
              <DataTableCell label="Currency">{broker.currency}</DataTableCell>
              <DataTableCell label="Status">{broker.status}</DataTableCell>
              <DataTableCell label="Portfolios">{broker.portfolioCount}</DataTableCell>
              <RowActionsCell
                itemName={broker.name}
                deleteDisabled={deletingName === broker.name}
                onEdit={() => setEditingBroker(broker)}
                onDelete={() => setConfirmingDelete(broker)}
              />
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
