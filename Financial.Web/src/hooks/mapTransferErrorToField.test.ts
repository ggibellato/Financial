import { describe, expect, it } from 'vitest'
import { mapTransferErrorToField } from './mapTransferErrorToField'

describe('mapTransferErrorToField', () => {
  it('maps an unresolvable source bank message to the sourceBank field', () => {
    const result = mapTransferErrorToField("Bank 'NotABank' was not found.", 'NotABank', 'Trading212')

    expect(result).toBe('sourceBank')
  })

  it('maps an unresolvable destination bank message to the destinationBank field', () => {
    const result = mapTransferErrorToField("Bank 'NotABank' was not found.", 'Barclays', 'NotABank')

    expect(result).toBe('destinationBank')
  })

  it('maps a same-bank message to the destinationBank field', () => {
    const result = mapTransferErrorToField(
      'A transfer must move money between two different banks.',
      'Barclays',
      'Barclays',
    )

    expect(result).toBe('destinationBank')
  })

  it('maps a non-positive amount message to the amount field', () => {
    const result = mapTransferErrorToField('Transfer amount must be greater than zero.', 'Barclays', 'Trading212')

    expect(result).toBe('amount')
  })

  it('returns null when the bank name in a not-found message matches neither current field', () => {
    const result = mapTransferErrorToField("Bank 'SomeOtherBank' was not found.", 'Barclays', 'Trading212')

    expect(result).toBeNull()
  })

  it('returns null for an unrecognized message', () => {
    const result = mapTransferErrorToField('Network request failed', 'Barclays', 'Trading212')

    expect(result).toBeNull()
  })
})
