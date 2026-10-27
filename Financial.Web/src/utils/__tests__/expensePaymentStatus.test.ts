import { describe, expect, it } from 'vitest'
import { isCardChargeStatus, isLockedByPaidStatement } from '../expensePaymentStatus'

describe('expensePaymentStatus', () => {
  it.each([
    ['ImmediatePayment', false, false],
    ['CreditCardCharge', false, true],
    ['CreditCardSettled', true, false],
    ['CreditCardStatementPaid', true, true],
  ])('%s: locked by a paid statement %s, card charge %s', (status, locked, cardCharge) => {
    expect(isLockedByPaidStatement(status)).toBe(locked)
    expect(isCardChargeStatus(status)).toBe(cardCharge)
  })
})
