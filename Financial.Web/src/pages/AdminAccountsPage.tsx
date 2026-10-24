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
import { getErrorMessage } from '../utils/formatters'

type FormTarget = { mode: 'create' } | { mode: 'edit'; account: AccountDto }

export default function AdminAccountsPage() {
  const { accounts, templates, isLoading, error, retry, createAccount, updateAccount, deactivateAccount, deleteAccount } =
    useAdminAccounts()
  const { showSuccess } = useSuccessToast()
  const [formTarget, setFormTarget] = useState<FormTarget | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<AccountDto | null>(null)
  const [isDeleteBusy, setIsDeleteBusy] = useState(false)
  const [deactivateError, setDeactivateError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const handleFormSubmit = async ({ isActive, ...values }: AccountUpdateDto) => {
    if (formTarget?.mode === 'edit') {
      await updateAccount(formTarget.account.id, { ...values, isActive })
      showSuccess('Account updated')
    } else {
      await createAccount(values)
      showSuccess('Account created')
    }
    setFormTarget(null)
  }

  const requestDelete = (account: AccountDto) => {
    setActionError(null)
    setDeactivateError(null)
    setDeleteTarget(account)
  }

  const closeDelete = () => setDeleteTarget(null)

  const confirmDeactivate = async () => {
    if (!deleteTarget) return
    setIsDeleteBusy(true)
    setDeactivateError(null)
    try {
      await deactivateAccount(deleteTarget)
      setDeleteTarget(null)
      showSuccess('Account deactivated')
    } catch (err: unknown) {
      setDeactivateError(getErrorMessage(err, 'The account could not be deactivated.'))
    } finally {
      setIsDeleteBusy(false)
    }
  }

  const confirmDelete = async () => {
    if (!deleteTarget) return
    setIsDeleteBusy(true)
    try {
      await deleteAccount(deleteTarget.id)
      showSuccess('Account deleted')
    } catch (err: unknown) {
      setActionError(getErrorMessage(err, 'The account could not be deleted.'))
    } finally {
      setIsDeleteBusy(false)
      setDeleteTarget(null)
    }
  }

  const blockedName = deleteTarget?.name ?? ''

  return (
    <AdminEntityPage
      title="Accounts"
      createLabel="New account"
      onCreate={() => setFormTarget({ mode: 'create' })}
      actionError={actionError}
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
            open={deleteTarget?.hasRecords === true}
            title={`Can't delete ${blockedName}`}
            message={`${blockedName} has transactions and cannot be deleted. Deactivate it instead.`}
            confirmLabel={deleteTarget?.isActive ? 'Deactivate' : undefined}
            cancelLabel={deleteTarget?.isActive ? 'Cancel' : 'Close'}
            isBusy={isDeleteBusy}
            error={deactivateError}
            onConfirm={() => void confirmDeactivate()}
            onCancel={closeDelete}
          />
          <ConfirmDeleteDialog
            open={deleteTarget?.hasRecords === false}
            entityLabel="Account"
            message={<>&ldquo;{blockedName}&rdquo; will be permanently removed.</>}
            isBusy={isDeleteBusy}
            onConfirm={() => void confirmDelete()}
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
                deleteDisabled={isDeleteBusy && deleteTarget?.id === account.id}
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
