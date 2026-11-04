import { useEffect, useRef } from 'react'
import { Button, MessageBar, MessageBarActions, MessageBarBody, Skeleton, SkeletonItem, Spinner, Tab, TabList, Text } from '@fluentui/react-components'
import { Link, useNavigate } from 'react-router-dom'
import AccountActionBar from '../components/AccountActionBar'
import { canTransfer, deleteConfirmation, deletedMessage, formActionsOf, isTransferAction, linesDeletedWith, needsFormOptions, recordNoun } from '../components/accountActions'
import AccountHeader from '../components/AccountHeader'
import AccountTransactionsGrid from '../components/AccountTransactionsGrid'
import CardStatementsTab from '../components/CardStatementsTab'
import BalanceAdjustmentForm from '../components/BalanceAdjustmentForm'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import DiscardChangesDialog from '../components/DiscardChangesDialog'
import EditMovementForm from '../components/EditMovementForm'
import ErrorState from '../components/ErrorState'
import ExpenseForm from '../components/ExpenseForm'
import IncomeForm from '../components/IncomeForm'
import ManagePotsDialog from '../components/ManagePotsDialog'
import MoveChargeForm from '../components/MoveChargeForm'
import PayCardStatementForm from '../components/PayCardStatementForm'
import PayRecurringBillForm from '../components/PayRecurringBillForm'
import PotDepositForm from '../components/PotDepositForm'
import PotWithdrawalForm from '../components/PotWithdrawalForm'
import PotsTab from '../components/PotsTab'
import TransferForm from '../components/TransferForm'
import { NO_CREDIT_CARDS, useAccountActionForms } from '../hooks/useAccountActionForms'
import { FORM_OPTIONS_LOAD_ERROR, useAccountFormOptions } from '../hooks/useAccountFormOptions'
import { useAccountRowDelete } from '../hooks/useAccountRowDelete'
import { ACCOUNT_LOAD_ERROR, useAccountScreen, type AccountScreenTab } from '../hooks/useAccountScreen'
import { useCardStatements } from '../hooks/useCardStatements'
import { OPERATION_LOAD_ERROR, OPERATION_NOT_FOUND, useOperationRoute } from '../hooks/useOperationRoute'
import { POTS_LOAD_ERROR, usePots } from '../hooks/usePots'
import { useSuccessToast } from '../hooks/useSuccessToast'
import { formatLongMonthYear } from '../utils/formatters'
import './AccountScreenPage.css'

const FORM_AREA_ID = 'account-screen-form'

