export const CHARGE_STATUS = 'CreditCardCharge'
export const SETTLED_STATUS = 'CreditCardSettled'
export const STATEMENT_PAID_STATUS = 'CreditCardStatementPaid'

export function isLockedByPaidStatement(paymentStatus: string): boolean {
  return paymentStatus === SETTLED_STATUS || paymentStatus === STATEMENT_PAID_STATUS
}

export function isCardChargeStatus(paymentStatus: string): boolean {
  return paymentStatus === CHARGE_STATUS || paymentStatus === STATEMENT_PAID_STATUS
}
