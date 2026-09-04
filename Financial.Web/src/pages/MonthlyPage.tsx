import { Tab, TabList } from '@fluentui/react-components'
import type { SelectTabData, SelectTabEvent } from '@fluentui/react-components'
import { useState } from 'react'
import BalanceAdjustmentForm from '../components/BalanceAdjustmentForm'
import BankOperationsSection from '../components/BankOperationsSection'
import BanksGrid from '../components/BanksGrid'
import CardsGrid from '../components/CardsGrid'
import CategoryTotalsGrid from '../components/CategoryTotalsGrid'
import ErrorState from '../components/ErrorState'
import ExpenseForm from '../components/ExpenseForm'
import ExpensesSection from '../components/ExpensesSection'
import IncomeForm from '../components/IncomeForm'
import IncomeSection from '../components/IncomeSection'
import IncomingGrid from '../components/IncomingGrid'
import LoadingState from '../components/LoadingState'
import TransferForm from '../components/TransferForm'
import { useBalanceAdjustmentForm } from '../hooks/useBalanceAdjustmentForm'
import { useBankOperations } from '../hooks/useBankOperations'
import { useCreditCards } from '../hooks/useCreditCards'
import { useExpenseForm } from '../hooks/useExpenseForm'
import { useIncomeForm } from '../hooks/useIncomeForm'
import { useMonthly } from '../hooks/useMonthly'
import { useTransferForm } from '../hooks/useTransferForm'
import { confirmThenRun } from '../utils/confirmThenRun'
import './MonthlyPage.css'

type MonthlyTabId = 'summary' | 'expense' | 'card' | 'incoming' | 'bank'

const MONTHLY_TABS: { id: MonthlyTabId; label: string }[] = [
  { id: 'summary', label: 'Summary' },
  { id: 'expense', label: 'Bank expenses' },
  { id: 'card', label: 'Credit Card expenses' },
  { id: 'incoming', label: 'Income' },
  { id: 'bank', label: 'Bank balance adjustment' },
]

