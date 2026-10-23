import { useEffect, useMemo, useRef, useState } from 'react'
import type { AccountDetailDto, CreditCardDto, IncomeDto } from '../api/types'
import type { AccountFormAction } from '../components/accountActions'
import type { AccountFormOptions } from './useAccountFormOptions'
import { useDiscardPrompt } from './useDiscardPrompt'
import { useExpenseForm } from './useExpenseForm'
import { useIncomeForm } from './useIncomeForm'

export const NO_CREDIT_CARDS: CreditCardDto[] = []

const FIRST_FIELD_SELECTOR = 'input, select, textarea'
const NEXT_ENTRY_FIELD_SELECTOR = 'input[name="description"]'

function incomeSavedMessage(saved: IncomeDto): string {
  return saved.splitToReserve ? 'Income added and split to reserve' : 'Income added'
}

export function useAccountActionForms(
  routeAccountId: string,
  account: AccountDetailDto | null,
  options: AccountFormOptions,
  onSaved: (message: string) => void,
) {
  const isAccountCurrent = account !== null && account.id.toLowerCase() === routeAccountId.toLowerCase()
  const accountId = isAccountCurrent ? account.id : undefined
  const accountName = isAccountCurrent ? account.name : undefined
  const lockedBank = useMemo(
    () => (accountId && accountName ? { id: accountId, name: accountName } : undefined),
    [accountId, accountName],
  )
  const isNextEntryFocusPending = useRef(false)
  const settings = { lockedBank, keepOpenAfterSave: true }

  const expenseForm = useExpenseForm(
    options.banks,
    options.categories,
    NO_CREDIT_CARDS,
    () => {
      onSaved('Expense added')
      isNextEntryFocusPending.current = true
    },
    settings,
  )
  const incomeForm = useIncomeForm(
    options.incomeSources,
    (saved) => {
      onSaved(incomeSavedMessage(saved))
      isNextEntryFocusPending.current = true
    },
    settings,
  )

  const openers: Record<AccountFormAction, () => void> = {
    AddExpense: () => expenseForm.showCreateForm('bank'),
    AddIncome: () => incomeForm.showCreateIncomeForm(),
  }

  const activeAction: AccountFormAction | null = expenseForm.isOpen
    ? 'AddExpense'
    : incomeForm.isIncomeFormOpen
      ? 'AddIncome'
      : null
  const isDirty = expenseForm.isDirty || incomeForm.isIncomeDirty
  const discardPrompt = useDiscardPrompt(isDirty)
  const openerRef = useRef<HTMLButtonElement | null>(null)
  const formAreaRef = useRef<HTMLDivElement | null>(null)

  const closeForms = () => {
    if (expenseForm.isOpen) expenseForm.cancelForm()
    if (incomeForm.isIncomeFormOpen) incomeForm.cancelIncomeForm()
  }

  const [formsAccountId, setFormsAccountId] = useState(routeAccountId)
  if (formsAccountId !== routeAccountId) {
    setFormsAccountId(routeAccountId)
    closeForms()
  }

  useEffect(() => {
    if (activeAction) formAreaRef.current?.querySelector<HTMLElement>(FIRST_FIELD_SELECTOR)?.focus()
  }, [activeAction])

  useEffect(() => {
    if (!isNextEntryFocusPending.current) return
    isNextEntryFocusPending.current = false
    formAreaRef.current?.querySelector<HTMLElement>(NEXT_ENTRY_FIELD_SELECTOR)?.focus()
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

  const guardMonthChange = (changeMonth: () => void) =>
    discardPrompt.confirmDiscard(() => {
      if (isDirty) closeForms()
      changeMonth()
    })

  const guardAccountChange = (changeAccount: () => void) =>
    discardPrompt.confirmDiscard(() => {
      closeForms()
      changeAccount()
    })

  return {
    expenseForm,
    incomeForm,
    lockedBank,
    isAccountCurrent,
    activeAction,
    discardPrompt,
    formAreaRef,
    openAction,
    closeActiveForm,
    guardMonthChange,
    guardAccountChange,
  }
}
