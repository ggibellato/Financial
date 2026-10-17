import type { Currency } from '../api/types'

export const CURRENCIES = ['BRL', 'GBP', 'USD'] as const satisfies readonly Currency[]

export const REPORTING_CURRENCIES = ['GBP', 'BRL', 'USD'] as const satisfies readonly Currency[]

export function isCurrency(value: string): value is Currency {
  return (CURRENCIES as readonly string[]).includes(value)
}
