import type { CreditCardDto } from '../api/types'

/**
 * Defaults a credit-card expense's invoice month to the standard date-derived month, unless
 * the selected card already has a later existing invoice queued up - see the WPF equivalent,
 * ExpenseWorkflowViewModel.ApplyCardInvoiceDefaultIfAuto.
 */
export function computeDefaultInvoiceMonth(date: string, creditCardId: string, creditCards: CreditCardDto[]): string {
  const standardDefault = date ? date.slice(0, 7) : ''
  const card = creditCards.find((c) => c.id === creditCardId)
  const cardDefault = card?.latestInvoiceDate ? card.latestInvoiceDate.slice(0, 7) : null
  return cardDefault && cardDefault > standardDefault ? cardDefault : standardDefault
}
