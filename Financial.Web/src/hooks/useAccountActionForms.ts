import { useEffect, useMemo, useRef, useState } from 'react'
import type { AccountDetailDto, AccountTransactionDto, CreditCardDto, IncomeDto } from '../api/types'
import { rowKey, type AccountFormAction } from '../components/accountActions'
import type { AccountFormOptions } from './useAccountFormOptions'
import { useAccountTransferForm } from './useAccountTransferForm'
import { useBalanceAdjustmentForm } from './useBalanceAdjustmentForm'
import { useDiscardPrompt } from './useDiscardPrompt'
import { useExpenseForm } from './useExpenseForm'
import { useIncomeForm } from './useIncomeForm'
import { useMoveChargeForm } from './useMoveChargeForm'
import { usePayRecurringBill } from './usePayRecurringBill'
import type { YearMonth } from './useMonthSelection'

export const NO_CREDIT_CARDS: CreditCardDto[] = []

const FIRST_FIELD_SELECTOR = 'input, select, textarea'
const NEXT_ENTRY_FIELD_SELECTOR = 'input[name="description"]'

function incomeSavedMessage(saved: IncomeDto): string {
  return saved.splitToReserve ? 'Income added and split to reserve' : 'Income added'
}

export interface ScreenForm {
  isDirty: boolean
  close: () => void
}

const NO_SCREEN_FORM: ScreenForm = { isDirty: false, close: () => {} }

