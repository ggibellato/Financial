import { useState } from 'react'
import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import AccountFormDialog from '../components/AccountFormDialog'
import AdminEntityPage from '../components/AdminEntityPage'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import ConfirmDialog from '../components/ConfirmDialog'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import TruncatedText from '../components/TruncatedText'
import { useAdminAccounts } from '../hooks/useAdminAccounts'
import { useSuccessToast } from '../hooks/useSuccessToast'
import type { AccountDto, AccountUpdateDto } from '../api/types'
import { getSaveErrorMessage } from '../utils/formatters'

type FormTarget = { mode: 'create' } | { mode: 'edit'; account: AccountDto }

export default function AdminAccountsPage() {
  const {
    accounts,
    templates,
    isLoading,
    error,
    retry,
    createAccount,
    updateAccount,
    deactivateAccount,
    deletingId,
    deleteError,
    deleteAccount,
  } = useAdminAccounts()
  const { showSuccess } = useSuccessToast()
  const [formTarget, setFormTarget] = useState<FormTarget | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<AccountDto | null>(null)
  const [isDeleteOpen, setIsDeleteOpen] = useState(false)
  const [isDeactivating, setIsDeactivating] = useState(false)
  const [deactivateError, setDeactivateError] = useState<string | null>(null)

  const handleFormSubmit = async (request: AccountUpdateDto) => {
    if (formTarget?.mode === 'edit') {
      await updateAccount(formTarget.account.id, request)
      showSuccess('Account updated')
    } else {
      const { templateName, name, currency, openingBalance, balanceStartDate, roundUpEnabled } = request
      await createAccount({ templateName, name, currency, openingBalance, balanceStartDate, roundUpEnabled })
      showSuccess('Account created')
    }
    setFormTarget(null)
  }

  const requestDelete = (account: AccountDto) => {
    setDeactivateError(null)
    setDeleteTarget(account)
    setIsDeleteOpen(true)
  }

  const closeDelete = () => setIsDeleteOpen(false)

  const confirmDeactivate = async () => {
    if (!deleteTarget) return
    setIsDeactivating(true)
    setDeactivateError(null)
    try {
      await deactivateAccount(deleteTarget)
      setIsDeleteOpen(false)
      showSuccess('Account deactivated')
    } catch (err: unknown) {
      setDeactivateError(getSaveErrorMessage(err, 'The account could not be deactivated.'))
    } finally {
      setIsDeactivating(false)
    }
  }

  const confirmDelete = () => {
    if (!deleteTarget) return
    setIsDeleteOpen(false)
    deleteAccount(deleteTarget.id, () => showSuccess('Account deleted'))
  }

  const targetName = deleteTarget?.name ?? ''
  const canDeactivate = deleteTarget?.isActive === true

  return (
    <AdminEntityPage
      title="Accounts"
      createLabel="New account"
      onCreate={() => setFormTarget({ mode: 'create' })}
      createDisabled={templates.length === 0}
      actionError={deleteError}
      isLoading={isLoading}
      loadError={error}
      onRetry={retry}
      isEmpty={accounts.length === 0}
      emptyMessage="No accounts yet"
      dialogs={
        <>
          {formTarget && (
            <AccountFormDialog
              account={formTarget.mode === 'edit' ? formTarget.account : null}
              templates={templates}
              onCancel={() => setFormTarget(null)}
              onSubmit={handleFormSubmit}
            />
          )}
          <ConfirmDialog
            open={isDeleteOpen && deleteTarget?.hasRecords === true}
            title={`Can't delete ${targetName}`}
            message={
              canDeactivate
                ? `${targetName} has transactions and cannot be deleted. Deactivate it instead.`
                : `${targetName} has transactions and cannot be deleted. It is already inactive.`
            }
            confirmLabel={canDeactivate ? 'Deactivate' : undefined}
            cancelLabel={canDeactivate ? 'Cancel' : 'Close'}
            isBusy={isDeactivating}
            error={deactivateError}
            onConfirm={() => void confirmDeactivate()}
            onCancel={closeDelete}
          />
          <ConfirmDeleteDialog
            open={isDeleteOpen && deleteTarget?.hasRecords === false}
            entityLabel="account"
            message={<>&ldquo;{targetName}&rdquo; will be permanently removed.</>}
            onConfirm={confirmDelete}
            onCancel={closeDelete}
          />
        </>
      }
    >
      <Table aria-label="Accounts" className="data-table">
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Name</TableHeaderCell>
            <TableHeaderCell>Template</TableHeaderCell>
            <TableHeaderCell>Currency</TableHeaderCell>
            <TableHeaderCell>Active</TableHeaderCell>
            <TableHeaderCell className="data-table__col--action" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {accounts.map((account) => (
            <TableRow key={account.id}>
              <DataTableCell label="Name"><TruncatedText text={account.name} /></DataTableCell>
              <DataTableCell label="Template">{account.templateName}</DataTableCell>
              <DataTableCell label="Currency">{account.currency}</DataTableCell>
              <DataTableCell label="Active">{account.isActive ? 'Active' : 'Inactive'}</DataTableCell>
              <RowActionsCell
                itemName={account.name}
                deleteDisabled={deletingId === account.id}
                onEdit={() => setFormTarget({ mode: 'edit', account })}
                onDelete={() => requestDelete(account)}
              />
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
