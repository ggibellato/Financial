import { describe, expect, it } from 'vitest'
import type { AccountTransactionDto, ReserveMovementDto } from '../../api/types'
import { accountTransactionRow } from '../../test/accountTransactionRow'
import { ACTION_LABELS, canMoveToStatement, deleteConfirmation, deletedMessage, formActionsOf, isOlderIncomeLine, kindLabel, operationPath, recordNoun, recordTitle } from '../accountActions'

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
  const OPERATION = { operationId: 'op1', potsAccountId: 'p1', year: 2026, month: 3, incomeId: null }

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

describe('refund wording', () => {
  const REFUND = accountTransactionRow({ id: 'r1', kind: 'CardRefund', date: '2026-10-08', description: 'Shoes returned', amount: 40, canEdit: true })

  it('offers Add refund as a form action', () => {
    expect(formActionsOf(['AddCharge', 'AddRefund'])).toEqual(['AddCharge', 'AddRefund'])
    expect(ACTION_LABELS.AddRefund).toBe('Add refund')
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
  const LINK = { operationId: 'op1', potsAccountId: 'reserva', year: 2026, month: 10, incomeId: 'i1' }

  it('is true for a movement of an income with no operation', () => {
    expect(isOlderIncomeLine(movementRow('m1', { incomeId: 'i1' }))).toBe(true)
  })

  it('is false for a linked income line, a manual line and other kinds of record', () => {
    expect(isOlderIncomeLine(movementRow('m1', { incomeId: 'i1' }, { operation: LINK }))).toBe(false)
    expect(isOlderIncomeLine(movementRow('m2'))).toBe(false)
    expect(isOlderIncomeLine(accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-05', description: 'Bonus', amount: 25 }))).toBe(false)
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

describe('deletedMessage', () => {
  it('reports the lines a split delete removed and a single record by its title', () => {
    expect(deletedMessage(movementRow('m1', { splitLineCount: 2 }))).toBe('2 pot transaction lines deleted')
    expect(deletedMessage(movementRow('m2', { amount: -25 }))).toBe('Pot transaction deleted')
  })
})
