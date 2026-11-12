import type { AccountTransactionDto, AccountTransactionOperationDto, AccountTransactionStatementDto, MonthTransactionDto } from '../api/types'
import { formatMonthInputValue, formatShortDateUtc } from '../utils/formatters'

export type AccountFormAction =
  | 'AddExpense'
  | 'AddCharge'
  | 'AddRefund'
  | 'RefundCreditToBank'
  | 'ReceiveCardCredit'
  | 'AddIncome'
  | 'TransferTo'
  | 'TransferFrom'
  | 'PayRecurringBill'
  | 'PayCardStatement'
  | 'Reconcile'
  | 'Deposit'
  | 'Withdraw'
  | 'ManagePots'
  | 'WithdrawFromPots'
  | 'AddEntry'
  | 'Settle'

export const ACTION_LABELS: Record<AccountFormAction, string> = {
  AddExpense: 'New expense',
  AddCharge: 'New charge',
  AddRefund: 'New refund',
  RefundCreditToBank: 'Refund credit to bank',
  ReceiveCardCredit: 'Receive card credit',
  AddIncome: 'New income',
  TransferTo: 'Transfer to',
  TransferFrom: 'Transfer from',
  PayRecurringBill: 'Pay recurring bill',
  PayCardStatement: 'Pay card statement',
  Reconcile: 'Adjust balance',
  Deposit: 'New deposit',
  Withdraw: 'New withdrawal',
  ManagePots: 'Manage pots',
  WithdrawFromPots: 'Withdraw from pots',
  AddEntry: 'New entry',
  Settle: 'Settle',
}

const TRANSFER_ACTIONS: readonly AccountFormAction[] = ['TransferTo', 'TransferFrom']
const ACTIONS_WITHOUT_FORM_OPTIONS: readonly AccountFormAction[] = ['PayCardStatement', 'ManagePots', 'RefundCreditToBank', 'ReceiveCardCredit', 'AddEntry', 'Settle']

const KIND_LABELS: Record<string, string> = {
  CardCharge: 'Card charge',
  CardRefund: 'Refund',
  CardPayment: 'Card payment',
  CardCreditRefund: 'Card credit refund',
  TransferIn: 'Transfer in',
  TransferOut: 'Transfer out',
  Adjustment: 'Balance adjustment',
  PotTransaction: 'Pot transaction',
  SettlementEntry: 'Settlement entry',
}

const RECORD_NOUNS: Record<string, string> = {
  Expense: 'expense',
  CardCharge: 'card charge',
  CardRefund: 'refund',
  CardPayment: 'card payment',
  CardCreditRefund: 'credit refund',
  Income: 'income',
  TransferIn: 'transfer',
  TransferOut: 'transfer',
  Adjustment: 'balance adjustment',
  PotTransaction: 'pot transaction',
  SettlementEntry: 'entry',
}

export function isFormAction(action: string): action is AccountFormAction {
  return Object.hasOwn(ACTION_LABELS, action)
}

export function formActionsOf(allowedActions: readonly string[]): AccountFormAction[] {
  return allowedActions.filter(isFormAction)
}

export function opensDialog(action: AccountFormAction): boolean {
  return action === 'ManagePots'
}

export function needsFormOptions(action: AccountFormAction): boolean {
  return !ACTIONS_WITHOUT_FORM_OPTIONS.includes(action)
}

export function isTransferAction(action: AccountFormAction): boolean {
  return TRANSFER_ACTIONS.includes(action)
}

export function canTransfer(allowedActions: readonly string[]): boolean {
  return formActionsOf(allowedActions).some(isTransferAction)
}

export function kindLabel(kind: string): string {
  return Object.hasOwn(KIND_LABELS, kind) ? KIND_LABELS[kind] : kind
}

export function recordNoun(kind: string): string {
  return Object.hasOwn(RECORD_NOUNS, kind) ? RECORD_NOUNS[kind] : 'record'
}

export function recordTitle(kind: string): string {
  const noun = recordNoun(kind)
  return noun.charAt(0).toUpperCase() + noun.slice(1)
}

