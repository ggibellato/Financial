import { useEffect, useRef } from 'react'
import { Button, MessageBar, MessageBarActions, MessageBarBody, Skeleton, SkeletonItem, Spinner, Tab, Text } from '@fluentui/react-components'
import { Link, useNavigate } from 'react-router-dom'
import PageTabList from '../components/PageTabList'
import { useTabPanel } from '../hooks/useTabPanel'
import AccountActionBar from '../components/AccountActionBar'
import { canTransfer, deleteConfirmation, deletedMessage, formActionsOf, isCreditRefund, isTransferAction, needsFormOptions, operationPath, recordNoun } from '../components/accountActions'
import AccountHeader from '../components/AccountHeader'
import AccountTransactionsGrid from '../components/AccountTransactionsGrid'
import CardCreditRefundForm from '../components/CardCreditRefundForm'
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
import SettlementEntryForm from '../components/SettlementEntryForm'
import TransferForm from '../components/TransferForm'
import { useAccountActionForms } from '../hooks/useAccountActionForms'
import { FORM_OPTIONS_LOAD_ERROR, useAccountFormOptions } from '../hooks/useAccountFormOptions'
import { useAccountRowDelete } from '../hooks/useAccountRowDelete'
import { ACCOUNT_LOAD_ERROR, useAccountScreen, type AccountScreenTab } from '../hooks/useAccountScreen'
import { useCardStatements } from '../hooks/useCardStatements'
import { OPERATION_LOAD_ERROR, OPERATION_NOT_FOUND, useOperationRoute } from '../hooks/useOperationRoute'
import { POTS_LOAD_ERROR, usePots } from '../hooks/usePots'
import { useSuccessToast } from '../hooks/useSuccessToast'
import { formatLongMonthYear } from '../utils/formatters'
import { settlementCounterparty } from '../utils/settlementBalance'
import './AccountScreenPage.css'

const FORM_AREA_ID = 'account-screen-form'