export default function AccountScreenPage() {
  const screen = useAccountScreen()
  const navigate = useNavigate()
  const { account, hasPots } = screen
  const allowedActions = account?.allowedActions ?? []
  const opensOperation = hasPots && screen.operationId !== null
  const usesFormOptions = opensOperation || formActionsOf(allowedActions).some(needsFormOptions)
  const formOptions = useAccountFormOptions(usesFormOptions, account && canTransfer(allowedActions) ? account.id : null)
  const { showSuccess } = useSuccessToast()
  const isFocusAfterRowLeavesPending = useRef(false)
  const pots = usePots(hasPots ? screen.accountId : null, screen.year, screen.month)
  const notifySaved = (message: string) => {
    isFocusAfterRowLeavesPending.current = true
    void screen.refreshSilently()
    void pots.refresh()
    showSuccess(message)
  }
  const isCard = account?.hasStatements ?? false
  const hasTabs = screen.tabs.length > 0
  const activeTab = screen.tab
  const cardStatements = useCardStatements(isCard && activeTab === 'statements' ? screen.accountId : null, notifySaved)
  const forms = useAccountActionForms(
    screen.accountId,
    account,
    formOptions.options,
    { year: screen.year, month: screen.month },
    notifySaved,
    { isDirty: cardStatements.isPayFormDirty, close: cardStatements.closePayForm },
    { accounts: screen.accounts, hasPots },
  )
  const {
    expenseForm,
    incomeForm,
    adjustmentForm,
    transferForm,
    payBillForm,
    payStatementForm,
    moveForm,
    potTransactionForm,
    depositForm,
    withdrawalForm,
    operationDelete,
    isManagePotsOpen,
    closeManagePots,
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
  const operationRoute = useOperationRoute(
    screen.operationId,
    screen.accountId,
    opensOperation && isAccountCurrent && formOptions.isReady,
    forms.isOperationFormOpen,
    forms.openOperation,
    screen.clearOperation,
  )
  const transactionsHeadingRef = useRef<HTMLHeadingElement | null>(null)
  const rowDelete = useAccountRowDelete((row) => {
    linesDeletedWith(row, screen.transactions).forEach(closeFormsOf)
    notifySaved(deletedMessage(row, screen.transactions))
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
            !isAccountCurrent ||
            (needsFormOptions(action) && !formOptions.isReady) ||
            (isTransferAction(action) && !formOptions.areCounterpartsReady) ||
            (action === 'ManagePots' && pots.data === null)
          }
          onAction={openAction}
        />
      )}

      {formOptions.error && usesFormOptions && (
        <ErrorState message={FORM_OPTIONS_LOAD_ERROR} onRetry={formOptions.retry} />
      )}

      <div id={FORM_AREA_ID} ref={formAreaRef} tabIndex={-1} className="account-screen__form-area">
        {operationRoute.isLoading && <Spinner size="tiny" labelPosition="after" label="Loading the deposit or withdrawal…" />}
        {operationRoute.error && <ErrorState message={OPERATION_LOAD_ERROR} onRetry={operationRoute.retry} />}
        {operationRoute.isNotFound && (
          <MessageBar role="alert" intent="error">
            <MessageBarBody>{OPERATION_NOT_FOUND}</MessageBarBody>
            <MessageBarActions>
              <Button appearance="primary" onClick={screen.clearOperation}>
                Dismiss
              </Button>
            </MessageBarActions>
          </MessageBar>
        )}
        {depositForm.isOpen && (
          <PotDepositForm
            form={depositForm}
            banks={formOptions.options.banks}
            incomeSources={formOptions.options.incomeSources}
            onFromIncome={forms.openDepositFromIncome}
            onDelete={() => depositForm.operationId && operationDelete.request(depositForm.operationId, 'deposit')}
            onCancel={closeActiveForm}
          />
        )}
        {withdrawalForm.isOpen && (
          <PotWithdrawalForm
            form={withdrawalForm}
            banks={formOptions.options.banks}
            lockedBank={lockedBank}
            onDelete={() => withdrawalForm.operationId && operationDelete.request(withdrawalForm.operationId, 'withdrawal')}
            onCancel={closeActiveForm}
          />
        )}
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
        {potTransactionForm.edit && (
          <EditMovementForm
            title="Edit pot transaction"
            potLabel="Pot"
            bucketId={potTransactionForm.edit.values.editMovementBucketId}
            amount={potTransactionForm.edit.values.editMovementAmount}
            date={potTransactionForm.edit.values.editMovementDate}
            description={potTransactionForm.edit.values.editMovementDescription}
            buckets={pots.data?.pots ?? []}
            isSaving={potTransactionForm.edit.isSaving}
            error={potTransactionForm.edit.error}
            errorFields={potTransactionForm.edit.errorFields}
            onFieldChange={potTransactionForm.setField}
            onSave={potTransactionForm.submit}
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
            potsAccountId={incomeForm.incomePotsAccountId}
            potsAccounts={incomeForm.potsAccounts}
            potsAccountsStatus={incomeForm.potsAccountsStatus}
            onRetryPotsAccounts={incomeForm.retryPotsAccounts}
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

      {hasTabs && (
        <TabList
          className="account-screen__tabs"
          aria-label={isCard ? 'Card views' : 'Pots views'}
          selectedValue={activeTab}
          onTabSelect={(_, data) => guardChange(() => screen.setTab(data.value as AccountScreenTab))}
        >
          {screen.tabs.map((tab) => (
            <Tab key={tab.value} id={`account-screen-tab-${tab.value}`} value={tab.value} aria-controls="account-screen-panel">
              {tab.label}
            </Tab>
          ))}
        </TabList>
      )}

      <section
        id={hasTabs ? 'account-screen-panel' : undefined}
        role={hasTabs ? 'tabpanel' : undefined}
        className="account-screen__transactions"
        aria-labelledby={hasTabs ? `account-screen-tab-${activeTab}` : 'account-screen-transactions'}
      >
        {!isAccountCurrent && !screen.error ? (
          <Skeleton aria-label="Loading account details" className="account-screen__grid-skeleton">
            <SkeletonItem />
            <SkeletonItem />
            <SkeletonItem />
          </Skeleton>
        ) : screen.error ? (
          <ErrorState message={ACCOUNT_LOAD_ERROR} onRetry={screen.retry} />
        ) : activeTab === 'statements' ? (
          <CardStatementsTab
            cardStatements={cardStatements}
            viewedYear={screen.year}
            viewedMonth={screen.month}
            currency={account?.currency ?? 'GBP'}
            canAct={account?.isActive ?? false}
            guardChange={guardChange}
          />
        ) : activeTab === 'pots' ? (
          <PotsTab
            summary={pots.data}
            hasError={pots.error !== null}
            onRetry={pots.retry}
            year={screen.year}
            month={screen.month}
          />
        ) : (
          <>
            <Text as="h2" id="account-screen-transactions" ref={transactionsHeadingRef} tabIndex={-1} size={400} weight="semibold">
              Transactions
            </Text>
            {pots.error && <ErrorState message={POTS_LOAD_ERROR} onRetry={pots.retry} />}
            <AccountTransactionsGrid
              transactions={screen.transactions}
              showsPotColumns={hasPots}
              isLoading={screen.isLoading}
              emptyMessage={`No transactions in ${formatLongMonthYear(screen.year, screen.month)}`}
              dateHeader={isCard ? 'Purchased' : 'Date'}
              isEditDisabled={(row) => !isAccountCurrent || (row.movement ? pots.data === null : !formOptions.isReady)}
              onEdit={openRowEdit}
              onDelete={rowDelete.requestDelete}
              onMove={isCard ? openRowMove : undefined}
              isMoveDisabled={!isAccountCurrent}
              onOpenStatement={(path) => guardChange(() => navigate(path))}
              onOpenOperation={(path, link) => forms.followOperationLink(link, () => navigate(path))}
            />
          </>
        )}
      </section>

      <ConfirmDeleteDialog
        open={rowDelete.isOpen}
        entityLabel={recordNoun(deleteKind)}
        message={rowDelete.row ? deleteConfirmation(rowDelete.row, screen.transactions) : ''}
        isBusy={rowDelete.isDeleting}
        error={rowDelete.error}
        onConfirm={rowDelete.confirm}
        onCancel={rowDelete.cancel}
      />

      <ConfirmDeleteDialog
        open={operationDelete.isOpen}
        entityLabel={operationDelete.noun}
        message={`Delete this ${operationDelete.noun}? Its pot lines and bank expenses are deleted with it. This cannot be undone.`}
        isBusy={operationDelete.isDeleting}
        error={operationDelete.error}
        onConfirm={operationDelete.confirm}
        onCancel={operationDelete.cancel}
      />

      {isManagePotsOpen && account && (
        <ManagePotsDialog
          potsAccountId={account.id}
          accountName={account.name}
          initialPots={pots.data?.pots ?? []}
          onSaved={() => {
            closeManagePots()
            notifySaved('Pots saved')
          }}
          onClose={closeManagePots}
        />
      )}

      <DiscardChangesDialog
        open={discardPrompt.isPromptOpen}
        onKeepEditing={discardPrompt.keepEditing}
        onDiscard={discardPrompt.discard}
      />
    </div>
  )
}
