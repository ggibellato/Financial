import { useEffect, useMemo, useRef, useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountDetailDto, AccountDto, AccountTransactionDto } from '../api/types'
import { rowKey, type AccountFormAction, type PotOperationNoun } from '../components/accountActions'
import type { AccountFormOptions } from './useAccountFormOptions'
import { useAccountTransferForm } from './useAccountTransferForm'
import { useBalanceAdjustmentForm } from './useBalanceAdjustmentForm'
import { useCardCreditRefundForm } from './useCardCreditRefundForm'
import { useConfirmedDelete } from './useConfirmedDelete'
import { useDiscardPrompt } from './useDiscardPrompt'
import { useExpenseForm } from './useExpenseForm'
import { incomeSavedMessage, useIncomeForm } from './useIncomeForm'
import { useMoveChargeForm } from './useMoveChargeForm'
import { usePayCardStatementForm } from './usePayCardStatementForm'
import { usePayRecurringBill } from './usePayRecurringBill'
import type { OperationToOpen } from './useOperationRoute'
import { usePotDepositForm } from './usePotDepositForm'
import { usePotTransactionEditForm } from './usePotTransactionEditForm'
import { usePotWithdrawalForm } from './usePotWithdrawalForm'
import { useSettlementEntryForm } from './useSettlementEntryForm'
import type { YearMonth } from './useMonthSelection'
import { deleteFailedMessage } from '../utils/formatters'

const FIRST_FIELD_SELECTOR = 'input, select, textarea'
const NEXT_ENTRY_FIELD_SELECTOR = 'input[name="description"]'
const RESTING_FOCUS_SELECTOR = '[role="tab"][aria-selected="true"], #account-screen-transactions'

export interface ScreenForm {
  isDirty: boolean
  close: () => void
}

const NO_SCREEN_FORM: ScreenForm = { isDirty: false, close: () => {} }

export interface PotsContext {
  accounts: AccountDto[]
  hasPots: boolean
}

const NO_POTS: PotsContext = { accounts: [], hasPots: false }

interface OperationDeleteTarget {
  operationId: string
  noun: PotOperationNoun
}

const OPERATION_ROW_KEY_PREFIX = 'operation:'
const operationRowKey = (operationId: string) => `${OPERATION_ROW_KEY_PREFIX}${operationId}`
const isOperationRowKey = (key: string) => key.startsWith(OPERATION_ROW_KEY_PREFIX)

const OPERATION_TITLES: Record<PotOperationNoun, string> = { deposit: 'Deposit', withdrawal: 'Withdrawal' }

