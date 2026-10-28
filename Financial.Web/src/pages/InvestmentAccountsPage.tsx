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
import InvestmentAccountFormDialog, { type InvestmentAccountSourceOption } from '../components/InvestmentAccountFormDialog'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import { useAccounts } from '../hooks/useAccounts'
import { useInvestmentAccounts } from '../hooks/useInvestmentAccounts'
import { useCreditCards } from '../hooks/useCreditCards'
import type { AccountDto, InvestmentAccountDto } from '../api/types'
import TruncatedText from '../components/TruncatedText'

function sourceDisplay(
  account: InvestmentAccountDto,
  creditCards: { id: string; name: string }[],
  accounts: AccountDto[],
): string {
  if (account.source === 'CreditCard') {
    return creditCards.find((c) => c.id === account.creditCardId)?.name ?? '—'
  }
  if (account.source === 'PotsAccountTotal') {
    const potsAccount = accounts.find((a) => a.id === account.potsAccountId)
    if (!potsAccount) return '—'
    return potsAccount.isActive ? `${potsAccount.name} total` : `${potsAccount.name} total (inactive)`
  }
  return '—'
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
  const { creditCards, error: creditCardsError, retry: retryCreditCards } = useCreditCards()
  const { accounts, error: accountsError, retry: retryAccounts } = useAccounts()
  const [editingAccount, setEditingAccount] = useState<InvestmentAccountDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState<InvestmentAccountDto | null>(null)

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

  const handleConfirmDelete = () => {
    if (!confirmingDelete) return
    deleteInvestmentAccount(confirmingDelete.id)
    setConfirmingDelete(null)
  }

  return (
    <AdminEntityPage
      title="Investment Accounts"
      createLabel="Create Investment Account"
      onCreate={() => setIsCreating(true)}
      actionError={deleteError}
      belowActionError={
        <>
          {creditCardsError && (
            <MessageBar intent="warning">
              <MessageBarBody>
                Couldn&rsquo;t load credit cards, so the Source dropdown&rsquo;s Credit Card option won&rsquo;t list any
                cards right now.{' '}
                <Button appearance="transparent" size="small" onClick={retryCreditCards}>
                  Retry
                </Button>
              </MessageBarBody>
            </MessageBar>
          )}
          {accountsError && (
            <MessageBar intent="warning">
              <MessageBarBody>
                Couldn&rsquo;t load accounts, so the Pots account total option won&rsquo;t list any Pots accounts and
                the list can&rsquo;t name them right now.{' '}
                <Button appearance="transparent" size="small" aria-label="Retry loading accounts" onClick={retryAccounts}>
                  Retry
                </Button>
              </MessageBarBody>
            </MessageBar>
          )}
        </>
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
          {confirmingDelete && (
            <ConfirmDeleteDialog
              entityLabel="Investment Account"
              message={
                confirmingDelete.hasNonZeroInvestmentSnapshot ? (
                  <>&ldquo;{confirmingDelete.name}&rdquo; has a non-zero balance and cannot be deleted.</>
                ) : (
                  <>&ldquo;{confirmingDelete.name}&rdquo; will be permanently removed.</>
                )
              }
              confirmDisabled={confirmingDelete.hasNonZeroInvestmentSnapshot}
              onConfirm={handleConfirmDelete}
              onCancel={() => setConfirmingDelete(null)}
            />
          )}
        </>
      }
    >
      <Table aria-label="Investment Accounts" className="data-table">
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Name</TableHeaderCell>
            <TableHeaderCell>Active</TableHeaderCell>
            <TableHeaderCell>Liability</TableHeaderCell>
            <TableHeaderCell>Source</TableHeaderCell>
            <TableHeaderCell className="data-table__col--action" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {investmentAccounts.map((account) => (
            <TableRow key={account.id}>
              <DataTableCell label="Name"><TruncatedText text={account.name} /></DataTableCell>
              <DataTableCell label="Active">{account.isActive ? 'Yes' : 'No'}</DataTableCell>
              <DataTableCell label="Liability">{account.isLiability ? 'Yes' : 'No'}</DataTableCell>
              <DataTableCell label="Source">{sourceDisplay(account, creditCards, accounts)}</DataTableCell>
              <RowActionsCell
                itemName={account.name}
                deleteDisabled={deletingId === account.id}
                onEdit={() => setEditingAccount(account)}
                onDelete={() => setConfirmingDelete(account)}
              />
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