export default function MonthlyPage() {
  const {
    year,
    month,
    monthInputValue,
    setMonthInputValue,
    expenses,
    unpaidCardCharges,
    categoryTotals,
    categoryTotalsSum,
    cardStatements,
    banks,
    incomeSources,
    categories,
    adjustmentTotal,
    bankTotals,
    bankTotalsSum,
    roundUpTotalsSum,
    isLoading,
    error,
    retry,
    refreshSilently,
    deleteExpense,
    markPaidSources,
    setMarkPaidSource,
    markStatementPaid,
    unmarkStatementPaid,
    incomes,
    incomeTotals,
    totalIncoming,
    titheSummary,
    carryForwardUpdating,
    updateCarryForwardInclusion,
    deleteIncome,
    listActionError,
    listActionWarning,
  } = useMonthly()

  const confirmAndDeleteExpense = (id: string) => confirmThenRun('Delete this expense?', () => deleteExpense(id))

  const confirmAndDeleteIncome = (id: string) => confirmThenRun('Delete this income entry?', () => deleteIncome(id))

  const confirmAndUnmarkStatementPaid = (id: string) =>
    confirmThenRun('Unmark this statement as paid? Its settled charges revert to unsettled.', () =>
      unmarkStatementPaid(id),
    )

  const confirmAndDeleteTransfer = (id: string) =>
    confirmThenRun('Delete this transfer?', () => bankOperations.deleteTransfer(id))

  const confirmAndDeleteAdjustment = (bankId: string, id: string) =>
    confirmThenRun('Delete this balance adjustment?', () => bankOperations.deleteAdjustment(bankId, id))

  const creditCardsData = useCreditCards()

  const {
    isOpen: isExpenseFormOpen,
    isEditing,
    date,
    description,
    value,
    categoryId,
    paymentSource,
    creditCardId,
    creditCardName,
    invoiceDate,
    roundUpAmount,
    countsAsTithe,
    paymentMode,
    isSettled,
    isSaving,
    saveError,
    saveErrorFields,
    showCreateForm,
    showEditForm,
    cancelForm,
    setField,
    submit,
  } = useExpenseForm(banks, categories, creditCardsData.creditCards, () => {
    refreshSilently()
    creditCardsData.refreshSilently()
  })

  const {
    isIncomeFormOpen,
    isIncomeEditing,
    incomeDate,
    incomeSource,
    incomeGrossValue,
    incomeNetValue,
    incomeBank,
    incomeDescription,
    incomeSplitToReserve,
    isSavingIncome,
    saveIncomeError,
    saveIncomeErrorFields,
    splitConfirmationMessage,
    showCreateIncomeForm,
    showEditIncomeForm,
    cancelIncomeForm,
    setIncomeField,
    submitIncome,
  } = useIncomeForm(incomeSources, refreshSilently)

  const bankOperations = useBankOperations(year, month, banks, refreshSilently)
  const activeCreditCards = creditCardsData.creditCards.filter((c) => c.isActive)
  const activeCategories = categories.filter((c) => c.active)
  const transferForm = useTransferForm(banks, () => {
    refreshSilently()
    bankOperations.refreshSilently()
  })
  const adjustmentForm = useBalanceAdjustmentForm(bankTotals, () => {
    refreshSilently()
    bankOperations.refreshSilently()
  })

  const [activeTab, setActiveTab] = useState<MonthlyTabId>('summary')

  const isBankFormVisible = transferForm.isOpen || adjustmentForm.isOpen

  const expenseFormElement = isExpenseFormOpen && (
    <ExpenseForm
      isEditing={isEditing}
      date={date}
      description={description}
      value={value}
      categoryId={categoryId}
      paymentSource={paymentSource}
      creditCardId={creditCardId}
      creditCardName={creditCardName}
      invoiceDate={invoiceDate}
      roundUpAmount={roundUpAmount}
      countsAsTithe={countsAsTithe === 'true'}
      paymentMode={paymentMode}
      banks={banks}
      categories={activeCategories}
      creditCards={activeCreditCards}
      isSettled={isSettled}
      isSaving={isSaving}
      saveError={saveError}
      saveErrorFields={saveErrorFields}
      onFieldChange={setField}
      onSave={submit}
      onCancel={cancelForm}
    />
  )

  const handleTabClick = (tabId: MonthlyTabId) => {
    if ((activeTab === 'expense' || activeTab === 'card') && isExpenseFormOpen) {
      cancelForm()
    }
    if (activeTab === 'incoming' && isIncomeFormOpen) {
      cancelIncomeForm()
    }
    if (activeTab === 'bank' && isBankFormVisible) {
      if (transferForm.isOpen) transferForm.cancel()
      if (adjustmentForm.isOpen) adjustmentForm.cancel()
    }
    setActiveTab(tabId)
  }

  const handleTabSelect = (_event: SelectTabEvent, data: SelectTabData) => handleTabClick(data.value as MonthlyTabId)

  return (
    <div className="monthly-page">
      <div className="monthly-page__header">
        <div className="monthly-page__month-picker">
          <label htmlFor="monthly-month">Month</label>
          <input
            id="monthly-month"
            type="month"
            value={monthInputValue}
            onChange={(e) => setMonthInputValue(e.target.value)}
          />
        </div>
      </div>

      <TabList selectedValue={activeTab} onTabSelect={handleTabSelect}>
        {MONTHLY_TABS.map((tab) => (
          <Tab key={tab.id} value={tab.id}>
            {tab.label}
          </Tab>
        ))}
      </TabList>

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <div className="monthly-page__content">
          {activeTab === 'summary' && (
          <div className="monthly-page__summary-groups">
            <div className="monthly-page__grids-row">
              <CategoryTotalsGrid categoryTotals={categoryTotals} categoryTotalsSum={categoryTotalsSum} />
              <CardsGrid
                cardStatements={cardStatements}
                banks={banks}
                adjustmentTotal={adjustmentTotal}
                statementActionError={listActionError}
                statementActionWarning={listActionWarning}
                markPaidSources={markPaidSources}
                setMarkPaidSource={setMarkPaidSource}
                markStatementPaid={markStatementPaid}
                unmarkStatementPaid={confirmAndUnmarkStatementPaid}
              />
            </div>
            <div className="monthly-page__grids-row">
              <BanksGrid bankTotals={bankTotals} bankTotalsSum={bankTotalsSum} roundUpTotalsSum={roundUpTotalsSum} />
              <IncomingGrid
                incomeTotals={incomeTotals}
                totalIncoming={totalIncoming}
                titheSummary={titheSummary}
                carryForwardUpdating={carryForwardUpdating}
                onToggleCarryForward={updateCarryForwardInclusion}
                carryForwardActionError={listActionError}
                carryForwardActionWarning={listActionWarning}
              />
            </div>
          </div>
          )}

          {activeTab === 'expense' && (
            <>
              <BanksGrid bankTotals={bankTotals} bankTotalsSum={bankTotalsSum} roundUpTotalsSum={roundUpTotalsSum} />

              {expenseFormElement}

              <ExpensesSection
                expenses={expenses}
                onEdit={showEditForm}
                onDelete={confirmAndDeleteExpense}
                onNewExpense={() => showCreateForm('bank')}
              />
            </>
          )}

          {activeTab === 'card' && (
            <>
              <CardsGrid
                cardStatements={cardStatements}
                banks={banks}
                adjustmentTotal={adjustmentTotal}
                markPaidSources={markPaidSources}
                setMarkPaidSource={setMarkPaidSource}
                markStatementPaid={markStatementPaid}
                unmarkStatementPaid={confirmAndUnmarkStatementPaid}
                creditCards={creditCardsData.creditCards}
                updatingCardId={creditCardsData.updatingCardId}
                updateError={creditCardsData.error ?? creditCardsData.updateError}
                onUpdateCreditCard={creditCardsData.updateCreditCard}
              />

              {expenseFormElement}

              <ExpensesSection
                expenses={unpaidCardCharges}
                onEdit={showEditForm}
                onDelete={confirmAndDeleteExpense}
                onNewExpense={() => showCreateForm('card')}
              />
            </>
          )}

          {activeTab === 'incoming' && (
            <>
              <IncomingGrid
                incomeTotals={incomeTotals}
                totalIncoming={totalIncoming}
                titheSummary={titheSummary}
                carryForwardUpdating={carryForwardUpdating}
                onToggleCarryForward={updateCarryForwardInclusion}
                carryForwardActionError={listActionError}
                carryForwardActionWarning={listActionWarning}
              />

              {isIncomeFormOpen && (
                <IncomeForm
                  isEditing={isIncomeEditing}
                  date={incomeDate}
                  incomeSource={incomeSource}
                  grossValue={incomeGrossValue}
                  netValue={incomeNetValue}
                  bank={incomeBank}
                  description={incomeDescription}
                  splitToReserve={incomeSplitToReserve === 'true'}
                  banks={banks}
                  incomeSources={incomeSources}
                  isSaving={isSavingIncome}
                  saveError={saveIncomeError}
                  saveErrorFields={saveIncomeErrorFields}
                  onFieldChange={setIncomeField}
                  onSave={submitIncome}
                  onCancel={cancelIncomeForm}
                />
              )}

              <IncomeSection
                incomes={incomes}
                onEdit={showEditIncomeForm}
                onDelete={confirmAndDeleteIncome}
                onNewIncome={showCreateIncomeForm}
                splitConfirmationMessage={splitConfirmationMessage}
              />
            </>
          )}

          {activeTab === 'bank' && bankOperations.isLoading && <LoadingState />}

          {activeTab === 'bank' && !bankOperations.isLoading && bankOperations.error && (
            <ErrorState message={bankOperations.error} onRetry={bankOperations.retry} />
          )}

          {activeTab === 'bank' && !bankOperations.isLoading && !bankOperations.error && (
            <>
              <BanksGrid bankTotals={bankTotals} bankTotalsSum={bankTotalsSum} roundUpTotalsSum={roundUpTotalsSum} />

              {transferForm.isOpen && (
                <TransferForm
                  isEditing={transferForm.isEditing}
                  date={transferForm.date}
                  sourceBank={transferForm.sourceBank}
                  destinationBank={transferForm.destinationBank}
                  amount={transferForm.amount}
                  note={transferForm.note}
                  banks={banks}
                  isSaving={transferForm.isSaving}
                  saveError={transferForm.saveError}
                  saveErrorFields={transferForm.saveErrorFields}
                  onFieldChange={transferForm.setField}
                  onSave={transferForm.submit}
                  onCancel={transferForm.cancel}
                />
              )}
              {adjustmentForm.isOpen && (
                <BalanceAdjustmentForm
                  isEditing={adjustmentForm.isEditing}
                  bankName={adjustmentForm.bankName}
                  bankDisplayName={adjustmentForm.bankDisplayName}
                  banks={banks}
                  currentBalance={adjustmentForm.currentBalance}
                  date={adjustmentForm.date}
                  targetBalance={adjustmentForm.targetBalance}
                  note={adjustmentForm.note}
                  isSaving={adjustmentForm.isSaving}
                  saveError={adjustmentForm.saveError}
                  saveErrorFields={adjustmentForm.saveErrorFields}
                  savedDelta={adjustmentForm.savedDelta}
                  onFieldChange={adjustmentForm.setField}
                  onSave={adjustmentForm.submit}
                  onCancel={adjustmentForm.cancel}
                />
              )}

              <BankOperationsSection
                operations={bankOperations.operations}
                onNewTransfer={() => transferForm.openCreateForm()}
                onNewBalanceCorrection={() => adjustmentForm.openCreateForm()}
                onEditTransfer={transferForm.openEditForm}
                onEditAdjustment={adjustmentForm.openEditForm}
                onDeleteTransfer={confirmAndDeleteTransfer}
                onDeleteAdjustment={confirmAndDeleteAdjustment}
              />
            </>
          )}
        </div>
      )}
    </div>
  )
}
