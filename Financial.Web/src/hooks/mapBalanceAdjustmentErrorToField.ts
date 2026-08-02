export type BalanceAdjustmentFormField = 'bankName' | 'date' | 'targetBalance' | 'note'

/**
 * Maps F02's known, fixed balance adjustment validation error message to the field it
 * belongs to. The bank picker (create mode only) is validated client-side by disabling
 * Save until a bank is chosen, so no server error can ever target it; an unrecognized
 * message falls back to a general banner.
 */
export function mapBalanceAdjustmentErrorToField(message: string): BalanceAdjustmentFormField | null {
  if (message.includes('cannot be negative')) return 'targetBalance'

  return null
}
