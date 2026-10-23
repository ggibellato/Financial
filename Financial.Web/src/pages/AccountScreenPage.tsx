import { useEffect, useRef } from 'react'
import { Skeleton, SkeletonItem, Text } from '@fluentui/react-components'
import { Link } from 'react-router-dom'
import AccountActionBar from '../components/AccountActionBar'
import { formActionsOf, recordNoun } from '../components/accountActions'
import AccountHeader from '../components/AccountHeader'
import AccountTransactionsGrid from '../components/AccountTransactionsGrid'
import BalanceAdjustmentForm from '../components/BalanceAdjustmentForm'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import DiscardChangesDialog from '../components/DiscardChangesDialog'
import ErrorState from '../components/ErrorState'
import ExpenseForm from '../components/ExpenseForm'
import IncomeForm from '../components/IncomeForm'
import TransferForm from '../components/TransferForm'
import { NO_CREDIT_CARDS, useAccountActionForms } from '../hooks/useAccountActionForms'
import { FORM_OPTIONS_LOAD_ERROR, useAccountFormOptions } from '../hooks/useAccountFormOptions'
import { useAccountRowDelete } from '../hooks/useAccountRowDelete'
import { ACCOUNT_LOAD_ERROR, useAccountScreen } from '../hooks/useAccountScreen'
import { useSuccessToast } from '../hooks/useSuccessToast'
import { formatLongMonthYear } from '../utils/formatters'
import './AccountScreenPage.css'

const FORM_AREA_ID = 'account-screen-form'
const TRANSFER_ACTIONS = ['TransferTo', 'TransferFrom']

function capitalize(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1)
}

