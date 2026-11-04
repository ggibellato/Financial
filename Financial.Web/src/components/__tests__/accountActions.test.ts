import { describe, expect, it } from 'vitest'
import type { AccountTransactionDto, ReserveMovementDto } from '../../api/types'
import { accountTransactionRow } from '../../test/accountTransactionRow'
import { deleteConfirmation, deletedMessage, kindLabel, linesDeletedWith, operationPath, recordNoun, recordTitle } from '../accountActions'

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
  it('opens the Transactions tab of the operation\'s Pots account in its month', () => {
    expect(operationPath({ operationId: 'op1', potsAccountId: 'p1', year: 2026, month: 3, incomeId: null })).toBe(
      '/cashflow/accounts/p1?month=2026-03&tab=transactions&operation=op1',
    )
  })
})

describe('pot transaction wording', () => {
  it('labels a pot movement as a pot transaction', () => {
    expect(kindLabel('PotTransaction')).toBe('Pot transaction')
    expect(recordNoun('PotTransaction')).toBe('pot transaction')
    expect(recordTitle('PotTransaction')).toBe('Pot transaction')
  })
})

describe('deleteConfirmation', () => {
  it('counts the positive unlinked lines with the same date and description', () => {
    const line = movementRow('m1')
    const rows = [line, movementRow('m2'), movementRow('m3'), movementRow('m4')]

    expect(deleteConfirmation(line, rows)).toBe('Delete this pot transaction? It is part of a split, so all 4 lines will be deleted. This cannot be undone.')
  })

  it('leaves out lines the server keeps: other dates, other descriptions, withdrawals, linked lines and income lines', () => {
    const line = movementRow('m1')
    const rows = [
      line,
      movementRow('other-date', { date: '2026-10-06' }),
      movementRow('other-text', { description: 'Refund' }),
      movementRow('withdrawal', { amount: -25 }),
      movementRow('zero', { amount: 0 }),
      movementRow('income', { incomeId: 'i1' }),
      movementRow('linked', {}, { operation: { operationId: 'op1', potsAccountId: 'reserva', year: 2026, month: 10, incomeId: null } }),
    ]

    expect(deleteConfirmation(line, rows)).toBe(SINGLE_LINE)
  })

  it('deletes a withdrawal alone, even when a positive line shares its text', () => {
    const withdrawal = movementRow('m1', { amount: -25 })

    expect(deleteConfirmation(withdrawal, [withdrawal, movementRow('m2'), movementRow('m3')])).toBe(SINGLE_LINE)
  })

  it('reports the lines a split delete removed and a single record by its title', () => {
    const line = movementRow('m1')
    const other = movementRow('m2')
    const withdrawal = movementRow('m3', { amount: -25 })

    expect(linesDeletedWith(line, [line, other, withdrawal])).toEqual([line, other])
    expect(deletedMessage(line, [line, other, withdrawal])).toBe('2 pot transaction lines deleted')
    expect(deletedMessage(withdrawal, [line, other, withdrawal])).toBe('Pot transaction deleted')
  })

  it('keeps the plain question for other kinds of record', () => {
    const expense = accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-05', description: 'Bonus', amount: 25 })

    expect(deleteConfirmation(expense, [expense, movementRow('m1')])).toBe('Delete this expense? This cannot be undone.')
  })
})
