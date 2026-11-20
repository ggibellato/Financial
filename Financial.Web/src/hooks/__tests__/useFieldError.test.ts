import { describe, expect, it } from 'vitest'
import { useFieldError } from '../useFieldError'

describe('useFieldError', () => {
  const errors = { amount: 'Amount must be a positive number.' }

  it.each([
    ['its own error for an invalid field', errors, 'Amount must be a positive number.'],
    ['null for a field with no error', { date: 'Date is required.' }, null],
    ['null when there are no field errors', null, null],
    ['null before any save', undefined, null],
  ])('returns %s', (_, fieldErrors, expected) => {
    expect(useFieldError<'amount' | 'date'>(fieldErrors)('amount')).toBe(expected)
  })
})
