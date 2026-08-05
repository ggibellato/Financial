import { useState } from 'react'
import BalanceAdjustmentForm from '../components/BalanceAdjustmentForm'
import BankOperationsSection from '../components/BankOperationsSection'
import BanksGrid from '../components/BanksGrid'
import CardsGrid from '../components/CardsGrid'
import CategoryTotalsGrid from '../components/CategoryTotalsGrid'
import ErrorState from '../components/ErrorState'
import ExpenseForm, { type ExpenseFormField } from '../components/ExpenseForm'
import ExpensesSection from '../components/ExpensesSection'
import IncomeForm, { type IncomeFormField } from '../components/IncomeForm'
import IncomeSection from '../components/IncomeSection'
import IncomingGrid from '../components/IncomingGrid'
import LoadingState from '../components/LoadingState'
import TransferForm from '../components/TransferForm'
import { useBalanceAdjustmentForm } from '../hooks/useBalanceAdjustmentForm'
import { useBankOperations } from '../hooks/useBankOperations'
import {
  useMonthly,
  type CreateFormField,
  type CreateIncomeField,
  type EditField,
  type EditIncomeField,
} from '../hooks/useMonthly'
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

const CREATE_FIELD_BY_FORM_FIELD: Record<ExpenseFormField, CreateFormField> = {
  date: 'createDate',
  description: 'createDescription',
  value: 'createValue',
  category: 'createCategory',
  paymentSource: 'createPaymentSource',
  cardTag: 'createCardTag',
  roundUpAmount: 'createRoundUpAmount',
}

const EDIT_FIELD_BY_FORM_FIELD: Record<ExpenseFormField, EditField> = {
  date: 'editDate',
  description: 'editDescription',
  value: 'editValue',
  category: 'editCategory',
  paymentSource: 'editPaymentSource',
  cardTag: 'editCardTag',
  roundUpAmount: 'editRoundUpAmount',
}

const CREATE_INCOME_FIELD_BY_FORM_FIELD: Record<IncomeFormField, CreateIncomeField> = {
  date: 'createIncomeDate',
  incomeSource: 'createIncomeSource',
  grossValue: 'createIncomeGrossValue',
  netValue: 'createIncomeNetValue',
  bank: 'createIncomeBank',
}

const EDIT_INCOME_FIELD_BY_FORM_FIELD: Record<IncomeFormField, EditIncomeField> = {
  date: 'editIncomeDate',
  incomeSource: 'editIncomeSource',
  grossValue: 'editIncomeGrossValue',
  netValue: 'editIncomeNetValue',
  bank: 'editIncomeBank',
}

