import { describe, expect, it } from 'vitest'
import { mapBalanceAdjustmentErrorToField } from './mapBalanceAdjustmentErrorToField'

describe('mapBalanceAdjustmentErrorToField', () => {
  it('maps a negative balance message to the targetBalance field', () => {
    const result = mapBalanceAdjustmentErrorToField('Balance cannot be negative.')

    expect(result).toBe('targetBalance')
  })

  it('returns null for an unresolvable bank message', () => {
    const result = mapBalanceAdjustmentErrorToField("Bank 'NotABank' was not found.")

    expect(result).toBeNull()
  })

  it('returns null for an unrecognized message', () => {
    const result = mapBalanceAdjustmentErrorToField('Network request failed')

    expect(result).toBeNull()
  })
})