export default function AccountScreenPage() {
  const screen = useAccountScreen()
  const { account } = screen
  const allowedActions = account?.allowedActions ?? []
  const hasFormActions = formActionsOf(allowedActions).length > 0
  const canTransfer = allowedActions.some((action) => TRANSFER_ACTIONS.includes(action))
  const formOptions = useAccountFormOptions(hasFormActions, account && canTransfer ? account.id : null)
  const { showSuccess } = useSuccessToast()
  const notifySaved = (message: string) => {
    void screen.refreshSilently()
    showSuccess(message)
  }
  const forms = useAccountActionForms(
    screen.accountId,
    account,
    formOptions.options,
    { year: screen.year, month: screen.month },
    notifySaved,
  )
  const {
    expenseForm,
    incomeForm,
    adjustmentForm,
    transferForm,
    discardPrompt,
    lockedBank,
    isAccountCurrent,
    activeAction,
    formAreaRef,
    openAction,
    openRowEdit,
    closeActiveForm,
    closeFormsOf,
    guardChange,
  } = forms
  const transactionsHeadingRef = useRef<HTMLHeadingElement | null>(null)
  const isFocusAfterDeletePending = useRef(false)
  const rowDelete = useAccountRowDelete((row) => {
    closeFormsOf(row)
    isFocusAfterDeletePending.current = true
    notifySaved(`${capitalize(recordNoun(row.kind))} deleted`)
  })
  const deleteNoun = rowDelete.row ? recordNoun(rowDelete.row.kind) : ''

  useEffect(() => {
    if (!isFocusAfterDeletePending.current || document.activeElement !== document.body) return
    isFocusAfterDeletePending.current = false
    transactionsHeadingRef.current?.focus()
  }, [screen.transactions])

  if (screen.isNotFound) {
    return (
      <div className="account-screen">
        <p role="alert">This account does not exist.</p>
        <Link to="/cashflow/accounts">Go to Accounts</Link>
      </div>
    )
  }

  if (screen.error && !account) {
    return (
      <div className="account-screen">
        <ErrorState message={ACCOUNT_LOAD_ERROR} onRetry={screen.retry} />
      </div>
    )
  }

  return (
    <div className="account-screen">
      {account ? (
        <AccountHeader
          account={account}
          accounts={screen.accounts}
          selectedAccountId={screen.accountId}
          monthInputValue={screen.monthInputValue}
          isBusy={screen.isLoading}
          onMonthChange={(value) => guardChange(() => screen.setMonthInputValue(value))}
          onAccountSelect={(accountId) => guardChange(() => screen.selectAccount(accountId))}
        />
      ) : (
        <Skeleton aria-label="Loading account" className="account-screen__header-skeleton">
          <SkeletonItem size={32} />
          <SkeletonItem size={16} />
        </Skeleton>
      )}

      {account && (
        <AccountActionBar
          allowedActions={account.allowedActions}
          activeAction={activeAction}
          formAreaId={FORM_AREA_ID}
          disabled={!formOptions.isReady || !isAccountCurrent}
          onAction={openAction}
        />
      )}

      {formOptions.error && hasFormActions && (
        <ErrorState message={FORM_OPTIONS_LOAD_ERROR} onRetry={formOptions.retry} />
      )}

      <div id={FORM_AREA_ID} ref={formAreaRef} className="account-screen__form-area">
        {expenseForm.isOpen && (
          <ExpenseForm
            isEditing={expenseForm.isEditing}
            date={expenseForm.date}
            description={expenseForm.description}
            value={expenseForm.value}
            categoryId={expenseForm.categoryId}
            paymentSource={expenseForm.paymentSource}
            creditCardId={expenseForm.creditCardId}
            creditCardName={expenseForm.creditCardName}
            invoiceDate={expenseForm.invoiceDate}
            roundUpAmount={expenseForm.roundUpAmount}
            countsAsTithe={expenseForm.countsAsTithe === 'true'}
            paymentMode={expenseForm.paymentMode}
            banks={formOptions.options.banks}
            categories={formOptions.options.categories}
            creditCards={NO_CREDIT_CARDS}
            isSettled={expenseForm.isSettled}
            isSaving={expenseForm.isSaving}
            lockedBank={lockedBank}
            saveError={expenseForm.saveError}
            saveErrorFields={expenseForm.saveErrorFields}
            onFieldChange={expenseForm.setField}
            onSave={expenseForm.submit}
            onCancel={closeActiveForm}
          />
        )}
        {incomeForm.isIncomeFormOpen && (
          <IncomeForm
            isEditing={incomeForm.isIncomeEditing}
            date={incomeForm.incomeDate}
            incomeSource={incomeForm.incomeSource}
            grossValue={incomeForm.incomeGrossValue}
            netValue={incomeForm.incomeNetValue}
            bank={incomeForm.incomeBank}
            description={incomeForm.incomeDescription}
            splitToReserve={incomeForm.incomeSplitToReserve === 'true'}
            banks={formOptions.options.banks}
            incomeSources={formOptions.options.incomeSources}
            isSaving={incomeForm.isSavingIncome}
            lockedBank={lockedBank}
            saveError={incomeForm.saveIncomeError}
            saveErrorFields={incomeForm.saveIncomeErrorFields}
            onFieldChange={incomeForm.setIncomeField}
            onSave={incomeForm.submitIncome}
            onCancel={closeActiveForm}
          />
        )}
        {transferForm.isOpen && lockedBank && (
          <TransferForm
            isEditing={transferForm.isEditing}
            date={transferForm.date}
            sourceBank={transferForm.sourceBank}
            destinationBank={transferForm.destinationBank}
            amount={transferForm.amount}
            destinationAmount={transferForm.destinationAmount}
            destinationCurrency={transferForm.destinationCurrency}
            note={transferForm.note}
            banks={transferForm.counterpartOptions}
            lockedSide={{ side: transferForm.direction === 'to' ? 'source' : 'destination', name: lockedBank.name }}
            isSaving={transferForm.isSaving}
            saveError={transferForm.saveError}
            saveErrorFields={transferForm.saveErrorFields}
            onFieldChange={transferForm.setField}
            onSave={transferForm.submit}
            onCancel={closeActiveForm}
          />
        )}
        {adjustmentForm.isOpen && (
          <BalanceAdjustmentForm
            isEditing={adjustmentForm.isEditing}
            bankName={adjustmentForm.bankName}
            bankDisplayName={adjustmentForm.bankDisplayName}
            banks={formOptions.options.banks}
            currentBalance={adjustmentForm.currentBalance}
            date={adjustmentForm.date}
            targetBalance={adjustmentForm.targetBalance}
            note={adjustmentForm.note}
            isSaving={adjustmentForm.isSaving}
            saveError={adjustmentForm.saveError}
            saveErrorFields={adjustmentForm.saveErrorFields}
            savedDelta={adjustmentForm.savedDelta}
            lockedBank={lockedBank}
            onFieldChange={adjustmentForm.setField}
            onSave={adjustmentForm.submit}
            onCancel={closeActiveForm}
          />
        )}
      </div>

      <section className="account-screen__transactions" aria-labelledby="account-screen-transactions">
        <Text as="h2" id="account-screen-transactions" ref={transactionsHeadingRef} tabIndex={-1} size={400} weight="semibold">
          Transactions
        </Text>
        {screen.error ? (
          <ErrorState message={ACCOUNT_LOAD_ERROR} onRetry={screen.retry} />
        ) : (
          <AccountTransactionsGrid
            transactions={screen.transactions}
            isLoading={screen.isLoading}
            emptyMessage={`No transactions in ${formatLongMonthYear(screen.year, screen.month)}`}
            isEditDisabled={!formOptions.isReady || !isAccountCurrent}
            onEdit={openRowEdit}
            onDelete={rowDelete.requestDelete}
          />
        )}
      </section>

      <ConfirmDeleteDialog
        open={rowDelete.isOpen}
        entityLabel={capitalize(deleteNoun)}
        message={`Delete this ${deleteNoun}? This cannot be undone.`}
        confirmDisabled={rowDelete.isDeleting}
        error={rowDelete.error}
        onConfirm={rowDelete.confirm}
        onCancel={rowDelete.cancel}
      />

      <DiscardChangesDialog
        open={discardPrompt.isPromptOpen}
        onKeepEditing={discardPrompt.keepEditing}
        onDiscard={discardPrompt.discard}
      />
    </div>
  )
}
