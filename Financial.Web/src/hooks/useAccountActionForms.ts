import { useEffect, useMemo, useRef, useState } from 'react'
import type { AccountDetailDto, AccountTransactionDto, CreditCardDto, IncomeDto } from '../api/types'
import type { AccountFormAction } from '../components/accountActions'
import type { ViewedMonth } from '../utils/createFormDefaults'
import type { AccountFormOptions } from './useAccountFormOptions'
import { useAccountTransferForm } from './useAccountTransferForm'
import { useBalanceAdjustmentForm } from './useBalanceAdjustmentForm'
import { useDiscardPrompt } from './useDiscardPrompt'
import { useExpenseForm } from './useExpenseForm'
import { useIncomeForm } from './useIncomeForm'

export const NO_CREDIT_CARDS: CreditCardDto[] = []

const FIRST_FIELD_SELECTOR = 'input, select, textarea'
const NEXT_ENTRY_FIELD_SELECTOR = 'input[name="description"]'

function incomeSavedMessage(saved: IncomeDto): string {
  return saved.splitToReserve ? 'Income added and split to reserve' : 'Income added'
}

export function rowKey(row: AccountTransactionDto): string {
  return `${row.kind}-${row.id}`
}

export function useAccountActionForms(
  routeAccountId: string,
  account: AccountDetailDto | null,
  options: AccountFormOptions,
  viewedMonth: ViewedMonth,
  onSaved: (message: string) => void,
) {
  const isAccountCurrent = account !== null && account.id.toLowerCase() === routeAccountId.toLowerCase()
  const currentAccount = isAccountCurrent ? account : null
  const accountId = currentAccount?.id
  const accountName = currentAccount?.name
  const accountCurrency = currentAccount?.currency
  const lockedBank = useMemo(
    () => (accountId && accountName ? { id: accountId, name: accountName } : undefined),
    [accountId, accountName],
  )
  const transferAccount = useMemo(
    () => (accountId && accountName && accountCurrency ? { id: accountId, name: accountName, currency: accountCurrency } : undefined),
    [accountId, accountName, accountCurrency],
  )
  const balances = currentAccount ? [{ bankId: currentAccount.id, bank: currentAccount.name, balance: currentAccount.balance }] : []
  const isNextEntryFocusPending = useRef(false)
  const settings = { lockedBank, keepOpenAfterSave: true, viewedMonth }

  const saved = (message: string) => {
    onSaved(message)
    isNextEntryFocusPending.current = true
  }

  const expenseForm = useExpenseForm(
    options.banks,
    options.categories,
    NO_CREDIT_CARDS,
    (isEdit) => saved(isEdit ? 'Expense updated' : 'Expense added'),
    settings,
  )
  const incomeForm = useIncomeForm(
    options.incomeSources,
    (income, isEdit) => saved(isEdit ? 'Income updated' : incomeSavedMessage(income)),
    settings,
  )
  const adjustmentForm = useBalanceAdjustmentForm(balances, () => saved('Balance corrected'), settings)
  const transferForm = useAccountTransferForm(
    transferAccount,
    options,
    (isEdit) => saved(isEdit ? 'Transfer updated' : 'Transfer saved'),
    settings,
  )

  const openers: Record<AccountFormAction, () => void> = {
    AddExpense: () => expenseForm.showCreateForm('bank'),
    AddIncome: () => incomeForm.showCreateIncomeForm(),
    TransferTo: () => transferForm.openCreateForm('to'),
    TransferFrom: () => transferForm.openCreateForm('from'),
    Reconcile: () => adjustmentForm.openCreateForm(),
  }

  const createAction = (): AccountFormAction | null => {
    if (expenseForm.isOpen && !expenseForm.isEditing) return 'AddExpense'
    if (incomeForm.isIncomeFormOpen && !incomeForm.isIncomeEditing) return 'AddIncome'
    if (transferForm.isOpen && !transferForm.isEditing) return transferForm.direction === 'to' ? 'TransferTo' : 'TransferFrom'
    if (adjustmentForm.isOpen && !adjustmentForm.isEditing) return 'Reconcile'
    return null
  }
  const activeAction = createAction()
  const isAnyFormOpen = expenseForm.isOpen || incomeForm.isIncomeFormOpen || transferForm.isOpen || adjustmentForm.isOpen
  const [editedRowKey, setEditedRowKey] = useState<string | null>(null)
  const activeFormKey = activeAction ?? (isAnyFormOpen ? `edit:${editedRowKey}` : null)

  const isDirty = expenseForm.isDirty || incomeForm.isIncomeDirty || transferForm.isDirty || adjustmentForm.isDirty
  const discardPrompt = useDiscardPrompt(isDirty)
  const openerRef = useRef<HTMLButtonElement | null>(null)
  const formAreaRef = useRef<HTMLDivElement | null>(null)

  const closeForms = () => {
    if (expenseForm.isOpen) expenseForm.cancelForm()
    if (incomeForm.isIncomeFormOpen) incomeForm.cancelIncomeForm()
    if (transferForm.isOpen) transferForm.cancel()
    if (adjustmentForm.isOpen) adjustmentForm.cancel()
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

  const closeFormsOf = (row: AccountTransactionDto) => {
    if (activeAction === null && isAnyFormOpen && editedRowKey === rowKey(row)) closeForms()
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
    lockedBank,
    isAccountCurrent,
    activeAction,
    discardPrompt,
    formAreaRef,
    openAction,
    openRowEdit,
    closeActiveForm,
    closeFormsOf,
    guardChange,
  }
}
