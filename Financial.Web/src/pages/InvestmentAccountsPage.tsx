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
import InvestmentAccountFormDialog from '../components/InvestmentAccountFormDialog'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useInvestmentAccounts } from '../hooks/useInvestmentAccounts'
import type { InvestmentAccountDto } from '../api/types'
import './InvestmentAccountsPage.css'

export default function InvestmentAccountsPage() {
  const styles = useFormPanelStyles()
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
  const [editingAccount, setEditingAccount] = useState<InvestmentAccountDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState<InvestmentAccountDto | null>(null)

  const handleSubmit = (name: string, isActive: boolean, isLiability: boolean) =>
    editingAccount
      ? updateInvestmentAccount(editingAccount.id, { name, isActive, isLiability })
      : createInvestmentAccount({ name, isActive, isLiability })

  const closeFormDialog = () => {
    setEditingAccount(null)
    setIsCreating(false)
  }

  const handleFormSubmit = async (name: string, isActive: boolean, isLiability: boolean) => {
    const result = await handleSubmit(name, isActive, isLiability)
    closeFormDialog()
    return result
  }

  const handleConfirmDelete = () => {
    if (!confirmingDelete) return
    deleteInvestmentAccount(confirmingDelete.id)
    setConfirmingDelete(null)
  }

  return (
    <section className="investment-accounts-page">
      <header className="investment-accounts-page__header">
        <h2>Investment Accounts</h2>
        <Button appearance="primary" icon={<AddRegular />} onClick={() => setIsCreating(true)}>
          Create Investment Account
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
      ) : investmentAccounts.length === 0 ? (
        <p className="investment-accounts-page__empty">No investment accounts yet — create one to get started.</p>
      ) : (
        <Table aria-label="Investment Accounts">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Name</TableHeaderCell>
              <TableHeaderCell>Active</TableHeaderCell>
              <TableHeaderCell>Liability</TableHeaderCell>
              <TableHeaderCell className="data-table__col--action" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {investmentAccounts.map((account) => (
              <TableRow key={account.id}>
                <TableCell>{account.name}</TableCell>
                <TableCell>{account.isActive ? 'Yes' : 'No'}</TableCell>
                <TableCell>{account.isLiability ? 'Yes' : 'No'}</TableCell>
                <TableCell className="data-table__col--action">
                  <div className="data-table__actions-cell">
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<EditRegular />}
                      aria-label={`Edit ${account.name}`}
                      onClick={() => setEditingAccount(account)}
                    />
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<DeleteRegular />}
                      aria-label={`Delete ${account.name}`}
                      disabled={deletingId === account.id}
                      onClick={() => setConfirmingDelete(account)}
                    />
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {(isCreating || editingAccount) && (
        <InvestmentAccountFormDialog
          investmentAccount={editingAccount}
          onCancel={closeFormDialog}
          onSubmit={handleFormSubmit}
        />
      )}

      {confirmingDelete && (
        <Dialog open onOpenChange={(_, data) => { if (!data.open) setConfirmingDelete(null) }}>
          <DialogSurface aria-describedby={undefined}>
            <DialogBody>
              <DialogTitle>Delete Investment Account</DialogTitle>
              <DialogContent>
                {confirmingDelete.hasNonZeroInvestmentSnapshot ? (
                  <p>&ldquo;{confirmingDelete.name}&rdquo; has a non-zero balance and cannot be deleted.</p>
                ) : (
                  <p>&ldquo;{confirmingDelete.name}&rdquo; will be permanently removed.</p>
                )}
              </DialogContent>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleConfirmDelete}
                  disabled={confirmingDelete.hasNonZeroInvestmentSnapshot}
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
