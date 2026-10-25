import type { CurrencyTotalDto } from '../api/types'
import type { ReactNode } from 'react'
import { formatN2 } from './formatters'

export interface SectionTotalItem {
  label: string
  value: ReactNode
}

export interface CurrencyFigure {
  label: string
  pick: (total: CurrencyTotalDto) => number
}

export function currencyFooterItems(currencyTotals: CurrencyTotalDto[], figures: CurrencyFigure[]): SectionTotalItem[] {
  if (currencyTotals.length <= 1) {
    const [total] = currencyTotals
    return figures.map(({ label, pick }) => ({ label, value: formatN2(total ? pick(total) : 0) }))
  }
  return currencyTotals.flatMap((total) =>
    figures.map(({ label, pick }) => ({ label: `${label} ${total.currency}`, value: formatN2(pick(total)) })),
  )
}
