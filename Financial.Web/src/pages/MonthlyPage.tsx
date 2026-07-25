import { useState } from 'react'
import BanksGrid from '../components/BanksGrid'
import CardsGrid from '../components/CardsGrid'
import CategoryTotalsGrid from '../components/CategoryTotalsGrid'
import ErrorState from '../components/ErrorState'
import ExpenseForm, { type ExpenseFormField } from '../components/ExpenseForm'
import ExpensesSection from '../components/ExpensesSection'
import IncomeSection from '../components/IncomeSection'
import IncomingGrid from '../components/IncomingGrid'
import LoadingState from '../components/LoadingState'
import {
  useMonthly,
  INCOME_SOURCES_WITH_GROSS_VALUE,
  type CreateFormField,
  type CreateIncomeField,
  type EditField,
  type EditIncomeField,
} from '../hooks/useMonthly'
import type { BankDto } from '../api/types'
import './MonthlyPage.css'

type MonthlyTabId = 'summary' | 'expense' | 'incoming'

const MONTHLY_TABS: { id: MonthlyTabId; label: string }[] = [
  { id: 'summary', label: 'Summary' },
  { id: 'expense', label: 'Expense' },
  { id: 'incoming', label: 'Incoming' },
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

const INCOME_SOURCES = ['Gleison', 'Ariana', 'Lottery', 'DividendoJuros']

type IncomeFormField = 'date' | 'incomeSource' | 'grossValue' | 'netValue' | 'bank'

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

interface IncomeFormProps {
  isEditing: boolean
  date: string
  incomeSource: string
  grossValue: string
  netValue: string
  bank: string
  banks: BankDto[]
  isSaving: boolean
  saveError: string | null
  onFieldChange: (field: IncomeFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

function IncomeForm({
  isEditing,
  date,
  incomeSource,
  grossValue,
  netValue,
  bank,
  banks,
  isSaving,
  saveError,
  onFieldChange,
  onSave,
  onCancel,
}: IncomeFormProps) {
  const showGrossValueField = INCOME_SOURCES_WITH_GROSS_VALUE.includes(incomeSource)
  return (
    <div className="monthly-page__form-panel">
      <p className="monthly-page__form-title">{isEditing ? 'Edit Income' : 'New Income'}</p>
      <div className="monthly-page__form">
        <div className="monthly-page__form-field">
          <label htmlFor="income-date">Date</label>
          <input
            id="income-date"
            type="date"
            value={date}
            onChange={(e) => onFieldChange('date', e.target.value)}
          />
        </div>
        <div className="monthly-page__form-field">
          <label htmlFor="income-source">Source</label>
          <select
            id="income-source"
            value={incomeSource}
            onChange={(e) => onFieldChange('incomeSource', e.target.value)}
          >
            {INCOME_SOURCES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>
        {showGrossValueField && (
          <div className="monthly-page__form-field">
            <label htmlFor="income-gross-value">Gross Value</label>
            <input
              id="income-gross-value"
              type="number"
              step="0.01"
              value={grossValue}
              onChange={(e) => onFieldChange('grossValue', e.target.value)}
            />
          </div>
        )}
        <div className="monthly-page__form-field">
          <label htmlFor="income-net-value">Net Value</label>
          <input
            id="income-net-value"
            type="number"
            step="0.01"
            value={netValue}
            onChange={(e) => onFieldChange('netValue', e.target.value)}
          />
        </div>
        <div className="monthly-page__form-field">
          <label htmlFor="income-bank">Bank</label>
          <select id="income-bank" value={bank} onChange={(e) => onFieldChange('bank', e.target.value)}>
            {banks.map((b) => (
              <option key={b.name} value={b.name}>
                {b.name}
              </option>
            ))}
          </select>
        </div>
      </div>
      <div className="monthly-page__form-actions">
        <button className="monthly-page__submit-btn" type="button" disabled={isSaving} onClick={onSave}>
          {isSaving ? 'Saving...' : isEditing ? 'Save' : 'Add Income'}
        </button>
        <button className="monthly-page__cancel-btn" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
      {saveError && <p className="monthly-page__error">{saveError}</p>}
    </div>
  )
}

export default function MonthlyPage() {
  const {
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

  const [activeTab, setActiveTab] = useState<MonthlyTabId>('summary')

  const isEditing = editingId !== null
  const isFormVisible = isCreateFormOpen || isEditing
  const isIncomeEditing = editingIncomeId !== null
  const isIncomeFormVisible = isIncomeCreateFormOpen || isIncomeEditing

  const handleTabClick = (tabId: MonthlyTabId) => {
    if (activeTab === 'expense' && isFormVisible) {
      if (isEditing) cancelEdit()
      else cancelCreateForm()
    }
    if (activeTab === 'incoming' && isIncomeFormVisible) {
      if (isIncomeEditing) cancelEditIncome()
      else cancelCreateIncomeForm()
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
        </div>
      )}
    </div>
  )
}