export function useAccountActionForms(
  routeAccountId: string,
  account: AccountDetailDto | null,
  options: AccountFormOptions,
  viewedMonth: YearMonth,
  onSaved: (message: string) => void,
  screenForm: ScreenForm = NO_SCREEN_FORM,
  { accounts, hasPots }: PotsContext = NO_POTS,
) {
  const isAccountCurrent = account !== null && account.id.toLowerCase() === routeAccountId.toLowerCase()
  const currentAccount = isAccountCurrent ? account : null
  const accountId = currentAccount?.id
  const accountName = currentAccount?.name
  const accountCurrency = currentAccount?.currency
  const isCard = currentAccount?.hasStatements ?? false
  const currentAccountOption = useMemo(
    () => (accountId && accountName && accountCurrency ? { id: accountId, name: accountName, currency: accountCurrency } : undefined),
    [accountId, accountName, accountCurrency],
  )
  const lockedBank = isCard || hasPots ? undefined : currentAccountOption
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
    (isEdit, isRefund) => {
      const noun = isRefund ? 'Refund' : expenseNoun
      saved(isEdit ? `${noun} updated` : `${noun} added`)
    },
    { ...settings, lockedCard },
  )
  const incomeForm = useIncomeForm(
    options.incomeSources,
    options.banks,
    (_, isEdit, potsAccountName) => saved(isEdit ? 'Income updated' : hasPots && potsAccountName ? 'Deposit saved' : incomeSavedMessage(potsAccountName)),
    { ...settings, keepOpenAfterSave: !hasPots },
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
  const payStatementForm = usePayCardStatementForm(lockedBank, () => saved('Statement paid'))
  const creditRefundForm = useCardCreditRefundForm(
    isCard ? 'card' : 'bank',
    currentAccountOption,
    Math.max(0, -(currentAccount?.totalOwed ?? 0)),
    () => saved(isCard ? 'Credit refunded' : 'Card credit received'),
  )
  const moveForm = useMoveChargeForm(onSaved)
  const potTransactionForm = usePotTransactionEditForm(() => saved('Pot transaction updated'))
  const depositForm = usePotDepositForm(hasPots ? currentAccountOption : undefined, viewedMonth, () => saved('Deposit saved'))
  const withdrawalForm = usePotWithdrawalForm(accounts, options.categories, viewedMonth, () => saved('Withdrawal saved'))
  const settlementEntryForm = useSettlementEntryForm(currentAccount, viewedMonth, saved)
  const [isManagePotsOpen, setIsManagePotsOpen] = useState(false)

  const openers: Record<AccountFormAction, () => void> = {
    AddExpense: () => expenseForm.showCreateForm('bank'),
    AddCharge: () => expenseForm.showCreateForm('card'),
    AddRefund: () => expenseForm.showCreateRefundForm(),
    RefundCreditToBank: () => creditRefundForm.open(),
    ReceiveCardCredit: () => creditRefundForm.open(),
    AddIncome: () => incomeForm.showCreateIncomeForm(),
    TransferTo: () => transferForm.openCreateForm('to'),
    TransferFrom: () => transferForm.openCreateForm('from'),
    PayRecurringBill: () => payBillForm.open(),
    PayCardStatement: () => payStatementForm.open(),
    Reconcile: () => adjustmentForm.openCreateForm(),
    Deposit: () => depositForm.open(),
    Withdraw: () => withdrawalForm.openFromPotsAccount(routeAccountId),
    ManagePots: () => setIsManagePotsOpen(true),
    WithdrawFromPots: () => withdrawalForm.openFromBank(routeAccountId),
    AddEntry: () => settlementEntryForm.openAdd(),
    Settle: () => settlementEntryForm.openSettle(),
  }

  const createAction = (): AccountFormAction | null => {
    if (expenseForm.isOpen && !expenseForm.isEditing) return expenseForm.isRefund ? 'AddRefund' : isCard ? 'AddCharge' : 'AddExpense'
    if (incomeForm.isIncomeFormOpen && !incomeForm.isIncomeEditing) return hasPots ? 'Deposit' : 'AddIncome'
    if (depositForm.isOpen && !depositForm.isEditing) return 'Deposit'
    if (withdrawalForm.isOpen && !withdrawalForm.isEditing) return withdrawalForm.isBankLocked ? 'WithdrawFromPots' : 'Withdraw'
    if (transferForm.isOpen && !transferForm.isEditing) return transferForm.direction === 'to' ? 'TransferTo' : 'TransferFrom'
    if (adjustmentForm.isOpen && !adjustmentForm.isEditing) return 'Reconcile'
    if (payBillForm.isOpen) return 'PayRecurringBill'
    if (payStatementForm.isOpen) return 'PayCardStatement'
    if (creditRefundForm.isOpen) return creditRefundForm.mode === 'card' ? 'RefundCreditToBank' : 'ReceiveCardCredit'
    if (settlementEntryForm.isOpen && !settlementEntryForm.isEditing) return settlementEntryForm.isSettling ? 'Settle' : 'AddEntry'
    if (isManagePotsOpen) return 'ManagePots'
    return null
  }
  const activeAction = createAction()
  const isAnyFormOpen =
    expenseForm.isOpen ||
    incomeForm.isIncomeFormOpen ||
    transferForm.isOpen ||
    adjustmentForm.isOpen ||
    moveForm.isOpen ||
    potTransactionForm.isOpen ||
    depositForm.isOpen ||
    withdrawalForm.isOpen ||
    settlementEntryForm.isOpen
  const [editedRowKey, setEditedRowKey] = useState<string | null>(null)
  const editingRowKey = activeAction === null && isAnyFormOpen ? editedRowKey : null
  const activeFormKey = activeAction
    ? `${activeAction}${incomeForm.isIncomeFormOpen ? ':income' : ''}`
    : editingRowKey && `${moveForm.isOpen ? 'move' : 'edit'}:${editingRowKey}`

  const isDirty =
    expenseForm.isDirty ||
    incomeForm.isIncomeDirty ||
    transferForm.isDirty ||
    adjustmentForm.isDirty ||
    payBillForm.isDirty ||
    payStatementForm.isDirty ||
    creditRefundForm.isDirty ||
    moveForm.isDirty ||
    potTransactionForm.isDirty ||
    depositForm.isDirty ||
    withdrawalForm.isDirty ||
    settlementEntryForm.isDirty ||
    screenForm.isDirty
  const discardPrompt = useDiscardPrompt(isDirty)
  const openerRef = useRef<HTMLElement | null>(null)
  const formAreaRef = useRef<HTMLDivElement | null>(null)

  const closeForms = () => {
    if (expenseForm.isOpen) expenseForm.cancelForm()
    if (incomeForm.isIncomeFormOpen) incomeForm.cancelIncomeForm()
    if (transferForm.isOpen) transferForm.cancel()
    if (adjustmentForm.isOpen) adjustmentForm.cancel()
    if (payBillForm.isOpen) payBillForm.cancel()
    if (payStatementForm.isOpen) payStatementForm.cancel()
    if (creditRefundForm.isOpen) creditRefundForm.cancel()
    if (moveForm.isOpen) moveForm.cancel()
    if (potTransactionForm.isOpen) potTransactionForm.cancel()
    if (depositForm.isOpen) depositForm.cancel()
    if (withdrawalForm.isOpen) withdrawalForm.cancel()
    if (settlementEntryForm.isOpen) settlementEntryForm.cancel()
    setIsManagePotsOpen(false)
    screenForm.close()
  }

  const [formsAccountId, setFormsAccountId] = useState(routeAccountId)
  if (formsAccountId !== routeAccountId) {
    setFormsAccountId(routeAccountId)
    closeForms()
  }

  useEffect(() => {
    if (activeFormKey) {
      const firstField = formAreaRef.current?.querySelector<HTMLInputElement>(FIRST_FIELD_SELECTOR)
      const checkedChoice = firstField?.type === 'radio' ? formAreaRef.current?.querySelector<HTMLElement>(`input[name="${firstField.name}"]:checked`) : null
      const fieldToFocus = checkedChoice ?? firstField
      fieldToFocus?.focus()
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

  const restoreFocus = () => {
    if (openerRef.current?.isConnected) openerRef.current.focus()
    else formAreaRef.current?.parentElement?.querySelector<HTMLElement>(RESTING_FOCUS_SELECTOR)?.focus()
  }

  const closeActiveForm = () => {
    closeForms()
    restoreFocus()
  }

  const operationDelete = useConfirmedDelete<OperationDeleteTarget>(
    ({ operationId }) => apiClient.deletePotOperation(operationId),
    ({ noun }) => deleteFailedMessage(noun),
    ({ operationId, noun }) => {
      if (editingRowKey === operationRowKey(operationId)) closeForms()
      onSaved(`${OPERATION_TITLES[noun]} deleted`)
    },
  )

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
      else if (row.movement) potTransactionForm.open(row.movement)
      else if (row.settlement) settlementEntryForm.openEdit(row.settlement)
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

  const openDepositFromIncome = () => {
    discardPrompt.confirmDiscard(() => {
      closeForms()
      incomeForm.showDepositIncomeForm(accountId ?? routeAccountId)
    })
  }

  const isOperationFormOpen = depositForm.isEditing || withdrawalForm.isEditing || incomeForm.isIncomeEditing

  const openOperation = ({ operation, income }: OperationToOpen) =>
    discardPrompt.confirmDiscard(() => {
      closeForms()
      setEditedRowKey(operationRowKey(operation.operationId))
      if (income) incomeForm.showEditIncomeForm(income)
      else if (operation.kind === 'Deposit') depositForm.openEdit(operation)
      else withdrawalForm.openEdit(operation)
    })

  const followOperation = (operationId: string, opener: HTMLElement, navigate: () => void) => {
    if (isOperationFormOpen && editedRowKey === operationRowKey(operationId)) {
      formAreaRef.current?.querySelector<HTMLElement>(FIRST_FIELD_SELECTOR)?.focus()
      return
    }
    discardPrompt.confirmDiscard(() => {
      closeForms()
      openerRef.current = opener
      navigate()
    })
  }

  const closeFormsOf = (row: AccountTransactionDto) => {
    if (editingRowKey === rowKey(row)) closeForms()
  }

  const closeFormOfRemovedRow = (rows: readonly AccountTransactionDto[]) => {
    if (editingRowKey && !isOperationRowKey(editingRowKey) && !settlementEntryForm.warning && !rows.some((row) => rowKey(row) === editingRowKey)) closeActiveForm()
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
    payStatementForm,
    creditRefundForm,
    moveForm,
    potTransactionForm,
    depositForm,
    withdrawalForm,
    settlementEntryForm,
    operationDelete,
    isOperationFormOpen,
    isManagePotsOpen,
    closeManagePots: () => setIsManagePotsOpen(false),
    lockedBank,
    lockedCard,
    isAccountCurrent,
    activeAction,
    discardPrompt,
    formAreaRef,
    openAction,
    openRowEdit,
    openRowMove,
    openDepositFromIncome,
    openOperation,
    followOperation,
    closeActiveForm,
    restoreFocus,
    closeFormsOf,
    closeFormOfRemovedRow,
    guardChange,
  }
}