export default function AccountScreenPage() {
  const screen = useAccountScreen()
  const navigate = useNavigate()
  const { account, hasPots, hasParticipants } = screen
  const allowedActions = account?.allowedActions ?? []
  const usesFormOptions = screen.operationId !== null || formActionsOf(allowedActions).some(needsFormOptions)
  const formOptions = useAccountFormOptions(usesFormOptions, account && canTransfer(allowedActions) ? account.id : null)
  const { showSuccess } = useSuccessToast()
  const isRefreshAfterSavePending = useRef(false)
  const pots = usePots(hasPots ? screen.accountId : null, screen.year, screen.month)
  const notifySaved = (message: string) => {
    isRefreshAfterSavePending.current = true
    void screen.refreshSilently()
    void pots.refresh()
    showSuccess(message)
  }
  const isCard = account?.hasStatements ?? false
  const isAccountActive = account?.isActive ?? false
  const hasTabs = screen.tabs.length > 0
  const activeTab = screen.tab
  const tabPanel = useTabPanel(activeTab)
  const cardStatements = useCardStatements(isCard && activeTab === 'statements' ? screen.accountId : null, notifySaved)
  const forms = useAccountActionForms(
    screen.accountId,
    account,
    formOptions.options,
    { year: screen.year, month: screen.month },
    (message) => {
      notifySaved(message)
      cardStatements.retry()
    },
    { isDirty: cardStatements.isFormDirty, close: cardStatements.closeForms },
    { accounts: screen.accounts, hasPots },
  )
  const {
    expenseForm,
    incomeForm,
    adjustmentForm,
    transferForm,
    payBillForm,
    payStatementForm,
    creditRefundForm,
    moveForm,
    potTransactionForm,
    depositForm,
    withdrawalForm,
    settlementEntryForm,
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
    closeFormOfRemovedRow,
    guardChange,
  } = forms
  const operationRoute = useOperationRoute(
    screen.operationId,
    hasPots ? screen.accountId : null,
    isAccountCurrent && formOptions.isReady,
    forms.isOperationFormOpen,
    forms.openOperation,
    screen.clearOperation,
  )
  const transactionsHeadingRef = useRef<HTMLHeadingElement | null>(null)
  const rowDelete = useAccountRowDelete((row) => {
    closeFormsOf(row)
    if (isCreditRefund(row)) payStatementForm.retry()
    notifySaved(deletedMessage(row))
  })
  const deleteKind = rowDelete.row?.kind ?? ''
  const gridColumns =
    hasParticipants && account
      ? ({ columns: 'settlement', otherParticipant: settlementCounterparty(account), showsConverted: account.currentBalances.length > 1 } as const)
      : ({ columns: hasPots ? 'pots' : 'account' } as const)

  useEffect(() => {
    if (!isRefreshAfterSavePending.current) return
    isRefreshAfterSavePending.current = false
    closeFormOfRemovedRow(screen.transactions)
    if (document.activeElement !== document.body) return
    if (transactionsHeadingRef.current) transactionsHeadingRef.current.focus()
    else forms.restoreFocus()
    // eslint-disable-next-line react-hooks/exhaustive-deps -- runs only when a refresh brings new rows
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
          hasParticipants={hasParticipants}
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
          disabledReasons={account.disabledActions}
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

      <div id={FORM_AREA_ID} ref={formAreaRef} className="account-screen__form-area">
        {operationRoute.isLoading && (
          <div role="status">
            <Spinner size="tiny" labelPosition="after" label="Loading the deposit or withdrawal…" />
          </div>
        )}
        {operationRoute.error && <ErrorState message={OPERATION_LOAD_ERROR} onRetry={operationRoute.retry} />}
        {operationRoute.isNotFound && (
          <MessageBar role="alert" intent="error">
            <MessageBarBody>{OPERATION_NOT_FOUND}</MessageBarBody>
            <MessageBarActions>
              <Button
                appearance="primary"
                onClick={() => {
                  screen.clearOperation()
                  forms.restoreFocus()
                }}
              >
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
            onDelete={() => depositForm.operationId && operationDelete.request({ operationId: depositForm.operationId, noun: 'deposit' })}
            onCancel={closeActiveForm}
          />
        )}
        {withdrawalForm.isOpen && (
          <PotWithdrawalForm
            form={withdrawalForm}
            banks={formOptions.options.banks}
            lockedBank={lockedBank}
            onDelete={() => withdrawalForm.operationId && operationDelete.request({ operationId: withdrawalForm.operationId, noun: 'withdrawal' })}
            onCancel={closeActiveForm}
          />
        )}
        {settlementEntryForm.isOpen && <SettlementEntryForm form={settlementEntryForm} onCancel={closeActiveForm} />}
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
            invoiceMonthMovedFrom={expenseForm.invoiceMonthMovedFrom}
            roundUpAmount={expenseForm.roundUpAmount}
            countsAsTithe={expenseForm.countsAsTithe === 'true'}
            paymentMode={expenseForm.paymentMode}
            banks={formOptions.options.banks}
            categories={formOptions.options.categories}
            isRefund={expenseForm.isRefund}
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
            currency={account?.currency ?? ''}
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
            funding={incomeForm.incomeFunding}
            banks={formOptions.options.banks}
            incomeSources={formOptions.options.incomeSources}
            isSaving={incomeForm.isSavingIncome}
            lockedBank={incomeForm.incomeLockedBank}
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
        {creditRefundForm.isOpen && <CardCreditRefundForm refundForm={creditRefundForm} onCancel={closeActiveForm} />}
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
        <PageTabList
          aria-label={isCard ? 'Card views' : 'Pots views'}
          selectedValue={activeTab}
          onTabSelect={(_, data) => guardChange(() => screen.setTab(data.value as AccountScreenTab))}
        >
          {screen.tabs.map((tab) => (
            <Tab key={tab.value} value={tab.value} {...tabPanel.tabProps(tab.value)}>
              {tab.label}
            </Tab>
          ))}
        </PageTabList>
      )}

      <section
        {...(hasTabs ? tabPanel.panelProps : { 'aria-labelledby': 'account-screen-transactions' })}
        className="account-screen__transactions"
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
            canAct={isAccountActive}
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
              key={screen.accountId}
              accountId={screen.accountId}
              transactions={screen.transactions}
              {...gridColumns}
              isLoading={screen.isLoading}
              emptyMessage={`No transactions in ${formatLongMonthYear(screen.year, screen.month)}.`}
              dateHeader={isCard ? 'Purchased' : 'Date'}
              isEditDisabled={(row) => !isAccountCurrent || (!row.settlement && !row.operation && (row.movement ? pots.data === null : !formOptions.isReady))}
              onEdit={openRowEdit}
              onDelete={rowDelete.requestDelete}
              canDeleteCreditRefunds={isAccountActive}
              onMove={isCard ? openRowMove : undefined}
              isMoveDisabled={!isAccountCurrent}
              onOpenStatement={(path) => guardChange(() => navigate(path))}
              onOpenOperation={(operation, button) => forms.followOperation(operation.operationId, button, () => navigate(operationPath(operation, screen.accountId)))}
              onDeleteOperation={({ operationId }, noun) => operationDelete.request({ operationId, noun })}
            />
          </>
        )}
      </section>

      <ConfirmDeleteDialog
        open={rowDelete.isOpen}
        entityLabel={recordNoun(deleteKind)}
        message={rowDelete.row ? deleteConfirmation(rowDelete.row) : ''}
        isBusy={rowDelete.isDeleting}
        error={rowDelete.error}
        onConfirm={rowDelete.confirm}
        onCancel={rowDelete.cancel}
      />

      <ConfirmDeleteDialog
        open={operationDelete.isOpen}
        entityLabel={operationDelete.target?.noun ?? ''}
        message={`Delete this ${operationDelete.target?.noun ?? ''}? Its pot lines and any bank expenses are deleted with it. This cannot be undone.`}
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
