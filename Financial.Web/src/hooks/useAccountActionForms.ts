import { useEffect, useMemo, useRef } from 'react'
import type { AccountDetailDto, CreditCardDto } from '../api/types'
import type { AccountFormOptions } from './useAccountFormOptions'
import { useDiscardPrompt } from './useDiscardPrompt'
import { useExpenseForm } from './useExpenseForm'
import { useIncomeForm } from './useIncomeForm'

export const NO_CREDIT_CARDS: CreditCardDto[] = []

export function useAccountActionForms(
  account: AccountDetailDto | null,
  options: AccountFormOptions,
  onSaved: (message: string) => void,
) {
  const accountId = account?.id
  const accountName = account?.name
  const lockedBank = useMemo(
    () => (accountId && accountName ? { id: accountId, name: accountName } : undefined),
    [accountId, accountName],
  )
  const settings = { lockedBank, keepOpenAfterSave: true }

  const expenseForm = useExpenseForm(options.banks, options.categories, NO_CREDIT_CARDS, () => onSaved('Expense added'), settings)
  const incomeForm = useIncomeForm(options.incomeSources, () => onSaved('Income added'), settings)

  const activeAction = expenseForm.isOpen ? 'AddExpense' : incomeForm.isIncomeFormOpen ? 'AddIncome' : null
  const isDirty = expenseForm.isDirty || incomeForm.isIncomeDirty
  const discardPrompt = useDiscardPrompt(isDirty)
  const openerRef = useRef<HTMLButtonElement | null>(null)
  const formAreaRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (activeAction) formAreaRef.current?.querySelector<HTMLElement>('input, select, textarea')?.focus()
  }, [activeAction])

  const closeForms = () => {
    if (expenseForm.isOpen) expenseForm.cancelForm()
    if (incomeForm.isIncomeFormOpen) incomeForm.cancelIncomeForm()
  }

  const openAction = (action: string, button: HTMLButtonElement) => {
    if (action === activeAction) return
    discardPrompt.confirmDiscard(() => {
      closeForms()
      openerRef.current = button
      if (action === 'AddExpense') expenseForm.showCreateForm('bank')
      if (action === 'AddIncome') incomeForm.showCreateIncomeForm()
    })
  }

  const cancelActiveForm = () => {
    closeForms()
    openerRef.current?.focus()
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
    activeAction,
    discardPrompt,
    formAreaRef,
    openAction,
    cancelActiveForm,
    guardMonthChange,
    guardAccountChange,
  }
}