export default function MonthlyPage() {
  const {
    year,
    month,
    monthInputValue,
    setMonthInputValue,
    expenses,
    categoryTotals,
    categoryTotalsSum,
    cardStatements,
    banks,
    adjustmentTotal,
    bankTotals,
    bankTotalsSum,
    roundUpTotalsSum,
    isLoading,
    error,
    retry,
    isCreateFormOpen,
    createDate,
    createDescription,
    createValue,
    createCategory,
    createPaymentSource,
    createCardTag,
    createRoundUpAmount,
    createPaymentMode,
    setCreatePaymentMode,
    isCreating,
    createError,
    showCreateForm,
    cancelCreateForm,
    setCreateField,
    submitCreate,
    editingId,
    editDate,
    editDescription,
    editValue,
    editCategory,
    editPaymentSource,
    editCardTag,
    editRoundUpAmount,
    editPaymentMode,
    editIsSettled,
    setEditPaymentMode,
    isSaving,
    saveError,
    setEditField,
    showEditForm,
    cancelEdit,
    saveEdit,
    deleteExpense,
    markPaidSources,
    setMarkPaidSource,
    markStatementPaid,
    unmarkStatementPaid,
    incomes,
    isIncomeCreateFormOpen,
    createIncomeDate,
    createIncomeSource,
    createIncomeGrossValue,
    createIncomeNetValue,
    createIncomeBank,
    isCreatingIncome,
    createIncomeError,
    showCreateIncomeForm,
    cancelCreateIncomeForm,
    setCreateIncomeField,
    submitCreateIncome,
    editingIncomeId,
    editIncomeDate,
    editIncomeSource,
    editIncomeGrossValue,
    editIncomeNetValue,
    editIncomeBank,
    isSavingIncome,
    saveIncomeError,
    setEditIncomeField,
    showEditIncomeForm,
    cancelEditIncome,
    saveEditIncome,
    deleteIncome,
    incomeTotals,
    totalIncoming,
    titheSummary,
  } = useMonthly()

  const bankOperations = useBankOperations(year, month, banks, retry)
  const transferForm = useTransferForm(banks, () => {
    retry()
    bankOperations.retry()
  })
  const adjustmentForm = useBalanceAdjustmentForm(bankTotals, () => {
    retry()
    bankOperations.retry()
  })

  const [activeTab, setActiveTab] = useState<MonthlyTabId>('summary')

  const isEditing = editingId !== null
  const isFormVisible = isCreateFormOpen || isEditing
  const isIncomeEditing = editingIncomeId !== null
  const isIncomeFormVisible = isIncomeCreateFormOpen || isIncomeEditing
  const isBankFormVisible = transferForm.isOpen || adjustmentForm.isOpen

  const handleTabClick = (tabId: MonthlyTabId) => {
    if (activeTab === 'expense' && isFormVisible) {
      if (isEditing) cancelEdit()
      else cancelCreateForm()
    }
    if (activeTab === 'incoming' && isIncomeFormVisible) {
      if (isIncomeEditing) cancelEditIncome()
      else cancelCreateIncomeForm()
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
                markPaidSources={markPaidSources}
                setMarkPaidSource={setMarkPaidSource}
                markStatementPaid={markStatementPaid}
                unmarkStatementPaid={unmarkStatementPaid}
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
              {isFormVisible && (
                <ExpenseForm
                  isEditing={isEditing}
                  date={isEditing ? editDate : createDate}
                  description={isEditing ? editDescription : createDescription}
                  value={isEditing ? editValue : createValue}
                  category={isEditing ? editCategory : createCategory}
                  paymentSource={isEditing ? editPaymentSource : createPaymentSource}
                  cardTag={isEditing ? editCardTag : createCardTag}
                  roundUpAmount={isEditing ? editRoundUpAmount : createRoundUpAmount}
                  paymentMode={isEditing ? editPaymentMode : createPaymentMode}
                  banks={banks}
                  isSettled={isEditing && editIsSettled}
                  isSaving={isEditing ? isSaving : isCreating}
                  saveError={isEditing ? saveError : createError}
                  onFieldChange={(field, value) =>
                    isEditing
                      ? setEditField(EDIT_FIELD_BY_FORM_FIELD[field], value)
                      : setCreateField(CREATE_FIELD_BY_FORM_FIELD[field], value)
                  }
                  onModeChange={isEditing ? setEditPaymentMode : setCreatePaymentMode}
                  onSave={isEditing ? saveEdit : submitCreate}
                  onCancel={isEditing ? cancelEdit : cancelCreateForm}
                />
              )}

              <ExpensesSection
                expenses={expenses}
                onEdit={showEditForm}
                onDelete={deleteExpense}
                onNewExpense={showCreateForm}
              />
            </>
          )}

          {activeTab === 'card' && (
            <CardsGrid
              cardStatements={cardStatements}
              banks={banks}
              adjustmentTotal={adjustmentTotal}
              markPaidSources={markPaidSources}
              setMarkPaidSource={setMarkPaidSource}
              markStatementPaid={markStatementPaid}
              unmarkStatementPaid={unmarkStatementPaid}
            />
          )}

          {activeTab === 'incoming' && (
            <>
              {isIncomeFormVisible && (
                <IncomeForm
                  isEditing={isIncomeEditing}
                  date={isIncomeEditing ? editIncomeDate : createIncomeDate}
                  incomeSource={isIncomeEditing ? editIncomeSource : createIncomeSource}
                  grossValue={isIncomeEditing ? editIncomeGrossValue : createIncomeGrossValue}
                  netValue={isIncomeEditing ? editIncomeNetValue : createIncomeNetValue}
                  bank={isIncomeEditing ? editIncomeBank : createIncomeBank}
                  banks={banks}
                  isSaving={isIncomeEditing ? isSavingIncome : isCreatingIncome}
                  saveError={isIncomeEditing ? saveIncomeError : createIncomeError}
                  onFieldChange={(field, value) =>
                    isIncomeEditing
                      ? setEditIncomeField(EDIT_INCOME_FIELD_BY_FORM_FIELD[field], value)
                      : setCreateIncomeField(CREATE_INCOME_FIELD_BY_FORM_FIELD[field], value)
                  }
                  onSave={isIncomeEditing ? saveEditIncome : submitCreateIncome}
                  onCancel={isIncomeEditing ? cancelEditIncome : cancelCreateIncomeForm}
                />
              )}

              <IncomeSection
                incomes={incomes}
                onEdit={showEditIncomeForm}
                onDelete={deleteIncome}
                onNewIncome={showCreateIncomeForm}
              />
            </>
          )}

          {activeTab === 'bank' && bankOperations.isLoading && <LoadingState />}

          {activeTab === 'bank' && !bankOperations.isLoading && bankOperations.error && (
            <ErrorState message={bankOperations.error} onRetry={bankOperations.retry} />
          )}

          {activeTab === 'bank' && !bankOperations.isLoading && !bankOperations.error && (
            <>
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
                bankFilter={bankOperations.bankFilter}
                banks={banks}
                onBankFilterChange={bankOperations.setBankFilter}
                onNewTransfer={() => transferForm.openCreateForm()}
                onNewBalanceCorrection={() => adjustmentForm.openCreateForm()}
                onEditTransfer={transferForm.openEditForm}
                onEditAdjustment={adjustmentForm.openEditForm}
                onDeleteTransfer={bankOperations.deleteTransfer}
                onDeleteAdjustment={bankOperations.deleteAdjustment}
              />
            </>
          )}
        </div>
      )}
    </div>
  )
}
