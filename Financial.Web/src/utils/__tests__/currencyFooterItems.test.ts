import { describe, expect, it } from 'vitest'
import type { CurrencyTotalDto } from '../../api/types'
import { currencyFooterItems, type CurrencyFigure } from '../currencyFooterItems'
import { formatN2 } from '../formatters'

function total(currency: string, balance: number): CurrencyTotalDto {
  return { currency, balance, roundUpTotal: 0, cardOutstandingTotal: 0 }
}

const BALANCE: CurrencyFigure[] = [{ label: 'Balance', pick: (t) => t.balance }]

describe('currencyFooterItems', () => {
  it('labels a single currency\'s figure without its code', () => {
    expect(currencyFooterItems([total('GBP', 12.5)], BALANCE)).toEqual([{ label: 'Balance', value: formatN2(12.5) }])
  })

  it('shows zero when there is no currency', () => {
    expect(currencyFooterItems([], BALANCE)).toEqual([{ label: 'Balance', value: formatN2(0) }])
  })

  it('names the currency of each figure when there are several', () => {
    expect(currencyFooterItems([total('GBP', 1), total('BRL', 2)], BALANCE)).toEqual([
      { label: 'Balance GBP', value: formatN2(1) },
      { label: 'Balance BRL', value: formatN2(2) },
    ])
  })
})
