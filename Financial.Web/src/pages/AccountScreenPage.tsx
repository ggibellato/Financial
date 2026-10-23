import { Skeleton, SkeletonItem, Text } from '@fluentui/react-components'
import { Link } from 'react-router-dom'
import AccountActionBar from '../components/AccountActionBar'
import AccountHeader from '../components/AccountHeader'
import AccountTransactionsGrid from '../components/AccountTransactionsGrid'
import DiscardChangesDialog from '../components/DiscardChangesDialog'
import ErrorState from '../components/ErrorState'
import ExpenseForm from '../components/ExpenseForm'
import IncomeForm from '../components/IncomeForm'
import { useAccountActionForms } from '../hooks/useAccountActionForms'
import { FORM_OPTIONS_LOAD_ERROR, useAccountFormOptions } from '../hooks/useAccountFormOptions'
import { ACCOUNT_LOAD_ERROR, useAccountScreen } from '../hooks/useAccountScreen'
import { useSuccessToast } from '../hooks/useSuccessToast'
import { formatLongMonthYear } from '../utils/formatters'
import './AccountScreenPage.css'

const FORM_AREA_ID = 'account-screen-form'

export default function AccountScreenPage() {
  const screen = useAccountScreen()
  const { account } = screen
  const formOptions = useAccountFormOptions()
  const { showSuccess } = useSuccessToast()
  const forms = useAccountActionForms(account, formOptions.options, (message) => {
    void screen.refreshSilently()
    showSuccess(message)
  })
  const { expenseForm, incomeForm, discardPrompt, lockedBank, activeAction, formAreaRef, openAction, cancelActiveForm, closeForms } =
    forms

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

  const changeMonth = (value: string) =>
    discardPrompt.confirmDiscard(() => {
      if (expenseForm.isDirty || incomeForm.isIncomeDirty) closeForms()
      screen.setMonthInputValue(value)
    })

  const changeAccount = (accountId: string) =>
    discardPrompt.confirmDiscard(() => {
      closeForms()
      screen.selectAccount(accountId)
    })

  return (
    <div className="account-screen">
      {account ? (
        <AccountHeader
          account={account}
          accounts={screen.accounts}
          selectedAccountId={screen.accountId}
          monthInputValue={screen.monthInputValue}
          isBusy={screen.isLoading}
          onMonthChange={changeMonth}
          onAccountSelect={changeAccount}
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
          disabled={formOptions.isLoading || formOptions.error !== null}
          onAction={openAction}
        />
      )}

      {formOptions.error && account && account.allowedActions.length > 0 && (
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
            creditCards={[]}
            isSettled={expenseForm.isSettled}
            isSaving={expenseForm.isSaving}
            lockedBank={lockedBank}
            saveError={expenseForm.saveError}
            saveErrorFields={expenseForm.saveErrorFields}
            onFieldChange={expenseForm.setField}
            onSave={expenseForm.submit}
            onCancel={cancelActiveForm}
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
            onCancel={cancelActiveForm}
          />
        )}
      </div>

      <section className="account-screen__transactions" aria-labelledby="account-screen-transactions">
        <Text as="h2" id="account-screen-transactions" size={400} weight="semibold">
          Transactions
        </Text>
        {screen.error ? (
          <ErrorState message={ACCOUNT_LOAD_ERROR} onRetry={screen.retry} />
        ) : (
          <AccountTransactionsGrid
            transactions={screen.transactions}
            isLoading={screen.isLoading}
            emptyMessage={`No transactions in ${formatLongMonthYear(screen.year, screen.month)}`}
          />
        )}
      </section>

      <DiscardChangesDialog
        open={discardPrompt.isPromptOpen}
        onKeepEditing={discardPrompt.keepEditing}
        onDiscard={discardPrompt.discard}
      />
    </div>
  )
}
