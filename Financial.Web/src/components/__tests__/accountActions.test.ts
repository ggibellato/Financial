import { describe, expect, it } from 'vitest'
import type { AccountTransactionDto, ExpenseDto, MonthTransactionDto, ReserveMovementDto } from '../../api/types'
import { accountTransactionRow } from '../../test/accountTransactionRow'
import { ACTION_LABELS, canMoveToStatement, deleteConfirmation, deletedMessage, formActionsOf, isOlderIncomeLine, kindLabel, monthRowEdit, rowDeleteBlockedReason, needsFormOptions, operationPath, recordNoun, recordTitle, transactionRowName } from '../accountActions'

const SINGLE_LINE = 'Delete this pot transaction? This cannot be undone.'

function movementRow(id: string, fields: Partial<ReserveMovementDto> = {}, row: Partial<AccountTransactionDto> = {}): AccountTransactionDto {
  const movement: ReserveMovementDto = { id, bucketId: 'p1', bucketName: 'Home', amount: 25, date: '2026-10-05', description: 'Bonus', incomeId: null, splitLineCount: 1, ...fields }
  return accountTransactionRow({
    id,
    kind: 'PotTransaction',
    date: movement.date,
    description: movement.description,
    amount: movement.amount,
    movement,
    ...row,
  })
}

describe('operationPath', () => {
  const OPERATION = { operationId: 'op1', kind: 'Deposit', potsAccountId: 'p1', year: 2026, month: 3, incomeId: null }

  it("opens the Transactions tab of the operation's Pots account in its month", () => {
    expect(operationPath(OPERATION, 'bank-1')).toBe('/cashflow/accounts/p1?month=2026-03&tab=transactions&operation=op1')
  })

  it('opens the income on the bank screen when the deposit comes from an income', () => {
    expect(operationPath({ ...OPERATION, incomeId: 'i1' }, 'bank-1')).toBe('/cashflow/accounts/bank-1?month=2026-03&operation=op1')
  })

  it('keeps an income deposit on the Pots screen when it is opened from there', () => {
    expect(operationPath({ ...OPERATION, incomeId: 'i1' }, 'P1')).toBe('/cashflow/accounts/p1?month=2026-03&tab=transactions&operation=op1')
  })
})

describe('pot transaction wording', () => {
  it('labels a pot movement as a pot transaction', () => {
    expect(kindLabel('PotTransaction')).toBe('Pot transaction')
    expect(recordNoun('PotTransaction')).toBe('pot transaction')
    expect(recordTitle('PotTransaction')).toBe('Pot transaction')
  })
})

describe('credit refund wording', () => {
  it('offers Refund credit to bank as a form action that needs no shared form options', () => {
    expect(formActionsOf(['RefundCreditToBank'])).toEqual(['RefundCreditToBank'])
    expect(ACTION_LABELS.RefundCreditToBank).toBe('Refund credit to bank')
    expect(needsFormOptions('RefundCreditToBank')).toBe(false)
  })

  it('offers Receive card credit on a bank as a form action that needs no shared form options', () => {
    expect(formActionsOf(['ReceiveCardCredit'])).toEqual(['ReceiveCardCredit'])
    expect(ACTION_LABELS.ReceiveCardCredit).toBe('Receive card credit')
    expect(needsFormOptions('ReceiveCardCredit')).toBe(false)
  })

  it('labels a credit refund row Card credit refund', () => {
    expect(kindLabel('CardCreditRefund')).toBe('Card credit refund')
    expect(recordNoun('CardCreditRefund')).toBe('credit refund')
  })
})

describe('refund wording', () => {
  const REFUND = accountTransactionRow({ id: 'r1', kind: 'CardRefund', date: '2026-10-08', description: 'Shoes returned', amount: 40, canEdit: true })

  it('offers Add refund as a form action', () => {
    expect(formActionsOf(['AddCharge', 'AddRefund'])).toEqual(['AddCharge', 'AddRefund'])
    expect(ACTION_LABELS.AddRefund).toBe('New refund')
  })

  it('labels a refund row Refund', () => {
    expect(kindLabel('CardRefund')).toBe('Refund')
    expect(recordNoun('CardRefund')).toBe('refund')
  })

  it('names the refund in the delete confirmation and message', () => {
    expect(deleteConfirmation(REFUND)).toBe('Delete this refund? This cannot be undone.')
    expect(deletedMessage(REFUND)).toBe('Refund deleted')
  })

  it('lets a refund move to a later statement like a charge', () => {
    expect(canMoveToStatement(REFUND)).toBe(true)
    expect(canMoveToStatement({ ...REFUND, kind: 'CardCharge' })).toBe(true)
    expect(canMoveToStatement({ ...REFUND, kind: 'Expense' })).toBe(false)
  })
})