function linesDeletedWith(row: AccountTransactionDto): number {
  return row.movement?.splitLineCount ?? 1
}

export function isCreditRefund(row: AccountTransactionDto): boolean {
  return row.kind === 'CardCreditRefund'
}

export function deleteConfirmation(row: AccountTransactionDto): string {
  const question = `Delete this ${recordNoun(row.kind)}?`
  if (isCreditRefund(row)) return `${question} The card's credit returns.`
  const lines = linesDeletedWith(row)
  return lines > 1
    ? `${question} It is part of a split, so all ${lines} lines will be deleted. This cannot be undone.`
    : `${question} This cannot be undone.`
}

export function deletedMessage(row: AccountTransactionDto): string {
  const lines = linesDeletedWith(row)
  return lines > 1 ? `${lines} ${recordNoun(row.kind)} lines deleted` : `${recordTitle(row.kind)} deleted`
}

export function isOlderIncomeLine(row: AccountTransactionDto): boolean {
  return row.movement?.incomeId != null && !row.operation
}

export function canMoveToStatement(row: AccountTransactionDto): boolean {
  return row.kind === 'CardCharge' || row.kind === 'CardRefund'
}

export function statementPath(statement: AccountTransactionStatementDto): string {
  return `/cashflow/accounts/${statement.accountId}?month=${formatMonthInputValue(statement.year, statement.month)}&tab=statements`
}

function isOnPotsScreen(operation: AccountTransactionOperationDto, screenAccountId: string): boolean {
  return operation.potsAccountId.toLowerCase() === screenAccountId.toLowerCase()
}

export function operationPath(operation: AccountTransactionOperationDto, screenAccountId: string): string {
  const month = formatMonthInputValue(operation.year, operation.month)
  return operation.incomeId != null && !isOnPotsScreen(operation, screenAccountId)
    ? `/cashflow/accounts/${screenAccountId}?month=${month}&operation=${operation.operationId}`
    : `/cashflow/accounts/${operation.potsAccountId}?month=${month}&tab=transactions&operation=${operation.operationId}`
}

export const BLANK_OPERATION_DESCRIPTION = 'Deposit from an income'

export type PotOperationNoun = 'deposit' | 'withdrawal'

const STATEMENT_PAYMENT_DELETE_REASON = 'Unmark the statement to remove this payment.'
const INACTIVE_ACCOUNT_REASON = 'This account is inactive, so its rows cannot be changed.'
const INCOME_LINE_EDIT_REASON = 'Edit from the income.'
const CREDIT_REFUND_EDIT_REASON = 'A credit refund cannot be edited. Delete it and refund the credit again.'
const PAID_STATEMENT_REASON = 'Its card statement is paid. Unmark the statement to change it.'
const READ_ONLY_REASON = 'This row cannot be changed here.'
const INCOME_DEPOSIT_DELETE_REASON = 'Delete the income to remove this deposit.'
const OTHER_SCREEN_OPERATION_DELETE_REASON = 'Delete it from the pots account.'

function operationDeleteBlockedReason(operation: AccountTransactionOperationDto, screenAccountId: string): string | undefined {
  if (operation.incomeId != null) return INCOME_DEPOSIT_DELETE_REASON
  return isOnPotsScreen(operation, screenAccountId) ? undefined : OTHER_SCREEN_OPERATION_DELETE_REASON
}

export function withdrawalOperationIds(rows: AccountTransactionDto[], screenAccountId: string): Set<string> {
  const ids = new Set<string>()
  for (const { operation, amount } of rows) {
    if (operation && (isOnPotsScreen(operation, screenAccountId) ? amount < 0 : amount > 0)) ids.add(operation.operationId)
  }
  return ids
}

export function operationNoun({ operationId }: AccountTransactionOperationDto, withdrawalIds: Set<string>): PotOperationNoun {
  return withdrawalIds.has(operationId) ? 'withdrawal' : 'deposit'
}

