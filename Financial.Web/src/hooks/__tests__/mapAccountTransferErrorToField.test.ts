import { describe, expect, it } from 'vitest'
import { mapAccountTransferErrorToField } from '../mapAccountTransferErrorToField'

describe('mapAccountTransferErrorToField', () => {
  it.each([
    ['Date must not be in the future.', 'to', 'date'],
    ['Destination amount must be greater than zero.', 'to', 'destinationAmount'],
    ['Enter the amount received in EUR.', 'from', 'destinationAmount'],
    ['Transfer amount must be greater than zero.', 'to', 'amount'],
    ['Note must be 200 characters or fewer.', 'to', 'note'],
    ['Transfers from Barclays to Chase are not allowed.', 'to', 'destinationBank'],
    ['Chase is inactive.', 'from', 'sourceBank'],
    ['Source and destination must be different accounts.', 'from', 'sourceBank'],
  ] as const)('maps "%s" on a transfer %s to %s', (message, direction, field) => {
    expect(mapAccountTransferErrorToField(message, direction)).toBe(field)
  })

  it('returns null for a message no field owns', () => {
    expect(mapAccountTransferErrorToField('Could not save. Check the connection and try again.', 'to')).toBeNull()
  })
})