export function useAccountActionForms(
  routeAccountId: string,
  account: AccountDetailDto | null,
  options: AccountFormOptions,
  viewedMonth: YearMonth,
  onSaved: (message: string) => void,
  screenForm: ScreenForm = NO_SCREEN_FORM,
) {
  const isAccountCurrent = account !== null && account.id.toLowerCase() === routeAccountId.toLowerCase()
  const currentAccount = isAccountCurrent ? account : null
  const accountId = currentAccount?.id
  const accountName = currentAccount?.name
  const accountCurrency = currentAccount?.currency
  const isCard = currentAccount?.hasStatements ?? false
  const lockedBank = useMemo(
    () =>
      !isCard && accountId && accountName && accountCurrency ? { id: accountId, name: accountName, currency: accountCurrency } : undefined,
    [isCard, accountId, accountName, accountCurrency],
  )
  const lockedCard = useMemo(
    () => (isCard && accountId && accountName ? { id: accountId, name: accountName } : undefined),
    [isCard, accountId, accountName],
  )
  const balances = currentAccount ? [{ bankId: currentAccount.id, bank: currentAccount.name, balance: currentAccount.balance }] : []
  const isNextEntryFocusPending = useRef(false)
  const settings = { lockedBank, keepOpenAfterSave: true, viewedMonth }
  const expenseNoun = isCard ? 'Charge' : 'Expense'

  const saved = (message: string) => {
    onSaved(message)
    isNextEntryFocusPending.current = true
  }

  const expenseForm = useExpenseForm(
    options.banks,
    options.categories,
    NO_CREDIT_CARDS,
    (isEdit) => saved(isEdit ? `${expenseNoun} updated` : `${expenseNoun} added`),
    { ...settings, lockedCard },
  )
  const incomeForm = useIncomeForm(
    options.incomeSources,
    options.banks,
    (income, isEdit) => saved(isEdit ? 'Income updated' : incomeSavedMessage(income)),
    settings,
  )
  const adjustmentForm = useBalanceAdjustmentForm(
    balances,
    options.banks,
    (isEdit) => saved(isEdit ? 'Balance adjustment updated' : 'Balance adjusted'),
    settings,
  )
  const transferForm = useAccountTransferForm(
    lockedBank,
    options,
    (isEdit) => saved(isEdit ? 'Transfer updated' : 'Transfer saved'),
    settings,
  )

  const payBillForm = usePayRecurringBill(lockedBank, () => saved('Bill paid'), settings)
  const moveForm = useMoveChargeForm(onSaved)

  const openers: Record<AccountFormAction, () => void> = {
    AddExpense: () => expenseForm.showCreateForm('bank'),
    AddCharge: () => expenseForm.showCreateForm('card'),
    AddIncome: () => incomeForm.showCreateIncomeForm(),
    TransferTo: () => transferForm.openCreateForm('to'),
    TransferFrom: () => transferForm.openCreateForm('from'),
    PayRecurringBill: () => payBillForm.open(),
    Reconcile: () => adjustmentForm.openCreateForm(),
  }

  const createAction = (): AccountFormAction | null => {
    if (expenseForm.isOpen && !expenseForm.isEditing) return isCard ? 'AddCharge' : 'AddExpense'
    if (incomeForm.isIncomeFormOpen && !incomeForm.isIncomeEditing) return 'AddIncome'
    if (transferForm.isOpen && !transferForm.isEditing) return transferForm.direction === 'to' ? 'TransferTo' : 'TransferFrom'
    if (adjustmentForm.isOpen && !adjustmentForm.isEditing) return 'Reconcile'
    if (payBillForm.isOpen) return 'PayRecurringBill'
    return null
  }
  const activeAction = createAction()
  const isAnyFormOpen =
    expenseForm.isOpen || incomeForm.isIncomeFormOpen || transferForm.isOpen || adjustmentForm.isOpen || moveForm.isOpen
  const [editedRowKey, setEditedRowKey] = useState<string | null>(null)
  const editingRowKey = activeAction === null && isAnyFormOpen ? editedRowKey : null
  const activeFormKey = activeAction ?? (editingRowKey && `${moveForm.isOpen ? 'move' : 'edit'}:${editingRowKey}`)

  const isDirty =
    expenseForm.isDirty ||
    incomeForm.isIncomeDirty ||
    transferForm.isDirty ||
    adjustmentForm.isDirty ||
    payBillForm.isDirty ||
    moveForm.isDirty ||
    screenForm.isDirty
  const discardPrompt = useDiscardPrompt(isDirty)
  const openerRef = useRef<HTMLButtonElement | null>(null)
  const formAreaRef = useRef<HTMLDivElement | null>(null)

  const closeForms = () => {
    if (expenseForm.isOpen) expenseForm.cancelForm()
    if (incomeForm.isIncomeFormOpen) incomeForm.cancelIncomeForm()
    if (transferForm.isOpen) transferForm.cancel()
    if (adjustmentForm.isOpen) adjustmentForm.cancel()
    if (payBillForm.isOpen) payBillForm.cancel()
    if (moveForm.isOpen) moveForm.cancel()
    screenForm.close()
  }

  const [formsAccountId, setFormsAccountId] = useState(routeAccountId)
  if (formsAccountId !== routeAccountId) {
    setFormsAccountId(routeAccountId)
    closeForms()
  }

  useEffect(() => {
    if (activeFormKey) {
      formAreaRef.current?.querySelector<HTMLElement>(FIRST_FIELD_SELECTOR)?.focus()
    } else if (document.activeElement === document.body) {
      openerRef.current?.focus()
    }
  }, [activeFormKey])

  useEffect(() => {
    if (!isNextEntryFocusPending.current) return
    isNextEntryFocusPending.current = false
    const formArea = formAreaRef.current
    const nextField =
      formArea?.querySelector<HTMLElement>(NEXT_ENTRY_FIELD_SELECTOR) ?? formArea?.querySelector<HTMLElement>('button')
    nextField?.focus()
  })

  const closeActiveForm = () => {
    closeForms()
    openerRef.current?.focus()
  }

  const openAction = (action: AccountFormAction, button: HTMLButtonElement) => {
    discardPrompt.confirmDiscard(() => {
      closeForms()
      openerRef.current = button
      if (action === activeAction) button.focus()
      else openers[action]()
    })
  }

  const openRowEdit = (row: AccountTransactionDto, button: HTMLButtonElement) => {
    discardPrompt.confirmDiscard(() => {
      closeForms()
      openerRef.current = button
      setEditedRowKey(rowKey(row))
      if (row.expense) expenseForm.showEditForm(row.expense)
      else if (row.income) incomeForm.showEditIncomeForm(row.income)
      else if (row.transfer) transferForm.openEditForm(row.transfer)
      else if (row.adjustment) adjustmentForm.openEditForm(row.adjustment)
    })
  }

  const openRowMove = (row: AccountTransactionDto, button: HTMLButtonElement) => {
    discardPrompt.confirmDiscard(() => {
      closeForms()
      openerRef.current = button
      setEditedRowKey(rowKey(row))
      if (row.expense) moveForm.open(row.expense)
    })
  }

  const closeFormsOf = (row: AccountTransactionDto) => {
    if (editingRowKey === rowKey(row)) closeForms()
  }

  const guardChange = (change: () => void) =>
    discardPrompt.confirmDiscard(() => {
      closeForms()
      change()
    })

  return {
    expenseForm,
    incomeForm,
    adjustmentForm,
    transferForm,
    payBillForm,
    moveForm,
    lockedBank,
    lockedCard,
    isAccountCurrent,
    activeAction,
    discardPrompt,
    formAreaRef,
    openAction,
    openRowEdit,
    openRowMove,
    closeActiveForm,
    closeFormsOf,
    guardChange,
  }
}
