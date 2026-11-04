import type { AccountTransactionDto, AccountTransactionOperationDto, AccountTransactionStatementDto } from '../api/types'
import { formatMonthInputValue } from '../utils/formatters'

export type AccountFormAction =
  | 'AddExpense'
  | 'AddCharge'
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

export const ACTION_LABELS: Record<AccountFormAction, string> = {
  AddExpense: 'Add expense',
  AddCharge: 'Add charge',
  AddIncome: 'Add income',
  TransferTo: 'Transfer to',
  TransferFrom: 'Transfer from',
  PayRecurringBill: 'Pay recurring bill',
  PayCardStatement: 'Pay card statement',
  Reconcile: 'Adjust balance',
  Deposit: 'Deposit',
  Withdraw: 'Withdraw',
  ManagePots: 'Manage pots',
  WithdrawFromPots: 'Withdraw from pots',
}

const TRANSFER_ACTIONS: readonly AccountFormAction[] = ['TransferTo', 'TransferFrom']
const ACTIONS_WITHOUT_FORM_OPTIONS: readonly AccountFormAction[] = ['PayCardStatement', 'ManagePots']

const KIND_LABELS: Record<string, string> = {
  CardCharge: 'Card charge',
  CardPayment: 'Card payment',
  TransferIn: 'Transfer in',
  TransferOut: 'Transfer out',
  Adjustment: 'Balance adjustment',
  PotTransaction: 'Pot transaction',
}

const RECORD_NOUNS: Record<string, string> = {
  Expense: 'expense',
  CardCharge: 'card charge',
  CardPayment: 'card payment',
  Income: 'income',
  TransferIn: 'transfer',
  TransferOut: 'transfer',
  Adjustment: 'balance adjustment',
  PotTransaction: 'pot transaction',
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

export function deleteConfirmation(row: AccountTransactionDto): string {
  const question = `Delete this ${recordNoun(row.kind)}?`
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
  return row.kind === 'CardCharge'
}

export function statementPath(statement: AccountTransactionStatementDto): string {
  return `/cashflow/accounts/${statement.accountId}?month=${formatMonthInputValue(statement.year, statement.month)}&tab=statements`
}

export function operationPath(operation: AccountTransactionOperationDto, screenAccountId: string): string {
  const month = formatMonthInputValue(operation.year, operation.month)
  const isOnPotsScreen = operation.potsAccountId.toLowerCase() === screenAccountId.toLowerCase()
  return operation.incomeId != null && !isOnPotsScreen
    ? `/cashflow/accounts/${screenAccountId}?month=${month}&operation=${operation.operationId}`
    : `/cashflow/accounts/${operation.potsAccountId}?month=${month}&tab=transactions&operation=${operation.operationId}`
}

export function rowKey(row: AccountTransactionDto): string {
  return `${row.kind}-${row.id}`
}
