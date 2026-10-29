import { useState } from 'react'
import {
  Button,
  MessageBar,
  MessageBarBody,
  Table,
  TableBody,
  TableHeader,
  TableHeaderCell,
  TableRow,
} from '@fluentui/react-components'
import AdminEntityPage from '../components/AdminEntityPage'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import IncomeSourceFormDialog from '../components/IncomeSourceFormDialog'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import { type AccountsStatus, useAccounts } from '../hooks/useAccounts'
import { useIncomeSources } from '../hooks/useIncomeSources'
import type { AccountDto, IncomeSourceDto } from '../api/types'
import TruncatedText from '../components/TruncatedText'

function splitsToDisplay(incomeSource: IncomeSourceDto, accounts: AccountDto[], accountsStatus: AccountsStatus): string {
  if (incomeSource.potsAccountId == null) return '—'
  const potsAccount = accounts.find((a) => a.id === incomeSource.potsAccountId)
  if (potsAccount) return potsAccount.isActive ? potsAccount.name : `${potsAccount.name} (inactive)`
  return accountsStatus === 'loading' ? 'Loading…' : 'Pots account (not loaded)'
}

export default function IncomeSourcesPage() {
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
  const { accounts, status: accountsStatus, error: accountsError, retry: retryAccounts } = useAccounts()
  const [editingIncomeSource, setEditingIncomeSource] = useState<IncomeSourceDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState<IncomeSourceDto | null>(null)

  const handleSubmit = (name: string, group: string, isActive: boolean, potsAccountId: string | null) =>
    editingIncomeSource
      ? updateIncomeSource(editingIncomeSource.id, { name, group, isActive, potsAccountId })
      : createIncomeSource({ name, group, isActive, potsAccountId })

  const closeFormDialog = () => {
    setEditingIncomeSource(null)
    setIsCreating(false)
  }

  const handleFormSubmit = async (name: string, group: string, isActive: boolean, potsAccountId: string | null) => {
    const result = await handleSubmit(name, group, isActive, potsAccountId)
    closeFormDialog()
    return result
  }

  const handleConfirmDelete = () => {
    if (!confirmingDelete) return
    deleteIncomeSource(confirmingDelete.id)
    setConfirmingDelete(null)
  }

  return (
    <AdminEntityPage
      title="Income Sources"
      createLabel="Create Income Source"
      onCreate={() => setIsCreating(true)}
      actionError={deleteError}
      belowActionError={
        accountsError && (
          <MessageBar intent="warning">
            <MessageBarBody>
              Couldn&rsquo;t load accounts, so the Pots account list and the Splits to column can&rsquo;t name them
              right now.{' '}
              <Button appearance="transparent" size="small" aria-label="Retry loading accounts" onClick={retryAccounts}>
                Retry
              </Button>
            </MessageBarBody>
          </MessageBar>
        )
      }
      isLoading={isLoading}
      loadError={error}
      onRetry={retry}
      isEmpty={incomeSources.length === 0}
      emptyMessage="No income sources yet — create one to get started."
      dialogs={
        <>
          {(isCreating || editingIncomeSource) && (
            <IncomeSourceFormDialog
              incomeSource={editingIncomeSource}
              accounts={accounts}
              accountsStatus={accountsStatus}
              onCancel={closeFormDialog}
              onSubmit={handleFormSubmit}
            />
          )}
          {confirmingDelete && (
            <ConfirmDeleteDialog
              entityLabel="Income Source"
              message={
                confirmingDelete.hasReferences ? (
                  <>&ldquo;{confirmingDelete.name}&rdquo; is still used by an income entry and cannot be deleted.</>
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
      <Table aria-label="Income Sources" className="data-table">
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Name</TableHeaderCell>
            <TableHeaderCell>Group</TableHeaderCell>
            <TableHeaderCell>Active</TableHeaderCell>
            <TableHeaderCell>Splits to</TableHeaderCell>
            <TableHeaderCell className="data-table__col--action" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {incomeSources.map((incomeSource) => (
            <TableRow key={incomeSource.id}>
              <DataTableCell label="Name"><TruncatedText text={incomeSource.name} /></DataTableCell>
              <DataTableCell label="Group"><TruncatedText text={incomeSource.group} /></DataTableCell>
              <DataTableCell label="Active">{incomeSource.isActive ? 'Yes' : 'No'}</DataTableCell>
              <DataTableCell label="Splits to">
                <TruncatedText text={splitsToDisplay(incomeSource, accounts, accountsStatus)} />
              </DataTableCell>
              <RowActionsCell
                itemName={incomeSource.name}
                deleteDisabled={deletingId === incomeSource.id}
                onEdit={() => setEditingIncomeSource(incomeSource)}
                onDelete={() => setConfirmingDelete(incomeSource)}
              />
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
