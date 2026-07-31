export type BalanceAdjustmentFormField = 'date' | 'targetBalance' | 'note'

/**
 * Maps F02's known, fixed balance adjustment validation error message to the field it
 * belongs to. There is no bank picker on this form (the bank is fixed by the row that
 * opened it), so an unresolvable-bank message has nowhere to attach inline and falls
 * back to a general banner, same as any unrecognized message.
 */
export function mapBalanceAdjustmentErrorToField(message: string): BalanceAdjustmentFormField | null {
  if (message.includes('cannot be negative')) return 'targetBalance'

  return null
}
