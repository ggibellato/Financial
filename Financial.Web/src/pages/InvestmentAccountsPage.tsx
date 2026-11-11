import { useState, type ReactNode } from 'react'
import {
  Button,
  MessageBar,
  MessageBarActions,
  MessageBarBody,
  Table,
  TableBody,
  TableHeader,
  TableHeaderCell,
  TableRow,
} from '@fluentui/react-components'
import AdminEntityPage from '../components/AdminEntityPage'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import InvestmentAccountFormDialog, { type InvestmentAccountSourceOption } from '../components/InvestmentAccountFormDialog'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import { rowDeleteState } from '../components/grid/rowDeleteState'
import { useAccounts } from '../hooks/useAccounts'
import { useInvestmentAccounts } from '../hooks/useInvestmentAccounts'
import { usePendingDelete } from '../hooks/usePendingDelete'
import type { AccountDto, InvestmentAccountDto } from '../api/types'
import TruncatedText from '../components/TruncatedText'
import ActionsColumnHeader from '../components/grid/ActionsColumnHeader'
import MissingValue from '../components/MissingValue'

function sourceDisplay(account: InvestmentAccountDto, accounts: AccountDto[]): ReactNode {
  if (account.source === 'CreditCard') {
    return accounts.find((a) => a.id === account.creditCardId)?.name ?? <MissingValue />
  }
  if (account.source === 'PotsAccountTotal') {
    const potsAccount = accounts.find((a) => a.id === account.potsAccountId)
    if (!potsAccount) return <MissingValue />
    return potsAccount.isActive ? `${potsAccount.name} total` : `${potsAccount.name} total (inactive)`
  }
  return <MissingValue />
}

export default function InvestmentAccountsPage() {
  const {
    investmentAccounts,
    isLoading,
    error,
    retry,
    createInvestmentAccount,
    updateInvestmentAccount,
    deletingId,
    deleteError,
    deleteInvestmentAccount,
  } = useInvestmentAccounts()
  const { accounts, error: accountsError, retry: retryAccounts } = useAccounts()
  const creditCards = accounts.filter((account) => account.capabilities.includes('HasStatements'))
  const [editingAccount, setEditingAccount] = useState<InvestmentAccountDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const deletion = usePendingDelete((row: InvestmentAccountDto) => deleteInvestmentAccount(row.id))
  const pendingDelete = deletion.pendingRow

  const handleSubmit = (
    name: string,
    isActive: boolean,
    isLiability: boolean,
    source: InvestmentAccountSourceOption,
    creditCardId: string | null,
    potsAccountId: string | null,
  ) =>
    editingAccount
      ? updateInvestmentAccount(editingAccount.id, { name, isActive, isLiability, source, creditCardId, potsAccountId })
      : createInvestmentAccount({ name, isActive, isLiability, source, creditCardId, potsAccountId })

  const closeFormDialog = () => {
    setEditingAccount(null)
    setIsCreating(false)
  }

  const handleFormSubmit = async (
    name: string,
    isActive: boolean,
    isLiability: boolean,
    source: InvestmentAccountSourceOption,
    creditCardId: string | null,
    potsAccountId: string | null,
  ) => {
    const result = await handleSubmit(name, isActive, isLiability, source, creditCardId, potsAccountId)
    closeFormDialog()
    return result
  }

  return (
    <AdminEntityPage
      title="Investment accounts"
      createLabel="New investment account"
      onCreate={() => setIsCreating(true)}
      actionError={deleteError}
      belowActionError={
        accountsError && (
          <MessageBar intent="warning">
            <MessageBarBody>
              Couldn&rsquo;t load accounts, so the credit card and Pots account pickers are empty and the Source column
              shows — for linked accounts.
            </MessageBarBody>
            <MessageBarActions>
              <Button appearance="primary" aria-label="Retry loading accounts" onClick={retryAccounts}>
                Retry
              </Button>
            </MessageBarActions>
          </MessageBar>
        )
      }
      isLoading={isLoading}
      loadError={error}
      onRetry={retry}
      isEmpty={investmentAccounts.length === 0}
      emptyMessage="No investment accounts yet — create one to get started."
      dialogs={
        <>
          {(isCreating || editingAccount) && (
            <InvestmentAccountFormDialog
              investmentAccount={editingAccount}
              creditCards={creditCards}
              accounts={accounts}
              onCancel={closeFormDialog}
              onSubmit={handleFormSubmit}
            />
          )}
          {pendingDelete && (
            <ConfirmDeleteDialog
              entityLabel="investment account"
              message={
                pendingDelete.hasNonZeroInvestmentSnapshot ? (
                  <>&ldquo;{pendingDelete.name}&rdquo; has a non-zero balance and cannot be deleted.</>
                ) : (
                  <>&ldquo;{pendingDelete.name}&rdquo; will be permanently removed.</>
                )
              }
              confirmDisabled={pendingDelete.hasNonZeroInvestmentSnapshot}
              onConfirm={deletion.confirm}
              onCancel={deletion.cancel}
            />
          )}
        </>
      }
    >
      <Table aria-label="Investment accounts" className="data-table">
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Name</TableHeaderCell>
            <TableHeaderCell>Active</TableHeaderCell>
            <TableHeaderCell>Liability</TableHeaderCell>
            <TableHeaderCell>Source</TableHeaderCell>
            <ActionsColumnHeader />
          </TableRow>
        </TableHeader>
        <TableBody>
          {investmentAccounts.map((account) => (
            <TableRow key={account.id}>
              <DataTableCell label="Name"><TruncatedText text={account.name} /></DataTableCell>
              <DataTableCell label="Active">{account.isActive ? 'Yes' : 'No'}</DataTableCell>
              <DataTableCell label="Liability">{account.isLiability ? 'Yes' : 'No'}</DataTableCell>
              <DataTableCell label="Source">{sourceDisplay(account, accounts)}</DataTableCell>
              <RowActionsCell
                itemName={account.name}
                {...rowDeleteState(deletingId, account.id)}
                onEdit={() => setEditingAccount(account)}
                onDelete={() => deletion.request(account)}
              />
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
