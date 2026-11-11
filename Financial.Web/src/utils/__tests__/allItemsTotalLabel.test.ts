import { describe, expect, it } from 'vitest'
import { allItemsTotalLabel } from '../allItemsTotalLabel'

describe('allItemsTotalLabel', () => {
  it('names the items the total covers while filtered', () => {
    expect(allItemsTotalLabel('Total', 'categories', true)).toBe('Total (all categories)')
  })

  it('keeps the label as is when not filtered', () => {
    expect(allItemsTotalLabel('Total', 'categories', false)).toBe('Total')
  })
})
