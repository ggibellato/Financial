import { useState } from 'react'
import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import AdminEntityPage from '../components/AdminEntityPage'
import BankFormDialog from '../components/BankFormDialog'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import { useBanks } from '../hooks/useBanks'
import type { BankDto } from '../api/types'
import TruncatedText from '../components/TruncatedText'

export default function BanksPage() {
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
    <AdminEntityPage
      title="Banks"
      createLabel="Create Bank"
      onCreate={() => setIsCreating(true)}
      actionError={deleteError}
      isLoading={isLoading}
      loadError={error}
      onRetry={retry}
      isEmpty={banks.length === 0}
      emptyMessage="No banks yet — create one to get started."
      dialogs={
        <>
          {(isCreating || editingBank) && (
            <BankFormDialog bank={editingBank} onCancel={closeFormDialog} onSubmit={handleFormSubmit} />
          )}
          {confirmingDelete && (
            <ConfirmDeleteDialog
              entityLabel="Bank"
              message={
                confirmingDelete.hasReferences ? (
                  <>
                    &ldquo;{confirmingDelete.name}&rdquo; still has balance history or transactions and cannot be
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
      <Table aria-label="Banks" className="data-table">
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
              <DataTableCell label="Name"><TruncatedText text={bank.name} /></DataTableCell>
              <DataTableCell label="Round-up">{bank.roundUpEnabled ? 'Enabled' : 'Disabled'}</DataTableCell>
              <DataTableCell label="Opening Balance">{bank.openingBalance}</DataTableCell>
              <RowActionsCell
                itemName={bank.name}
                deleteDisabled={deletingId === bank.id}
                onEdit={() => setEditingBank(bank)}
                onDelete={() => setConfirmingDelete(bank)}
              />
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
