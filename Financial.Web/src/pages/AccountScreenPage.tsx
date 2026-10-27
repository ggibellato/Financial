import { useEffect, useRef } from 'react'
import { Skeleton, SkeletonItem, Tab, TabList, Text } from '@fluentui/react-components'
import { Link, useNavigate } from 'react-router-dom'
import AccountActionBar from '../components/AccountActionBar'
import { canTransfer, formActionsOf, isTransferAction, recordNoun, recordTitle } from '../components/accountActions'
import AccountHeader from '../components/AccountHeader'
import AccountTransactionsGrid from '../components/AccountTransactionsGrid'
import CardStatementsTab from '../components/CardStatementsTab'
import BalanceAdjustmentForm from '../components/BalanceAdjustmentForm'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import DiscardChangesDialog from '../components/DiscardChangesDialog'
import ErrorState from '../components/ErrorState'
import ExpenseForm from '../components/ExpenseForm'
import IncomeForm from '../components/IncomeForm'
import MoveChargeForm from '../components/MoveChargeForm'
import PayCardStatementForm from '../components/PayCardStatementForm'
import PayRecurringBillForm from '../components/PayRecurringBillForm'
import TransferForm from '../components/TransferForm'
import { NO_CREDIT_CARDS, useAccountActionForms } from '../hooks/useAccountActionForms'
import { FORM_OPTIONS_LOAD_ERROR, useAccountFormOptions } from '../hooks/useAccountFormOptions'
import { useAccountRowDelete } from '../hooks/useAccountRowDelete'
import { ACCOUNT_LOAD_ERROR, useAccountScreen, type AccountScreenTab } from '../hooks/useAccountScreen'
import { useCardStatements } from '../hooks/useCardStatements'
import { useSuccessToast } from '../hooks/useSuccessToast'
import { formatLongMonthYear } from '../utils/formatters'
import './AccountScreenPage.css'

const FORM_AREA_ID = 'account-screen-form'