export function rowKey(row: AccountTransactionDto): string {
  return `${row.kind}-${row.id}`
}

export function operationDeleteConfirmation(noun: PotOperationNoun): string {
  return `Delete this ${noun}? Its pot lines and any bank expenses are deleted with it. This cannot be undone.`
}

const OPERATION_TITLES: Record<PotOperationNoun, string> = { deposit: 'Deposit', withdrawal: 'Withdrawal' }

export function operationDeletedMessage(noun: PotOperationNoun): string {
  return `${OPERATION_TITLES[noun]} deleted`
}

export function shownDescription(row: AccountTransactionDto): string | null {
  return row.description || (row.operation ? BLANK_OPERATION_DESCRIPTION : null)
}

export function transactionRowName(row: AccountTransactionDto, withdrawalIds: Set<string>): string {
  const kind = row.operation ? operationNoun(row.operation, withdrawalIds) : recordNoun(row.kind)
  const description = shownDescription(row)
  const pot = row.kind === 'PotTransaction' ? row.counterpart : null
  return [description ? `${kind} ${description}` : kind, pot, formatShortDateUtc(row.date)].filter(Boolean).join(', ')
}

export type RowEdit =
  | { kind: 'record' }
  | { kind: 'statement'; statement: AccountTransactionStatementDto }
  | { kind: 'operation'; operation: AccountTransactionOperationDto; accountId: string }
  | { kind: 'blocked'; reason: string }

export function rowEdit(row: AccountTransactionDto, accountId: string | null, accountIsActive: boolean): RowEdit {
  if (row.statement) return { kind: 'statement', statement: row.statement }
  if (!accountIsActive) return { kind: 'blocked', reason: INACTIVE_ACCOUNT_REASON }
  if (row.operation && accountId) return { kind: 'operation', operation: row.operation, accountId }
  if (row.canEdit) return { kind: 'record' }
  if (isOlderIncomeLine(row)) return { kind: 'blocked', reason: INCOME_LINE_EDIT_REASON }
  if (isCreditRefund(row)) return { kind: 'blocked', reason: CREDIT_REFUND_EDIT_REASON }
  return { kind: 'blocked', reason: readOnlyReason(row, accountId) }
}

function readOnlyReason(row: AccountTransactionDto, accountId: string | null): string {
  const { expense } = row
  if (!canMoveToStatement(row) || !expense) return READ_ONLY_REASON
  if (expense.creditCardId && accountId && expense.creditCardId.toLowerCase() !== accountId.toLowerCase()) {
    const card = expense.creditCardName ?? 'card'
    return `Its ${card} statement is paid. Unmark it on the ${card} account to change it.`
  }
  return PAID_STATEMENT_REASON
}

export function rowDeleteBlockedReason(row: AccountTransactionDto, accountId: string | null, accountIsActive: boolean): string | undefined {
  if (row.statement) return STATEMENT_PAYMENT_DELETE_REASON
  if (!accountIsActive) return INACTIVE_ACCOUNT_REASON
  if (row.operation && accountId) return operationDeleteBlockedReason(row.operation, accountId)
  if (row.canEdit || isCreditRefund(row)) return undefined
  return isOlderIncomeLine(row) ? INCOME_DEPOSIT_DELETE_REASON : readOnlyReason(row, accountId)
}

interface MonthRowEdit {
  path?: string
  blockedReason?: string
}

export function monthRowEdit({ accountId, accountIsActive, transaction: row }: MonthTransactionDto, month: string): MonthRowEdit {
  const edit = rowEdit(row, accountId, accountIsActive)
  switch (edit.kind) {
    case 'statement':
      return { path: statementPath(edit.statement) }
    case 'operation':
      return { path: operationPath(edit.operation, edit.accountId) }
    case 'record':
      return {
        path: accountId
          ? `/cashflow/accounts/${accountId}?month=${month}&tab=transactions&edit=${rowKey(row)}`
          : `/cashflow/accounts?month=${month}&tab=income&edit=${row.id}`,
      }
    default:
      return { blockedReason: edit.reason }
  }
}
