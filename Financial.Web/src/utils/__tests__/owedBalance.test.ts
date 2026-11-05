import { describe, expect, it } from 'vitest'
import type { AccountStatementDto } from '../../api/types'
import { balanceLabel, formatLabelledBalance, formatOwedOrCredit, formatShownBalance, nothingDueText, statementStatusText } from '../owedBalance'

describe('owedBalance', () => {
  it('labels an owed balance "Owed" and any other "Balance"', () => {
    expect(balanceLabel(true)).toBe('Owed')
    expect(balanceLabel(false)).toBe('Balance')
  })

  it('shows an owed balance negated in the usual number format', () => {
    expect(formatShownBalance(-1812.3, true)).toBe('1,812.30')
    expect(formatLabelledBalance(-812.33, true)).toBe('Owed 812.33')
  })

  it('shows nothing owed as 0.00, not -0.00', () => {
    expect(formatShownBalance(0, true)).toBe('0.00')
    expect(formatLabelledBalance(0, true)).toBe('Owed 0.00')
  })

  it('keeps a balance that is not owed as it is, with no label', () => {
    expect(formatShownBalance(-50, false)).toBe('-50.00')
    expect(formatLabelledBalance(2800.25, false)).toBe('2,800.25')
  })
})

describe('formatOwedOrCredit', () => {
  it('shows an amount owed as a plain figure', () => {
    expect(formatOwedOrCredit(3394.25)).toBe('3,394.25')
  })

  it('shows a negative total owed as credit', () => {
    expect(formatOwedOrCredit(-35)).toBe('Credit 35.00')
  })

  it('shows nothing owed as 0.00, never -0.00', () => {
    expect(formatOwedOrCredit(0)).toBe('0.00')
    expect(formatOwedOrCredit(-0)).toBe('0.00')
  })
})

describe('statementStatusText', () => {
  const statement = (overrides: Partial<AccountStatementDto>): AccountStatementDto => ({
    id: 's1', year: 2026, month: 11, total: 0, creditIn: 0, outstandingTotal: 0, creditCarried: 0, status: 'NothingDue', isPaid: false, dueDate: null, paidOn: null, paidAmount: null, paidFromAccountName: null, ...overrides,
  })

  it('names each status', () => {
    expect(statementStatusText(statement({ status: 'Unpaid' }))).toBe('Unpaid')
    expect(statementStatusText(statement({ creditCarried: 35 }))).toBe('Nothing due · 35.00 credit carried')
    expect(statementStatusText(statement({ status: 'Paid', isPaid: true, paidOn: '2026-11-12' }))).toBe('Paid 12 Nov 2026')
    expect(statementStatusText(statement({ status: 'Paid', isPaid: true }))).toBe('Paid')
  })
})

describe('nothingDueText', () => {
  it('says only Nothing due with no credit', () => {
    expect(nothingDueText(0)).toBe('Nothing due')
  })

  it('names the credit carried', () => {
    expect(nothingDueText(35)).toBe('Nothing due · 35.00 credit carried')
  })
})