export default function AccountScreenPage() {
  const screen = useAccountScreen()
  const navigate = useNavigate()
  const { account } = screen
  const allowedActions = account?.allowedActions ?? []
  const hasFormActions = formActionsOf(allowedActions).length > 0
  const formOptions = useAccountFormOptions(hasFormActions, account && canTransfer(allowedActions) ? account.id : null)
  const { showSuccess } = useSuccessToast()
  const isFocusAfterRowLeavesPending = useRef(false)
  const notifySaved = (message: string) => {
    isFocusAfterRowLeavesPending.current = true
    void screen.refreshSilently()
    showSuccess(message)
  }
  const isCard = account?.hasStatements ?? false
  const activeTab: AccountScreenTab = isCard ? screen.tab : 'transactions'
  const cardStatements = useCardStatements(isCard && activeTab === 'statements' ? screen.accountId : null, notifySaved)
  const forms = useAccountActionForms(
    screen.accountId,
    account,
    formOptions.options,
    { year: screen.year, month: screen.month },
    notifySaved,
    { isDirty: cardStatements.isPayFormDirty, close: cardStatements.closePayForm },
  )
  const {
    expenseForm,
    incomeForm,
    adjustmentForm,
    transferForm,
    payBillForm,
    payStatementForm,
    moveForm,
    discardPrompt,
    lockedBank,
    lockedCard,
    isAccountCurrent,
    activeAction,
    formAreaRef,
    openAction,
    openRowEdit,
    openRowMove,
    closeActiveForm,
    closeFormsOf,
    guardChange,
  } = forms
  const transactionsHeadingRef = useRef<HTMLHeadingElement | null>(null)
  const rowDelete = useAccountRowDelete((row) => {
    closeFormsOf(row)
    notifySaved(`${recordTitle(row.kind)} deleted`)
  })
  const deleteKind = rowDelete.row?.kind ?? ''

  useEffect(() => {
    if (!isFocusAfterRowLeavesPending.current) return
    isFocusAfterRowLeavesPending.current = false
    if (document.activeElement === document.body) transactionsHeadingRef.current?.focus()
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
          isActionDisabled={(action) =>
            !formOptions.isReady || !isAccountCurrent || (isTransferAction(action) && !formOptions.areCounterpartsReady)
          }
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
            lockedCard={lockedCard}
            saveError={expenseForm.saveError}
            saveErrorFields={expenseForm.saveErrorFields}
            onFieldChange={expenseForm.setField}
            onSave={expenseForm.submit}
            onCancel={closeActiveForm}
          />
        )}
        {moveForm.isOpen && <MoveChargeForm moveForm={moveForm} onCancel={closeActiveForm} />}
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
        {transferForm.isOpen && (
          <TransferForm
            isEditing={transferForm.isEditing}
            date={transferForm.date}
            sourceBank={transferForm.sourceBank}
            destinationBank={transferForm.destinationBank}
            amount={transferForm.amount}
            destinationAmount={transferForm.destinationAmount}
            sourceCurrency={transferForm.sourceCurrency}
            destinationCurrency={transferForm.destinationCurrency}
            note={transferForm.note}
            banks={transferForm.counterpartOptions}
            lockedSide={transferForm.lockedSide}
            isSaving={transferForm.isSaving}
            saveError={transferForm.saveError}
            saveErrorFields={transferForm.saveErrorFields}
            onFieldChange={transferForm.setField}
            onSave={transferForm.submit}
            onCancel={closeActiveForm}
          />
        )}
        {payBillForm.isOpen && lockedBank && (
          <PayRecurringBillForm
            bills={payBillForm.bills}
            isLoadingBills={payBillForm.isLoadingBills}
            billsError={payBillForm.billsError}
            billId={payBillForm.billId}
            values={payBillForm.values}
            categories={formOptions.options.categories}
            lockedBank={lockedBank}
            isSaving={payBillForm.isSaving}
            saveError={payBillForm.saveError}
            fieldErrors={payBillForm.fieldErrors}
            billError={payBillForm.billError}
            onRetryBills={payBillForm.retryBills}
            onSelectBill={payBillForm.selectBill}
            onFieldChange={payBillForm.setField}
            onSave={payBillForm.submit}
            onCancel={closeActiveForm}
          />
        )}
        {payStatementForm.isOpen && lockedBank && (
          <PayCardStatementForm payForm={payStatementForm} lockedBank={lockedBank} onCancel={closeActiveForm} />
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
            isBankLocked
            currency={account?.currency}
            balanceLabel={`Balance at end of ${formatLongMonthYear(screen.year, screen.month)}`}
            onFieldChange={adjustmentForm.setField}
            onSave={adjustmentForm.submit}
            onCancel={closeActiveForm}
          />
        )}
      </div>

      {isCard && (
        <TabList
          className="account-screen__tabs"
          aria-label="Card views"
          selectedValue={activeTab}
          onTabSelect={(_, data) => guardChange(() => screen.setTab(data.value as AccountScreenTab))}
        >
          <Tab id="account-screen-tab-transactions" value="transactions" aria-controls="account-screen-panel">
            Transactions
          </Tab>
          <Tab id="account-screen-tab-statements" value="statements" aria-controls="account-screen-panel">
            Statements
          </Tab>
        </TabList>
      )}

      <section
        id={isCard ? 'account-screen-panel' : undefined}
        role={isCard ? 'tabpanel' : undefined}
        className="account-screen__transactions"
        aria-labelledby={isCard ? `account-screen-tab-${activeTab}` : 'account-screen-transactions'}
      >
        {activeTab === 'statements' ? (
          <CardStatementsTab
            cardStatements={cardStatements}
            viewedYear={screen.year}
            viewedMonth={screen.month}
            currency={account?.currency ?? 'GBP'}
            canAct={account?.isActive ?? false}
            guardChange={guardChange}
          />
        ) : (
          <>
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
                dateHeader={isCard ? 'Purchased' : 'Date'}
                isEditDisabled={!formOptions.isReady || !isAccountCurrent}
                onEdit={openRowEdit}
                onDelete={rowDelete.requestDelete}
                onMove={isCard ? openRowMove : undefined}
                isMoveDisabled={!isAccountCurrent}
                onOpenStatement={(path, event) => {
                  event.preventDefault()
                  guardChange(() => navigate(path))
                }}
              />
            )}
          </>
        )}
      </section>

      <ConfirmDeleteDialog
        open={rowDelete.isOpen}
        entityLabel={recordNoun(deleteKind)}
        message={`Delete this ${recordNoun(deleteKind)}? This cannot be undone.`}
        isBusy={rowDelete.isDeleting}
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
