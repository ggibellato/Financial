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
import './MonthlyPage.css'

type MonthlyTabId = 'summary' | 'expense' | 'card' | 'incoming' | 'bank'

const MONTHLY_TABS: { id: MonthlyTabId; label: string }[] = [
  { id: 'summary', label: 'Summary' },
  { id: 'expense', label: 'Expense' },
  { id: 'card', label: 'Credit Card' },
  { id: 'incoming', label: 'Income' },
  { id: 'bank', label: 'Bank' },
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
    deleteExpense,
    markPaidSources,
    setMarkPaidSource,
    markStatementPaid,
    unmarkStatementPaid,
    incomes,
    incomeTotals,
    totalIncoming,
    titheSummary,
    deleteIncome,
    listActionError,
    listActionWarning,
  } = useMonthly()

  // Confirmation belongs to the caller, not to the data hook. A hook that calls window.confirm
  // can only be tested by stubbing a browser global, and it decides for every caller that a
  // prompt is wanted at all. This page already owned its confirmations for other actions.
  const confirmAndDeleteExpense = (id: string) => {
    if (window.confirm('Delete this expense?')) deleteExpense(id)
  }

  const confirmAndDeleteIncome = (id: string) => {
    if (window.confirm('Delete this income entry?')) deleteIncome(id)
  }

  const confirmAndUnmarkStatementPaid = (id: string) => {
    if (window.confirm('Unmark this statement as paid? Its settled charges revert to unsettled.')) {
      unmarkStatementPaid(id)
    }
  }

  const confirmAndDeleteTransfer = (id: string) => {
    if (window.confirm('Delete this transfer?')) bankOperations.deleteTransfer(id)
  }

  const confirmAndDeleteAdjustment = (bankId: string, id: string) => {
    if (window.confirm('Delete this balance adjustment?')) bankOperations.deleteAdjustment(bankId, id)
  }

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
    showCreateForm,
    showEditForm,
    cancelForm,
    setField,
    submit,
  } = useExpenseForm(banks, categories, retry)

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
    splitConfirmationMessage,
    showCreateIncomeForm,
    showEditIncomeForm,
    cancelIncomeForm,
    setIncomeField,
    submitIncome,
  } = useIncomeForm(incomeSources, retry)

  const bankOperations = useBankOperations(year, month, banks, retry)
  const creditCardsData = useCreditCards()
  const activeCreditCards = creditCardsData.creditCards.filter((c) => c.isActive)
  const activeCategories = categories.filter((c) => c.active)
  const transferForm = useTransferForm(banks, () => {
    retry()
    bankOperations.retry()
  })
  const adjustmentForm = useBalanceAdjustmentForm(bankTotals, () => {
    retry()
    bankOperations.retry()
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

      <div className="monthly-page__tabs">
        {MONTHLY_TABS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            className={`monthly-page__tab${activeTab === tab.id ? ' monthly-page__tab--active' : ''}`}
            onClick={() => handleTabClick(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

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
              <IncomingGrid incomeTotals={incomeTotals} totalIncoming={totalIncoming} titheSummary={titheSummary} />
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
              <IncomingGrid incomeTotals={incomeTotals} totalIncoming={totalIncoming} titheSummary={titheSummary} />

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
                  saveErrorField={transferForm.saveErrorField}
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
                  saveErrorField={adjustmentForm.saveErrorField}
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