describe('isOlderIncomeLine', () => {
  const LINK = { operationId: 'op1', kind: 'Deposit', potsAccountId: 'reserva', year: 2026, month: 10, incomeId: 'i1' }

  it('is true for a movement of an income with no operation', () => {
    expect(isOlderIncomeLine(movementRow('m1', { incomeId: 'i1' }))).toBe(true)
  })

  it('is false for a linked income line, a manual line and other kinds of record', () => {
    expect(isOlderIncomeLine(movementRow('m1', { incomeId: 'i1' }, { operation: LINK }))).toBe(false)
    expect(isOlderIncomeLine(movementRow('m2'))).toBe(false)
    expect(isOlderIncomeLine(accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-05', description: 'Bonus', amount: 25 }))).toBe(false)
  })
})

describe('a settlement entry row', () => {
  const ENTRY = accountTransactionRow({ id: 's1', kind: 'SettlementEntry', date: '2026-10-11', amount: 80 })

  it('is labelled as an entry', () => {
    expect(ACTION_LABELS.AddEntry).toBe('New entry')
    expect(needsFormOptions('AddEntry')).toBe(false)
    expect(ACTION_LABELS.Settle).toBe('Settle')
    expect(needsFormOptions('Settle')).toBe(false)
    expect(kindLabel('SettlementEntry')).toBe('Settlement entry')
    expect(recordNoun('SettlementEntry')).toBe('entry')
  })

  it('asks and reports its delete as an entry', () => {
    expect(deleteConfirmation(ENTRY)).toBe('Delete this entry? This cannot be undone.')
    expect(deletedMessage(ENTRY)).toBe('Entry deleted')
  })
})

describe('deleteConfirmation', () => {
  it("states the server's count for a line of a split", () => {
    expect(deleteConfirmation(movementRow('m1', { splitLineCount: 4 }))).toBe(
      'Delete this pot transaction? It is part of a split, so all 4 lines will be deleted. This cannot be undone.',
    )
  })

  it('has no split warning when the server counts one line', () => {
    expect(deleteConfirmation(movementRow('m1', { amount: -25, splitLineCount: 1 }))).toBe(SINGLE_LINE)
  })

  it('keeps the plain question for other kinds of record', () => {
    expect(deleteConfirmation(accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-05', description: 'Bonus', amount: 25 }))).toBe(
      'Delete this expense? This cannot be undone.',
    )
  })
})

describe('a credit refund delete', () => {
  const CREDIT_REFUND = accountTransactionRow({ id: 'cr1', kind: 'CardCreditRefund', date: '2026-10-15', amount: -35 })

  it('says the credit returns in the confirmation and names it in the toast', () => {
    expect(deleteConfirmation(CREDIT_REFUND)).toBe("Delete this credit refund? The card's credit returns.")
    expect(deletedMessage(CREDIT_REFUND)).toBe('Credit refund deleted')
  })
})

describe('deletedMessage', () => {
  it('reports the lines a split delete removed and a single record by its title', () => {
    expect(deletedMessage(movementRow('m1', { splitLineCount: 2 }))).toBe('2 pot transaction lines deleted')
    expect(deletedMessage(movementRow('m2', { amount: -25 }))).toBe('Pot transaction deleted')
  })
})

describe('All transactions row actions', () => {
  const MONTH = '2026-10'
  const INACTIVE = 'This account is inactive, so its rows cannot be changed.'

  const monthRowDelete = ({ transaction, accountId, accountIsActive }: MonthTransactionDto) => rowDeleteBlockedReason(transaction, accountId, accountIsActive)

  function monthRow(transaction: AccountTransactionDto, accountId: string | null = 'bank-1', accountIsActive = true): MonthTransactionDto {
    return { accountId, accountName: accountId ? 'Barclays' : null, accountCurrency: accountId ? 'GBP' : null, accountIsActive, transaction }
  }

  const expense = accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-14', amount: -42.1 })
  const payment = accountTransactionRow({ id: 't1', kind: 'CardPayment', date: '2026-10-05', amount: -100, canEdit: false, statement: { accountId: 'card-1', year: 2026, month: 9 } })
  const deposit = { operationId: 'op1', kind: 'Deposit', potsAccountId: 'pots-1', year: 2026, month: 10, incomeId: null }

  it('edits an account row on its account screen in the viewed month, and deletes it here', () => {
    expect(monthRowEdit(monthRow(expense), MONTH)).toEqual({ path: '/cashflow/accounts/bank-1?month=2026-10&tab=transactions&edit=Expense-e1' })
    expect(monthRowDelete(monthRow(expense))).toBeUndefined()
  })

  it('edits a bankless income on the Income tab', () => {
    const income = accountTransactionRow({ id: 'i1', kind: 'Income', date: '2026-10-15', amount: 85.2, currency: null })
    expect(monthRowEdit(monthRow(income, null), MONTH)).toEqual({ path: '/cashflow/accounts?month=2026-10&tab=income&edit=i1' })
    expect(monthRowDelete(monthRow(income, null))).toBeUndefined()
  })

  it("opens a statement payment's statement and says where to remove it", () => {
    expect(monthRowEdit(monthRow(payment), MONTH)).toEqual({ path: '/cashflow/accounts/card-1?month=2026-09&tab=statements' })
    expect(monthRowDelete(monthRow(payment))).toBe('Unmark the statement to remove this payment.')
  })

  it('opens a pot operation, deleting it only from its Pots account', () => {
    const bankSide = accountTransactionRow({ id: 'e9', kind: 'Expense', date: '2026-10-10', amount: -90, canEdit: false, operation: deposit })
    const potsSide = accountTransactionRow({ id: 'p3', kind: 'PotTransaction', date: '2026-10-10', amount: 90, canEdit: false, operation: deposit })
    expect(monthRowEdit(monthRow(bankSide), MONTH)).toEqual({ path: '/cashflow/accounts/pots-1?month=2026-10&tab=transactions&operation=op1' })
    expect(monthRowDelete(monthRow(bankSide))).toBe('Delete it from the pots account.')
    expect(monthRowDelete(monthRow(potsSide, 'pots-1'))).toBeUndefined()
    expect(monthRowDelete(monthRow({ ...potsSide, operation: { ...deposit, incomeId: 'i1' } }, 'pots-1'))).toBe(
      'Delete the income to remove this deposit.',
    )
  })

  it('sends a split income line to its income', () => {
    const line = movementRow('m1', { incomeId: 'i1' }, { canEdit: false })
    expect(monthRowEdit(monthRow(line, 'pots-1'), MONTH)).toEqual({ blockedReason: 'Edit from the income.' })
    expect(monthRowDelete(monthRow(line, 'pots-1'))).toBe('Delete the income to remove this deposit.')
  })

  it('lets a credit refund on an active account be deleted but not edited', () => {
    const refund = accountTransactionRow({ id: 'r1', kind: 'CardCreditRefund', date: '2026-10-20', amount: 30, canEdit: false })
    expect(monthRowEdit(monthRow(refund), MONTH)).toEqual({ blockedReason: 'A credit refund cannot be edited. Delete it and refund the credit again.' })
    expect(monthRowDelete(monthRow(refund))).toBeUndefined()
    expect(monthRowDelete(monthRow(refund, 'bank-1', false))).toBe(INACTIVE)
  })

  it('sends a charge on a paid statement to its statement', () => {
    const paid = accountTransactionRow({ id: 'c1', kind: 'CardCharge', date: '2026-10-28', amount: -1.1, canEdit: false, expense: { id: 'c1', creditCardId: 'card-1', paymentStatus: 'CreditCardCharge' } as ExpenseDto })
    const reason = 'Its card statement is paid. Unmark the statement to change it.'
    expect(monthRowEdit(monthRow(paid, 'card-1'), MONTH)).toEqual({ blockedReason: reason })
    expect(monthRowDelete(monthRow(paid, 'card-1'))).toBe(reason)
  })

  it('sends a settled charge to its statement, its bank copy to the card, and blocks an inactive pots operation', () => {
    const charge = (fields: Partial<ExpenseDto>) =>
      accountTransactionRow({ id: 'c9', kind: 'CardCharge', date: '2026-10-02', amount: -9, canEdit: false, expense: { id: 'c9', creditCardId: 'card-1', creditCardName: 'BA Amex', paymentStatus: 'CreditCardCharge', ...fields } as ExpenseDto })
    const settled = charge({ paymentStatus: 'CreditCardSettled' })
    expect(monthRowEdit(monthRow(settled, 'card-1'), MONTH)).toEqual({ blockedReason: 'Its card statement is paid. Unmark the statement to change it.' })
    expect(monthRowDelete(monthRow(charge({}), 'card-1'))).toBe('Its card statement is paid. Unmark the statement to change it.')
    expect(monthRowDelete(monthRow(settled, 'bank-1'))).toBe('Its BA Amex statement is paid. Unmark it on the BA Amex account to change it.')
    const potsSide = accountTransactionRow({ id: 'p3', kind: 'PotTransaction', date: '2026-10-10', amount: 90, canEdit: false, operation: deposit })
    expect(monthRowEdit(monthRow(potsSide, 'pots-1', false), MONTH)).toEqual({ blockedReason: INACTIVE })
    expect(monthRowDelete(monthRow(potsSide, 'pots-1', false))).toBe(INACTIVE)
  })

  it("says an inactive account's rows cannot be changed", () => {
    const readOnly = { ...expense, canEdit: false }
    expect(monthRowEdit(monthRow(readOnly, 'bank-9', false), MONTH)).toEqual({ blockedReason: INACTIVE })
    expect(monthRowDelete(monthRow(readOnly, 'bank-9', false))).toBe(INACTIVE)
  })

  it("names a row by its kind, description and date, and a pot operation's row by the operation's own kind", () => {
    expect(transactionRowName({ ...expense, description: 'Tesco' })).toBe('expense Tesco, 14/10/2026')
    const blank = accountTransactionRow({ id: 'p3', kind: 'PotTransaction', date: '2026-10-10', amount: 90, operation: { ...deposit, kind: 'Withdrawal' } })
    expect(transactionRowName(blank)).toBe('withdrawal Deposit from an income, 10/10/2026')
    expect(transactionRowName({ ...blank, amount: -90, operation: deposit })).toBe('deposit Deposit from an income, 10/10/2026')
  })
})
