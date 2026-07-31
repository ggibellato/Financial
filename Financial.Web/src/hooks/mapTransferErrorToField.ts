export type TransferFormField = 'date' | 'sourceBank' | 'destinationBank' | 'amount' | 'note'

/**
 * Maps F01's known, fixed transfer validation error strings to the form field each one
 * belongs to, so the caller can render the message inline under that field instead of a
 * generic banner. Returns null when the message doesn't match a known pattern (e.g. a
 * network failure), signalling the caller should fall back to a general error banner.
 */
export function mapTransferErrorToField(
  message: string,
  sourceBank: string,
  destinationBank: string,
): TransferFormField | null {
  const bankNotFoundMatch = message.match(/^Bank '(.+)' was not found\.$/)
  if (bankNotFoundMatch) {
    const missingBank = bankNotFoundMatch[1]
    if (missingBank === sourceBank) return 'sourceBank'
    if (missingBank === destinationBank) return 'destinationBank'
    return null
  }

  if (message.includes('different banks')) return 'destinationBank'
  if (message.includes('greater than zero')) return 'amount'

  return null
}
