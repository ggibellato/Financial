import { describe, expect, it } from 'vitest'
import { PAID_BY_OTHER, PAID_BY_YOU, paidByLabel } from '../paidBy'

describe('paidByLabel', () => {
  it('reads You for your own payment and the other participant for theirs', () => {
    expect(paidByLabel(PAID_BY_YOU, 'Mãe')).toBe('You')
    expect(paidByLabel(PAID_BY_OTHER, 'Mãe')).toBe('Mãe')
  })

  it('falls back to the other participant when the row has no payer', () => {
    expect(paidByLabel(undefined, 'Mãe')).toBe('Mãe')
  })
})
